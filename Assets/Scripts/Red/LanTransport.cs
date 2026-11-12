using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LabQuimicaVR
{
    /// <summary>TCP delimitado por LF. Los hilos solo manejan bytes; Unity consume eventos en Update.</summary>
    public sealed class LanTransport : IDisposable
    {
        public const int DefaultPort = 47771;
        public const int MaxFrameBytes = 8192;
        public const int MaxGuests = 7;
        public sealed class Event
        {
            public string Peer, Kind, Payload;
        }
        sealed class Peer
        {
            public string Id;
            public TcpClient Socket;
            public readonly ConcurrentQueue<byte[]> Outbound = new ConcurrentQueue<byte[]>();
            public readonly AutoResetEvent WakeWriter = new AutoResetEvent(false);
            public volatile bool Closed;
            public volatile bool CloseWhenDrained;
        }
        readonly ConcurrentQueue<Event> events = new ConcurrentQueue<Event>();
        readonly Dictionary<string, Peer> peers = new Dictionary<string, Peer>();
        readonly object gate = new object();
        readonly UTF8Encoding utf8 = new UTF8Encoding(false, true);
        TcpListener listener;
        TcpClient connectingSocket;
        volatile bool stopped;
        int sequence;
        long receivedBytes;
        public int Port { get; private set; }
        public long ReceivedBytes => Interlocked.Read(ref receivedBytes);

        public static bool IsValidAddress(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string[] parts = text.Split('.');
            if (parts.Length != 4) return false;
            foreach (string part in parts)
                if (part.Length < 1 || part.Length > 3 || !byte.TryParse(part, out _) || part.Trim() != part) return false;
            if (!IPAddress.TryParse(text, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork) return false;
            byte[] bytes = ip.GetAddressBytes();
            return bytes[0] > 0 && bytes[0] < 224 && bytes[3] != 255;
        }

        public static string LocalAddresses()
        {
            var result = new List<string>();
            try
            {
                foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                    foreach (var address in nic.GetIPProperties().UnicastAddresses)
                    {
                        string ip = address.Address.ToString();
                        if (address.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address.Address) && !ip.StartsWith("169.254."))
                            if (!result.Contains(ip)) result.Add(ip);
                    }
                }
            }
            catch (Exception) { }
            return result.Count == 0 ? "Sin IPv4: conecta Wi-Fi o activa tu punto de acceso" : string.Join(" / ", result);
        }

        public void Listen(int port = DefaultPort)
        {
            if (listener != null || stopped) throw new InvalidOperationException("Transporte ya iniciado o cerrado.");
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start(MaxGuests);
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            new Thread(AcceptLoop) { IsBackground = true, Name = "Quimikaze LAN accept" }.Start();
        }

        public void Connect(string address, int port = DefaultPort)
        {
            if (!IsValidAddress(address) || port < 1 || port > 65535) throw new ArgumentException("IPv4 o puerto inválido.");
            new Thread(() =>
            {
                TcpClient socket = new TcpClient(AddressFamily.InterNetwork);
                try
                {
                    lock (gate)
                    {
                        if (stopped) { socket.Close(); return; }
                        connectingSocket = socket;
                    }
                    var pending = socket.BeginConnect(address, port, null, null);
                    using (pending.AsyncWaitHandle)
                    {
                        if (!pending.AsyncWaitHandle.WaitOne(4000)) throw new TimeoutException("No respondió el anfitrión (4 s).");
                        socket.EndConnect(pending);
                    }
                    AddPeer("server", socket);
                }
                catch (Exception ex)
                {
                    socket.Close();
                    Enqueue("server", "error", ex is TimeoutException ? ex.Message : "No se pudo conectar. Revisa IP, Wi-Fi y firewall.");
                }
                finally { lock (gate) if (connectingSocket == socket) connectingSocket = null; }
            }) { IsBackground = true, Name = "Quimikaze LAN connect" }.Start();
        }

        void AcceptLoop()
        {
            while (!stopped)
            {
                try
                {
                    var socket = listener.AcceptTcpClient();
                    bool full;
                    lock (gate) full = peers.Count >= MaxGuests;
                    if (full)
                    {
                        // Este código corre en el aceptador de fondo, nunca en Unity.
                        try
                        {
                            socket.SendTimeout = 200;
                            byte[] reject = Encoding.UTF8.GetBytes("{\"version\":" + LanSession.ProtocolVersion + ",\"type\":\"error\",\"message\":\"Aula llena: máximo 8 jugadores.\"}\n");
                            socket.GetStream().Write(reject, 0, reject.Length);
                        }
                        catch (Exception ex) when (ex is IOException || ex is SocketException || ex is InvalidOperationException) { }
                        socket.Close();
                        continue;
                    }
                    AddPeer("p" + Interlocked.Increment(ref sequence), socket);
                }
                catch (SocketException) { if (!stopped) Enqueue("", "error", "Se interrumpió el servidor LAN."); break; }
                catch (ObjectDisposedException) { break; }
            }
        }

        void AddPeer(string id, TcpClient socket)
        {
            var peer = new Peer { Id = id, Socket = socket };
            socket.NoDelay = true;
            socket.ReceiveTimeout = 15000;
            socket.SendTimeout = 1500;
            lock (gate)
            {
                if (stopped) { socket.Close(); return; }
                peers.Add(id, peer);
            }
            Enqueue(id, "open", "");
            new Thread(() => ReadLoop(peer)) { IsBackground = true, Name = "Quimikaze LAN read" }.Start();
            new Thread(() => WriteLoop(peer)) { IsBackground = true, Name = "Quimikaze LAN write" }.Start();
        }

        void WriteLoop(Peer peer)
        {
            try
            {
                var stream = peer.Socket.GetStream();
                while (!stopped && !peer.Closed)
                {
                    peer.WakeWriter.WaitOne(250);
                    while (!stopped && !peer.Closed && peer.Outbound.TryDequeue(out var bytes))
                        stream.Write(bytes, 0, bytes.Length);
                    if (peer.CloseWhenDrained && peer.Outbound.IsEmpty) break;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException || ex is ObjectDisposedException || ex is InvalidOperationException) { }
            finally { ClosePeer(peer.Id); peer.WakeWriter.Dispose(); }
        }

        void ReadLoop(Peer peer)
        {
            try
            {
                var stream = peer.Socket.GetStream();
                var buffer = new byte[MaxFrameBytes];
                var chunk = new byte[4096];
                int count = 0;
                while (!stopped)
                {
                    int read = stream.Read(chunk, 0, chunk.Length);
                    if (read == 0) break;
                    Interlocked.Add(ref receivedBytes, read);
                    for (int i = 0; i < read; i++)
                    {
                        byte b = chunk[i];
                        if (b == '\n')
                        {
                            if (count == 0) continue;
                            // Decodificar solo la trama completa conserva secuencias UTF8 entre lecturas.
                            string line = utf8.GetString(buffer, 0, count);
                            count = 0;
                            if (events.Count >= 512) throw new IOException("Límite de mensajes excedido.");
                            Enqueue(peer.Id, "line", line);
                        }
                        else
                        {
                            if (count >= buffer.Length) throw new IOException("Trama demasiado grande.");
                            buffer[count++] = b;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException || ex is ObjectDisposedException || ex is DecoderFallbackException || ex is InvalidOperationException) { }
            finally { ClosePeer(peer.Id); }
        }

        public bool TryDequeue(out Event item) => events.TryDequeue(out item);
        void Enqueue(string peer, string kind, string payload)
        {
            if (!stopped) events.Enqueue(new Event { Peer = peer, Kind = kind, Payload = payload });
        }
        public bool Send(string id, string json)
        {
            if (json == null || json.IndexOf('\n') >= 0) return false;
            byte[] bytes = utf8.GetBytes(json + "\n");
            if (bytes.Length > MaxFrameBytes) return false;
            Peer peer;
            lock (gate) if (!peers.TryGetValue(id, out peer)) return false;
            if (peer.Closed) return false;
            // Nunca escribir sockets en Unity: el escritor de fondo tiene una cola acotada.
            if (peer.Outbound.Count >= 32) { ClosePeer(id); return false; }
            peer.Outbound.Enqueue(bytes);
            try { peer.WakeWriter.Set(); }
            catch (ObjectDisposedException) { return false; }
            return true;
        }
        public void ClosePeer(string id)
        {
            Peer peer;
            lock (gate)
            {
                if (!peers.TryGetValue(id, out peer)) return;
                peers.Remove(id);
            }
            peer.Closed = true;
            peer.Socket.Close();
            try { peer.WakeWriter.Set(); } catch (ObjectDisposedException) { }
            Enqueue(id, "close", "Conexión finalizada o sin respuesta durante 15 s.");
        }

        public void SendAndClose(string id, string json)
        {
            if (!Send(id, json)) { ClosePeer(id); return; }
            Peer peer;
            lock (gate) if (!peers.TryGetValue(id, out peer)) return;
            peer.CloseWhenDrained = true;
            try { peer.WakeWriter.Set(); } catch (ObjectDisposedException) { }
        }
        public void Dispose()
        {
            stopped = true;
            listener?.Stop();
            lock (gate)
            {
                connectingSocket?.Close();
                connectingSocket = null;
                foreach (var peer in peers.Values)
                {
                    peer.Closed = true;
                    peer.Socket.Close();
                    try { peer.WakeWriter.Set(); } catch (ObjectDisposedException) { }
                }
                peers.Clear();
            }
            while (events.TryDequeue(out _)) { }
        }
    }
}

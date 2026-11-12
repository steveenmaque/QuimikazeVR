using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>Pruebas reales con sockets TCP loopback. No sustituyen las pruebas Android/Wi-Fi.</summary>
    public static class LanValidation
    {
        public static readonly List<string> Passed = new List<string>();
        static int checks;
        public static int Checks => checks;
        [MenuItem("Quimikaze/Validar aula LAN (localhost)", priority = 81)]
        public static void Run()
        {
            Passed.Clear();
            checks = 0;
            Addresses();
            Frames();
            Backpressure();
            ProtocolAndSkins();
            LobbyAndQuiz();
            Capacity();
            FailedJoinAndTimeout();
            UnityEngine.Debug.Log("[LAN] " + checks + " verificaciones superadas: " + string.Join("; ", Passed));
        }

        static void Assert(bool condition, string text)
        {
            if (!condition) throw new Exception("LAN: " + text);
            checks++;
        }
        static void Wait(Func<bool> condition, Action pump, string detail, int timeoutMs = 5000)
        {
            var timer = Stopwatch.StartNew();
            while (!condition())
            {
                pump();
                if (timer.ElapsedMilliseconds > timeoutMs) throw new TimeoutException("LAN: " + detail);
                Thread.Sleep(5);
            }
            checks++;
        }

        static void Addresses()
        {
            Assert(LanTransport.IsValidAddress("127.0.0.1"), "loopback válido");
            Assert(LanTransport.IsValidAddress("192.168.43.1"), "hotspot válido");
            foreach (string address in new[] { "", "localhost", "192.168.1", "300.1.1.1", "0.0.0.0", "255.255.255.255", "192.168.0.255", "::1", "1.2.3.4\n", "1.2. 3.4" })
                Assert(!LanTransport.IsValidAddress(address), "dirección inválida rechazada: " + address);
            Assert(LanSession.CleanAlias("<b>Alumno</b>\n;").IndexOf('<') < 0, "alias sin marcado");
            Assert(LanSession.CleanAlias(new string('a', 100)).Length == 18, "alias limitado");
            for (int skin = 0; skin < 4; skin++) Assert(LanSession.IsValidSkin(skin), "personaje válido " + skin);
            foreach (int skin in new[] { -1, 4, int.MaxValue }) Assert(!LanSession.IsValidSkin(skin), "personaje inválido " + skin);
            Passed.Add("IPv4, alias y límites");
        }

        static void Frames()
        {
            using (var transport = new LanTransport())
            {
                transport.Listen(0);
                using (var second = new LanTransport())
                {
                    bool conflict = false;
                    try { second.Listen(transport.Port); } catch (SocketException) { conflict = true; }
                    Assert(conflict, "puerto ocupado detectado");
                }
                using (var raw = new TcpClient())
                {
                    raw.Connect("127.0.0.1", transport.Port);
                    string peer = null, received = null;
                    var frames = new List<string>();
                    Action drain = () => { while (transport.TryDequeue(out var item)) { if (item.Kind == "open") peer = item.Peer; if (item.Kind == "line") { received = item.Payload; frames.Add(item.Payload); } } };
                    Wait(() => peer != null, drain, "aceptar conexión TCP");
                    var stream = raw.GetStream();
                    byte[] line = Encoding.UTF8.GetBytes("{\"texto\":\"química\"}\n");
                    int split = Array.IndexOf(line, (byte)0xc3) + 1;
                    Assert(split > 1 && line[split - 1] == 0xc3 && line[split] == 0xad, "fragmento divide los bytes C3/AD de í");
                    stream.Write(line, 0, split);
                    Wait(() => transport.ReceivedBytes == split, drain, "primera lectura recibe únicamente hasta C3");
                    drain();
                    Assert(received == null, "trama parcial no emitida");
                    stream.Write(line, split, line.Length - split);
                    Wait(() => received != null, drain, "reensamblar trama");
                    Assert(received == "{\"texto\":\"química\"}", "UTF8 reensamblado");
                    Assert(frames.Count == 1, "fragmentos generan una sola trama");
                    byte[] combined = Encoding.UTF8.GetBytes("{\"id\":1}\n{\"id\":2}\n");
                    stream.Write(combined, 0, combined.Length);
                    Wait(() => frames.Count == 3, drain, "dos tramas en una sola escritura");
                    Assert(frames[1] == "{\"id\":1}" && frames[2] == "{\"id\":2}", "tramas agrupadas conservan orden y límites");
                    Assert(!transport.Send(peer, new string('a', LanTransport.MaxFrameBytes)), "envío demasiado grande rechazado");
                    Assert(!transport.Send(peer, "inyección\nsegunda trama"), "LF interno rechazado");
                    byte[] large = new byte[LanTransport.MaxFrameBytes + 1];
                    for (int i = 0; i < large.Length; i++) large[i] = 65;
                    stream.Write(large, 0, large.Length);
                    bool closed = false;
                    Wait(() => closed, () => { while (transport.TryDequeue(out var item)) if (item.Kind == "close") closed = true; }, "cerrar trama entrante sobredimensionada");
                }
            }
            Passed.Add("TCP real, fragmentación UTF8, puerto ocupado y trama maliciosa");
        }

        static void LobbyAndQuiz()
        {
            using (var host = new LanSession())
            using (var a = new LanSession())
            using (var b = new LanSession())
            {
                float clock = 0;
                Action pump = () => { clock += 0.01f; host.Pump(clock); a.Pump(clock); b.Pump(clock); };
                host.Host("Docente", 0, 2);
                host.StartExploration();
                Assert(!host.State.explorationStarted && host.State.worldScene == "MenuPrincipal", "un solo jugador permanece en lobby previo");
                host.StartRound(6);
                Assert(host.State.phase == "lobby", "no se inicia solo");
                a.Join("Alumno A", "127.0.0.1", host.Port, 1);
                b.Join("Alumno B", "127.0.0.1", host.Port, 3);
                Wait(() => host.State.members.Length == 3 && a.Connected && b.Connected && b.State.members.Length == 3, pump, "unir 3 jugadores");
                Assert(a.SelfId != b.SelfId && a.SelfId != "host", "identidad asignada por servidor");
                Assert(host.Self.skin == 2 && a.Self.skin == 1 && b.Self.skin == 3, "personajes elegidos viajan en handshake");
                host.SetSkin(0); a.SetSkin(2); b.SetSkin(1);
                Wait(() => host.Self.skin == 0 && a.Self.skin == 2 && b.Self.skin == 1 && b.State.members.First(m => m.id == "host").skin == 0, pump, "cambio de personaje sincronizado");
                host.SetSkin(4); a.SetSkin(-1);
                Assert(host.Self.skin == 0 && a.Self.skin == 2, "selecciones locales inválidas no cambian personaje");
                a.StartExploration(); host.StartExploration();
                for (int i = 0; i < 8; i++) { pump(); Thread.Sleep(3); }
                Assert(!host.State.explorationStarted, "ni invitado ni anfitrión sin listos inician exploración");
                host.FollowElement("ElementoDetalle", 8);
                Assert(host.State.worldScene == "MenuPrincipal", "navegación no evita configuración previa");
                a.SetReady(true); b.SetReady(true);
                Wait(() => host.State.members.All(m => m.ready), pump, "todos listos antes de entrar al laboratorio");
                host.StartExploration();
                Wait(() => a.State.explorationStarted && b.State.explorationStarted && b.State.worldScene == "TablaPeriodica", pump, "inicio compartido desde menú hacia tabla");
                Assert(host.State.phase == "lobby", "entrar laboratorio no dispara quiz");
                host.FollowElement("ElementoDetalle", 8);
                Wait(() => a.State.worldScene == "ElementoDetalle" && b.State.worldElement == 8, pump, "seguir elemento del anfitrión");
                a.FollowElement("TablaPeriodica", 6);
                for (int i = 0; i < 8; i++) { pump(); Thread.Sleep(3); }
                Assert(host.State.worldScene == "ElementoDetalle" && host.State.worldElement == 8, "cliente no cambia escena compartida");
                host.PublishPose(100, 0, 0, 0, "ElementoDetalle", 8);
                Assert(!host.Self.hasPose, "posición fuera de límites rechazada");
                host.PublishPose(float.NaN, 0, 0, 0, "ElementoDetalle", 8);
                Assert(!host.Self.hasPose, "posición NaN rechazada");
                host.PublishPose(1, 0, 2, 400, "ElementoDetalle", 8);
                Assert(host.Self.hasPose && host.Self.yaw == 40, "pose válida y yaw normalizado");
                a.PublishPose(-1, 0, 1, 90, "ElementoDetalle", 8);
                b.PublishPose(1, 0, 1, 90, "TablaPeriodica", 6);
                Wait(() => b.State.members.Any(m => m.id == a.SelfId && m.hasPose && m.x == -1), pump, "poses compartidas mediante snapshot5Hz");
                Assert(!host.State.members.First(m => m.id == b.SelfId).hasPose, "pose de otra escena rechazada");
                host.FollowElement("TablaPeriodica", 6);
                Wait(() => a.State.worldScene == "TablaPeriodica", pump, "volver juntos a la tabla");
                Assert(host.State.members.All(m => !m.hasPose), "poses antiguas se limpian al cambiar escena");
                a.SetReady(false); b.SetReady(false);
                Wait(() => host.State.members.Where(m => m.id != "host").All(m => !m.ready), pump, "consentimiento renovado para probar reto");
                a.StartRound(8);
                a.RevealQuestion();
                host.StartRound(6);
                for (int i = 0; i < 10; i++) { pump(); Thread.Sleep(3); }
                Assert(host.State.phase == "lobby", "solo host inicia con todos listos");
                a.SetReady(true); b.SetReady(true);
                Wait(() => host.State.members.All(m => m.ready), pump, "todos listos");
                host.StartRound(0);
                Assert(host.State.phase == "lobby", "elemento fuera de rango rechazado");
                host.StartRound(6);
                Wait(() => a.State.phase == "quiz" && b.State.phase == "quiz", pump, "sincronizar inicio");
                Assert(a.State.element == 6 && b.State.element == 6, "mismo elemento");
                using (var late = new LanSession())
                {
                    late.Join("Tardío", "127.0.0.1", host.Port);
                    Wait(() => late.Status.Contains("ronda iniciada"), () => { pump(); late.Pump(clock); }, "rechazo de lobby iniciado con motivo explícito");
                    Assert(!late.Connected && host.State.members.Length == 3, "invitado tardío no altera ronda");
                }
                int completions = 0;
                a.RoundCompleted += (z, score) => { Assert(z == 6 && score == 3, "resultado local válido"); completions++; };
                for (int step = 0; step < 3; step++)
                {
                    int expectedStep = step;
                    Wait(() => a.State.step == expectedStep && b.State.step == expectedStep, pump, "sincronizar pregunta");
                    var e = ElementDatabase.Get(6);
                    string answer = step == 0 ? "6" : step == 1 ? CategoryInfo.Name(e.Category) : e.Period.ToString();
                    int correct = Array.IndexOf(host.State.options, answer);
                    Assert(correct >= 0 && host.State.options.Distinct().Count() == 3, "opciones únicas y solución existente");
                    host.Answer(correct); host.Answer(correct);
                    a.Answer(correct); a.Answer(correct);
                    if (step < 2) b.Answer((correct + 1) % 3);
                    else
                    {
                        Wait(() => host.State.members.Count(m => m.answered) == 2, pump, "esperar dos respuestas antes de cierre docente");
                        a.RevealQuestion();
                        for (int i = 0; i < 8; i++) { pump(); Thread.Sleep(3); }
                        Assert(!host.State.revealed, "cliente no revela respuestas");
                        host.Next();
                        Assert(host.State.step == 2, "no avanzar mientras hay respuestas pendientes");
                        host.RevealQuestion();
                    }
                    Wait(() => host.State.revealed && a.State.revealed && b.State.revealed, pump, "revelar al responder todos");
                    Assert(host.Self.points == (step + 1) * 100, "duplicado no suma puntos");
                    Assert(b.Self.points == 0, "respuesta incorrecta sin puntos");
                    a.Next();
                    for (int i = 0; i < 4; i++) pump();
                    Assert(host.State.step == step, "cliente no avanza ronda");
                    host.Next();
                }
                Wait(() => a.State.phase == "summary" && b.State.phase == "summary", pump, "resumen compartido");
                Assert(completions == 1, "un registro por ronda");
                host.ReturnToLobby();
                Wait(() => a.State.phase == "lobby", pump, "segunda ronda requiere nuevo listo");
                Assert(host.State.members.Where(m => m.id != "host").All(m => !m.ready), "ready reiniciado");
                b.Dispose();
                Wait(() => host.State.members.Length == 2, pump, "eliminar cliente desconectado");
                host.Dispose();
                Wait(() => !a.Connected, pump, "cierre host notificado");
            }
            Passed.Add("lobby, autoridad, escena/poses compartidas, quiz, cierre de espera y desconexión");
        }

        [Serializable]
        public sealed class ProtocolReply
        {
            public int version;
            public string type, message;
        }

        static void ProtocolAndSkins()
        {
            using (var host = new LanSession())
            {
                host.Host("Host", 0, 1);
                float clock = 0;
                Action pump = () => host.Pump(clock += 0.01f);
                foreach (int version in new[] { 1, 999 })
                    CheckRejected(host, "{\"version\":" + version + ",\"type\":\"join\",\"alias\":\"Antiguo\",\"skin\":0}", "incompatible", pump);
                foreach (int skin in new[] { -1, 4, int.MaxValue })
                    CheckRejected(host, "{\"version\":" + LanSession.ProtocolVersion + ",\"type\":\"join\",\"alias\":\"Inválido\",\"skin\":" + skin + "}", "no válido", pump);
                using (var raw = new LanTransport())
                {
                    raw.Connect("127.0.0.1", host.Port);
                    bool open = false;
                    Action step = () => { pump(); while (raw.TryDequeue(out var item)) if (item.Kind == "open") open = true; };
                    Wait(() => open, step, "conexión protocolo actual");
                    raw.Send("server", "{\"version\":" + LanSession.ProtocolVersion + ",\"type\":\"join\",\"alias\":\"Robot\",\"skin\":3}");
                    Wait(() => host.State.members.Length == 2, step, "aceptar personaje robot válido");
                    var guest = host.State.members.Single(m => m.id != "host");
                    raw.Send("server", "{\"version\":" + LanSession.ProtocolVersion + ",\"type\":\"skin\",\"skin\":99,\"self\":\"host\"}");
                    raw.Send("server", "{\"version\":" + LanSession.ProtocolVersion + ",\"type\":\"ready\",\"ready\":true}");
                    Wait(() => guest.ready, step, "procesar detrás de personaje inválido");
                    Assert(guest.skin == 3 && host.Self.skin == 1, "skin inválida por red rechazada sin alterar identidad");
                    raw.Send("server", "{\"version\":" + LanSession.ProtocolVersion + ",\"type\":\"skin\",\"skin\":0,\"self\":\"host\"}");
                    Wait(() => guest.skin == 0, step, "personaje válido se actualiza");
                    Assert(host.Self.skin == 1, "cliente no suplanta personaje del anfitrión");
                }
            }
            Passed.Add("protocolo2, rechazo de versiones antiguas y personajes inválidos, identidad del servidor");
        }

        static void CheckRejected(LanSession host, string json, string expected, Action pump)
        {
            using (var raw = new LanTransport())
            {
                raw.Connect("127.0.0.1", host.Port);
                bool open = false, closed = false;
                ProtocolReply reply = null;
                Action step = () =>
                {
                    pump();
                    while (raw.TryDequeue(out var item))
                    {
                        if (item.Kind == "open") open = true;
                        else if (item.Kind == "close") closed = true;
                        else if (item.Kind == "line") reply = JsonUtility.FromJson<ProtocolReply>(item.Payload);
                    }
                };
                Wait(() => open, step, "abrir conexión a rechazar");
                raw.Send("server", json);
                Wait(() => reply != null && closed, step, "rechazo explícito y cierre");
                Assert(reply.version == LanSession.ProtocolVersion && reply.type == "error" && reply.message.Contains(expected), "motivo de rechazo válido");
                Assert(host.State.members.Length == 1, "rechazado no se incorpora al lobby");
            }
        }

        static void Capacity()
        {
            var guests = new List<LanSession>();
            using (var host = new LanSession())
            {
                float clock = 0;
                Action pump = () => { clock += 0.01f; host.Pump(clock); foreach (var guest in guests) guest.Pump(clock); };
                try
                {
                    host.Host("Host", 0);
                    for (int i = 0; i < LanTransport.MaxGuests; i++)
                    {
                        var guest = new LanSession(); guests.Add(guest); guest.Join("Alumno " + i, "127.0.0.1", host.Port);
                    }
                    Wait(() => host.State.members.Length == 8 && guests.All(g => g.Connected), pump, "8 jugadores LAN");
                    var extra = new LanSession(); guests.Add(extra); extra.Join("Noveno", "127.0.0.1", host.Port);
                    Wait(() => extra.Status.Contains("Aula llena"), pump, "aula llena con motivo explícito");
                    Assert(host.State.members.Length == 8 && !extra.Connected, "noveno jugador rechazado");
                }
                finally { foreach (var guest in guests) guest.Dispose(); }
            }
            Passed.Add("capacidad máxima 8 jugadores");
        }

        static void Backpressure()
        {
            using (var transport = new LanTransport())
            using (var raw = new TcpClient())
            {
                transport.Listen(0);
                raw.ReceiveBufferSize = 1024;
                raw.Connect("127.0.0.1", transport.Port);
                string peer = null;
                Wait(() => peer != null, () => { while (transport.TryDequeue(out var item)) if (item.Kind == "open") peer = item.Peer; }, "aceptar cliente lento");
                string frame = new string('x', LanTransport.MaxFrameBytes - 2);
                bool rejected = false;
                var timer = Stopwatch.StartNew();
                for (int i = 0; i < 1000; i++)
                    if (!transport.Send(peer, frame)) { rejected = true; break; }
                Assert(rejected, "cliente que no lee alcanza cola acotada");
                Assert(timer.ElapsedMilliseconds < 1200, "Send no espera timeout de socket en hilo principal");
            }
            Passed.Add("backpressure: escritor de fondo y cola limitada sin espera de socket");
        }

        static void FailedJoinAndTimeout()
        {
            int unusedPort;
            using (var probe = new LanTransport()) { probe.Listen(0); unusedPort = probe.Port; }
            using (var failed = new LanSession())
            {
                failed.Join("Sin servidor", "127.0.0.1", unusedPort);
                float clock = 0;
                Wait(() => failed.Status.Contains("No se pudo conectar"), () => failed.Pump(clock += 0.01f), "conexión rechazada con motivo");
                Assert(!failed.Connected, "fallo no deja cliente conectado");
            }
            using (var cancelled = new LanSession())
            {
                cancelled.Join("Cancelado", "127.0.0.1", unusedPort);
                cancelled.Dispose();
                Thread.Sleep(30);
                cancelled.Pump(1);
                Assert(!cancelled.Connected && cancelled.Status.Contains("cerrada"), "Dispose cancela conexión pendiente sin evento tardío");
            }
            var silentServer = new TcpListener(IPAddress.Loopback, 0);
            silentServer.Start();
            try
            {
                using (var waiting = new LanSession())
                {
                    float clock = 0;
                    waiting.Join("Espera", "127.0.0.1", ((IPEndPoint)silentServer.LocalEndpoint).Port);
                    Wait(silentServer.Pending, () => waiting.Pump(clock += 0.01f), "aceptar servidor silencioso");
                    using (var silentPeer = silentServer.AcceptTcpClient())
                        Wait(() => waiting.Status.Contains("Se perdió"), () => waiting.Pump(clock += 0.01f), "timeout real 15 s de servidor sin respuesta", 17000);
                    Assert(!waiting.Connected, "timeout limpia conexión");
                }
            }
            finally { silentServer.Stop(); }
            Passed.Add("conexión rechazada, cancelación durante Connect y timeout TCP real15s");
        }
    }
}

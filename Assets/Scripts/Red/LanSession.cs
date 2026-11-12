using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LabQuimicaVR
{
    [Serializable]
    public sealed class LanMember
    {
        public string id, alias;
        public bool ready, answered;
        public int points, correct;
        public bool hasPose;
        public float x, y, z, yaw;
    }
    [Serializable]
    public sealed class LanSnapshot
    {
        public string phase = "lobby", question = "", explanation = "";
        public string[] options = new string[0];
        public LanMember[] members = new LanMember[0];
        public int element = 6, step, round, revision;
        public string worldScene = "TablaPeriodica";
        public int worldElement = 6, poseTick;
        public bool revealed;
    }
    [Serializable]
    sealed class LanPacket
    {
        public int version = 1;
        public string type, alias, self, message;
        public string scene;
        public float px, py, pz, yaw;
        public bool ready;
        public int choice, round, step, z;
        public LanSnapshot state;
    }

    /// <summary>Solo el anfitrión decide preguntas, identidad, estado y puntos. Se bombea desde el hilo Unity.</summary>
    public sealed class LanSession : IDisposable
    {
        public const int ProtocolVersion = 1;
        public const int MaxPlayers = 8;
        public LanSnapshot State { get; private set; } = new LanSnapshot();
        public string SelfId { get; private set; }
        public string Status { get; private set; } = "Conecta todos los celulares al mismo Wi-Fi o punto de acceso.";
        public bool IsHost { get; private set; }
        public bool Connected { get; private set; }
        public event Action Changed;
        public event Action<int, int> RoundCompleted;
        public LanMember Self => State.members.FirstOrDefault(m => m.id == SelfId);
        public int Port => transport?.Port ?? 0;

        LanTransport transport;
        string alias;
        string terminalReason;
        float lastHeartbeat;
        float now, lastPoseBroadcast;
        bool posesDirty;
        readonly Dictionary<string, float> lastPoseAt = new Dictionary<string, float>();
        int expectedAnswer, completedRound = -1;
        bool disposed;
        readonly HashSet<string> joined = new HashSet<string>();

        public static string CleanAlias(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Estudiante";
            var chars = value.Trim().Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_').Take(18).ToArray();
            return chars.Length == 0 ? "Estudiante" : new string(chars);
        }

        public void Host(string name, int port = LanTransport.DefaultPort)
        {
            if (transport != null) throw new InvalidOperationException("Cierra la sesión anterior.");
            alias = CleanAlias(name);
            transport = new LanTransport();
            try { transport.Listen(port); }
            catch { transport.Dispose(); transport = null; throw; }
            IsHost = Connected = true;
            SelfId = "host";
            State = new LanSnapshot { members = new[] { new LanMember { id = SelfId, alias = alias, ready = true } } };
            Status = "Aula creada. Comparte la IPv4 mostrada; hasta 8 jugadores.";
            Changed?.Invoke();
        }

        public void Join(string name, string address, int port = LanTransport.DefaultPort)
        {
            if (transport != null) throw new InvalidOperationException("Cierra la sesión anterior.");
            if (!LanTransport.IsValidAddress(address)) throw new ArgumentException("Escribe una IPv4 completa, por ejemplo 192.168.1.10.");
            alias = CleanAlias(name);
            transport = new LanTransport();
            Status = "Conectando con el anfitrión…";
            transport.Connect(address, port);
            Changed?.Invoke();
        }

        public void Pump(float unscaledTime)
        {
            if (disposed || transport == null) return;
            now = unscaledTime;
            int budget = 128;
            while (budget-- > 0 && transport.TryDequeue(out var item))
            {
                if (item.Kind == "open")
                {
                    if (!IsHost) Send("server", new LanPacket { type = "join", alias = alias });
                }
                else if (item.Kind == "line") Process(item.Peer, item.Payload);
                else if (item.Kind == "error") { Status = item.Payload; Changed?.Invoke(); }
                else if (item.Kind == "close")
                {
                    if (IsHost)
                    {
                        joined.Remove(item.Peer);
                        lastPoseAt.Remove(item.Peer);
                        State.members = State.members.Where(m => m.id != item.Peer).ToArray();
                        Status = "Un compañero se desconectó. Puede volver al terminar la ronda.";
                        CheckReveal();
                        Broadcast();
                    }
                    else
                    {
                        Connected = false;
                        Status = terminalReason ?? "Se perdió la conexión. Vuelve a entrar cuando el anfitrión cree el aula.";
                        Changed?.Invoke();
                    }
                }
            }
            if (IsHost && posesDirty && unscaledTime - lastPoseBroadcast >= 0.2f)
            {
                lastPoseBroadcast = unscaledTime;
                posesDirty = false;
                State.poseTick++;
                Broadcast(false);
            }
            if (unscaledTime - lastHeartbeat > 2f)
            {
                lastHeartbeat = unscaledTime;
                if (IsHost) Broadcast(false);
                else if (Connected) Send("server", new LanPacket { type = "ping" });
            }
        }

        void Process(string peer, string json)
        {
            LanPacket packet;
            try { packet = JsonUtility.FromJson<LanPacket>(json); }
            catch (ArgumentException) { transport.ClosePeer(peer); return; }
            if (packet == null || packet.version != ProtocolVersion || string.IsNullOrEmpty(packet.type))
            { transport.ClosePeer(peer); return; }
            if (IsHost)
            {
                if (packet.type == "join" && !joined.Contains(peer))
                {
                    if (State.phase != "lobby" || State.members.Length >= MaxPlayers)
                    {
                        transport.SendAndClose(peer, JsonUtility.ToJson(new LanPacket { type = "error", message = "Aula llena o ronda iniciada. Espera al próximo lobby." }));
                        return;
                    }
                    joined.Add(peer);
                    State.members = State.members.Concat(new[] { new LanMember { id = peer, alias = CleanAlias(packet.alias) } }).ToArray();
                    Send(peer, new LanPacket { type = "welcome", self = peer });
                    Broadcast();
                    return;
                }
                if (!joined.Contains(peer)) { transport.ClosePeer(peer); return; }
                ApplyRequest(peer, packet);
            }
            else if (peer == "server")
            {
                if (packet.type == "welcome") { SelfId = packet.self; Connected = true; Status = "Conectado. Marca Listo para comenzar."; }
                else if (packet.type == "state" && ValidState(packet.state))
                {
                    bool onlyPose = State.revision == packet.state.revision && State.members.Length > 0;
                    if (onlyPose && State.poseTick == packet.state.poseTick) return;
                    State = packet.state;
                    CheckCompletion();
                    if (onlyPose) return;
                }
                else if (packet.type == "error") { terminalReason = packet.message; Status = packet.message; }
                else return;
                Changed?.Invoke();
            }
        }

        static bool ValidState(LanSnapshot s)
        {
            return s != null && s.members != null && s.members.Length <= MaxPlayers && s.members.All(m => m != null && m.id != null && m.alias != null)
                && s.element >= 1 && s.element <= 118 && s.step >= 0 && s.step <= 2 && (s.phase == "lobby" || s.phase == "quiz" || s.phase == "summary")
                && s.options != null && s.options.Length <= 3
                && (s.worldScene == "TablaPeriodica" || s.worldScene == "ElementoDetalle") && s.worldElement >= 1 && s.worldElement <= 118
                && s.members.All(m => !m.hasPose || (Finite(m.x) && Finite(m.y) && Finite(m.z) && Finite(m.yaw) && Math.Abs(m.x) <= 14 && m.y >= -1 && m.y <= 8 && m.z >= -8 && m.z <= 14));
        }

        public void SetReady(bool ready) => Request(new LanPacket { type = "ready", ready = ready });
        public void StartRound(int z) => Request(new LanPacket { type = "start", z = z });
        public void Answer(int choice) => Request(new LanPacket { type = "answer", choice = choice, round = State.round, step = State.step });
        public void RevealQuestion() => Request(new LanPacket { type = "reveal" });
        public void Next() => Request(new LanPacket { type = "next" });
        public void ReturnToLobby() => Request(new LanPacket { type = "lobby" });
        public void FollowElement(string scene, int element) => Request(new LanPacket { type = "follow", scene = scene, z = element });
        public void PublishPose(float x, float y, float z, float yaw, string scene, int element)
            => Request(new LanPacket { type = "pose", px = x, py = y, pz = z, yaw = yaw, scene = scene, z = element });

        void Request(LanPacket packet)
        {
            if (!Connected || disposed) return;
            if (IsHost) ApplyRequest("host", packet);
            else Send("server", packet);
        }

        void ApplyRequest(string peer, LanPacket packet)
        {
            var member = State.members.FirstOrDefault(m => m.id == peer);
            if (member == null) return;
            switch (packet.type)
            {
                case "ping": return;
                case "follow":
                    if (peer != "host" || (packet.scene != "TablaPeriodica" && packet.scene != "ElementoDetalle") || packet.z < 1 || packet.z > 118) return;
                    if (State.phase == "quiz" && (packet.scene != "ElementoDetalle" || packet.z != State.element)) return;
                    if (State.worldScene == packet.scene && State.worldElement == packet.z) return;
                    State.worldScene = packet.scene;
                    State.worldElement = packet.z;
                    foreach (var m in State.members) m.hasPose = false;
                    lastPoseAt.Clear();
                    break;
                case "pose":
                    if (packet.scene != State.worldScene || (packet.scene == "ElementoDetalle" && packet.z != State.worldElement)) return;
                    if (!Finite(packet.px) || !Finite(packet.py) || !Finite(packet.pz) || !Finite(packet.yaw)
                        || Math.Abs(packet.px) > 14f || packet.py < -1 || packet.py > 8 || packet.pz < -8 || packet.pz > 14 || Math.Abs(packet.yaw) > 3600) return;
                    if (lastPoseAt.TryGetValue(peer, out float previous) && now - previous < 0.1f) return;
                    lastPoseAt[peer] = now;
                    member.x = packet.px; member.y = packet.py; member.z = packet.pz;
                    member.yaw = (packet.yaw % 360 + 360) % 360;
                    member.hasPose = true;
                    posesDirty = true;
                    return;
                case "ready":
                    if (State.phase != "lobby" || peer == "host") return;
                    member.ready = packet.ready;
                    break;
                case "start":
                    if (peer != "host" || State.phase != "lobby" || State.members.Length < 2 || State.members.Any(m => !m.ready) || packet.z < 1 || packet.z > 118) return;
                    State.element = packet.z;
                    State.worldScene = "ElementoDetalle";
                    State.worldElement = packet.z;
                    foreach (var m in State.members) m.hasPose = false;
                    lastPoseAt.Clear();
                    State.round++;
                    State.step = 0;
                    State.phase = "quiz";
                    foreach (var m in State.members) { m.points = 0; m.correct = 0; }
                    PrepareQuestion();
                    break;
                case "answer":
                    if (State.phase != "quiz" || State.revealed || member.answered || packet.round != State.round || packet.step != State.step || packet.choice < 0 || packet.choice >= State.options.Length) return;
                    member.answered = true;
                    if (packet.choice == expectedAnswer) { member.points += 100; member.correct++; }
                    CheckReveal();
                    break;
                case "next":
                    if (peer != "host" || State.phase != "quiz" || !State.revealed) return;
                    if (State.step == 2) { State.phase = "summary"; CheckCompletion(); }
                    else { State.step++; PrepareQuestion(); }
                    break;
                case "reveal":
                    if (peer != "host" || State.phase != "quiz" || State.revealed) return;
                    foreach (var m in State.members) m.answered = true;
                    CheckReveal();
                    break;
                case "lobby":
                    if (peer != "host" || State.phase != "summary") return;
                    State.phase = "lobby";
                    State.explanation = "";
                    State.options = new string[0];
                    foreach (var m in State.members) { m.ready = m.id == "host"; m.answered = false; }
                    break;
                default: return;
            }
            Broadcast();
        }

        static bool Finite(float number) => !float.IsNaN(number) && !float.IsInfinity(number);

        void PrepareQuestion()
        {
            var e = ElementDatabase.Get(State.element);
            string correct;
            if (State.step == 0)
            {
                State.question = "¿Cuántos protones tiene un átomo de " + e.Name + "?";
                State.options = new[] { e.Z.ToString(), (e.Z + 1).ToString(), (e.Z + 2).ToString() };
                correct = e.Z.ToString();
            }
            else if (State.step == 1)
            {
                State.question = "¿A qué categoría de la leyenda pertenece " + e.Name + "?";
                correct = CategoryInfo.Name(e.Category);
                State.options = new[] { correct, CategoryInfo.Name((ElementCategory)(((int)e.Category + 1) % 10)), CategoryInfo.Name((ElementCategory)(((int)e.Category + 3) % 10)) };
            }
            else
            {
                State.question = "¿En qué periodo (fila) se ubica " + e.Name + "?";
                correct = e.Period.ToString();
                State.options = new[] { correct, (e.Period % 7 + 1).ToString(), ((e.Period + 1) % 7 + 1).ToString() };
            }
            var rng = new System.Random(State.round * 131 + State.element * 7 + State.step);
            for (int i = State.options.Length - 1; i > 0; i--) { int j = rng.Next(i + 1); string tmp = State.options[i]; State.options[i] = State.options[j]; State.options[j] = tmp; }
            expectedAnswer = Array.IndexOf(State.options, correct);
            State.revealed = false;
            State.explanation = "";
            foreach (var m in State.members) m.answered = false;
        }

        void CheckReveal()
        {
            if (State.phase != "quiz" || State.members.Any(m => !m.answered)) return;
            State.revealed = true;
            var e = ElementDatabase.Get(State.element);
            State.explanation = "Respuesta: " + State.options[expectedAnswer] + ". " + (State.step == 0
                ? "El número atómico Z indica la cantidad de protones del núcleo."
                : State.step == 1 ? e.Name + " está clasificado en " + CategoryInfo.Name(e.Category) + ". Consulta la leyenda de categorías."
                : "El periodo es la fila de la tabla periódica. " + e.Name + " ocupa la fila " + e.Period + ".");
        }

        void CheckCompletion()
        {
            if (State.phase != "summary" || completedRound == State.round || Self == null) return;
            completedRound = State.round;
            RoundCompleted?.Invoke(State.element, Self.correct);
        }

        void Send(string peer, LanPacket packet) => transport?.Send(peer, JsonUtility.ToJson(packet));
        void Broadcast(bool notify = true)
        {
            if (!IsHost || transport == null) return;
            if (notify) State.revision++;
            var packet = new LanPacket { type = "state", state = State };
            foreach (string peer in joined.ToArray()) Send(peer, packet);
            if (notify) Changed?.Invoke();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            transport?.Dispose();
            transport = null;
            Connected = false;
            Status = "Sesión cerrada. El progreso de aprendizaje se conserva.";
        }
    }
}

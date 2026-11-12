using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LabQuimicaVR
{
    [Serializable]
    public class ChallengeResult
    {
        public int Z, FirstTryCorrect, Mistakes, Stars, AwardedPoints, TotalPoints, Attempt;
    }

    [Serializable]
    public class ElementLearningRecord
    {
        public int Z, Attempts, BestFirstTry, LastFirstTry, LastMistakes, TotalMistakes;
        public bool Explored;
        public string LastCompletedUtc = "";
        public int Stars => Attempts == 0 ? 0 : Math.Max(1, BestFirstTry);
    }

    /// <summary>Modelo sin Unity ni E/S: permite probar guardado, dominio y puntuación.</summary>
    [Serializable]
    public class LearningRecordStore
    {
        public int Version = 1;
        public List<ElementLearningRecord> Records = new List<ElementLearningRecord>();
        public int TotalPoints => Records.Sum(r => (r.Explored ? 5 : 0) + r.Stars * 25);
        public int ExploredCount => Records.Count(r => r.Explored);
        public int CompletedCount => Records.Count(r => r.Attempts > 0);
        public int MasteredCount => Records.Count(r => r.Attempts > 0 && r.BestFirstTry >= 2);

        public ElementLearningRecord Find(int z) => Records.Find(r => r.Z == z);

        ElementLearningRecord Get(int z)
        {
            if (z < 1 || z > 118) throw new ArgumentOutOfRangeException(nameof(z));
            var record = Find(z);
            if (record != null) return record;
            record = new ElementLearningRecord { Z = z };
            Records.Add(record);
            return record;
        }

        public bool Explore(int z)
        {
            var record = Get(z);
            if (record.Explored) return false;
            record.Explored = true;
            return true;
        }

        public ChallengeResult Complete(int z, int firstTryCorrect, int mistakes)
        {
            if (firstTryCorrect < 0 || firstTryCorrect > 3) throw new ArgumentOutOfRangeException(nameof(firstTryCorrect));
            if (mistakes < 3 - firstTryCorrect || mistakes > 6) throw new ArgumentOutOfRangeException(nameof(mistakes));
            int before = TotalPoints;
            var record = Get(z);
            record.Explored = true;
            record.Attempts = Math.Min(100000, record.Attempts + 1);
            record.BestFirstTry = Math.Max(record.BestFirstTry, firstTryCorrect);
            record.LastFirstTry = firstTryCorrect;
            record.LastMistakes = mistakes;
            record.TotalMistakes = Math.Min(600000, record.TotalMistakes + mistakes);
            record.LastCompletedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            return new ChallengeResult { Z = z, FirstTryCorrect = firstTryCorrect, Mistakes = mistakes,
                Stars = Math.Max(1, firstTryCorrect), AwardedPoints = TotalPoints - before,
                TotalPoints = TotalPoints, Attempt = record.Attempts };
        }

        /// <summary>Datos locales incompletos o dañados nunca deben impedir entrar al laboratorio.</summary>
        public void Sanitize()
        {
            if (Records == null) Records = new List<ElementLearningRecord>();
            Records = Records.Where(r => r != null && r.Z >= 1 && r.Z <= 118)
                .GroupBy(r => r.Z).Select(g => g.OrderByDescending(r => r.BestFirstTry).First()).ToList();
            foreach (var r in Records)
            {
                r.Attempts = Math.Max(0, Math.Min(100000, r.Attempts));
                r.BestFirstTry = Math.Max(0, Math.Min(3, r.BestFirstTry));
                r.LastFirstTry = Math.Max(0, Math.Min(3, r.LastFirstTry));
                r.LastMistakes = Math.Max(0, Math.Min(6, r.LastMistakes));
                r.TotalMistakes = Math.Max(r.LastMistakes, Math.Min(600000, r.TotalMistakes));
                if (!string.IsNullOrEmpty(r.LastCompletedUtc))
                    r.LastCompletedUtc = DateTime.TryParse(r.LastCompletedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var completed)
                        ? completed.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture) : "";
                if (r.Attempts > 0) r.Explored = true;
            }
        }
    }

    public static class LearningProgress
    {
        const string SaveKey = "Quimikaze.Learning.v1";
        static LearningRecordStore store;
        public static event Action<ChallengeResult> ChallengeRecorded;
        public static event Action Changed;
        public static string LastSaveError { get; private set; }
        public static LearningRecordStore Store { get { EnsureLoaded(); return store; } }
        public static int TotalPoints => Store.TotalPoints;
        public static int ExploredCount => Store.ExploredCount;
        public static int CompletedCount => Store.CompletedCount;
        public static int MasteredCount => Store.MasteredCount;
        public static int TotalStars => Store.Records.Sum(r => r.Stars);
        public static int GetStars(int z) => Store.Find(z)?.Stars ?? 0;
        public static int GetAttempts(int z) => Store.Find(z)?.Attempts ?? 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuntime()
        {
            store = null;
            ChallengeRecorded = null;
            Changed = null;
            LastSaveError = null;
        }

        static void EnsureLoaded()
        {
            if (store != null) return;
            try
            {
                string json = PlayerPrefs.GetString(SaveKey, "");
                store = string.IsNullOrEmpty(json) ? new LearningRecordStore() : JsonUtility.FromJson<LearningRecordStore>(json);
                if (store == null || store.Version != 1) store = new LearningRecordStore();
                store.Sanitize();
            }
            catch (Exception ex)
            {
                store = new LearningRecordStore();
                LastSaveError = "No se pudo recuperar el progreso anterior. " + ex.Message;
                Debug.LogWarning(LastSaveError);
            }
        }

        static void Save()
        {
            try
            {
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Store));
                PlayerPrefs.Save();
                LastSaveError = null;
            }
            catch (Exception ex)
            {
                LastSaveError = "El progreso sigue en esta sesión, pero no se pudo guardar en el dispositivo.";
                Debug.LogWarning(LastSaveError + " " + ex.Message);
            }
            Changed?.Invoke();
        }

        public static void RecordExploration(int z)
        {
            if (Store.Explore(z)) Save();
        }

        public static ChallengeResult RecordChallenge(int z, int firstTryCorrect, int mistakes)
        {
            var result = Store.Complete(z, firstTryCorrect, mistakes);
            Save();
            ChallengeRecorded?.Invoke(result);
            return result;
        }

        public static string BuildCsv(LearningRecordStore source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            source.Sanitize();
            var csv = new StringBuilder("elemento_Z;simbolo;explorado;intentos;mejor_aciertos_sin_pista;ultimo_aciertos_sin_pista;ultimo_errores;errores_acumulados;medallas;dominio;ultima_fecha_UTC\n");
            foreach (var e in ElementDatabase.All)
            {
                var r = source.Find(e.Z);
                csv.Append(e.Z).Append(';').Append(e.Symbol).Append(';').Append(r != null && r.Explored ? "si" : "no")
                    .Append(';').Append(r?.Attempts ?? 0).Append(';').Append(r?.BestFirstTry ?? 0)
                    .Append(';').Append(r?.LastFirstTry ?? 0).Append(';').Append(r?.LastMistakes ?? 0)
                    .Append(';').Append(r?.TotalMistakes ?? 0).Append(';').Append(r?.Stars ?? 0)
                    .Append(';').Append(r != null && r.Attempts > 0 && r.BestFirstTry >= 2 ? "logrado" : "en_practica")
                    .Append(';').Append(r?.LastCompletedUtc ?? "").Append('\n');
            }
            return csv.ToString();
        }

        public static string ExportCsv()
        {
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Informes"));
            string path = Path.Combine(Application.persistentDataPath, "Informes", "Quimikaze_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".csv");
            File.WriteAllText(path, BuildCsv(Store), new UTF8Encoding(true));
            return path;
        }
    }
}

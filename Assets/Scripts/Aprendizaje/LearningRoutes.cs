using System.Linq;

namespace LabQuimicaVR
{
    public sealed class LearningMission
    {
        public readonly string Title, Context, Objective;
        public readonly int[] Elements;
        public LearningMission(string title, string context, string objective, params int[] elements)
        { Title = title; Context = context; Objective = objective; Elements = elements; }
        public int Achieved(LearningRecordStore store) => Elements.Count(z => (store.Find(z)?.Stars ?? 0) >= 2);
        public bool Complete(LearningRecordStore store) => Achieved(store) == Elements.Length;
        public int NextElement(LearningRecordStore store) => Elements.FirstOrDefault(z => (store.Find(z)?.Stars ?? 0) < 2);
    }

    public static class LearningRoutes
    {
        // Propuesta didáctica para 10–14 años; debe validarse con el docente de cada institución.
        public static readonly LearningMission[] Missions =
        {
            new LearningMission("1 · Los ingredientes de la vida", "Investiga los átomos presentes en el agua y los seres vivos.",
                "Relaciona categoría de la leyenda, estructura y propiedades de H, C y O.", 1, 6, 8),
            new LearningMission("2 · Del mar a la mesa", "Compara tres elementos relacionados con sales y minerales.",
                "Compara categoría de la leyenda, estructura y propiedades de Na, Mg y Cl.", 11, 12, 17),
            new LearningMission("3 · Materiales que nos rodean", "Explora elementos usados en cables, estructuras y tecnología.",
                "Compara categoría, estructura y posición o estado de Al, Si y Cu.", 13, 14, 29),
            new LearningMission("4 · Una familia especial", "¿Qué comparten los gases nobles de las luces y la atmósfera?",
                "Reconoce la familia de gases nobles, estructura y propiedades de He, Ne y Ar.", 2, 10, 18),
            new LearningMission("5 · Minerales del Perú", "Investiga elementos asociados a la minería y sus propiedades.",
                "Identifica y compara categoría, estructura y propiedades de Fe, Ag y Au.", 26, 47, 79),
            new LearningMission("6 · Ciencia en la frontera", "Distingue propiedades conocidas de datos aún no confirmados.",
                "Reconoce categoría, estructura e inexistencia de isótopos estables en U, Am y Og.", 92, 95, 118)
        };
        public static int CurrentIndex(LearningRecordStore store)
        {
            for (int i = 0; i < Missions.Length; i++) if (!Missions[i].Complete(store)) return i;
            return Missions.Length - 1;
        }
        public static int CompletedCount(LearningRecordStore store) => Missions.Count(m => m.Complete(store));
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace LightWeaver.Core
{
    /// <summary>
    /// Sahnede aktif olan tüm ILightReactive nesnelerinin sıfır tahsisli (Zero-GC) merkezi listesi.
    /// Fizik motorunu (Physics2D) kapalı collider'lar üzerinde zorlamadan,
    /// fener konisinin her karede 0 Byte GC ile tüm hedefleri taramasını sağlar.
    /// </summary>
    public static class LightReactiveRegistry
    {
        private static readonly List<ILightReactive> ActiveReactives = new List<ILightReactive>(128);

        public static IReadOnlyList<ILightReactive> All => ActiveReactives;

        public static void Register(ILightReactive reactive)
        {
            if (reactive != null && !ActiveReactives.Contains(reactive))
            {
                ActiveReactives.Add(reactive);
            }
        }

        public static void Unregister(ILightReactive reactive)
        {
            if (reactive != null)
            {
                ActiveReactives.Remove(reactive);
            }
        }

        public static void ClearAll()
        {
            ActiveReactives.Clear();
        }
    }
}

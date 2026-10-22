using UnityEngine;

namespace Palimpsest
{
    /// <summary>
    /// The game has no scene asset. This bootstrap is what makes the repository
    /// playable: when Play mode starts in any scene - including the default empty
    /// one - it creates the Palimpsest root and lets Arena.Awake build the entire
    /// world out of primitives.
    ///
    /// If a Palimpsest root already exists in the open scene it is left alone, so
    /// hand-placed setups (see the Palimpsest menu in the editor) still win.
    /// </summary>
    public static class PalimpsestBootstrap
    {
        public const string RootName = "The Palimpsest";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (Arena.Instance != null) return;
            if (UnityEngine.Object.FindObjectOfType<Arena>() != null) return;

            Spawn();
        }

        /// <summary>Creates the root object and attaches the arena. Idempotent.</summary>
        public static Arena Spawn()
        {
            Arena existing = UnityEngine.Object.FindObjectOfType<Arena>();
            if (existing != null)
            {
                Debug.Log("[Palimpsest] An arena already exists in this scene; not spawning another.");
                return existing;
            }

            GameObject root = new GameObject(RootName);
            Arena arena = root.AddComponent<Arena>();

            Debug.Log("[Palimpsest] A fresh folio was bound into the scene at runtime.");
            return arena;
        }
    }
}

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Palimpsest.EditorTools
{
    /// <summary>
    /// Editor conveniences for a repository that intentionally ships no scene files.
    ///
    /// Unity serialises scenes as YAML, but this project's rule is that every object
    /// must be authored in code, so these menus build the scene through the editor
    /// API instead. Nothing here is required to play: entering Play mode in any
    /// scene auto-boots the arena.
    /// </summary>
    public static class PalimpsestMenus
    {
        const string SceneFolder = "Assets/Scenes";
        const string ScenePath = SceneFolder + "/Palimpsest.unity";

        [MenuItem("Palimpsest/New Folio Scene", priority = 0)]
        public static void CreateFolioScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject(PalimpsestBootstrap.RootName).AddComponent<Arena>();

            if (!Directory.Exists(SceneFolder)) Directory.CreateDirectory(SceneFolder);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[Palimpsest] Scene written to " + ScenePath + ". Press Play to write the first hand.");
        }

        [MenuItem("Palimpsest/Add Palimpsest To Current Scene", priority = 1)]
        public static void AddToCurrentScene()
        {
            if (Object.FindObjectOfType<Arena>() != null)
            {
                Debug.Log("[Palimpsest] This scene already holds a Palimpsest.");
                return;
            }

            GameObject root = new GameObject(PalimpsestBootstrap.RootName);
            root.AddComponent<Arena>();

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("[Palimpsest] Root added. Press Play to build the page.");
        }

        [MenuItem("Palimpsest/Burn The Archive", priority = 20)]
        public static void BurnArchive()
        {
            PageArchive.Burn();
            Debug.Log("[Palimpsest] The archive is burned. The next folio starts on blank vellum.");
        }

        /// <summary>
        /// Re-reads the ledger of contributing hands from disk and rebinds the copy the
        /// colophon ships in a build. The canonical file is hidden from Unity's importer
        /// by its leading dot, so the colophon reads it off disk in the editor and keeps
        /// an undotted mirror beside it for players; this menu forces that copy up to
        /// date and prints who is on the page.
        /// </summary>
        [MenuItem("Palimpsest/Rebind The Colophon", priority = 21)]
        public static void RebindColophon()
        {
            ContributorLedger.Forget();

            List<Contributor> hands = ContributorLedger.Load();
            Debug.Log("[Palimpsest] The colophon records " + hands.Count + " hand(s) at the margin:");

            for (int i = 0; i < hands.Count; i++)
            {
                Debug.Log("   " + Hud.Roman(i + 1) + ". " + hands[i].ShortName + " - " + hands[i].Epithet);
            }
        }

        [MenuItem("Palimpsest/Delete Everything On The Page", priority = 22)]
        public static void Purge()
        {
            Arena[] arenas = Object.FindObjectsOfType<Arena>();
            for (int i = 0; i < arenas.Length; i++)
            {
                Object.DestroyImmediate(arenas[i].gameObject);
            }
            Debug.Log("[Palimpsest] Removed " + arenas.Length + " arena(s) from the open scene.");
        }
    }
}
#endif

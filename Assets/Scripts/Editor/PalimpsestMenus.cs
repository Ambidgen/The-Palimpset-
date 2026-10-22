#if UNITY_EDITOR
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

        [MenuItem("Palimpsest/Delete Everything On The Page", priority = 21)]
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

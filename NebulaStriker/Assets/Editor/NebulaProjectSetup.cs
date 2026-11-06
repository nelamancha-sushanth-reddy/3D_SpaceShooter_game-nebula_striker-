using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NebulaStriker.EditorTools
{
    /// <summary>
    /// One-click project setup. On first load it creates Assets/Scenes/Main.unity
    /// (camera + GameManager), registers it in Build Settings and checks input handling.
    /// Also adds a "Nebula Striker" menu to the Unity menu bar.
    /// </summary>
    [InitializeOnLoad]
    public static class NebulaProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";

        static NebulaProjectSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(ScenePath))
            {
                CreateMainScene();
                Debug.Log("[Nebula Striker] Main scene created at " + ScenePath + " — press Play!");
            }
            else
            {
                EnsureBuildSettings();
                if (!SessionState.GetBool("NS_OpenedMain", false) && string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
                {
                    SessionState.SetBool("NS_OpenedMain", true);
                    EditorSceneManager.OpenScene(ScenePath);
                }
            }
            CheckInputHandling();
        }

        [MenuItem("Nebula Striker/Rebuild Main Scene", priority = 1)]
        public static void CreateMainScene()
        {
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.farClipPlane = 2500f;
            camGO.AddComponent<AudioListener>();
            camGO.transform.position = new Vector3(0f, 3.2f, -17f);

            new GameObject("GameManager").AddComponent<GameManager>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            EnsureBuildSettings();
        }

        [MenuItem("Nebula Striker/Open Main Scene", priority = 2)]
        public static void OpenMainScene()
        {
            if (!File.Exists(ScenePath)) CreateMainScene();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Nebula Striker/Play Game", priority = 3)]
        public static void PlayGame()
        {
            OpenMainScene();
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Nebula Striker/Reset High Score", priority = 20)]
        public static void ResetHighScore()
        {
            PlayerPrefs.DeleteKey("NebulaStriker_HighScore");
            PlayerPrefs.Save();
            Debug.Log("[Nebula Striker] High score reset.");
        }

        static void EnsureBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length > 0 && scenes[0].path == ScenePath) return;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void CheckInputHandling()
        {
#if !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogWarning("[Nebula Striker] The classic Input Manager is disabled. Set Edit > Project Settings > Player > " +
                             "Other Settings > Active Input Handling to 'Both' (or 'Input Manager (Old)') and restart Unity.");
            if (SessionState.GetBool("NS_InputAsked", false)) return;
            SessionState.SetBool("NS_InputAsked", true);
            if (EditorUtility.DisplayDialog("Nebula Striker",
                    "This game uses Unity's classic Input Manager, which is currently disabled.\n\n" +
                    "Switch 'Active Input Handling' to 'Both' now? (Unity must be restarted afterwards.)",
                    "Switch", "Later"))
            {
                SetInputHandling(2);
            }
#endif
        }

        static void SetInputHandling(int value)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null)
            {
                EditorUtility.DisplayDialog("Nebula Striker", "Please change it manually in Project Settings > Player.", "OK");
                return;
            }
            prop.intValue = value;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Nebula Striker", "Done. Please close and reopen the project.", "OK");
        }
    }
}

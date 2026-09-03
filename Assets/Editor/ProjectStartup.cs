#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SpaceSim.Editor
{
    /// <summary>
    /// Makes a fresh clone immediately useful without committing Library/UserSettings.
    /// Opens the gameplay scene once per editor session, but only when Unity starts
    /// with an empty unsaved scene. Existing/opened scenes are never replaced.
    /// </summary>
    public static class ProjectStartup
    {
        private const string MainScene = "Assets/Scenes/space.unity";
        private const string SessionKey = "SpaceSim.ProjectStartup.Checked";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += OpenMainSceneIfNeeded;
        }

        [MenuItem("SpaceSim/Open Main Scene %#m")]
        public static void OpenMainScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScene) == null)
            {
                UnityEngine.Debug.LogError($"SpaceSim main scene was not found at '{MainScene}'.");
                return;
            }

            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
        }

        private static void OpenMainSceneIfNeeded()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            // A real saved scene is already open: respect the developer's choice.
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                return;
            }

            // Never discard edits in an unsaved scene.
            if (activeScene.isDirty)
            {
                return;
            }

            OpenMainScene();
        }
    }
}
#endif

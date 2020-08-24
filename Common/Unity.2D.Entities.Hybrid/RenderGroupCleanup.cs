using UnityEngine.SceneManagement;

namespace Unity.U2D.Entities
{
#if UNITY_EDITOR
    
    [UnityEditor.InitializeOnLoad]
    internal static class OnExitClear
    {
        static OnExitClear()
        {
            UnityEditor.EditorApplication.playModeStateChanged += change =>
            {
                if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode ||
                    change == UnityEditor.PlayModeStateChange.EnteredEditMode)
                {
                    CommonHybridUtils.ClearSpriteRendererGroup();
                }
            };
            
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpening += OnSceneOpening;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += OnSceneClosing;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
        }

        private static void OnSceneOpening(string path, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            CommonHybridUtils.ClearSpriteRendererGroup();
        }
        private static void OnSceneClosing(Scene scene, bool close)
        {
            CommonHybridUtils.ClearSpriteRendererGroup();
        }

        private static void OnSceneChanged(Scene a, Scene b)
        {
            CommonHybridUtils.ClearSpriteRendererGroup();
        }
    }
    
#endif
}

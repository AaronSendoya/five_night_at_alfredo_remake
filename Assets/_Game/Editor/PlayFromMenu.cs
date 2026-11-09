using UnityEditor;
using UnityEditor.SceneManagement;

namespace Alfredo.EditorTools
{
    /// <summary>
    /// Al pulsar Play en el editor, el juego arranca siempre desde el menú principal (como en el APK),
    /// aunque esté abierta la escena de juego. La escena abierta no cambia.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMenu
    {
        static PlayFromMenu() => EditorApplication.delayCall += Apply;

        public static void Apply()
        {
            var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuSceneBuilder.ScenePath);
            if (menu != null) EditorSceneManager.playModeStartScene = menu;
        }
    }
}

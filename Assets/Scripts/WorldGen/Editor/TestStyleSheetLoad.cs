using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace WorldGen.Editor
{
    public static class TestStyleSheetLoad
    {
        [MenuItem("Tools/Reimport MainMenu UI")]
        public static void ReimportMainMenuUI()
        {
            RunTest();
            AssetDatabase.ImportAsset("Assets/UI/MainMenu/MainMenu.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/UI/MainMenu/MainMenu.uxml", ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log("<color=green>[MainMenuUI] MainMenu.uss y MainMenu.uxml reimportados forzosamente con éxito.</color>");
        }

        private static void RunTest()
        {
            string path = "Assets/UI/MainMenu/MainMenu.uss";
            var ss = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (ss == null)
            {
                Debug.LogError($"[TestStyleSheetLoad] FAILED to load StyleSheet at '{path}'. Type in database: {AssetDatabase.GetMainAssetTypeAtPath(path)}");
            }
            else
            {
                Debug.Log($"<color=green>[TestStyleSheetLoad] SUCCESS: StyleSheet loaded! Asset: {ss.name}</color>");
            }

            string uxmlPath = "Assets/UI/MainMenu/MainMenu.uxml";
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (uxml == null)
            {
                Debug.LogError($"[TestStyleSheetLoad] FAILED to load VisualTreeAsset at '{uxmlPath}'");
            }
            else
            {
                Debug.Log($"<color=green>[TestStyleSheetLoad] SUCCESS: VisualTreeAsset loaded! Child count: {uxml.CloneTree().childCount}</color>");
            }
        }
    }
}

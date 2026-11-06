using UnityEditor;
using UnityEngine;

namespace WorldGen.UI.Editor
{
    [InitializeOnLoad]
    public static class ForceRefreshUXML
    {
        static ForceRefreshUXML()
        {
            EditorApplication.delayCall += ReimportAll;
        }

        [MenuItem("AK47/Forzar Reimportación Menú UXML")]
        public static void ReimportAll()
        {
            string[] assets = new string[]
            {
                "Assets/UI/MainMenu/MainMenu.uss",
                "Assets/UI/MainMenu/MainMenu_Unified.uxml",
                // UXMLs por facción (heredados, se pueden eliminar cuando migres las escenas)
                "Assets/UI/MainMenu/MainMenu.uxml",
                "Assets/UI/MainMenu/MainMenu_Anarquista.uxml",
                "Assets/UI/MainMenu/MainMenu_Capitalista.uxml",
                "Assets/UI/MainMenu/MainMenu_Comunista.uxml",
                "Assets/UI/MainMenu/MainMenu_Fascista.uxml"
            };

            foreach (var path in assets)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"<b>[ForceRefreshUXML]</b> ✅ Reimportado con éxito: {path}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}

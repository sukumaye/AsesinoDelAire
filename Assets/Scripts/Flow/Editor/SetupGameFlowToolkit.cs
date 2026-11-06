#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using WorldGen.Flow;

namespace WorldGen.Flow.Editor
{
    public static class SetupGameFlowToolkit
    {
        [MenuItem("Tools/AK-47/Conectar Flujo UI Toolkit Completo (Opcional)")]
        public static void ConnectToolkitFlow()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // Importar UXML y USS
            AssetDatabase.ImportAsset("Assets/UI/GameFlow/GameFlow.uxml", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/UI/GameFlow/GameFlow.uss", ImportAssetOptions.ForceUpdate);

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameFlow/GameFlow.uxml");
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/MainMenu/MainMenuPanelSettings.asset");
            var ki21Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/JAP_Ki21.png");

            // Configurar escena 0_BrandIntro.unity
            string brandScenePath = "Assets/Scenes/0_BrandIntro.unity";
            if (System.IO.File.Exists(brandScenePath))
            {
                var scene = EditorSceneManager.OpenScene(brandScenePath, OpenSceneMode.Single);

                // Configurar cámara
                var camObj = GameObject.Find("Main Camera");
                if (camObj != null)
                {
                    var cam = camObj.GetComponent<Camera>();
                    if (cam != null)
                    {
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = new Color(0.04f, 0.04f, 0.05f, 1f);
                        cam.orthographic = true;
                        cam.orthographicSize = 5f;
                    }
                }

                // Configurar UI Document para el flujo
                var flowObj = GameObject.Find("GameFlow_UIDocument");
                if (flowObj == null) flowObj = new GameObject("GameFlow_UIDocument");

                var uiDoc = flowObj.GetComponent<UIDocument>();
                if (uiDoc == null) uiDoc = flowObj.AddComponent<UIDocument>();

                uiDoc.panelSettings = panelSettings;
                uiDoc.visualTreeAsset = visualTree;

                var toolkitCtrl = flowObj.GetComponent<GameFlowToolkitController>();
                if (toolkitCtrl == null) toolkitCtrl = flowObj.AddComponent<GameFlowToolkitController>();

                toolkitCtrl.japaneseKi21Sprite = ki21Sprite;
                toolkitCtrl.gameplaySceneName = "gameplay";
                toolkitCtrl.menuSceneName = "menu";

                // Limpiar GameFlowController antiguo si existía
                var oldCtrl = GameObject.Find("GameFlowController");
                if (oldCtrl != null && oldCtrl != flowObj) Object.DestroyImmediate(oldCtrl);

                EditorUtility.SetDirty(uiDoc);
                EditorUtility.SetDirty(toolkitCtrl);
                EditorUtility.SetDirty(flowObj);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                Debug.Log("<color=green><b>[UI Toolkit Setup]</b> ¡Flujo de juego (Logo, Facciones con JAP_Ki21, Cinemática y Gameplay) configurado con éxito en 0_BrandIntro.unity!</color>");
            }
        }
    }
}
#endif

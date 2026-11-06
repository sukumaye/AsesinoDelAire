#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using WorldGen.UI;

namespace WorldGen.Editor
{
    [InitializeOnLoad]
    public static class SetupMenuScene
    {
        static SetupMenuScene()
        {
            EditorApplication.delayCall += () =>
            {
                ConnectMenuUI();
            };
        }

        [MenuItem("Tools/AK-47/Conectar Menu UI Toolkit")]
        public static void ConnectMenuUI()
        {
            if (AssetDatabase.IsAssetImportWorkerProcess()) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            string scenePath = "Assets/Scenes/menu.unity";
            if (!System.IO.File.Exists(scenePath)) return;

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/MainMenu/MainMenuPanelSettings.asset");
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MainMenu/MainMenu_Unified.uxml");
            if (visualTree == null)
            {
                visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MainMenu/MainMenu.uxml");
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            bool isCurrent = activeScene.path == scenePath;

            if (!isCurrent)
            {
                // Si la escena activa no es menu.unity, solo reportar
                Debug.Log($"<b>[UI Toolkit]</b> Recursos listos: PanelSettings: {(panelSettings != null ? "OK" : "Falta")}, UXML: {(visualTree != null ? "OK" : "Falta")}. Abre 'menu.unity' para ver el menú.");
                return;
            }

            // 1. Configurar Main Camera
            var camObj = GameObject.Find("Main Camera");
            if (camObj != null)
            {
                var cam = camObj.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.04f, 0.06f, 0.05f, 1f);
                    cam.orthographic = true;
                    cam.orthographicSize = 5f;
                    EditorUtility.SetDirty(cam);
                }
            }

            // 2. Buscar o crear MainMenu_UI
            var menuObj = GameObject.Find("MainMenu_UI");
            if (menuObj == null)
            {
                menuObj = new GameObject("MainMenu_UI");
            }

            // 3. Asignar directamente a UIDocument
            var uiDoc = menuObj.GetComponent<UIDocument>();
            if (uiDoc == null) uiDoc = menuObj.AddComponent<UIDocument>();

            bool changed = false;

            if (panelSettings != null && uiDoc.panelSettings != panelSettings)
            {
                uiDoc.panelSettings = panelSettings;
                changed = true;
            }

            if (visualTree != null && uiDoc.visualTreeAsset != visualTree)
            {
                uiDoc.visualTreeAsset = visualTree;
                changed = true;
            }

            // 4. Configurar MainMenuController
            var controller = menuObj.GetComponent<MainMenuController>();
            if (controller == null)
            {
                controller = menuObj.AddComponent<MainMenuController>();
                changed = true;
            }

            if (controller != null)
            {
                if (controller.gameplaySceneName != "gameplay")
                {
                    controller.gameplaySceneName = "gameplay";
                    changed = true;
                }

                // Asignar los sprites y texturas de las 4 facciones
                SetupFactionAssets(controller);
                changed = true;
            }

            // 5. Asegurar que menu.unity esté limpio de cualquier GameFlowController antiguo
            var oldFlowObj = GameObject.Find("GameFlowController");
            if (oldFlowObj != null)
            {
                Object.DestroyImmediate(oldFlowObj);
                changed = true;
                Debug.Log("<color=yellow><b>[Menu Setup]</b> Se ha removido GameFlowController de 'menu.unity' para que el Menú Principal quede 100% limpio y separado.</color>");
            }

            if (changed)
            {
                EditorUtility.SetDirty(uiDoc);
                EditorUtility.SetDirty(menuObj);
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log("<color=green><b>[UI Toolkit Setup]</b> ¡Menú Principal de 4 Facciones y Radio analógica configurado con éxito en 'menu.unity'!</color>");
            }
        }

        public static void SetupFactionAssets(MainMenuController controller)
        {
            if (controller.factions == null || controller.factions.Count != 4)
            {
                controller.InitializeDefaultFactions();
            }

            // Anarquista
            var anar = controller.GetFactionDefinition(FactionType.Anarquista);
            if (anar != null)
            {
                anar.radioSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Anarquista/menu/radio_menu_final_2.png");
                anar.backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Anarquista/New Folder/fondo (2).png");
                anar.logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Anarquista/New Folder/logo (2).png");
                anar.frameVertical = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Anarquista/New Folder/frame_vertical_anarquista.png");
                anar.frameHorizontal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Anarquista/New Folder/frame_horizontal_anarquista.png");
                anar.factionCover = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Anarquista/New Folder/Anarquismo.png");
            }

            // Capitalista
            var cap = controller.GetFactionDefinition(FactionType.Capitalista);
            if (cap != null)
            {
                cap.radioSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Capitalista/menu/radio_menu_final_1.png");
                cap.backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Capitalista/New Folder/fondo (3).png");
                cap.logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Capitalista/New Folder/logo (1).png");
                cap.frameVertical = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Capitalista/New Folder/frame_vertical_capitalista.png");
                cap.frameHorizontal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Capitalista/New Folder/frame_horizontal_capitalista.png");
                cap.factionCover = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Capitalista/New Folder/Capitalismo.png");
            }

            // Comunista
            var com = controller.GetFactionDefinition(FactionType.Comunista);
            if (com != null)
            {
                com.radioSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Comunista/menu/radio_menu_final_4.png");
                com.backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Comunista/New Folder/fondo (1).png");
                com.logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Comunista/New Folder/logo (4).png");
                com.frameVertical = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Comunista/New Folder/frame_vertical_comunista.png");
                com.frameHorizontal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Comunista/New Folder/frame_horizontal_comunismo.png");
                com.factionCover = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Comunista/New Folder/Comunismo.png");
            }

            // Fascista
            var fas = controller.GetFactionDefinition(FactionType.Fascista);
            if (fas != null)
            {
                fas.radioSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Facista/menu/radio_menu_final_3.png");
                fas.backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Facista/New Folder/fondo (4).png");
                fas.logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Facista/New Folder/logo (3).png");
                fas.frameVertical = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Facista/New Folder/frame_vertical_facista.png");
                fas.frameHorizontal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Facista/New Folder/frame_horizontal_facista.png");
                fas.factionCover = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Interfaces/Facista/New Folder/Facsimo.png");
            }

            EditorUtility.SetDirty(controller);
        }
    }
}
#endif

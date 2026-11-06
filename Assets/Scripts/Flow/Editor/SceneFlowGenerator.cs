#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using WorldGen.Flow;

namespace WorldGen.Flow.Editor
{
    [InitializeOnLoad]
    public static class SceneFlowGenerator
    {
        private const string PanelSettingsPath = "Assets/UI/MainMenu/MainMenuPanelSettings.asset";
        private const string Ki21SpritePath = "Assets/Art/Sprites/JAP_Ki21.png";

        [MenuItem("Tools/AK-47/Flujo Inicial/1. Generar Escenas UI Toolkit Independientes (Reiko Estudios)")]
        public static void EnsureAllScenesGenerated()
        {
            RegenerateScenes(true);
        }

        [MenuItem("Tools/AK-47/Flujo Inicial/2. Resetear Progreso (Ver Selección y Tutorial de nuevo)")]
        public static void ResetFlowProgress()
        {
            GameFlowDataStore.ResetIntroFlow();
            EditorUtility.DisplayDialog("Flujo Inicial Reseteado", 
                "El progreso del flujo inicial ha sido reseteado en PlayerPrefs.\n\nAl iniciar el juego (o dar Play en 0_BrandIntro), verás:\n1) Presentación de Reiko Estudios (UI Toolkit)\n2) Selección de Facción (UI Toolkit - Solo 1 vez)\n3) Cinemática e Historia (UI Toolkit)\n4) Tutorial de Tiro Balístico (UI Toolkit)\n5) Menú Principal (Hangar)", 
                "Entendido");
        }

        [MenuItem("Tools/AK-47/Flujo Inicial/3. Marcar Como Completado (Saltar directo al Menú Principal)")]
        public static void MarkFlowCompleted()
        {
            GameFlowDataStore.MarkIntroFlowCompleted();
            EditorUtility.DisplayDialog("Flujo Inicial Completado", 
                "El flujo inicial ha sido marcado como COMPLETADO en PlayerPrefs.\n\nAl iniciar desde 0_BrandIntro, verás únicamente la Presentación y saltará DIRECTAMENTE a 'menu.unity' (sin pasar por selección ni tutorial).", 
                "Entendido");
        }

        public static void RegenerateScenes(bool forceRecreate = true)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            string scenesDir = "Assets/Scenes";
            if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);

            // 1. Escena 0_BrandIntro.unity (SOLO presentación de marca y logo en UI Toolkit)
            CreateBrandIntroScene(forceRecreate);

            // 2. Escena 1_FactionSelection.unity (SOLO selección de facciones en UI Toolkit - Solo saldrá 1 vez)
            CreateFactionSelectionScene(forceRecreate);

            // 3. Escena 2_StoryCinematic.unity (SOLO cinemática narrativa en UI Toolkit)
            CreateStoryCinematicScene(forceRecreate);

            // 4. Escena 3_TutorialMission.unity (SOLO tutorial interactivo en UI Toolkit)
            CreateTutorialMissionScene(forceRecreate);

            // 5. Limpiar menu.unity para que sea exclusivamente el Menú Principal
            CleanMenuScene();

            // 6. Configurar Build Settings en orden estricto
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green><b>[SceneFlowGenerator]</b> ¡Todas las 4 escenas UI Toolkit independientes han sido generadas y desacopladas con éxito!</color>\n" +
                      "• Escena 0: 0_BrandIntro (BrandIntro.uxml + BrandIntroToolkitController)\n" +
                      "• Escena 1: 1_FactionSelection (FactionSelection.uxml + FactionSelectionToolkitController - Solo 1 vez)\n" +
                      "• Escena 2: 2_StoryCinematic (StoryCinematic.uxml + StoryCinematicToolkitController)\n" +
                      "• Escena 3: 3_TutorialMission (TutorialMission.uxml + TutorialMissionToolkitController)\n" +
                      "• Escena 4: menu (Menú Principal / Hangar - 100% limpio)");
        }

        private static void CreateBrandIntroScene(bool force)
        {
            string scenePath = "Assets/Scenes/0_BrandIntro.unity";
            if (File.Exists(scenePath) && !force) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "0_BrandIntro";

            CreateMainCamera(new Color(0.04f, 0.04f, 0.05f, 1f));

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Flow/BrandIntro.uxml");

            var uiObj = new GameObject("BrandIntro_UIDocument");
            var uiDoc = uiObj.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;

            var ctrl = uiObj.AddComponent<BrandIntroToolkitController>();
            ctrl.nextSceneName = "1_FactionSelection";
            ctrl.menuSceneName = "menu";
            ctrl.brandName = "";
            ctrl.brandSubtitle = "";
            ctrl.initialBlackDelay = 0.3f;
            ctrl.fadeInDuration = 1.2f;
            ctrl.visibleDuration = 1.6f;
            ctrl.fadeOutDuration = 1.2f;
            ctrl.finalBlackDuration = 0.5f;

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.CloseScene(scene, true);
        }

        private static void CreateFactionSelectionScene(bool force)
        {
            string scenePath = "Assets/Scenes/1_FactionSelection.unity";
            if (File.Exists(scenePath) && !force) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "1_FactionSelection";

            CreateMainCamera(Color.black);

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Flow/FactionSelection.uxml");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Ki21SpritePath);

            var uiObj = new GameObject("FactionSelection_UIDocument");
            var uiDoc = uiObj.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;

            var ctrl = uiObj.AddComponent<FactionSelectionToolkitController>();
            ctrl.nextSceneName = "2_StoryCinematic";
            ctrl.menuSceneName = "menu";
            ctrl.allowRevisitInEditor = true;
            ctrl.planeSprite = sprite;

            // Pre-asignar los marcos y fondos directamente
            ctrl.anarquistaFrame = LoadSpriteAsset("Assets/Art/Interfaces/Anarquista/Anarquismo.png");
            ctrl.capitalistaFrame = LoadSpriteAsset("Assets/Art/Interfaces/Capitalista/Capitalismo.png");
            ctrl.comunistaFrame = LoadSpriteAsset("Assets/Art/Interfaces/Comunista/Comunismo.png");
            ctrl.fascistaFrame = LoadSpriteAsset("Assets/Art/Interfaces/Facista/Facsimo.png");

            ctrl.anarquistaStatsFrame = LoadSpriteAsset("Assets/Art/Interfaces/Anarquista/frame_horizontal_anarquista.png");
            ctrl.capitalistaStatsFrame = LoadSpriteAsset("Assets/Art/Interfaces/Capitalista/frame_horizontal_capitalista.png");
            ctrl.comunistaStatsFrame = LoadSpriteAsset("Assets/Art/Interfaces/Comunista/frame_horizontal_comunismo.png");
            ctrl.fascistaStatsFrame = LoadSpriteAsset("Assets/Art/Interfaces/Facista/frame_horizontal_facista.png");

            ctrl.anarquistaPlaneFrame = LoadSpriteAsset("Assets/Art/Interfaces/Anarquista/frame_vertical_anarquista.png");
            ctrl.capitalistaPlaneFrame = LoadSpriteAsset("Assets/Art/Interfaces/Capitalista/frame_vertical_capitalista.png");
            ctrl.comunistaPlaneFrame = LoadSpriteAsset("Assets/Art/Interfaces/Comunista/frame_vertical_comunista.png");
            ctrl.fascistaPlaneFrame = LoadSpriteAsset("Assets/Art/Interfaces/Facista/frame_vertical_facista.png");

            // Añadir y conectar el controlador de transición cinematográfica DOTween
            var transition = uiObj.AddComponent<CinematicFactionTransition>();
            transition.selectionController = ctrl;
            transition.currentScreenUIDocument = uiDoc;
            transition.nextSceneName = "2_StoryCinematic";
            transition.anarquistaTransitionBg = LoadSpriteAsset("Assets/Art/Interfaces/Anarquista/fondo (2).png");
            transition.capitalistaTransitionBg = LoadSpriteAsset("Assets/Art/Interfaces/Capitalista/fondo (3).png");
            transition.comunistaTransitionBg = LoadSpriteAsset("Assets/Art/Interfaces/Comunista/fondo (1).png");
            transition.fascistaTransitionBg = LoadSpriteAsset("Assets/Art/Interfaces/Facista/fondo (4).png");
            transition.anarquistaEmblem = LoadSpriteAsset("Assets/Art/Interfaces/Anarquista/logo (2).png");
            transition.capitalistaEmblem = LoadSpriteAsset("Assets/Art/Interfaces/Capitalista/logo (1).png");
            transition.comunistaEmblem = LoadSpriteAsset("Assets/Art/Interfaces/Comunista/logo (4).png");
            transition.fascistaEmblem = LoadSpriteAsset("Assets/Art/Interfaces/Facista/logo (3).png");
            transition.EnsureSceneHierarchySetup();
            ctrl.cinematicTransition = transition;

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.CloseScene(scene, true);
        }

        private static Sprite LoadSpriteAsset(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Sprite spr) return spr;
            }
            return null;
        }

        private static void CreateStoryCinematicScene(bool force)
        {
            string scenePath = "Assets/Scenes/2_StoryCinematic.unity";
            if (File.Exists(scenePath) && !force) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "2_StoryCinematic";

            CreateMainCamera(new Color(0.03f, 0.03f, 0.04f, 1f));

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Flow/StoryCinematic.uxml");

            var uiObj = new GameObject("StoryCinematic_UIDocument");
            var uiDoc = uiObj.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;

            var ctrl = uiObj.AddComponent<StoryCinematicToolkitController>();
            ctrl.nextSceneName = "3_TutorialMission";
            ctrl.scrollSpeed = 50f;

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.CloseScene(scene, true);
        }

        private static void CreateTutorialMissionScene(bool force)
        {
            string scenePath = "Assets/Scenes/3_TutorialMission.unity";
            if (File.Exists(scenePath) && !force) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "3_TutorialMission";

            CreateMainCamera(new Color(0.05f, 0.07f, 0.06f, 1f));

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Flow/TutorialMission.uxml");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Ki21SpritePath);

            var uiObj = new GameObject("TutorialMission_UIDocument");
            var uiDoc = uiObj.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;

            var ctrl = uiObj.AddComponent<TutorialMissionToolkitController>();
            ctrl.nextSceneName = "menu";
            ctrl.planeSprite = sprite;

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.CloseScene(scene, true);
        }

        private static void CleanMenuScene()
        {
            string menuScenePath = "Assets/Scenes/menu.unity";
            if (!File.Exists(menuScenePath)) return;

            var currentActive = EditorSceneManager.GetActiveScene();
            bool wasOpen = currentActive.path == menuScenePath;

            var scene = wasOpen ? currentActive : EditorSceneManager.OpenScene(menuScenePath, OpenSceneMode.Additive);

            bool modified = false;
            var rootObjs = scene.GetRootGameObjects();
            foreach (var go in rootObjs)
            {
                if (go.name == "GameFlowController" || go.name == "GameFlow_UIDocument" || go.name == "BrandIntroController" || go.name == "FactionSelectionController")
                {
                    Object.DestroyImmediate(go);
                    modified = true;
                }
            }

            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!wasOpen)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Camera CreateMainCamera(Color bg)
        {
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10f);
            return cam;
        }

        public static void UpdateBuildSettings()
        {
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/0_BrandIntro.unity",
                "Assets/Scenes/1_FactionSelection.unity",
                "Assets/Scenes/2_StoryCinematic.unity",
                "Assets/Scenes/3_TutorialMission.unity",
                "Assets/Scenes/menu.unity",
                "Assets/Scenes/gameplay.unity"
            };

            var editorBuildSettingsScenes = new EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; i++)
            {
                editorBuildSettingsScenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            }

            EditorBuildSettings.scenes = editorBuildSettingsScenes;
        }
    }
}
#endif

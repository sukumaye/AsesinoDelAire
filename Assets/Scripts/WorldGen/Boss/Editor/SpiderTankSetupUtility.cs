using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WorldGen.Boss;
using WorldGen.Core;

namespace WorldGen.EditorTools
{
    [InitializeOnLoad]
    public static class SpiderTankSetupUtility
    {
        private static bool hasRun = false;

        static SpiderTankSetupUtility()
        {
            EditorApplication.delayCall += AutoRunSetupIfInGameplayScene;
        }

        private static void AutoRunSetupIfInGameplayScene()
        {
            if (hasRun) return;
            hasRun = true;

            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (currentScene.IsValid() && (currentScene.name == "gameplay" || string.IsNullOrEmpty(currentScene.name)))
            {
                SetupSpiderTankInScene();
            }
        }

        [MenuItem("AK47/🚀 Coger Todo y Meter Tanque al Final del Camino (Afuera y en 0°)", false, 1)]
        public static void SetupSpiderTankInScene()
        {
            MapGenerator mapGen = Object.FindAnyObjectByType<MapGenerator>();

            // 1. Buscar si ya existe el Tanque en la escena
            GameObject tankGO = null;
            var spiderExisting = Object.FindAnyObjectByType<SpiderTankController>();
            if (spiderExisting != null)
            {
                tankGO = spiderExisting.gameObject;
            }
            else
            {
                var allTransforms = Object.FindObjectsByType<Transform>();
                foreach (var t in allTransforms)
                {
                    if (t.name.ToLower().Contains("tanque"))
                    {
                        tankGO = t.gameObject;
                        break;
                    }
                }
            }

            // 2. Si no existe, instanciar desde el asset .psb
            if (tankGO == null)
            {
                GameObject psbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Animation/tanque (2).psb");
                if (psbPrefab != null)
                {
                    tankGO = PrefabUtility.InstantiatePrefab(psbPrefab) as GameObject;
                    if (tankGO != null)
                    {
                        tankGO.name = "Tanque Boss";
                        Undo.RegisterCreatedObjectUndo(tankGO, "Instanciar Tanque Boss");
                    }
                }
            }

            if (tankGO == null)
            {
                Debug.LogError("[TanqueSetup] No se pudo encontrar ni instanciar 'tanque (2)'.");
                return;
            }

            // 3. Añadir o recuperar SpiderTankController
            SpiderTankController controller = tankGO.GetComponent<SpiderTankController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<SpiderTankController>(tankGO);
            }

            // 4. Forzar ángulo recto alineado en el eje a 0°
            controller.keepStrictRightAngle = true;
            controller.baseRotationZ = 0f;
            controller.EnforceRightAngle();

            // 5. Detectar huesos y posturas
            controller.EnsureBonesDetected();
            controller.SaveRestPoses(true);

            // 6. Posicionar un poco afuera de la pantalla al final del camino
            if (mapGen != null)
            {
                Vector3 roadEnd = mapGen.GetRoadEndPosition();
                Vector3 finalPos = roadEnd + new Vector3(0f, controller.outsideOffsetY, 0f) + controller.roadEndOffset;

                controller.SetAnchorPosition(finalPos);
                tankGO.transform.position = finalPos;
                controller.EnforceRightAngle();

                mapGen.endLevelCharacterInstance = tankGO.transform;
                mapGen.endLevelCharacterRotationZ = 0f;
                EditorUtility.SetDirty(mapGen);
            }

            // 7. Asegurar SortingGroup
            var sg = tankGO.GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (sg == null) sg = tankGO.AddComponent<UnityEngine.Rendering.SortingGroup>();
            sg.sortingOrder = 10;

            // 8. Hornear el AnimationClip limpio con 4 patas mecánicas y cabeza
            SpiderTankControllerEditor.BakeSpiderAnimationClip(controller);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(tankGO);
            EditorSceneManager.MarkSceneDirty(tankGO.scene);
            EditorSceneManager.SaveScene(tankGO.scene);

            Selection.activeGameObject = tankGO;
            EditorGUIUtility.PingObject(tankGO);

            Debug.Log($"<color=green><b>[TanqueSetup]</b> ¡Tanque Boss configurado! Posicionado AFUERA en ({tankGO.transform.position.x:F1}, {tankGO.transform.position.y:F1}), en ángulo recto (0°), 4 patas mecánicas (delanteras: Y, traseras: X) y patrón de ataque activo.</color>");
        }
    }
}

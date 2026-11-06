#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WorldGen.Flow.Editor
{
    public static class ResetGameFlowProgress
    {
        [MenuItem("AK-47/🔄 Resetear Flujo (Tutorial y Facciones)", false, 10)]
        public static void ResetProgress()
        {
            GameFlowDataStore.ResetIntroFlow();
            EditorUtility.DisplayDialog(
                "AK-47 — Flujo Inicial",
                "El flujo inicial se ha reseteado con éxito.\n\nAhora la app no irá directo al menú, sino que volverá a pasar por la Selección de Facción y el Tutorial de Calibración.",
                "Entendido"
            );
        }

        [MenuItem("AK-47/▶️ Abrir Escena Inicial (0_BrandIntro)", false, 11)]
        public static void OpenBrandIntroScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene("Assets/Scenes/0_BrandIntro.unity");
            }
        }
    }
}
#endif

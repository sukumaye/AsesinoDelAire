#if UNITY_EDITOR
using UnityEditor;

namespace WorldGen.Flow.Editor
{
    public static class SetupGameFlowScene
    {
        [MenuItem("Tools/AK-47/Flujo Inicial/Configurar y Limpiar Menú")]
        public static void ConnectFlowToMenuScene()
        {
            SceneFlowGenerator.RegenerateScenes(true);
        }
    }
}
#endif

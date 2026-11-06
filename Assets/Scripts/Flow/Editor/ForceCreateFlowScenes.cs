#if UNITY_EDITOR
using UnityEditor;

namespace WorldGen.Flow.Editor
{
    public static class ForceCreateFlowScenes
    {
        [MenuItem("Tools/AK-47/Flujo Inicial/Regenerar Escenas Ahora Mismo")]
        public static void GenerateScenes()
        {
            SceneFlowGenerator.RegenerateScenes(true);
        }
    }
}
#endif

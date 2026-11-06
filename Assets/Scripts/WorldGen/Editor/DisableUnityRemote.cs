#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DisableUnityRemote
{
    static DisableUnityRemote()
    {
        Apply();
    }

    [InitializeOnEnterPlayMode]
    static void OnEnterPlay(EnterPlayModeOptions options)
    {
        Apply();
    }

    [MenuItem("Tools/Reimportar Estilos UI")]
    public static void ReimportUIStyles()
    {
        AssetDatabase.ImportAsset("Assets/UI/Flow/FlowStyles.uss", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/UI/Flow/FactionSelection.uxml", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/UI/Flow/BrandIntro.uxml", ImportAssetOptions.ForceUpdate);
        Debug.Log("<color=green><b>[UI]</b> Estilos y UXML de Flow reimportados limpiamente.</color>");
    }

    [MenuItem("Tools/Desactivar Unity Remote")]
    public static void Apply()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess()) return;

        if (EditorSettings.unityRemoteDevice != "None")
        {
            EditorSettings.unityRemoteDevice = "None";
            Debug.Log("<color=green><b>[DisableUnityRemote]</b> Unity Remote desactivado con éxito en el Editor (None).</color>");
        }

        if (EditorSettings.enterPlayModeOptionsEnabled)
        {
            EditorSettings.enterPlayModeOptionsEnabled = false;
        }
    }
}
#endif

using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor;
using UnityEngine;

public class BuildProcess : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        string[] result = AssetDatabase.FindAssets("Config", new string[] { "Assets/Config/" });

        string path = AssetDatabase.GUIDToAssetPath(result[0]);
        var config = (Config)AssetDatabase.LoadAssetAtPath(path, typeof(Config));

        config.buildVersion = Application.platform == RuntimePlatform.IPhonePlayer
            ? PlayerSettings.iOS.buildNumber
            : PlayerSettings.Android.bundleVersionCode.ToString();

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
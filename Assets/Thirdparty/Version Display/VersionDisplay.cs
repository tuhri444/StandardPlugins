using System;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class VersionDisplay : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Text in front of the actual version number.")]
    private string _versionText;

    [SerializeField]
    [Tooltip("File that holds the build number after building.")]
    private Config _buildVersion;

    private TextMeshProUGUI _versionTextObject;

    private void Awake()
    {
        _versionTextObject = GetComponent<TextMeshProUGUI>();
#if UNITY_EDITOR
        _versionTextObject.text = $"{_versionText}:  {Application.version} ({PlayerSettings.Android.bundleVersionCode})";
#else
        _versionTextObject.text = $"{_versionText}:  {Application.version} ({_buildVersion.buildVersion})";
#endif


    }
}

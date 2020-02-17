using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CloudBuildLauncher
{
    /// <summary>
    /// CloudBuildLauncherの設定値
    /// </summary>
    [CreateAssetMenu]
    public class CloudBuildSettings : ScriptableObject
    {
        [SerializeField]
        public string orgId;

        [SerializeField]
        public string projectId;

        [SerializeField]
        public string apiToken;

        [SerializeField]
        public List<string> targetConfigs = new List<string>();

        const string settingsAssetPath = "Assets/CloudBuildLauncher/Editor/CloudBuildLauncher.asset";

        public static CloudBuildSettings LoadSettings()
        {
            return AssetDatabase.LoadAssetAtPath<CloudBuildSettings>(settingsAssetPath);
        }

        public static CloudBuildSettings CreateSettingsAsset()
        {
            var settings = ScriptableObject.CreateInstance<CloudBuildSettings>();
            AssetDatabase.CreateAsset(settings, settingsAssetPath);
            return settings;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CloudBuildLauncher
{
    /// <summary>
    /// CloudBuildLauncherの設定ウィンドウ
    /// </summary>
    public class CloudBuildSettingsWindow : EditorWindow
    {
        [SerializeField]
        private List<string> targetConfigs;

        private CloudBuildSettings settings;
        private bool initialized = false;

        [MenuItem("Window/Cloud Build Launcher/Settings")]
        public static void Open()
        {
            var win = GetWindow<CloudBuildSettingsWindow>("Settings");
            win.minSize = new Vector2(400, 400);
        }

        CloudBuildSettings LoadOrCreateSettings()
        {
            var settings = CloudBuildSettings.LoadSettings();

            if (settings == null)
            {
                settings = CloudBuildSettings.CreateSettingsAsset();
            }
            this.targetConfigs = settings.targetConfigs.ToList();
            return settings;
        }

        private void Initialize()
        {
            if (this.settings == null) {
                this.settings = LoadOrCreateSettings();
            }
        }

        private void OnGUI()
        {
            if (!initialized)
            {
                Initialize();
            }

            // title
            EditorGUILayout.LabelField("Cloud Build Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            var orgId = EditorGUILayout.TextField("Organization Name", settings.orgId);
            CommentBox("Organization Name can be found on the Organizations page, which has a link on your Unity ID account page.");
            var projectId = EditorGUILayout.TextField("Project ID", settings.projectId);
            CommentBox("Project ID can be found on Overview page on the Unity dashboard of the target project.\nProject ID is like 1234abcd-12ab-12ab-12ab-123456abcdef");
            var apiToken = EditorGUILayout.TextField("API Token", settings.apiToken);
            CommentBox("API Token can be found under `Settings` -> `Cloud Build` on the Unity dashboard of the target project.\nAPI Token is like 1234567890abcdef1234567890abcdef");

            var so = new SerializedObject(this);
            so.Update();
            EditorGUILayout.PropertyField(so.FindProperty("targetConfigs"), new GUIContent("Target Config IDs"), true);
            so.ApplyModifiedProperties();
            CommentBox("Config ID can be found as a URL path component of the edit pages of the Cloud Build configs. It is like the name of config but not always the same.");

            if (EditorGUI.EndChangeCheck() || settings.targetConfigs.Count != targetConfigs.Count)
            {
                Undo.RecordObject(settings, "Change Cloud Build Settings");
                settings.orgId = orgId;
                settings.projectId = projectId;
                settings.apiToken = apiToken;
                settings.targetConfigs = targetConfigs.ToList();
                EditorUtility.SetDirty(settings);
            }
        }

        void CommentBox(string text)
        {
            EditorGUILayout.HelpBox(text, MessageType.Info);
        }
    }
}
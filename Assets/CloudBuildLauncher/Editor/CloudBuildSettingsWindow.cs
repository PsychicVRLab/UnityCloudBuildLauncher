using System;
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
            targetConfigs = settings.targetConfigs.ToList();
            return settings;
        }

        private void Initialize()
        {
            if (settings == null) {
                settings = LoadOrCreateSettings();
            }

            if (settings != null
                && !string.IsNullOrEmpty(settings.orgId)
                && !string.IsNullOrEmpty(settings.projectId)
                && !string.IsNullOrEmpty(settings.apiToken))
            {
                var response = FetchBuildTargets();
                if (response != null)
                {
                    buildTargetIds = response.TargetIds.ToList();
                }
            }
        }

        private int selected;

        private List<string> buildTargetIds = new List<string>();

        private string filterText = string.Empty;

        private void OnGUI()
        {
            if (!initialized)
            {
                initialized = true;
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

            
            // build config filter
            GUILayout.BeginHorizontal();

            IEnumerable<string> filteredNames = buildTargetIds;
            if (!string.IsNullOrEmpty(filterText))
            {
                filteredNames = buildTargetIds.Where(name => name.Contains(filterText));
            }
            selected = EditorGUILayout.Popup("Select Config", selected, filteredNames.ToArray());

            GUILayout.Label("Filter:", GUILayout.Width(40));
            filterText = GUILayout.TextField(filterText, "SearchTextField", GUILayout.Width(80)).Trim();
            GUI.enabled = !string.IsNullOrEmpty(filterText);
            if (GUILayout.Button("Clear", "SearchCancelButton"))
            {
                filterText = string.Empty;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reload Configs", GUILayout.Width(100)))
            {
                var response = FetchBuildTargets();
                if (response != null)
                {
                    buildTargetIds = response.TargetIds.ToList();
                }
            }

            // Button "Add To Targets"
            var selectable = (selected >= 0 && selected < filteredNames.Count());
            EditorGUI.BeginDisabledGroup(!selectable);
            if (GUILayout.Button("Add To Targets"))
            {
                var selectedId = filteredNames.ElementAt(selected);
                targetConfigs.Add(selectedId);
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

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

        BuildTargetResponseModel FetchBuildTargets()
        {
            var api = new CloudBuildApi(settings);
            IEnumerator coLaunch = api.ListBuildTargets();
            while (coLaunch.MoveNext())
            {
                Debug.Log("Current: " + coLaunch.Current);
            }
            if ((string)coLaunch.Current != "error")
            {
                var json = (string) coLaunch.Current;
                Debug.Log("build targets: " + json);
                var modifiedJson = "{ \"targets\":" + json + "}";
                var result = JsonUtility.FromJson<BuildTargetResponseModel>(modifiedJson);
                if (result == null || result.TargetIds == null)
                {
                    Debug.LogError("result is null.");
                    return null;
                }
                Debug.Log("build target names: " + String.Join(", ", result.TargetIds));

                return result;

                // success
                //status += "start building target:" + targetId + " succeeded.\n";
            }
            else
            {
                // failure
                var msg = "list build targets failed.\n";
                //status += msg;
                throw new Exception(msg);
            }
        }

    }
}
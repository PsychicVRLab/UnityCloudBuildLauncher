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
                ReloadConfigs();
            }
        }

        private int selected;

        private List<string> buildTargetIds = new List<string>();

        private string filterText = string.Empty;

        private List<ProjectModel> projects;
        private int selectedProjectIndex;
        private Vector2 _scrollPosition = Vector2.zero;

        private void OnGUI()
        {
            if (!initialized)
            {
                initialized = true;
                Initialize();
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // title
            EditorGUILayout.LabelField("Cloud Build Settings", EditorStyles.boldLabel);

            // open launcher button
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Launcher Window..."))
            {
                LauncherWindow.Open();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);


            EditorGUI.BeginChangeCheck();

            var apiToken = EditorGUILayout.TextField("API Token", settings.apiToken);
            CommentBox("API Token can be found under `Settings` -> `Cloud Build` on the Unity dashboard of the target project.\nAPI Token is like 1234567890abcdef1234567890abcdef");

            // Project Selector
            if (string.IsNullOrEmpty(settings.orgId) && string.IsNullOrEmpty(settings.projectId))
            {
                GUILayout.Space(10);
                if (projects == null)
                {
                    if (GUILayout.Button("Select a project", GUILayout.Height(40)))
                    {
                        projects = FetchAllProjects();
                    }
                }
            }

            if (projects != null)
            {
                GUILayout.BeginHorizontal();
                var projectNames = projects.Select(project => project.name + " (" + project.orgName + ")");
                selectedProjectIndex = EditorGUILayout.Popup("Select Project", selectedProjectIndex, projectNames.ToArray());

                var projectSelectable = (selectedProjectIndex >= 0 && selectedProjectIndex < projects.Count);
                EditorGUI.BeginDisabledGroup(!projectSelectable);
                if (GUILayout.Button("Select", GUILayout.Width(100)))
                {
                    var project = projects.ElementAt(selectedProjectIndex);
                    settings.orgId = project.orgid;
                    settings.projectId = project.guid;
                    ReloadConfigs();
                }
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("Reload", GUILayout.Width(60)))
                {
                    projects = FetchAllProjects();
                }
                GUILayout.EndHorizontal();
            }

            // Organization, Project
            GUILayout.Space(20);
            var orgId = EditorGUILayout.TextField("Organization Name", settings.orgId);
            //CommentBox("Organization Name can be found on the Organizations page, which has a link on your Unity ID account page.");
            var projectId = EditorGUILayout.TextField("Project ID", settings.projectId);
            //CommentBox("Project ID can be found on Overview page on the Unity dashboard of the target project.\nProject ID is like 1234abcd-12ab-12ab-12ab-123456abcdef");

            if (!string.IsNullOrEmpty(settings.orgId) || !string.IsNullOrEmpty(settings.projectId))
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (projects == null)
                {
                    if (GUILayout.Button("Select other project"))
                    {
                        projects = FetchAllProjects();
                    }
                }
                GUILayout.EndHorizontal();
            }


            // configs
            GUILayout.Space(20);
            var so = new SerializedObject(this);
            so.Update();
            EditorGUILayout.PropertyField(so.FindProperty("targetConfigs"), new GUIContent("Target Config IDs"), true);
            so.ApplyModifiedProperties();
            //CommentBox("Config ID can be found as a URL path component of the edit pages of the Cloud Build configs. It is like the name of config but not always the same.");


            // config filter
            GUILayout.Space(20);
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
                ReloadConfigs();
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
            
            EditorGUILayout.EndScrollView();

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

        void ReloadConfigs()
        {
            var targetIds = FetchBuildTargetIds();
            if (targetIds != null)
            {
                buildTargetIds = targetIds.OrderBy(target => target).ToList();
            }
        }

        void CommentBox(string text)
        {
            EditorGUILayout.HelpBox(text, MessageType.Info);
        }

        List<string> FetchBuildTargetIds()
        {
            var targets = FetchBuildTargets();
            if (targets == null) return null;
            return targets.Select(target => target.buildtargetid).ToList();
        }

        List<BuildTargetModel> FetchBuildTargets()
        {
            var api = new CloudBuildApi(settings);
            IEnumerator fetchCoroutine = api.ListBuildTargets();
            var targets = FetchArrayFromServer<BuildTargetModel>("fetch build targets", fetchCoroutine);
            return targets;
        }

        private List<ProjectModel> FetchAllProjects()
        {
            var api = new CloudBuildApi(settings);
            IEnumerator fetchCoroutine = api.ListAllProjects();
            var projects = FetchArrayFromServer<ProjectModel>("fetch all projects", fetchCoroutine);
            return projects;
        }

        /// <summary>
        /// Fetch json array from the server.
        /// As JsonUtility can't directly read array json, this method wraps the array json to read correctly.
        /// </summary>
        /// <typeparam name="T">array element model type</typeparam>
        /// <param name="logTitle">title for logging</param>
        /// <param name="fetchCoroutine">coroutine to access api</param>
        /// <returns></returns>
        List<T> FetchArrayFromServer<T>(string logTitle, IEnumerator fetchCoroutine)
        {
            var api = new CloudBuildApi(settings);
            while (fetchCoroutine.MoveNext())
            {
                Debug.Log("FetchArrayFromServer: " + logTitle + " Current: " + fetchCoroutine.Current);
            }
            if ((string)fetchCoroutine.Current != "error")
            {
                var json = (string)fetchCoroutine.Current;
                Debug.Log("FetchArrayFromServer: " + logTitle + " json:" + json);
                var modifiedJson = "{ \"items\":" + json + "}";
                var result = JsonUtility.FromJson<GenericArrayResponseModel<T>>(modifiedJson);
                if (result == null || result.items == null)
                {
                    Debug.LogError("FetchArrayFromServer: " + logTitle + ": result is null.");
                    return null;
                }
                return result.items;
            }
            else
            {
                throw new Exception("FetchArrayFromServer: " + logTitle + ": failed.");
            }
        }
    }
}
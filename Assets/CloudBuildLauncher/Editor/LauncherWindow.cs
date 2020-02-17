using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using System.Threading;

namespace CloudBuildLauncher
{
    /// <summary>
    /// CloudBuildの複数Configのブランチだけ変えて
    /// ビルドかける作業がだいぶ大変なので操作を簡略化するツール
    /// </summary>
    public class LauncherWindow : EditorWindow
    {
        private const string cloudBuildApiDomain = "build-api.cloud.unity3d.com";
        private const string cloudBuildApiBaseUrl = "https://" + cloudBuildApiDomain + "/api/v1";

        private CloudBuildSettings settings;
        private GUIStyle styleWarningLabel;
        private bool initialized = false;

        [MenuItem("Window/Cloud Build Launcher/Launcher")]
        static void Open()
        {
            var settings = CloudBuildSettings.LoadSettings();

            if (settings == null)
            {
                CloudBuildSettingsWindow.Open();
            }
            else
            {
                var win = GetWindow<LauncherWindow>("Launcher");
                win.minSize = new Vector2(250, 400);
            }
        }

        HashSet<string> selectedConfigNames;

        private bool changeBranch = true;
        private string branchName = "";

        private string status;

        void SelectConfigName(string configName, bool select)
        {
            if (select)
            {
                selectedConfigNames.Add(configName);
            }
            else
            {
                selectedConfigNames.Remove(configName);
            }
        }

        bool ConfigNameSelected(string configName)
        {
            return selectedConfigNames.Contains(configName);
        }

        void Initialize()
        {
            styleWarningLabel = new GUIStyle(GUI.skin.label);
            styleWarningLabel.normal.textColor = Color.red;
            if (settings == null) {
                settings = CloudBuildSettings.LoadSettings();
            }
        }

        void OnGUI()
        {
            if (!initialized)
            {
                Initialize();
            }

            if (selectedConfigNames == null)
            {
                selectedConfigNames = new HashSet<string>();
            }

            // title
            EditorGUILayout.LabelField("CloudBuild Launcher", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // settings button
            if (GUILayout.Button("Open Settings...", GUILayout.MaxWidth(120)))
            {
                CloudBuildSettingsWindow.Open();
            }
            GUILayout.Space(10);

            // target configs
            EditorGUILayout.LabelField("Target configs", EditorStyles.boldLabel);
            foreach (var target in settings.targetConfigs)
            {
                var enabled = GUILayout.Toggle(ConfigNameSelected(target), target);
                SelectConfigName(target, enabled);
            }
            GUILayout.Space(10);

            // config adjustment
            EditorGUILayout.LabelField("Config adjustment", EditorStyles.boldLabel);
            changeBranch = EditorGUILayout.Toggle("Change git branch", changeBranch);
            if (changeBranch)
            {
                EditorGUILayout.LabelField("Git branch name");
                branchName = EditorGUILayout.TextField(branchName);
                if (string.IsNullOrEmpty(branchName))
                {
                    EditorGUILayout.LabelField("Please input a valid branch name.", styleWarningLabel);
                }
            }
            GUILayout.Space(20);

            // Launch button
            EditorGUI.BeginDisabledGroup(!IsInputValid());
            if (GUILayout.Button("Launch!"))
            {
                AdjustAndLaunchConfigs();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(10);

            // status text
            GUILayout.Label("Status:");
            EditorGUILayout.SelectableLabel(status, EditorStyles.textArea, GUILayout.MaxHeight(100), GUILayout.MinHeight(20));
        }

        bool IsInputValid()
        {
            if (selectedConfigNames.Count == 0)
            {
                return false;
            }
            if (changeBranch && string.IsNullOrEmpty(branchName))
            {
                return false;
            }
            return true;
        }

        void AdjustAndLaunchConfigs()
        {
            status = "";
            try
            {
                // selectedConfigNames をそのまま使っていないのは、Settingsから削除されて見えなくなっているconfigを
                // 使ってしまわないようにするため。
                foreach (var targetId in settings.targetConfigs)
                {
                    if (ConfigNameSelected(targetId))
                    {
                        AdjustAndLaunchConfig(targetId);
                    }
                }
            } catch(Exception e)
            {
                Debug.LogError("Error: " + e.Message + "\nStackTrace:" + e.StackTrace);
            }
        }

        void AdjustAndLaunchConfig(string targetId)
        {
            if (changeBranch)
            {
                // Adjust Config
                Debug.Log("Adjust config for target: " + targetId + " branch: " + branchName);
                IEnumerator co = ChangeConfigBranch(targetId, branchName);
                while (co.MoveNext())
                {
                    Debug.Log("Current: " + co.Current);
                }
                if ((string)co.Current == "true")
                {
                    // success
                    status += "adjusting target:" + targetId + " succeeded.\n";
                }
                else
                {
                    // failure
                    var msg = "adjusting target:" + targetId + " failed.\n";
                    status += msg;
                    throw new Exception(msg);
                }
            }

            // Launch the build
            Debug.Log("Launching target: " + targetId);
            IEnumerator coLaunch = LaunchBuild(targetId);
            while (coLaunch.MoveNext())
            {
                Debug.Log("Current: " + coLaunch.Current);
            }
            if ((string)coLaunch.Current == "true")
            {
                // success
                status += "start building target:" + targetId + " succeeded.\n";
            }
            else
            {
                // failure
                var msg = "start building target:" + targetId + " failed.\n";
                status += msg;
                throw new Exception(msg);
            }
        }

        string GetConfigUpdateUrl(string buildTargetId)
        {
            return String.Format("{0}/orgs/{1}/projects/{2}/buildtargets/{3}",
                cloudBuildApiBaseUrl, settings.orgId, settings.projectId, buildTargetId);
        }

        string GetBuildCreateUrl(string buildTargetId)
        {
            return String.Format("{0}/orgs/{1}/projects/{2}/buildtargets/{3}/builds",
                cloudBuildApiBaseUrl, settings.orgId, settings.projectId, buildTargetId);
        }

        string GetChangeConfigBranchPayload(string branchName)
        {
            return "{" +
                   "  \"settings\": {" +
                   "    \"scm\": {" +
                   "      \"type\": \"git\"," +
                   "      \"branch\":\"" + branchName + "\"" +
                   "    }" +
                   "  }" +
                   "}";
        }

        IEnumerator ChangeConfigBranch(string targetId, string branchName)
        {
            var jsonStr = GetChangeConfigBranchPayload(branchName);
            Debug.Log("jsonStr:" + jsonStr);
            var request = new UnityWebRequest(GetConfigUpdateUrl(targetId), "PUT");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonStr));
            request.SendAndWaitCompletion(settings.apiToken, 10.0f);
            if (request.isNetworkError)
            {
                Debug.Log("error: " + request.error);
                yield return "false";
            }
            else
            {
                if (request.responseCode == 200)
                {
                    Debug.Log("success!");
                    yield return "true";
                }
                else
                {
                    Debug.Log("failed. response code:" + request.responseCode);
                    yield return "false";
                }
            }
        }

        IEnumerator LaunchBuild(string targetId)
        {
            var request = new UnityWebRequest(GetBuildCreateUrl(targetId), "POST");
            request.SendAndWaitCompletion(settings.apiToken, 10.0f);
            if (request.isNetworkError)
            {
                Debug.Log("error: " + request.error);
                yield return "false";
            }
            else
            {
                if (request.responseCode == 202) // 202が返ってくるので注意
                {
                    Debug.Log("success!");
                    yield return "true";
                }
                else
                {
                    Debug.Log("failed. response code:" + request.responseCode);
                    yield return "false";
                }
            }
        }
    }

    static class UnityWebRequestExt
    {
        public static void SendAndWaitCompletion(this UnityWebRequest request, string apiToken, float timeout)
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Basic " + apiToken);
            request.SendWebRequest();
            double startTime = EditorApplication.timeSinceStartup;
            while (!request.isDone)
            {
                Thread.Sleep(50);
                double elapsed = EditorApplication.timeSinceStartup - startTime;
                if (elapsed > timeout) break;
            }
        }
    }
}
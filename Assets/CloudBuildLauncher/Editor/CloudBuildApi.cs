using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudBuildLauncher
{
    [Serializable]
    public class BuildTargetModel
    {
        public string name;
        public string buildtargetid;
    }

    [Serializable]
    public class BuildTargetResponseModel
    {
        public List<BuildTargetModel> targets;

        public IEnumerable<string> TargetIds
        {
            get
            {
                if (targets == null)
                {
                    return null;
                }

                return targets.Select(target => target.buildtargetid);
            }
        }
    }

    [Serializable]
    public class GenericArrayResponseModel<T>
    {
        public List<T> items;
    }


    [Serializable]
    public class ProjectModel
    {
        public string name;
        public string projectid;
        public string orgName;
        public string orgid;
        public string guid; // project guid
    }

    public class CloudBuildApi
    {
        private const string cloudBuildApiDomain = "build-api.cloud.unity3d.com";
        private const string cloudBuildApiBaseUrl = "https://" + cloudBuildApiDomain + "/api/v1";
        private CloudBuildSettings settings;

        public CloudBuildApi(CloudBuildSettings settings)
        {
            this.settings = settings;
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

        string GetBuildTargetsUrl()
        {
            return String.Format("{0}/orgs/{1}/projects/{2}/buildtargets",
                cloudBuildApiBaseUrl, settings.orgId, settings.projectId);
        }

        string GetAllProjectsUrl()
        {
            return String.Format("{0}/projects",
                cloudBuildApiBaseUrl, settings.projectId);
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

        public IEnumerator ChangeConfigBranch(string targetId, string branchName)
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

        public IEnumerator LaunchBuild(string targetId)
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

        public IEnumerator ListBuildTargets()
        {
            var request = new UnityWebRequest(GetBuildTargetsUrl(), "GET");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SendAndWaitCompletion(settings.apiToken, 10.0f);
            if (request.isNetworkError)
            {
                Debug.Log("error: " + request.error);
                yield return "error";
            }
            else
            {
                if (request.responseCode == 200)
                {
                    Debug.Log("success!");
                    yield return request.downloadHandler.text;
                }
                else
                {
                    Debug.Log("failed. response code:" + request.responseCode);
                    yield return "error";
                }
            }
        }

        public IEnumerator ListAllProjects()
        {
            var request = new UnityWebRequest(GetAllProjectsUrl(), "GET");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SendAndWaitCompletion(settings.apiToken, 10.0f);
            if (request.isNetworkError)
            {
                Debug.Log("error: " + request.error);
                yield return "error";
            }
            else
            {
                if (request.responseCode == 200)
                {
                    Debug.Log("success!");
                    yield return request.downloadHandler.text;
                }
                else
                {
                    Debug.Log("failed. response code:" + request.responseCode);
                    yield return "error";
                }
            }
        }
    }
}
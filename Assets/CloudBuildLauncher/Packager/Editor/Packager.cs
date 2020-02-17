using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CloudBuildLauncher
{
    /// <summary>
    /// Build .unitypackage for distribution.
    /// include: .cs script files
    /// exclude: .asset file, packager script
    /// </summary>
    public class Packager : MonoBehaviour
    {
        private const string assetPath = "Assets/CloudBuildLauncher/Editor";
        private const string packageName = "CloudBuildLauncher.unitypackage";

        [MenuItem("Window/Cloud Build Launcher/Build .unitypackage")]
        public static void BuildPackage()
        {
            var assetPaths = MakeAssetPathsInDir(new DirectoryInfo(assetPath), assetPath);
            Debug.Log("Package Target Files: " + string.Join("\n", assetPaths.ToArray()));
            AssetDatabase.ExportPackage(assetPaths.ToArray(), packageName, ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
        }

        /// <summary>
        /// get *.cs file paths from the path
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="dirPath"></param>
        private static List<string> MakeAssetPathsInDir(DirectoryInfo dir, string dirPath)
        {
            var assetPaths = new List<string>();
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                // .cs のみ処理する
                var ext = file.Extension.ToLower();
                if (ext == ".cs")
                {
                    assetPaths.Add(dirPath + "/" + file.Name);
                }
            }
            return assetPaths;
        }
    }
}

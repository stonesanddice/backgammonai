using UnityEditor;
using System.IO;
using UnityEngine;

namespace EngineCore.Editor
{
    public static class PackageDataInstaller
    {
        [MenuItem("Backgammon AI/Copy Data to StreamingAssets")]
        public static void CopyData()
        {
            // 1. Find the source (the package folder)
            string sourcePath = Path.GetFullPath("Packages/com.stonesandice.backgammonai/Runtime/Data");
            // 2. Find the destination (StreamingAssets is a special Unity folder)
            string destPath = Path.Combine(Application.streamingAssetsPath, "BackgammonData");

            if (!Directory.Exists(sourcePath)) {
                Debug.LogError("Could not find package data folder!");
                return;
            }

            // Create destination and copy files
            if (!Directory.Exists(destPath)) Directory.CreateDirectory(destPath);
            
            foreach (string file in Directory.GetFiles(sourcePath)) {
                string name = Path.GetFileName(file);
                if (name.EndsWith(".meta")) continue;
                File.Copy(file, Path.Combine(destPath, name), true);
            }

            AssetDatabase.Refresh();
            Debug.Log("Backgammon Data copied to StreamingAssets successfully!");
        }
    }
}
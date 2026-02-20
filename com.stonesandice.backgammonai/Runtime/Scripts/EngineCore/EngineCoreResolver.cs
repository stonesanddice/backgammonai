using System;
using System.IO;

namespace EngineCore
{
    public static class EnginePathResolver
    {
        private const string PackageName = "com.stonesandice.backgammonai";

        public static string GetDataDirectory()
        {
            // 1. Try Unity Editor Path
#if UNITY_EDITOR
            string editorPath = Path.GetFullPath($"Packages/{PackageName}/Runtime/Data");
            if (Directory.Exists(editorPath)) return editorPath;
#endif

            // 2. Try Unity Build Path (StreamingAssets)
#if UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            // We use reflection or strings here to avoid a hard dependency on UnityEngine 
            // if the project isn't being built by Unity.
            string buildPath = Path.Combine(UnityEngine.Application.streamingAssetsPath, "BackgammonData");
            if (Directory.Exists(buildPath)) return buildPath;
#endif

            // 3. Fallback for .NET CLI and Unit Tests
            return FindLocalDataDirectory();
        }

        private static string FindLocalDataDirectory()
        {
            // Start at the execution directory (e.g., bin/Debug/net7.0)
            var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            
            while (currentDir != null)
            {
                // Check for the Data folder in the current directory
                string potential = Path.Combine(currentDir.FullName, "Data");
                if (Directory.Exists(potential)) return potential;

                // Also check inside the UPM structure if we're running tests from the package
                string packageData = Path.Combine(currentDir.FullName, "Runtime", "Data");
                if (Directory.Exists(packageData)) return packageData;

                currentDir = currentDir.Parent;
            }
            throw new DirectoryNotFoundException("Could not find the 'Data' directory in any parent folders.");
        }
    }
}
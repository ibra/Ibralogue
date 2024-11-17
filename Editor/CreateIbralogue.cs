using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ibralogue.Editor
{
    public static class CreateIbralogue
    {
        private const string TemplatePath = "Ibralogue/Editor/Templates/";
        private const string DefaultScript = "DefaultDialogue.ibra.txt";

        /// <summary>
        /// Creates an Ibralogue file using Unity's <see cref="ProjectWindowUtil.CreateScriptAssetFromTemplateFile(string, string)"/> function.
        /// </summary>
        [MenuItem("Assets/Create/Ibralogue", false, 50)]
        public static void CreateDialogue()
        {
            string templateFullPath = GetTemplateFullPath();
            if (string.IsNullOrEmpty(templateFullPath) || !File.Exists(templateFullPath))
            {
                Debug.LogError("Template file not found at: " + templateFullPath);
                return;
            }

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templateFullPath, "New Dialogue.ibra");
        }

        /// <summary>
        /// Resolves the full path to the template, whether added directly to Assets folder or imported as a package.
        /// </summary>
        private static string GetTemplateFullPath()
        {
            // Check in Assets
            string assetsPath = Path.Combine("Assets/", TemplatePath, DefaultScript);
            if (File.Exists(assetsPath))
                return assetsPath;

            // Check in Packages
            string packagePath = GetPackagePath();
            if (!string.IsNullOrEmpty(packagePath))
            {
                string packageTemplatePath = Path.Combine(packagePath, TemplatePath, DefaultScript);
                if (File.Exists(packageTemplatePath))
                    return packageTemplatePath;
            }

            return null;
        }

        /// <summary>
        /// Locates the package's root path when imported via Git URL.
        /// </summary>
        private static string GetPackagePath()
        {
            string packageName = "com.ibra.ibralogue";
            string packageJsonPath = AssetDatabase.FindAssets("package")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => path.Contains(packageName));

            return !string.IsNullOrEmpty(packageJsonPath)
                ? Path.GetDirectoryName(packageJsonPath)
                : null;
        }
    }
}

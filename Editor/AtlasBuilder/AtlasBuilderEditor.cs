using UnityEditor;
using UnityEngine;

namespace UnityEngine.UI.Editor
{
    /// <summary>
    /// Atlas builder editor menu and window
    /// </summary>
    public class AtlasBuilderEditor : EditorWindow
    {
        private int maxSize = 2048;
        private int padding = 2;
        public string selectedFolderPath;

        [MenuItem("Assets/AssetTools/Build Atlas", false, 100)]
        private static void BuildAtlasFromSelection()
        {
            // Get selected folder
            string folderPath = GetSelectedFolderPath();
            if (string.IsNullOrEmpty(folderPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a folder containing textures", "OK");
                return;
            }

            // Show settings window
            AtlasBuilderEditor window = GetWindow<AtlasBuilderEditor>("Atlas Builder");
            window.selectedFolderPath = folderPath;
            window.Show();
        }

        [MenuItem("Assets/AssetTools/Build Atlas", true)]
        private static bool ValidateBuildAtlas()
        {
            // Only show menu when folder is selected
            return !string.IsNullOrEmpty(GetSelectedFolderPath());
        }

        private void OnGUI()
        {
            GUILayout.Label("Atlas Builder Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Show selected folder
            EditorGUILayout.LabelField("Selected Folder:", selectedFolderPath);
            EditorGUILayout.Space();

            // Max size slider
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max Atlas Size:", GUILayout.Width(120));
            maxSize = EditorGUILayout.IntSlider(maxSize, 256, 4096);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox($"Atlas will be limited to {maxSize}x{maxSize} pixels", MessageType.Info);
            EditorGUILayout.Space();

            // Padding slider
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sprite Padding:", GUILayout.Width(120));
            padding = EditorGUILayout.IntSlider(padding, 0, 10);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox($"Each sprite will have {padding} pixel padding on all sides", MessageType.Info);
            EditorGUILayout.Space();

            // Build button
            EditorGUILayout.Space();
            if (GUILayout.Button("Build Atlas", GUILayout.Height(30)))
            {
                BuildAtlas();
            }

            // Help text
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This tool will:\n" +
                "1. Collect all textures in the selected folder\n" +
                "2. Pack them into a single atlas texture\n" +
                "3. Create AtlasData asset with sprite information\n" +
                "4. Save results to Atlas subfolder",
                MessageType.Info
            );
        }

        private void BuildAtlas()
        {
            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                EditorUtility.DisplayDialog("Error", "No folder selected", "OK");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Building Atlas", "Processing textures...", 0.5f);

                // Build atlas
                AtlasBuilder.BuildAtlasFromFolder(selectedFolderPath, maxSize, padding);

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Success", "Atlas built successfully!", "OK");

                // Close window
                Close();
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Failed to build atlas: {e.Message}", "OK");
                Debug.LogException(e);
            }
        }

        private static string GetSelectedFolderPath()
        {
            // Get selected object in project window
            Object selectedObject = Selection.activeObject;
            if (selectedObject == null)
                return null;

            string path = AssetDatabase.GetAssetPath(selectedObject);
            if (string.IsNullOrEmpty(path))
                return null;

            // Check if it's a folder
            if (AssetDatabase.IsValidFolder(path))
            {
                return path;
            }

            return null;
        }

        [MenuItem("Tools/UGUI HUD/Atlas Builder Window")]
        private static void ShowWindow()
        {
            AtlasBuilderEditor window = GetWindow<AtlasBuilderEditor>("Atlas Builder");
            window.Show();
        }
    }

    /// <summary>
    /// Custom inspector for AtlasData
    /// </summary>
    [CustomEditor(typeof(AtlasData))]
    public class AtlasDataInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            AtlasData atlasData = target as AtlasData;

            EditorGUILayout.LabelField("Atlas Information", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Show atlas texture
            EditorGUILayout.ObjectField("Atlas Texture", atlasData.atlasTexture, typeof(Texture2D), false);

            if (atlasData.atlasTexture != null)
            {
                EditorGUILayout.LabelField("Texture Size:", $"{atlasData.atlasTexture.width} x {atlasData.atlasTexture.height}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Sprite Count: {atlasData.sprites.Count}");

            EditorGUILayout.Space();

            // Show sprite list
            if (atlasData.sprites.Count > 0)
            {
                EditorGUILayout.LabelField("Sprites:", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (var sprite in atlasData.sprites)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(sprite.name, GUILayout.Width(200));
                    EditorGUILayout.LabelField($"{sprite.rect.width}x{sprite.rect.height}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"UV: ({sprite.uvRect.x:F3}, {sprite.uvRect.y:F3}, {sprite.uvRect.width:F3}, {sprite.uvRect.height:F3})");
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            // Rebuild button
            if (GUILayout.Button("Rebuild Atlas"))
            {
                string assetPath = AssetDatabase.GetAssetPath(atlasData);
                string folderPath = System.IO.Path.GetDirectoryName(assetPath);
                string parentFolder = System.IO.Path.GetDirectoryName(folderPath);

                if (EditorUtility.DisplayDialog("Rebuild Atlas",
                    $"Rebuild atlas from folder: {parentFolder}?",
                    "Yes", "No"))
                {
                    AtlasBuilderEditor window = EditorWindow.GetWindow<AtlasBuilderEditor>("Atlas Builder");
                    window.selectedFolderPath = parentFolder;
                    window.Show();
                }
            }
        }
    }
}

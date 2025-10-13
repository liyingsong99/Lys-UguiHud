using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityEngine.UI.Editor
{
    /// <summary>
    /// Atlas builder for packing textures into atlas
    /// </summary>
    public static class AtlasBuilder
    {
        /// <summary>
        /// Build atlas from folder
        /// </summary>
        /// <param name="folderPath">Source folder path</param>
        /// <param name="maxSize">Maximum atlas size (default: 2048)</param>
        /// <param name="padding">Padding between sprites (default: 2)</param>
        public static void BuildAtlasFromFolder(string folderPath, int maxSize = 2048, int padding = 2)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("Invalid folder path");
                return;
            }

            // Validate max size
            if (maxSize < 256 || maxSize > 4096)
            {
                Debug.LogError($"Invalid max size: {maxSize}. Must be between 256 and 4096");
                return;
            }

            // Validate padding
            if (padding < 0 || padding > 10)
            {
                Debug.LogError($"Invalid padding: {padding}. Must be between 0 and 10");
                return;
            }

            Debug.Log($"Building atlas from folder: {folderPath}");
            Debug.Log($"Max size: {maxSize}, Padding: {padding}");

            // Find all textures in folder
            List<Texture2D> textures = CollectTexturesFromFolder(folderPath);
            if (textures.Count == 0)
            {
                Debug.LogError("No textures found in folder");
                return;
            }

            Debug.Log($"Found {textures.Count} textures");

            // Make textures readable and get updated references
            textures = MakeTexturesReadable(textures);

            // Pack textures
            PackingResult packResult = TexturePacker.Pack(textures, maxSize, padding);
            if (!packResult.success)
            {
                Debug.LogError("Failed to pack textures. Try increasing max size or reducing texture count");
                return;
            }

            Debug.Log($"Successfully packed textures into {packResult.atlasSize.x}x{packResult.atlasSize.y} atlas");

            // Generate atlas texture
            Texture2D atlasTexture = GenerateAtlasTexture(packResult);
            if (atlasTexture == null)
            {
                Debug.LogError("Failed to generate atlas texture");
                return;
            }

            // Create atlas data
            AtlasData atlasData = CreateAtlasData(packResult, atlasTexture);
            if (atlasData == null)
            {
                Debug.LogError("Failed to create atlas data");
                return;
            }

            // Save atlas assets
            SaveAtlasAssets(folderPath, atlasTexture, atlasData);

            Debug.Log($"Atlas created successfully: {atlasData.sprites.Count} sprites packed");
        }

        private static List<Texture2D> CollectTexturesFromFolder(string folderPath)
        {
            List<Texture2D> textures = new List<Texture2D>();

            // Get all asset paths in folder
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                if (texture != null)
                {
                    textures.Add(texture);
                }
            }

            return textures;
        }

        private static List<Texture2D> MakeTexturesReadable(List<Texture2D> textures)
        {
            List<Texture2D> updatedTextures = new List<Texture2D>();
            List<string> pathsToReload = new List<string>();

            foreach (var texture in textures)
            {
                string assetPath = AssetDatabase.GetAssetPath(texture);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (importer != null)
                {
                    bool needsReimport = false;

                    // Make texture readable
                    if (!importer.isReadable)
                    {
                        importer.isReadable = true;
                        needsReimport = true;
                    }

                    // Disable crunch compression
                    if (importer.crunchedCompression)
                    {
                        importer.crunchedCompression = false;
                        needsReimport = true;
                    }

                    // Apply changes if needed
                    if (needsReimport)
                    {
                        importer.SaveAndReimport();
                        pathsToReload.Add(assetPath);
                    }
                }
            }

            // Reload all textures to get updated references
            foreach (var texture in textures)
            {
                string assetPath = AssetDatabase.GetAssetPath(texture);
                Texture2D updatedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                updatedTextures.Add(updatedTexture);
            }

            return updatedTextures;
        }

        private static Texture2D GenerateAtlasTexture(PackingResult packResult)
        {
            int width = packResult.atlasSize.x;
            int height = packResult.atlasSize.y;

            Texture2D atlasTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] clearColors = new Color[width * height];
            for (int i = 0; i < clearColors.Length; i++)
            {
                clearColors[i] = Color.clear;
            }
            atlasTexture.SetPixels(clearColors);

            // Copy each texture to atlas
            foreach (var packed in packResult.packedRects)
            {
                Color[] pixels = packed.texture.GetPixels();
                atlasTexture.SetPixels(packed.rect.x, packed.rect.y, packed.rect.width, packed.rect.height, pixels);
            }

            atlasTexture.Apply();
            return atlasTexture;
        }

        private static AtlasData CreateAtlasData(PackingResult packResult, Texture2D atlasTexture)
        {
            AtlasData atlasData = ScriptableObject.CreateInstance<AtlasData>();
            atlasData.atlasTexture = atlasTexture;

            float invWidth = 1.0f / packResult.atlasSize.x;
            float invHeight = 1.0f / packResult.atlasSize.y;

            foreach (var packed in packResult.packedRects)
            {
                AtlasSpriteInfo spriteInfo = new AtlasSpriteInfo();
                spriteInfo.name = packed.name;
                spriteInfo.rect = new Rect(packed.rect.x, packed.rect.y, packed.rect.width, packed.rect.height);
                spriteInfo.originalSize = new Vector2(packed.texture.width, packed.texture.height);

                // Calculate UV rect (normalized 0-1)
                spriteInfo.uvRect = new Rect(
                    packed.rect.x * invWidth,
                    packed.rect.y * invHeight,
                    packed.rect.width * invWidth,
                    packed.rect.height * invHeight
                );

                atlasData.sprites.Add(spriteInfo);
            }

            return atlasData;
        }

        private static void SaveAtlasAssets(string folderPath, Texture2D atlasTexture, AtlasData atlasData)
        {
            // Create Atlas subfolder if it doesn't exist
            string atlasFolder = Path.Combine(folderPath, "Atlas");
            if (!AssetDatabase.IsValidFolder(atlasFolder))
            {
                string parentFolder = folderPath;
                AssetDatabase.CreateFolder(parentFolder, "Atlas");
            }

            // Get folder name for atlas naming
            string folderName = Path.GetFileName(folderPath);

            // Save atlas texture
            string texturePath = Path.Combine(atlasFolder, $"{folderName}_Atlas.png");
            byte[] pngData = atlasTexture.EncodeToPNG();
            File.WriteAllBytes(texturePath, pngData);
            AssetDatabase.ImportAsset(texturePath);

            // Create sprite metadata
            List<SpriteMetaData> spriteMetaData = new List<SpriteMetaData>();
            foreach (var spriteInfo in atlasData.sprites)
            {
                SpriteMetaData meta = new SpriteMetaData();
                meta.name = spriteInfo.name;
                meta.rect = spriteInfo.rect;
                meta.alignment = (int)SpriteAlignment.Center;
                meta.pivot = new Vector2(0.5f, 0.5f);
                meta.border = Vector4.zero;
                spriteMetaData.Add(meta);
            }

            // Configure texture import settings for Multiple Sprite mode
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter != null)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Multiple;
                textureImporter.isReadable = false;
                textureImporter.mipmapEnabled = false;
                textureImporter.filterMode = FilterMode.Bilinear;
                textureImporter.wrapMode = TextureWrapMode.Clamp;
                textureImporter.spritePixelsPerUnit = 100;
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                textureImporter.spritesheet = spriteMetaData.ToArray();
                textureImporter.SaveAndReimport();
            }

            // Reload atlas texture reference
            atlasData.atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            // Save atlas data
            string dataPath = Path.Combine(atlasFolder, $"{folderName}_AtlasData.asset");
            AssetDatabase.CreateAsset(atlasData, dataPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Ping the created asset
            EditorGUIUtility.PingObject(atlasData);

            Debug.Log($"Atlas saved to: {atlasFolder}");
            Debug.Log($"Atlas contains {spriteMetaData.Count} sprites that can be used directly in Unity");
        }
    }
}

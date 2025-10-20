using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;

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
        /// <param name="padding">Padding between sprites (default: 4)</param>
        public static void BuildAtlasFromFolder(string folderPath, int maxSize = 2048, int padding = 4)
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

            // Get the Atlas subfolder path to exclude it
            string atlasFolder = Path.Combine(folderPath, "Atlas");
            string normalizedAtlasFolder = atlasFolder.Replace('\\', '/');

            // Get all asset paths in folder
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string normalizedAssetPath = assetPath.Replace('\\', '/');

                // Skip textures in the Atlas subfolder
                if (normalizedAssetPath.StartsWith(normalizedAtlasFolder + "/"))
                {
                    continue;
                }

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

            // Copy each texture to atlas with edge extrusion to prevent bleeding
            foreach (var packed in packResult.packedRects)
            {
                Color[] pixels = packed.texture.GetPixels();
                int texWidth = packed.texture.width;
                int texHeight = packed.texture.height;

                // Copy main texture
                atlasTexture.SetPixels(packed.rect.x, packed.rect.y, texWidth, texHeight, pixels);

                // Extrude edges by 1 pixel to prevent bilinear filtering artifacts
                // Left edge
                if (packed.rect.x > 0)
                {
                    Color[] leftEdge = new Color[texHeight];
                    for (int y = 0; y < texHeight; y++)
                    {
                        leftEdge[y] = pixels[y * texWidth];  // First column
                    }
                    for (int x = packed.rect.x - 1; x >= Mathf.Max(0, packed.rect.x - 2); x--)
                    {
                        atlasTexture.SetPixels(x, packed.rect.y, 1, texHeight, leftEdge);
                    }
                }

                // Right edge
                if (packed.rect.x + texWidth < width)
                {
                    Color[] rightEdge = new Color[texHeight];
                    for (int y = 0; y < texHeight; y++)
                    {
                        rightEdge[y] = pixels[y * texWidth + (texWidth - 1)];  // Last column
                    }
                    for (int x = packed.rect.x + texWidth; x < Mathf.Min(width, packed.rect.x + texWidth + 2); x++)
                    {
                        atlasTexture.SetPixels(x, packed.rect.y, 1, texHeight, rightEdge);
                    }
                }

                // Bottom edge
                if (packed.rect.y > 0)
                {
                    Color[] bottomEdge = new Color[texWidth];
                    for (int x = 0; x < texWidth; x++)
                    {
                        bottomEdge[x] = pixels[x];  // First row
                    }
                    for (int y = packed.rect.y - 1; y >= Mathf.Max(0, packed.rect.y - 2); y--)
                    {
                        atlasTexture.SetPixels(packed.rect.x, y, texWidth, 1, bottomEdge);
                    }
                }

                // Top edge
                if (packed.rect.y + texHeight < height)
                {
                    Color[] topEdge = new Color[texWidth];
                    for (int x = 0; x < texWidth; x++)
                    {
                        topEdge[x] = pixels[(texHeight - 1) * texWidth + x];  // Last row
                    }
                    for (int y = packed.rect.y + texHeight; y < Mathf.Min(height, packed.rect.y + texHeight + 2); y++)
                    {
                        atlasTexture.SetPixels(packed.rect.x, y, texWidth, 1, topEdge);
                    }
                }
            }

            atlasTexture.Apply();
            return atlasTexture;
        }

        private static AtlasData CreateAtlasData(PackingResult packResult, Texture2D atlasTexture)
        {
            AtlasData atlasData = ScriptableObject.CreateInstance<AtlasData>();
            atlasData.atlasTexture = atlasTexture;

            float atlasWidth = packResult.atlasSize.x;
            float atlasHeight = packResult.atlasSize.y;

            foreach (var packed in packResult.packedRects)
            {
                AtlasSpriteInfo spriteInfo = new AtlasSpriteInfo();
                spriteInfo.name = packed.name;
                spriteInfo.rect = new Rect(packed.rect.x, packed.rect.y, packed.rect.width, packed.rect.height);
                spriteInfo.originalSize = new Vector2(packed.texture.width, packed.texture.height);

                // Calculate UV rect with half-pixel offset to avoid sampling edge pixels
                // This prevents white edges caused by bilinear filtering
                float halfPixelX = 0.5f / atlasWidth;
                float halfPixelY = 0.5f / atlasHeight;

                spriteInfo.uvRect = new Rect(
                    (packed.rect.x + halfPixelX) / atlasWidth,
                    (packed.rect.y + halfPixelY) / atlasHeight,
                    (packed.rect.width - 1.0f) / atlasWidth,  // Shrink by 1 pixel to avoid edge bleeding
                    (packed.rect.height - 1.0f) / atlasHeight
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

            // Generate naming based on parent folder name
            string parentFolderName = GetParentFolderName(folderPath);
            string atlasName = string.IsNullOrEmpty(parentFolderName) ? "Atlas" : $"Atlas_{parentFolderName}";
            string atlasDataName = string.IsNullOrEmpty(parentFolderName) ? "AtlasData" : $"AtlasData_{parentFolderName}";

            // Use dynamic naming based on parent folder
            string texturePath = Path.Combine(atlasFolder, $"{atlasName}.png");
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

            // Save atlas data with dynamic naming
            string dataPath = Path.Combine(atlasFolder, $"{atlasDataName}.asset");
            AssetDatabase.CreateAsset(atlasData, dataPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Ping the created asset
            EditorGUIUtility.PingObject(atlasData);

            Debug.Log($"Atlas saved to: {atlasFolder}");
            Debug.Log($"Atlas contains {spriteMetaData.Count} sprites that can be used directly in Unity");
        }

        /// <summary>
        /// 获取当前文件夹名称，用于生成Atlas和AtlasData的命名
        /// </summary>
        /// <param name="folderPath">当前文件夹路径</param>
        /// <returns>当前文件夹名称，如果无法获取则返回空字符串</returns>
        private static string GetParentFolderName(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return string.Empty;
            }

            try
            {
                // 标准化路径分隔符
                string normalizedPath = folderPath.Replace('\\', '/');

                // 移除末尾的斜杠
                if (normalizedPath.EndsWith("/"))
                {
                    normalizedPath = normalizedPath.TrimEnd('/');
                }

                // 直接获取当前文件夹名称
                string currentFolderName = Path.GetFileName(normalizedPath);
                if (string.IsNullOrEmpty(currentFolderName))
                {
                    return string.Empty;
                }

                // 验证文件夹名称是否有效（不包含特殊字符）
                if (currentFolderName.Contains(" ") ||
                    currentFolderName.Contains(".") ||
                    currentFolderName.Length < 2)
                {
                    return string.Empty;
                }

                return currentFolderName;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to extract folder name from path: {folderPath}. Error: {ex.Message}");
                return string.Empty;
            }
        }
    }
}

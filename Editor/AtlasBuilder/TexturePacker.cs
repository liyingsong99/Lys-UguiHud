using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine.UI.Editor
{
    /// <summary>
    /// Texture packing result containing packed rectangles
    /// </summary>
    public class PackingResult
    {
        /// <summary>
        /// Successfully packed rectangles
        /// </summary>
        public List<PackedRect> packedRects = new List<PackedRect>();

        /// <summary>
        /// Final atlas size
        /// </summary>
        public Vector2Int atlasSize;

        /// <summary>
        /// Whether all textures were successfully packed
        /// </summary>
        public bool success;
    }

    /// <summary>
    /// Packed rectangle with source texture reference
    /// </summary>
    public class PackedRect
    {
        /// <summary>
        /// Source texture
        /// </summary>
        public Texture2D texture;

        /// <summary>
        /// Source texture name
        /// </summary>
        public string name;

        /// <summary>
        /// Position in atlas
        /// </summary>
        public RectInt rect;
    }

    /// <summary>
    /// Texture packer using MaxRects algorithm
    /// </summary>
    public class TexturePacker
    {
        private class FreeRect
        {
            public int x;
            public int y;
            public int width;
            public int height;

            public FreeRect(int x, int y, int width, int height)
            {
                this.x = x;
                this.y = y;
                this.width = width;
                this.height = height;
            }
        }

        /// <summary>
        /// Pack textures into atlas using MaxRects algorithm
        /// </summary>
        /// <param name="textures">Source textures to pack</param>
        /// <param name="maxSize">Maximum atlas size</param>
        /// <param name="padding">Padding between sprites</param>
        /// <returns>Packing result</returns>
        public static PackingResult Pack(List<Texture2D> textures, int maxSize, int padding)
        {
            PackingResult result = new PackingResult();

            if (textures == null || textures.Count == 0)
            {
                result.success = false;
                return result;
            }

            // Sort textures by area (largest first) for better packing
            var sortedTextures = textures
                .Select(t => new { texture = t, area = t.width * t.height })
                .OrderByDescending(t => t.area)
                .Select(t => t.texture)
                .ToList();

            // Try different atlas sizes starting from 256
            int[] sizes = { 256, 512, 1024, 2048, 4096 };
            foreach (int size in sizes)
            {
                if (size > maxSize)
                    break;

                var packResult = TryPack(sortedTextures, size, size, padding);
                if (packResult.success)
                {
                    result = packResult;
                    break;
                }
            }

            return result;
        }

        private static PackingResult TryPack(List<Texture2D> textures, int width, int height, int padding)
        {
            PackingResult result = new PackingResult();
            result.atlasSize = new Vector2Int(width, height);

            List<FreeRect> freeRects = new List<FreeRect>();
            freeRects.Add(new FreeRect(0, 0, width, height));

            foreach (var texture in textures)
            {
                int texWidth = texture.width + padding * 2;
                int texHeight = texture.height + padding * 2;

                // Find best free rect for this texture
                FreeRect bestRect = FindBestRect(freeRects, texWidth, texHeight);
                if (bestRect == null)
                {
                    result.success = false;
                    return result;
                }

                // Place texture
                PackedRect packed = new PackedRect();
                packed.texture = texture;
                packed.name = texture.name;
                packed.rect = new RectInt(bestRect.x + padding, bestRect.y + padding, texture.width, texture.height);
                result.packedRects.Add(packed);

                // Split free rects
                SplitFreeRect(freeRects, bestRect, texWidth, texHeight);

                // Remove used rect
                freeRects.Remove(bestRect);

                // Remove overlapping rects
                PruneFreeRects(freeRects);
            }

            result.success = true;
            return result;
        }

        private static FreeRect FindBestRect(List<FreeRect> freeRects, int width, int height)
        {
            FreeRect bestRect = null;
            int bestShortSideFit = int.MaxValue;
            int bestLongSideFit = int.MaxValue;

            foreach (var rect in freeRects)
            {
                // Check if texture fits
                if (rect.width >= width && rect.height >= height)
                {
                    int leftoverHoriz = rect.width - width;
                    int leftoverVert = rect.height - height;
                    int shortSideFit = Mathf.Min(leftoverHoriz, leftoverVert);
                    int longSideFit = Mathf.Max(leftoverHoriz, leftoverVert);

                    if (shortSideFit < bestShortSideFit ||
                        (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                    {
                        bestRect = rect;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }
            }

            return bestRect;
        }

        private static void SplitFreeRect(List<FreeRect> freeRects, FreeRect usedRect, int width, int height)
        {
            // Split into two possible free rectangles
            if (usedRect.width > width)
            {
                freeRects.Add(new FreeRect(
                    usedRect.x + width,
                    usedRect.y,
                    usedRect.width - width,
                    usedRect.height
                ));
            }

            if (usedRect.height > height)
            {
                freeRects.Add(new FreeRect(
                    usedRect.x,
                    usedRect.y + height,
                    usedRect.width,
                    usedRect.height - height
                ));
            }
        }

        private static void PruneFreeRects(List<FreeRect> freeRects)
        {
            // Remove rects that are completely inside another rect
            for (int i = 0; i < freeRects.Count; i++)
            {
                for (int j = i + 1; j < freeRects.Count; )
                {
                    if (IsContainedIn(freeRects[i], freeRects[j]))
                    {
                        freeRects.RemoveAt(i);
                        i--;
                        break;
                    }
                    else if (IsContainedIn(freeRects[j], freeRects[i]))
                    {
                        freeRects.RemoveAt(j);
                    }
                    else
                    {
                        j++;
                    }
                }
            }
        }

        private static bool IsContainedIn(FreeRect a, FreeRect b)
        {
            return a.x >= b.x && a.y >= b.y &&
                   a.x + a.width <= b.x + b.width &&
                   a.y + a.height <= b.y + b.height;
        }
    }
}

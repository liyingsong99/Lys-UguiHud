using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI
{
    /// <summary>
    /// Atlas sprite info for storing sprite UV coordinates and size
    /// </summary>
    [Serializable]
    public class AtlasSpriteInfo
    {
        /// <summary>
        /// Sprite name
        /// </summary>
        public string name;

        /// <summary>
        /// Sprite rectangle in atlas texture (pixel coordinates)
        /// </summary>
        public Rect rect;

        /// <summary>
        /// Original texture size before packing
        /// </summary>
        public Vector2 originalSize;

        /// <summary>
        /// UV rectangle (normalized 0-1)
        /// </summary>
        public Rect uvRect;
    }

    /// <summary>
    /// Atlas data asset for storing atlas texture and sprite information
    /// </summary>
    [CreateAssetMenu(fileName = "AtlasData", menuName = "UGUI HUD/Atlas Data")]
    public class AtlasData : ScriptableObject
    {
        /// <summary>
        /// Atlas texture
        /// </summary>
        public Texture2D atlasTexture;

        /// <summary>
        /// List of sprites in the atlas
        /// </summary>
        public List<AtlasSpriteInfo> sprites = new List<AtlasSpriteInfo>();

        /// <summary>
        /// Get sprite info by name
        /// </summary>
        public AtlasSpriteInfo GetSpriteInfo(string spriteName)
        {
            return sprites.Find(s => s.name == spriteName);
        }

        /// <summary>
        /// Get sprite UV rect by name
        /// </summary>
        public Rect GetSpriteUV(string spriteName)
        {
            var spriteInfo = GetSpriteInfo(spriteName);
            return spriteInfo != null ? spriteInfo.uvRect : Rect.zero;
        }

        /// <summary>
        /// Check if sprite exists in atlas
        /// </summary>
        public bool HasSprite(string spriteName)
        {
            return sprites.Exists(s => s.name == spriteName);
        }

        /// <summary>
        /// Get sprite by name from atlas texture
        /// </summary>
        /// <param name="spriteName">Name of the sprite to get</param>
        /// <returns>Sprite object or null if not found</returns>
        public Sprite GetSprite(string spriteName)
        {
            var spriteInfo = GetSpriteInfo(spriteName);
            if (spriteInfo == null || atlasTexture == null)
            {
                return null;
            }

            return Sprite.Create(
                atlasTexture,
                spriteInfo.rect,
                new Vector2(0.5f, 0.5f), // pivot center
                100.0f // pixels per unit
            );
        }

        /// <summary>
        /// Get sprite by name with custom pivot and pixels per unit
        /// </summary>
        /// <param name="spriteName">Name of the sprite to get</param>
        /// <param name="pivot">Sprite pivot point (0-1)</param>
        /// <param name="pixelsPerUnit">Pixels per unit for the sprite</param>
        /// <returns>Sprite object or null if not found</returns>
        public Sprite GetSprite(string spriteName, Vector2 pivot, float pixelsPerUnit)
        {
            var spriteInfo = GetSpriteInfo(spriteName);
            if (spriteInfo == null || atlasTexture == null)
            {
                return null;
            }

            return Sprite.Create(
                atlasTexture,
                spriteInfo.rect,
                pivot,
                pixelsPerUnit
            );
        }
    }
}

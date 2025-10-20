using System;
using System.Text.RegularExpressions;

namespace UnityEngine.UI
{
    public class RichTextSprite
    {
        private RichText m_richText;
        private string m_name;
        private int m_vertexIndex;
        private Vector2 m_size;
        private Sprite m_sprite;
        private float m_fillAmount = 1.0f;
        private Image.Type m_type = Image.Type.Simple;

        public RichTextSprite(RichText richText)
        {
            if (null == richText)
            {
                throw new ArgumentNullException("RichText is null.");
            }

            m_richText = richText;
        }

        public bool SetValue(Match match)
        {
            if (null == match)
            {
                return false;
            }

            var keyCaptures = match.Groups[1].Captures;
            var valCaptures = match.Groups[2].Captures;

            var count = keyCaptures.Count;
            if (count != valCaptures.Count)
            {
                return false;
            }

            for (int i = 0; i < count; ++i)
            {
                var key = keyCaptures[i].Value;
                var val = valCaptures[i].Value;
                checkSetValue(match, key, val);
            }

            return true;
        }

        public void SetName(string name)
        {
            m_name = name;
        }

        public void SetSprite(string path)
        {
            // Load sprite from RichText's AtlasData
            if (m_richText != null && m_richText.m_AtlasData != null)
            {
                // Use the new GetSprite API for cleaner code
                m_sprite = m_richText.m_AtlasData.GetSprite(path);
                if (m_sprite == null)
                {
                    Debug.LogWarning($"[RichText] Sprite not found in AtlasData: {path}");
                }
            }
            else if (m_richText != null && m_richText.m_AtlasTexture != null)
            {
                // Fallback: Try to load from material's _SpriteTex if AtlasData is not set
                // This is a simplified approach - sprite rect would need to be calculated
                // For now, this path is not fully implemented
                Debug.LogWarning($"[RichText] AtlasData not set. Please assign m_AtlasData to use sprites. Sprite name: {path}");
            }
            else
            {
                Debug.LogWarning($"[RichText] Cannot load sprite: AtlasData and AtlasTexture are both null. Sprite name: {path}");
            }
        }

        public string GetName()
        {
            return m_name;
        }

        public void Reset()
        {
            SetName(null);
            m_type = Image.Type.Simple;
        }

        public Sprite GetSprite()
        {
            return m_sprite;
        }

        public int GetVertexIndex()
        {
            return m_vertexIndex;
        }

        public void SetVertexIndex(int index)
        {
            m_vertexIndex = index;
        }

        public Vector2 GetSize()
        {
            return m_size;
        }

        public Image.Type GetImageType()
        {
            return m_type;
        }

        private void checkSetValue(Match match, string key, string val)
        {
            if (key == "n" || key == "name")
            {
                SetName(val);
                SetSprite(val);  // Load sprite when name is set
            }
            else if (key == "s")
            {
                SetSprite(val);
            }
            else if (key == "w")
            {
                float width;
                float.TryParse(val, out width);
                m_size.x = width;
            }
            else if (key == "h")
            {
                float height;
                float.TryParse(val, out height);
                m_size.y = height;
            }
            else if (key == "size")
            {
                float size;
                float.TryParse(val, out size);
                m_size.x = size;
                m_size.y = size;
            }
            else if (key == "t")
            {
                if (val == "s")
                {
                    m_type = Image.Type.Sliced;
                }
            }
        }

        public void SetFillAmount(float amount)
        {
            amount = Mathf.Clamp01(amount);

            float eps = 0.001f;
            var delta = m_fillAmount - amount;
            if (delta > eps || delta < -eps)
            {
                m_fillAmount = amount;
                m_richText.SetVerticesDirty();
            }
        }

        public float GetFillAmount()
        {
            return m_fillAmount;
        }

        public static MatchCollection GetMatches(string strText)
        {
            return _spriteTagRegex.Matches(strText);
        }

        private static readonly string _spriteTagPattern = @"<quad(?:\s+(\w+)\s*=\s*(?<quota>['""]?)([\w\/]+)\k<quota>)+\s*\/>";
        private static readonly Regex _spriteTagRegex = new Regex(_spriteTagPattern, RegexOptions.Singleline);
    }
}

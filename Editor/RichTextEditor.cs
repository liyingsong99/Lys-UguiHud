using UnityEngine;
using UnityEditor;
using UnityEditor.UI;
using System;

namespace UnityEngine.UI
{
    [CustomEditor(typeof(RichText))]
    [CanEditMultipleObjects]
    public class RichTextEditor : GraphicEditor
    {

        protected override void OnEnable()
        {
            base.OnEnable();

            var serializedObject = this.serializedObject;
            m_Text = serializedObject.FindProperty("m_Text");
            m_FontData = serializedObject.FindProperty("m_FontData");
            m_AtlasTexture = serializedObject.FindProperty("m_AtlasTexture");
            m_AtlasData = serializedObject.FindProperty("m_AtlasData");
            m_UiMode = serializedObject.FindProperty("m_UiMode");

            m_lpfnParseText = System.Delegate.CreateDelegate(typeof(Action), serializedObject.targetObject, "parseText") as Action;

            EditorApplication.update += CheckText;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            EditorApplication.update -= CheckText;
        }

        private void CheckText()
        {
            var currentTextString = m_Text.stringValue;
            if (m_lastTextString != currentTextString)
            {
                m_lastTextString = currentTextString;

                var richText = serializedObject.targetObject as RichText;
                if (richText.IsActive() && null != m_lpfnParseText)
                {
                    m_lpfnParseText();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var richText = serializedObject.targetObject as RichText;

            EditorGUILayout.PropertyField(m_Text);
            EditorGUILayout.PropertyField(m_FontData);

            // Atlas settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Atlas Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AtlasData, new GUIContent("Atlas Data", "ScriptableObject containing sprite atlas information"));
            EditorGUILayout.PropertyField(m_AtlasTexture, new GUIContent("Atlas Texture", "Texture for shader (auto-set from AtlasData if available)"));

            // Auto-sync AtlasTexture from AtlasData
            if (richText.m_AtlasData != null && richText.m_AtlasData.atlasTexture != null)
            {
                if (richText.m_AtlasTexture != richText.m_AtlasData.atlasTexture)
                {
                    richText.m_AtlasTexture = richText.m_AtlasData.atlasTexture;
                    EditorUtility.SetDirty(richText);
                }
            }

            if (richText.m_AtlasTexture)
            {
                var atlasPath = AssetDatabase.GetAssetPath(richText.m_AtlasTexture);
                if (atlasPath.StartsWith("Assets/Resources/") && atlasPath.EndsWith(".png"))
                {
                    atlasPath = atlasPath.Substring("Assets/Resources/".Length, atlasPath.Length - "Assets/Resources/".Length - ".png".Length) + "/";
                    if (atlasPath != richText.AtlasTexturePath)
                    {
                        richText.AtlasTexturePath = atlasPath;
                    }
                }
            }

            EditorGUILayout.Space();

            // UI Mode with auto-selection info
            EditorGUILayout.LabelField("Render Settings", EditorStyles.boldLabel);

            // Show current mode
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(m_UiMode);
            EditorGUI.EndDisabledGroup();

            // Show auto-selection info
            var hasRichTextRender = richText.transform.GetComponentInParent<RichTextRender>() != null;
            var autoMode = hasRichTextRender ? ERichTextMode.ERTM_MergeText : ERichTextMode.ERTM_3DText;
            var modeColor = richText.m_UiMode == autoMode ? Color.green : Color.yellow;

            var prevColor = GUI.color;
            GUI.color = modeColor;
            EditorGUILayout.HelpBox(
                hasRichTextRender
                    ? $"Auto-selected: ERTM_MergeText (RichTextRender detected in parent)\nBatch rendering enabled for optimal performance."
                    : $"Auto-selected: ERTM_3DText (No RichTextRender in parent)\nIndependent 3D text rendering.",
                richText.m_UiMode == autoMode ? MessageType.Info : MessageType.Warning
            );
            GUI.color = prevColor;

            AppearanceControlsGUI();
            RaycastControlsGUI();
            serializedObject.ApplyModifiedProperties();
        }

        private SerializedProperty m_Text;
        private SerializedProperty m_FontData;
        private SerializedProperty m_AtlasTexture;
        private SerializedProperty m_AtlasData;
        private SerializedProperty m_UiMode;

        private string m_lastTextString;
        private Action m_lpfnParseText;

    }


}

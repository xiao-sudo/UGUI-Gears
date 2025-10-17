#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UIExt.Base.UIHollow.Editor
{
    [CustomEditor(typeof(UIHollowMask))]
    public class UIHollowMaskEditor : UnityEditor.Editor
    {
        private SerializedProperty m_InputConfigProp;
        private SerializedProperty m_HollowModeProp;
        private SerializedProperty m_EnableVisualMaskProp;
        private SerializedProperty m_RectMaskMaterialProp;
        private SerializedProperty m_CircleMaskMaterialProp;
        private SerializedProperty m_ShowDebugInfoProp;

        private void OnEnable()
        {
            m_InputConfigProp = serializedObject.FindProperty("m_InputConfig");
            m_HollowModeProp = serializedObject.FindProperty("m_HollowMode");
            m_EnableVisualMaskProp = serializedObject.FindProperty("m_EnableVisualMask");
            m_RectMaskMaterialProp = serializedObject.FindProperty("m_RectMaskMaterial");
            m_CircleMaskMaterialProp = serializedObject.FindProperty("m_CircleMaskMaterial");
            m_ShowDebugInfoProp = serializedObject.FindProperty("m_ShowDebugInfo");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader("Hollow Configuration");

            // 绘制配置属性
            DrawHollowConfig();

            EditorGUILayout.Space(10);

            // 绘制其他设置
            DrawHeader("Hollow Mode");
            EditorGUILayout.PropertyField(m_HollowModeProp);
            DrawHollowModeHelp();

            EditorGUILayout.Space(10);

            DrawHeader("Visual Settings");
            EditorGUILayout.PropertyField(m_EnableVisualMaskProp, new GUIContent("Enable Visual Mask"));

            if (m_EnableVisualMaskProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_RectMaskMaterialProp, new GUIContent("Rect Mask Material"));
                EditorGUILayout.PropertyField(m_CircleMaskMaterialProp, new GUIContent("Circle Mask Material"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            DrawHeader("Debug");
            EditorGUILayout.PropertyField(m_ShowDebugInfoProp, new GUIContent("Show Debug Info"));

            serializedObject.ApplyModifiedProperties();

            // 运行时信息
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                DrawRuntimeInfo();
            }
        }

        private void DrawHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private void DrawHollowConfig()
        {
            var targetProp = m_InputConfigProp.FindPropertyRelative("m_Target");
            var sizeModeProp = m_InputConfigProp.FindPropertyRelative("m_SizeMode");
            var customSizeProp = m_InputConfigProp.FindPropertyRelative("m_CustomSize");
            var customRadiusProp = m_InputConfigProp.FindPropertyRelative("m_CustomRadius");

            EditorGUILayout.PropertyField(targetProp, new GUIContent("Target RectTransform"));

            if (targetProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("请指定一个 RectTransform 作为镂空中心！", MessageType.Warning);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(sizeModeProp, new GUIContent("Size Mode"));

            var sizeMode = (HollowSizeMode)sizeModeProp.enumValueIndex;

            EditorGUI.indentLevel++;
            EditorGUILayout.Space(3);

            switch (sizeMode)
            {
                case HollowSizeMode.UseRectTransformSize:
                    EditorGUILayout.HelpBox(
                        "✓ 使用 RectTransform 的实际尺寸\n" +
                        "镂空区域完全匹配目标 UI 元素的大小",
                        MessageType.Info);
                    break;

                case HollowSizeMode.CustomRectSize:
                    EditorGUILayout.PropertyField(customSizeProp, new GUIContent("Custom Size"));
                    EditorGUILayout.HelpBox(
                        "✓ 以 RectTransform 为中心的自定义矩形\n" +
                        "指定宽高（Canvas 单位）",
                        MessageType.Info);
                    break;

                case HollowSizeMode.CustomCircleRadius:
                    EditorGUILayout.PropertyField(customRadiusProp, new GUIContent("Custom Radius"));
                    EditorGUILayout.HelpBox(
                        "✓ 以 RectTransform 为中心的圆形\n" +
                        "指定半径（Canvas 单位）",
                        MessageType.Info);
                    break;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawHollowModeHelp()
        {
            var mode = (UIHollowMask.HollowMode)m_HollowModeProp.enumValueIndex;
            string helpText = mode == UIHollowMask.HollowMode.HollowInside
                ? "镂空区域内：事件穿透；外部：事件阻挡"
                : "镂空区域外：事件穿透；内部：事件阻挡";

            EditorGUILayout.HelpBox(helpText, MessageType.Info);
        }

        private void DrawRuntimeInfo()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);

            var mask = target as UIHollowMask;
            if (mask != null)
            {
                EditorGUI.BeginDisabledGroup(true);

                var config = mask.InputConfig;
                EditorGUILayout.TextField("Config Type", "RectTransformHollowConfig");
                EditorGUILayout.Toggle("Config Valid", config?.IsValid() ?? false);

                if (config != null)
                {
                    EditorGUILayout.EnumPopup("Size Mode", config.SizeMode);
                }

                EditorGUI.EndDisabledGroup();
            }
        }
    }
}
#endif
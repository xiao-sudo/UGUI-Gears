using UnityEditor;
using UnityEngine;

namespace Editor.ImageAnimationTool
{
    public class ImageAnimationToolWindow : EditorWindow
    {
        [MenuItem("Tools/ImageAnimationTool")]
        private static void ShowWindow()
        {
            var window = GetWindow<ImageAnimationToolWindow>();
            window.titleContent = new GUIContent("Image Animation Tool");
            window.Show();
        }

        private ImageAnimationGenerator m_Generator;
        private Vector2 m_ScrollPosition;
        private float m_FrameRate = 12f;
        private bool m_LoopAnimation = true;
        private string m_AnimationName = "NewImageAnimation";

        private void OnEnable()
        {
            m_Generator = new ImageAnimationGenerator();
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            m_Generator.OnSelectionChanged();
            Repaint();
        }

        private void OnGUI()
        {
            try
            {
                GUILayout.Label("Image Animation Tool", EditorStyles.boldLabel);
                GUILayout.Space(10);

                // Animation settings
                GUILayout.Label("Animation Settings", EditorStyles.boldLabel);
                m_FrameRate = EditorGUILayout.FloatField("Frame Rate", m_FrameRate);
                m_LoopAnimation = EditorGUILayout.Toggle("Loop Animation", m_LoopAnimation);
                m_AnimationName = EditorGUILayout.TextField("Animation Name", m_AnimationName);

                GUILayout.Space(10);

                // Selected sprites display
                GUILayout.Label("Selected Sprites", EditorStyles.boldLabel);

                var selectedSprites = m_Generator.SelectedSprites;
                bool hasSprites = m_Generator.HasSelectedSprites();

                if (!hasSprites)
                {
                    EditorGUILayout.HelpBox("Please select Sprites or Texture2D assets to create animation.",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Found {m_Generator.GetSelectedSpriteCount()} sprites for animation.", MessageType.Info);

                    DrawSpriteList(selectedSprites);

                    GUILayout.Space(10);

                    // Generate animation button
                    if (GUILayout.Button("Generate Animation Clip", GUILayout.Height(30)))
                    {
                        GenerateAnimationClip();
                    }
                }
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"GUI Error: {e.Message}", MessageType.Error);
                Debug.LogError($"ImageAnimationToolWindow GUI Error: {e}");
            }
        }

        private void DrawSpriteList(System.Collections.Generic.List<Sprite> sprites)
        {
            if (sprites == null || sprites.Count == 0)
                return;

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Height(200));

            try
            {
                for (int i = 0; i < sprites.Count; i++)
                {
                    var sprite = sprites[i];
                    if (sprite == null)
                        continue;

                    GUILayout.BeginHorizontal();

                    try
                    {
                        var texture = AssetPreview.GetAssetPreview(sprite);
                        if (texture != null)
                        {
                            GUILayout.Label(texture, GUILayout.Width(32), GUILayout.Height(32));
                        }
                        else
                        {
                            // Placeholder when no texture
                            GUILayout.Label("", GUILayout.Width(32), GUILayout.Height(32));
                        }

                        GUILayout.Label($"{i + 1}. {sprite.name}", GUILayout.ExpandWidth(true));
                    }
                    finally
                    {
                        GUILayout.EndHorizontal();
                    }
                }
            }
            finally
            {
                GUILayout.EndScrollView();
            }
        }

        private void GenerateAnimationClip()
        {
            if (!m_Generator.HasSelectedSprites())
            {
                EditorUtility.DisplayDialog("Error", "No sprites selected!", "OK");
                return;
            }

            if (string.IsNullOrEmpty(m_AnimationName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter an animation name!", "OK");
                return;
            }

            var animationClip = m_Generator.GenerateAnimationFromSelection(
                m_AnimationName,
                m_FrameRate,
                m_LoopAnimation
            );

            if (animationClip != null)
            {
                // select generated clip
                Selection.activeObject = animationClip;
                EditorGUIUtility.PingObject(animationClip);

                EditorUtility.DisplayDialog("Success",
                    $"Animation clip '{m_AnimationName}' created successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error",
                    "Failed to create animation clip. Check console for details.", "OK");
            }
        }
    }
}
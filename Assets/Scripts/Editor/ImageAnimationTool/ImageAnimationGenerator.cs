using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Editor.ImageAnimationTool
{
    public class ImageAnimationGenerator
    {
        private readonly List<Sprite> m_SelectedSprites = new();
        private bool m_SelectionDirty = true;

        public List<Sprite> SelectedSprites
        {
            get
            {
                if (m_SelectionDirty)
                {
                    m_SelectionDirty = false;
                    GatherSelectedSprites(m_SelectedSprites);
                }

                return m_SelectedSprites;
            }
        }

        public void OnSelectionChanged()
        {
            m_SelectionDirty = true;
        }

        /// <summary>
        /// 生成UGUI Image的AnimationClip
        /// </summary>
        /// <param name="sprites">要制作动画的Sprite列表</param>
        /// <param name="animationName">动画名称</param>
        /// <param name="frameRate">帧率</param>
        /// <param name="loopAnimation">是否循环</param>
        /// <returns>生成的AnimationClip，如果失败返回null</returns>
        public static AnimationClip GenerateImageAnimationClip(List<Sprite> sprites, string animationName, float frameRate = 12f, bool loopAnimation = true)
        {
            if (sprites == null || sprites.Count == 0)
            {
                Debug.LogError("ImageAnimationGenerator: No sprites provided for animation generation.");
                return null;
            }

            if (string.IsNullOrEmpty(animationName))
            {
                Debug.LogError("ImageAnimationGenerator: Animation name cannot be null or empty.");
                return null;
            }

            try
            {
                // 创建AnimationClip
                var animationClip = new AnimationClip();
                animationClip.name = animationName;
                animationClip.frameRate = frameRate;
                
                // 设置循环属性（使用新的API）
                var settings = AnimationUtility.GetAnimationClipSettings(animationClip);
                settings.loopTime = loopAnimation;
                AnimationUtility.SetAnimationClipSettings(animationClip, settings);

                // 创建Sprite动画曲线
                var spriteBinding = new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Image),
                    propertyName = "m_Sprite"
                };

                float frameTime = 1f / frameRate;
                var objectReferenceKeyframes = CreateObjectReferenceKeyframes(sprites, frameTime);

                // 设置对象引用曲线
                AnimationUtility.SetObjectReferenceCurve(animationClip, spriteBinding, objectReferenceKeyframes);

                // 保存AnimationClip
                string assetPath = SaveAnimationClip(animationClip, animationName);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    Debug.Log($"ImageAnimationGenerator: Successfully created animation clip '{animationName}' at {assetPath}");
                    return animationClip;
                }
                else
                {
                    Debug.LogError("ImageAnimationGenerator: Failed to save animation clip.");
                    return null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ImageAnimationGenerator: Error generating animation clip: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 使用当前选中的Sprites生成AnimationClip
        /// </summary>
        /// <param name="animationName">动画名称</param>
        /// <param name="frameRate">帧率</param>
        /// <param name="loopAnimation">是否循环</param>
        /// <returns>生成的AnimationClip，如果失败返回null</returns>
        public AnimationClip GenerateAnimationFromSelection(string animationName, float frameRate = 12f, bool loopAnimation = true)
        {
            var selectedSprites = SelectedSprites;
            return GenerateImageAnimationClip(selectedSprites, animationName, frameRate, loopAnimation);
        }

        /// <summary>
        /// 获取选中Sprite的数量
        /// </summary>
        /// <returns>选中Sprite的数量</returns>
        public int GetSelectedSpriteCount()
        {
            return SelectedSprites.Count;
        }

        /// <summary>
        /// 检查是否有选中的Sprites
        /// </summary>
        /// <returns>如果有选中的Sprites返回true，否则返回false</returns>
        public bool HasSelectedSprites()
        {
            return SelectedSprites.Count > 0;
        }

        private void GatherSelectedSprites(List<Sprite> sprites)
        {
            sprites.Clear();

            var selections = Selection.objects;

            foreach (var selection in selections)
            {
                if (selection is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
                else if (selection is Texture2D texture2D)
                {
                    sprites.AddRange(LoadAllSpritesInTexture(texture2D));
                }
            }

            sprites.Sort(NaturalCompare);
        }

        private static List<Sprite> LoadAllSpritesInTexture(Texture2D texture)
        {
            return LoadAllSpritesAtPath(AssetDatabase.GetAssetPath(texture));
        }

        private static List<Sprite> LoadAllSpritesAtPath(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = new List<Sprite>();

            foreach (var asset in assets)
            {
                if (asset is Sprite sprite)
                    sprites.Add(sprite);
            }

            return sprites;
        }

        private static int NaturalCompare(Sprite l, Sprite r)
        {
            return EditorUtility.NaturalCompare(l.name, r.name);
        }

        private static ObjectReferenceKeyframe[] CreateObjectReferenceKeyframes(List<Sprite> sprites, float frameTime)
        {
            var keyframes = new ObjectReferenceKeyframe[sprites.Count];
            
            for (int i = 0; i < sprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * frameTime,
                    value = sprites[i]
                };
            }
            
            return keyframes;
        }

        private static string SaveAnimationClip(AnimationClip animationClip, string animationName)
        {
            // 确保Animations文件夹存在
            string folderPath = "Assets/Animations";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }

            string assetPath = $"{folderPath}/{animationName}.anim";
            
            // 如果文件已存在，添加数字后缀
            int counter = 1;
            string originalPath = assetPath;
            while (File.Exists(assetPath))
            {
                assetPath = $"{folderPath}/{animationName}_{counter}.anim";
                counter++;
            }

            AssetDatabase.CreateAsset(animationClip, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return assetPath;
        }
    }
}
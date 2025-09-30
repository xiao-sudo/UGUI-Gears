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
        /// Generate AnimationClip for UGUI Image
        /// </summary>
        /// <param name="sprites">List of Sprites to animate</param>
        /// <param name="animationName">Animation name</param>
        /// <param name="frameRate">Frame rate</param>
        /// <param name="loopAnimation">Whether to loop the animation</param>
        /// <param name="targetDirectory">Target save directory, use default if null</param>
        /// <returns>Generated AnimationClip, null if failed</returns>
        public static AnimationClip GenerateImageAnimationClip(List<Sprite> sprites, string animationName, float frameRate = 12f, bool loopAnimation = true, string targetDirectory = null)
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
                // Create AnimationClip
                var animationClip = new AnimationClip();
                animationClip.name = animationName;
                animationClip.frameRate = frameRate;
                
                // Set loop property (using new API)
                var settings = AnimationUtility.GetAnimationClipSettings(animationClip);
                settings.loopTime = loopAnimation;
                AnimationUtility.SetAnimationClipSettings(animationClip, settings);

                // Create Sprite animation curve
                var spriteBinding = new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Image),
                    propertyName = "m_Sprite"
                };

                float frameTime = 1f / frameRate;
                var objectReferenceKeyframes = CreateObjectReferenceKeyframes(sprites, frameTime);

                // Set object reference curve
                AnimationUtility.SetObjectReferenceCurve(animationClip, spriteBinding, objectReferenceKeyframes);

                // Save AnimationClip
                string assetPath = SaveAnimationClip(animationClip, animationName, targetDirectory);
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
        /// Generate AnimationClip using currently selected Sprites
        /// </summary>
        /// <param name="animationName">Animation name</param>
        /// <param name="frameRate">Frame rate</param>
        /// <param name="loopAnimation">Whether to loop the animation</param>
        /// <returns>Generated AnimationClip, null if failed</returns>
        public AnimationClip GenerateAnimationFromSelection(string animationName, float frameRate = 12f, bool loopAnimation = true)
        {
            var selectedSprites = SelectedSprites;
            string targetDirectory = GetSelectedSpritesDirectory();
            return GenerateImageAnimationClip(selectedSprites, animationName, frameRate, loopAnimation, targetDirectory);
        }

        /// <summary>
        /// Get the count of selected Sprites
        /// </summary>
        /// <returns>Count of selected Sprites</returns>
        public int GetSelectedSpriteCount()
        {
            return SelectedSprites.Count;
        }

        /// <summary>
        /// Check if there are selected Sprites
        /// </summary>
        /// <returns>True if there are selected Sprites, false otherwise</returns>
        public bool HasSelectedSprites()
        {
            return SelectedSprites.Count > 0;
        }

        /// <summary>
        /// Get the directory path of selected Sprites
        /// </summary>
        /// <returns>Directory path of Sprites, null if cannot be determined</returns>
        public string GetSelectedSpritesDirectory()
        {
            var selectedSprites = SelectedSprites;
            if (selectedSprites.Count == 0)
                return null;

            // Get the path of the first Sprite
            string firstSpritePath = AssetDatabase.GetAssetPath(selectedSprites[0]);
            if (string.IsNullOrEmpty(firstSpritePath))
                return null;

            // Extract directory path
            string directory = System.IO.Path.GetDirectoryName(firstSpritePath);
            
            // Check if all Sprites are in the same directory
            foreach (var sprite in selectedSprites)
            {
                string spritePath = AssetDatabase.GetAssetPath(sprite);
                if (string.IsNullOrEmpty(spritePath))
                    continue;
                    
                string spriteDir = System.IO.Path.GetDirectoryName(spritePath);
                if (directory != spriteDir)
                {
                    // If Sprites are not in the same directory, return Assets root
                    return "Assets";
                }
            }

            return directory;
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

        private static string SaveAnimationClip(AnimationClip animationClip, string animationName, string targetDirectory = null)
        {
            // Determine save directory
            string folderPath = targetDirectory ?? "Assets/Animations";
            
            // Ensure target directory exists
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                // If directory doesn't exist, try to create it
                string[] pathParts = folderPath.Split('/');
                if (pathParts.Length > 1)
                {
                    string parentPath = string.Join("/", pathParts, 0, pathParts.Length - 1);
                    string folderName = pathParts[pathParts.Length - 1];
                    
                    if (AssetDatabase.IsValidFolder(parentPath))
                    {
                        AssetDatabase.CreateFolder(parentPath, folderName);
                    }
                    else
                    {
                        // If parent directory doesn't exist either, fallback to Assets/Animations
                        folderPath = "Assets/Animations";
                        if (!AssetDatabase.IsValidFolder(folderPath))
                        {
                            AssetDatabase.CreateFolder("Assets", "Animations");
                        }
                    }
                }
            }

            string assetPath = $"{folderPath}/{animationName}.anim";
            
            // If file already exists, add numeric suffix
            int counter = 1;
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
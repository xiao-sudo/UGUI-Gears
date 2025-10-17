using UnityEngine;

namespace UIExt.Utility
{
    public static class UIRect
    {
        private const int CORNER_COUNT = 4;

        private const int LEFT_BOTTOM_CORNER_INDEX = 0;
        private const int LEFT_TOP_CORNER_INDEX = 1;
        private const int RIGHT_TOP_CORNER_INDEX = 2;
        private const int RIGHT_BOTTOM_CORNER_INDEX = 3;

        private static readonly Vector3[] CACHE_CORNERS = new Vector3[CORNER_COUNT];


        private static Vector2 GetCanvasRectSize(Canvas rootCanvas)
        {
            var pixelSize = rootCanvas.pixelRect.size;
            return pixelSize;
        }

        private static Vector2 GetCorrectScreenSize()
        {
            if (Display.main != null)
            {
                return new Vector2(Display.main.systemWidth, Display.main.systemHeight);
            }

            var resolution = Screen.currentResolution;
            if (resolution.width > 0 && resolution.height > 0)
            {
                return new Vector2(resolution.width, resolution.height);
            }

            return new Vector2(Screen.width, Screen.height);
        }

        private static Vector2 GetAutoScreenSize(Canvas rootCanvas)
        {
            var uiCamera = rootCanvas.worldCamera;

            switch (rootCanvas.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    return GetCanvasRectSize(rootCanvas);

                case RenderMode.ScreenSpaceCamera:
                case RenderMode.WorldSpace:
                    if (uiCamera != null)
                    {
                        return new Vector2(uiCamera.pixelWidth, uiCamera.pixelHeight);
                    }

                    return GetCorrectScreenSize();

                default:
                    return GetCorrectScreenSize();
            }
        }

        /// <summary>
        /// Get RectTransform's Rect (in pixels) in Screen Space, Origin (0, 0) is the left bottom corner of the screen
        /// </summary>
        /// <param name="target">target transform</param>
        /// <param name="uiCamera">current ui camera</param>
        /// <returns>rect in Screen Space</returns>
        public static Rect GetRectInScreenSpace(RectTransform target, Camera uiCamera = null)
        {
            target.GetWorldCorners(CACHE_CORNERS);

            var lbPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, CACHE_CORNERS[LEFT_BOTTOM_CORNER_INDEX]);
            var rtPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, CACHE_CORNERS[RIGHT_TOP_CORNER_INDEX]);

            var xMin = lbPosition.x;
            var yMin = lbPosition.y;
            var width = rtPosition.x - lbPosition.x;
            var height = rtPosition.y - lbPosition.y;

            return new Rect(xMin, yMin, width, height);
        }

        /// <summary>
        /// Get RectTransform's Normalized Rect in Screen Space ((0, 0) is the left bottom)
        /// </summary>
        /// <param name="target">target transform</param>
        /// <param name="rootCanvas"></param>
        /// <returns>rect in Shader Screen Space</returns>
        public static Rect GetNormalizedRectInScreenSpace(RectTransform target, Canvas rootCanvas)
        {
            var uiCamera = rootCanvas.worldCamera;
            var rectInPx = GetRectInScreenSpace(target, uiCamera);
            var screenSizeInPx = GetAutoScreenSize(rootCanvas);

            var invScreenWidth = 1 / screenSizeInPx.x;
            var invScreenHeight = 1 / screenSizeInPx.y;

            var normalizedMinX = rectInPx.xMin * invScreenWidth;
            var normalizedMinY = rectInPx.yMin * invScreenHeight;
            var normalizedWidth = rectInPx.width * invScreenWidth;
            var normalizedHeight = rectInPx.height * invScreenHeight;

            return new Rect(normalizedMinX, normalizedMinY, normalizedWidth, normalizedHeight);
        }

        /// <summary>
        /// Set Target rect position and size by source target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <param name="offset">(x, y) as center offset and (z, w) as size offset</param>
        /// <returns></returns>
        public static bool SetTargetRectBySource(RectTransform target, RectTransform source, Vector4 offset)
        {
            source.GetWorldCorners(CACHE_CORNERS);

            var lbPosition = CACHE_CORNERS[LEFT_BOTTOM_CORNER_INDEX];
            var rtPosition = CACHE_CORNERS[RIGHT_TOP_CORNER_INDEX];

            var targetParent = target.parent as RectTransform;

            if (null == targetParent)
            {
                Debug.LogError("target RectTransform Must have parent RectTransform");
                return false;
            }

            var localLbPos = targetParent.InverseTransformPoint(lbPosition);
            var localRtPos = targetParent.InverseTransformPoint(rtPosition);

            var width = localRtPos.x - localLbPos.x;
            var height = localRtPos.y - localLbPos.y;
            Vector2 center = (localLbPos + localRtPos) * 0.5f;

            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);

            target.anchoredPosition = center + new Vector2(offset.x, offset.y);
            target.sizeDelta = new Vector2(width + offset.z, height + offset.w);

            return true;
        }

        public static bool AreRectEquals(RectTransform l, RectTransform r, float tolerence = 0.001f)
        {
            if (null == l || null == r)
                return false;

            var lRect = l.rect;
            var rRect = r.rect;

            return Mathf.Abs(lRect.width - rRect.width) < tolerence &&
                   Mathf.Abs(lRect.height - rRect.height) < tolerence &&
                   Mathf.Abs(lRect.x - rRect.x) < tolerence &&
                   Mathf.Abs(lRect.y - rRect.y) < tolerence;
        }
    }
}
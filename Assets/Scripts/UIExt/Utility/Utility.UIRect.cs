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
            var ltPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, CACHE_CORNERS[LEFT_TOP_CORNER_INDEX]);
            var rtPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, CACHE_CORNERS[RIGHT_TOP_CORNER_INDEX]);

            var xMin = lbPosition.x;
            var yMin = lbPosition.y;
            var width = Vector3.Distance(ltPosition, rtPosition);
            var height = Vector3.Distance(ltPosition, lbPosition);

            return new Rect(xMin, yMin, width, height);
        }

        /// <summary>
        /// Get RectTransform's Normalized Rect in Screen Space ((0, 0) is the screen center in shader)
        /// </summary>
        /// <param name="target">target transform</param>
        /// <param name="rootCanvas"></param>
        /// <returns>rect in Shader Screen Space</returns>
        public static Rect GetNormalizedRectInScreenSpaceWithCenterAsOrigin(RectTransform target, Canvas rootCanvas)
        {
            var uiCamera = rootCanvas.worldCamera;
            var rectInPx = GetRectInScreenSpace(target, uiCamera);
            var screenSizeInPx = GetAutoScreenSize(rootCanvas);

            var shaderMinX = rectInPx.xMin - screenSizeInPx.x * 0.5f;
            var shaderMinY = rectInPx.yMin - screenSizeInPx.y * 0.5f;

            var invScreenWidth = 1 / screenSizeInPx.x;
            var invScreenHeight = 1 / screenSizeInPx.y;

            var normalizedMinX = shaderMinX * invScreenWidth;
            var normalizedMinY = shaderMinY * invScreenHeight;
            var normalizedWidth = rectInPx.width * invScreenWidth;
            var normalizedHeight = rectInPx.height * invScreenHeight;

            return new Rect(normalizedMinX, normalizedMinY, normalizedWidth, normalizedHeight);
        }

        public static bool SetTargetRectBySource(RectTransform target, RectTransform source)
        {
            source.GetWorldCorners(CACHE_CORNERS);

            var lbPosition = CACHE_CORNERS[LEFT_BOTTOM_CORNER_INDEX];
            var ltPosition = CACHE_CORNERS[LEFT_TOP_CORNER_INDEX];
            var rtPosition = CACHE_CORNERS[RIGHT_TOP_CORNER_INDEX];

            var targetParent = target.parent as RectTransform;

            if (null == targetParent)
            {
                Debug.LogError("target RectTransform Must have parent RectTransform");
                return false;
            }

            var localLbPos = targetParent.InverseTransformPoint(lbPosition);
            var localLtPos = targetParent.InverseTransformPoint(ltPosition);
            var localRtPos = targetParent.InverseTransformPoint(rtPosition);

            var width = localRtPos.x - localLtPos.x;
            var height = localLtPos.y - localLbPos.y;
            Vector2 center = (localLbPos + localRtPos) * 0.5f;

            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);

            target.anchoredPosition = center;
            target.sizeDelta = new Vector2(width, height);

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
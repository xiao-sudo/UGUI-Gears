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

        public static Rect GetRectInScreenSpace(RectTransform target, Camera uiCamera = null)
        {
            target.GetWorldCorners(CACHE_CORNERS);

            var lbPosition = CACHE_CORNERS[LEFT_BOTTOM_CORNER_INDEX];
            var ltPosition = CACHE_CORNERS[LEFT_TOP_CORNER_INDEX];
            var rtPosition = CACHE_CORNERS[RIGHT_TOP_CORNER_INDEX];

            if (null != uiCamera)
            {
                lbPosition = uiCamera.WorldToScreenPoint(lbPosition);
                ltPosition = uiCamera.WorldToScreenPoint(ltPosition);
                rtPosition = uiCamera.WorldToScreenPoint(rtPosition);
            }

            var xMin = lbPosition.x;
            var yMin = lbPosition.y;
            var width = Vector3.Distance(ltPosition, rtPosition);
            var height = Vector3.Distance(ltPosition, lbPosition);

            return new Rect(xMin, yMin, width, height);
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
using UnityEngine;

namespace UIExt.Base.UIHollow
{
    /// <summary>
    /// 镂空形状判断接口
    /// 用于判断屏幕坐标点是否在指定形状内
    /// </summary>
    public interface IHollowShape
    {
        /// <summary>
        /// 判断屏幕坐标点是否在形状内
        /// </summary>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <param name="eventCamera">UI摄像机</param>
        /// <returns>true: 在形状内, false: 在形状外</returns>
        bool IsPointInside(Vector2 screenPoint, Camera eventCamera);

        /// <summary>
        /// 更新 Shader 参数 (用于视觉镂空效果)
        /// </summary>
        /// <param name="material">材质</param>
        /// <param name="rootCanvas">根画布</param>
        void UpdateShaderProperties(Material material, Canvas rootCanvas);

        /// <summary>
        /// 获取形状的边界矩形 (用于优化判断)
        /// </summary>
        /// <returns>边界矩形</returns>
        Rect GetBounds();
    }
}

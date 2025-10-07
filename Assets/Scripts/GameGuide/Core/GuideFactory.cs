namespace GameGuide.Core
{
    /// <summary>
    /// Guide system factory - provides convenient methods to create guide components
    /// </summary>
    public static class GuideFactory
    {
        #region Create Guide Item

        /// <summary>
        /// Create a guide item
        /// </summary>
        public static GuideItem CreateGuideItem(string itemId = null, string description = "")
        {
            return new GuideItem(itemId, description);
        }

        #endregion

        #region Create Guide Group

        /// <summary>
        /// Create a guide group
        /// </summary>
        public static GuideGroup CreateGuideGroup(string groupId = null, string groupName = "", string description = "")
        {
            return new GuideGroup(groupId, groupName, description);
        }

        #endregion
    }
}
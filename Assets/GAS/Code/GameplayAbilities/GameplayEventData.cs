namespace GAS
{
    public struct GameplayEventData
    {
        /// <summary>
        /// Creates gameplay event data for the supplied event tag.
        /// </summary>
        public GameplayEventData(
            GameplayTag eventTag)
        {
            EventTag = eventTag;
        }

        public GameplayTag EventTag
        {
            get;
            set;
        }
    }
}
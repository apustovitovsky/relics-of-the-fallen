namespace GAS
{
    public readonly struct GameplayEventData
    {
        public GameplayEventData(
            GameplayTag tag,
            string activationGUID)
        {
            Tag = tag;
            ActivationGUID = activationGUID;
        }

        public GameplayTag Tag { get; }

        public string ActivationGUID { get; }
    }
}
namespace RelicsOfTheFallen.Targeting
{
    public interface ITargetProvider
    {
        ITargetable CurrentTarget { get; }
    }
}
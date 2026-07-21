namespace RelicsOfTheFallen.Character
{
    public interface ICharacterInputSource
    {
        CharacterInputState Current { get; }
    }
}
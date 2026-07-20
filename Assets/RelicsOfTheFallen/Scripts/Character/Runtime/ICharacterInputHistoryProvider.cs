using System;

namespace RelicsOfTheFallen.Character
{
    public interface ICharacterInputHistoryProvider
    {
        CharacterInputHistory SentInputHistory { get; }

        event Action<CharacterInputCommand> CommandRecorded;
    }
}
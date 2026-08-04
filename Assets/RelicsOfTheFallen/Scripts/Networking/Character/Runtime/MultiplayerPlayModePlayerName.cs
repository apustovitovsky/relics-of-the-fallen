#if UNITY_EDITOR

using System;

namespace RelicsOfTheFallen.Networking
{
    internal static class MultiplayerPlayModePlayerName
    {
        private const string k_NameArgument = "-name";
        private const string k_PlayerPrefix = "Player";

        /// <summary>
        /// Returns the configured Multiplayer Play Mode player name.
        /// </summary>
        internal static string Get()
        {
            string[] arguments =
                Environment.GetCommandLineArgs();

            for (
                int index = 0;
                index < arguments.Length - 1;
                index++)
            {
                if (!string.Equals(
                        arguments[index],
                        k_NameArgument,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                return Format(
                    arguments[index + 1]);
            }

            return "Player 1";
        }

        private static string Format(string playerName)
        {
            if (!playerName.StartsWith(
                    k_PlayerPrefix,
                    StringComparison.Ordinal))
            {
                return playerName;
            }

            string playerNumber = playerName[
                k_PlayerPrefix.Length..];

            if (!int.TryParse(
                    playerNumber,
                    out int number))
            {
                return playerName;
            }

            return $"{k_PlayerPrefix} {number}";
        }
    }
}

#endif
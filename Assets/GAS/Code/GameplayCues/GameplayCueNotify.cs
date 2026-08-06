using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Defines common tag routing and event handling for Unity gameplay cue notify assets.
    /// </summary>
    public abstract class GameplayCueNotify :
        ScriptableObject
    {
        [field: SerializeField]
        public GameplayTag GameplayCueTag
        {
            get;
            private set;
        }

        [field: SerializeField]
        public bool IsOverride
        {
            get;
            private set;
        } = true;

        /// <summary>
        /// Returns whether this notify handles the supplied gameplay cue event.
        /// </summary>
        public abstract bool HandlesEvent(
            GameplayCueEvent eventType);

        /// <summary>
        /// Handles one gameplay cue event for the supplied target and parameters.
        /// </summary>
        public abstract void HandleGameplayCue(
            GameObject target,
            GameplayCueEvent eventType,
            GameplayCueParameters parameters);
    }
}
namespace GAS.Common
{
    public static class CommonGameplayTags
    {
        private const string k_ActivateFailActivationGroupTagName =
            "Ability.ActivateFail.ActivationGroup";

        private const string k_StateCastingTagName =
            "State.Casting";

        /// <summary>
        /// Returns the failure tag used when an ability activation group is blocked.
        /// </summary>
        public static GameplayTag ActivateFailActivationGroupTag =>
            GameplayTagLibrary.Instance.GetByName(
                k_ActivateFailActivationGroupTagName);

        /// <summary>
        /// Returns the reusable state tag identifying an active ability cast.
        /// </summary>
        public static GameplayTag StateCastingTag =>
            GameplayTagLibrary.Instance.GetByName(
                k_StateCastingTagName);
    }
}
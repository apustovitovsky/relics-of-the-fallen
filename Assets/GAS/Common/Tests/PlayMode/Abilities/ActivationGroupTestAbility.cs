using System;

namespace GAS.Common.Tests
{
    [Serializable]
    internal sealed class ActivationGroupTestAbility :
        CommonGameplayAbility
    {
        /// <summary>
        /// Configures the activation group represented by this test ability.
        /// </summary>
        public void SetActivationGroup(
            GameplayAbilityActivationGroup activationGroup)
        {
            ActivationGroup = activationGroup;
        }
    }
}
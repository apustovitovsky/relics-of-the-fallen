using UnityEngine;

namespace GAS
{
    internal sealed class GameplayEffectContextObjectContainer :
        IGameplayEffectContextObjectProvider
    {
        public Object SourceObject
        {
            get;
        }

        public GameObject Instigator
        {
            get;
        }

        public GameObject EffectCauser
        {
            get;
        }

        /// <summary>
        /// Creates context object references backed by direct Unity object instances.
        /// </summary>
        public GameplayEffectContextObjectContainer(
            GameObject instigator,
            GameObject effectCauser,
            Object sourceObject)
        {
            Instigator = instigator;
            EffectCauser = effectCauser;
            SourceObject = sourceObject;
        }
    }
}
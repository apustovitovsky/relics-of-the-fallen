using UnityEngine;

namespace GAS
{
    public interface IGameplayEffectContextObjectProvider
    {
        Object SourceObject
        {
            get;
        }

        GameObject Instigator
        {
            get;
        }

        GameObject EffectCauser
        {
            get;
        }
    }
}
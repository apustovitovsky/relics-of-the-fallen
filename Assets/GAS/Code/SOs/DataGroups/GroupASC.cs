using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace GAS {
    /// <summary>
    /// This object holds the initialization data for an ASC.
    /// </summary>
    [CreateAssetMenu(menuName = "GAS/DataGroup - AbilitySystemComponent", fileName = "GroupASC_")]
    [Serializable]
    public class GroupASC : PrefixedScriptableObject {
        [Tooltip("Attributes to be added to an ASC. Will be sorted automatically on script reload.")]
        public GroupAttribute attributes;
        public GroupAttributeProcessor attributeProcessors;
        public GroupGA abilities;

        /// <summary>
        /// Recreates the component attributes from its initialization data.
        /// </summary>
        public void AddAttributes(
            AbilitySystemComponent abilitySystem)
        {
            abilitySystem.attributes.Clear();

            if (attributes == null)
            {
                Debug.LogWarning(
                    abilitySystem.name +
                    " has no attributes.");

                return;
            }

            foreach (
                AttributeInitialData initialData
                in attributes.group)
            {
                abilitySystem.attributes.Add(
                    new Attribute(
                        initialData.attributeName,
                        initialData.baseValue));
            }
        }

        public void AddAttributeProcessors(AbilitySystemComponent asc) {
            asc.attributesProcessors.Clear();
            if (attributeProcessors == null) return;
            foreach (var attProcessor in attributeProcessors.group) {
                Type processorType = attProcessor.GetType();
                AttributeProcessor newProcessor = (AttributeProcessor)Activator.CreateInstance(processorType);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(attProcessor), newProcessor);
                asc.attributesProcessors.Add(newProcessor);
            }
        }

        public void OnEnable() {
            if (attributes == null)
            {
                Debug.LogError("NULL attributes in " + name);
            }
        }

        public override void OnValidate() {
            base.OnValidate();
        }

        /// <summary>
        /// Grants every configured ability definition to the target ability system.
        /// </summary>
        public void GrantAbilities(
            AbilitySystemComponent abilitySystem)
        {
            abilitySystem.ClearAllAbilities();

            if (abilities == null)
            {
                return;
            }

            foreach (
                GameplayAbilitySO definitionAsset
                in abilities.group)
            {
                if (definitionAsset == null)
                {
                    Debug.LogError(
                        "Ability group contains a missing definition asset.",
                        abilities);

                    continue;
                }

                abilitySystem.GrantAbility(
                    definitionAsset);
            }
        }
    }

}


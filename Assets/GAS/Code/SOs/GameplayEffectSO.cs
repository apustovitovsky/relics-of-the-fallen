using UnityEngine;
using System;
using EasyButtons;

namespace GAS
{
    [CreateAssetMenu(menuName = "GAS/GameplayEffectSO", fileName = "GE_")]
    [Serializable]
    public class GameplayEffectSO : ScriptableObject
    {
        public GameplayEffect ge;

#if UNITY_EDITOR
        private void OnValidate()
        {
            string assetPath =
                UnityEditor.AssetDatabase.GetAssetPath(
                    this);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            const string prefix =
                "GE_";

            string assetName =
                System.IO.Path.GetFileNameWithoutExtension(
                    assetPath);

            if (
                !assetName.StartsWith(
                    prefix,
                    StringComparison.Ordinal))
            {
                UnityEditor.AssetDatabase.RenameAsset(
                    assetPath,
                    prefix + assetName);

                UnityEditor.AssetDatabase.SaveAssets();
            }

            ge.name =
                assetName.Replace(
                    prefix,
                    string.Empty);

        }
#endif


        [Button("ADD MODIFIER WITH TYPE", Expanded = true, Spacing = ButtonSpacing.Before)]
        public void ADD_MODIFIER_VIA_EDITOR(ModifierType modifierType)
        {// Used to create new mod types on editor. Not recommended to add mods to ges at runtime. But hey, if it works for you, who am I to say no?
            Helpers.AddModifier(modifierType, ge);
        }
    }
}
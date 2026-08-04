
using UnityEngine;
using System;


namespace GAS
{
    // [CreateAssetMenu(menuName = "GAS/GameplayAbility", fileName = "GA_")]
    [Serializable]
    public abstract class GameplayAbilitySO : ScriptableObject
    {
        [SerializeReference] public GameplayAbility ga;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // private void OnEnable() {
            if (!string.IsNullOrEmpty(name))
            {
                string prefix = "GA_";
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(GetEntityId());
                string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (!assetName.Contains(prefix))
                {
                    UnityEditor.AssetDatabase.RenameAsset(assetPath, prefix + assetName);
                    UnityEditor.AssetDatabase.SaveAssets();
                }
                ga.name = assetName.Replace(prefix, "");
            }
        }
#endif
    }
}
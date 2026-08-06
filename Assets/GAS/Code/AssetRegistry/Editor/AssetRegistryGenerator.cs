#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace GAS
{
    internal static class AssetRegistryGenerator
    {
        private const string k_RegistryFilter =
            "t:AssetRegistry";

        private const string k_GameplayAssetFilter =
            "t:GameplayEffectSO t:GameplayAbilitySO " +
            "t:GameplayAbilityMontage t:AttributeName t:GameplayTag";

        /// <summary>
        /// Rebuilds the gameplay asset registry from supported Unity assets.
        /// </summary>
        internal static void Rebuild()
        {
            AssetRegistry registry =
                FindRegistry();

            string[] assetGuids =
                AssetDatabase.FindAssets(
                    k_GameplayAssetFilter);

            Array.Sort(
                assetGuids,
                StringComparer.Ordinal);

            SerializedObject serializedRegistry =
                new(registry);

            SerializedProperty entriesProperty =
                serializedRegistry.FindProperty(
                    "m_Entries");

            entriesProperty.arraySize =
                assetGuids.Length;

            for (
                int index = 0;
                index < assetGuids.Length;
                index++)
            {
                string assetGuid =
                    assetGuids[index];

                string assetPath =
                    AssetDatabase.GUIDToAssetPath(
                        assetGuid);

                ScriptableObject asset =
                    AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                        assetPath);

                if (asset == null)
                {
                    throw new InvalidOperationException(
                        $"Gameplay asset '{assetPath}' could not be loaded.");
                }

                SerializedProperty entryProperty =
                    entriesProperty.GetArrayElementAtIndex(
                        index);

                entryProperty
                    .FindPropertyRelative("m_AssetName")
                    .stringValue =
                        asset.name;

                entryProperty
                    .FindPropertyRelative("m_Id")
                    .stringValue =
                        assetGuid;

                entryProperty
                    .FindPropertyRelative("m_Asset")
                    .objectReferenceValue =
                        asset;
            }

            serializedRegistry.ApplyModifiedProperties();

            EditorUtility.SetDirty(
                registry);

            AssetDatabase.SaveAssets();

            Debug.Log(
                $"Rebuilt asset registry with {assetGuids.Length} gameplay assets.",
                registry);
        }

        private static AssetRegistry FindRegistry()
        {
            string[] registryGuids =
                AssetDatabase.FindAssets(
                    k_RegistryFilter);

            if (registryGuids.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one AssetRegistry asset, but found {registryGuids.Length}.");
            }

            string registryPath =
                AssetDatabase.GUIDToAssetPath(
                    registryGuids[0]);

            AssetRegistry registry =
                AssetDatabase.LoadAssetAtPath<AssetRegistry>(
                    registryPath);

            if (registry == null)
            {
                throw new InvalidOperationException(
                    $"Asset registry '{registryPath}' could not be loaded.");
            }

            return registry;
        }
    }
}

#endif
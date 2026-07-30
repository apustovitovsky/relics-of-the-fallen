using System;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

namespace GAS
{
    [CreateAssetMenu(
        menuName = "GAS/Asset Registry",
        fileName = "AssetRegistry")]
    public sealed class AssetRegistry :
        ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {

            [SerializeField, ReadOnly]
            private string m_AssetName;

            [SerializeField]
            private ScriptableObject m_Asset;

            [SerializeField, HideInInspector]
            private string m_Id;

            public AssetId Id =>
                new(m_Id);

            public ScriptableObject Asset =>
                m_Asset;
        }

        [SerializeField]
        private Entry[] m_Entries =
            Array.Empty<Entry>();

        private Dictionary<
            AssetId,
            ScriptableObject> m_AssetsById;

        private Dictionary<
            ScriptableObject,
            AssetId> m_IdsByAsset;

        public IReadOnlyList<Entry> Entries =>
            m_Entries;

        /// <summary>
        /// Returns the registered asset with the requested identity and type.
        /// </summary>
        public T GetAsset<T>(
            AssetId id)
            where T : ScriptableObject
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "Asset ID must be valid.",
                    nameof(id));
            }

            EnsureLookup();

            if (
                !m_AssetsById.TryGetValue(
                    id,
                    out ScriptableObject asset))
            {
                throw new KeyNotFoundException(
                    $"Asset registry does not contain ID '{id}'.");
            }

            if (asset is not T typedAsset)
            {
                throw new InvalidOperationException(
                    $"Asset '{id}' is '{asset.GetType().FullName}', not '{typeof(T).FullName}'.");
            }

            return typedAsset;
        }

        /// <summary>
        /// Returns the stable identity registered for the requested asset.
        /// </summary>
        public AssetId GetAssetId(
            ScriptableObject asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(
                    nameof(asset));
            }

            EnsureReverseLookup();

            if (
                !m_IdsByAsset.TryGetValue(
                    asset,
                    out AssetId id))
            {
                throw new KeyNotFoundException(
                    $"Asset '{asset.name}' is not registered.");
            }

            return id;
        }

        private void OnValidate()
        {
            m_AssetsById =
                null;

            m_IdsByAsset =
                null;
        }

        private void EnsureLookup()
        {
            if (m_AssetsById != null)
            {
                return;
            }

            BuildLookup();
        }

        private void EnsureReverseLookup()
        {
            if (m_IdsByAsset != null)
            {
                return;
            }

            EnsureLookup();
            BuildReverseLookup();
        }

        /// <summary>
        /// Builds the reverse runtime lookup and rejects duplicate asset references.
        /// </summary>
        private void BuildReverseLookup()
        {
            Dictionary<
                ScriptableObject,
                AssetId> idsByAsset =
                    new(m_AssetsById.Count);

            foreach (
                KeyValuePair<
                    AssetId,
                    ScriptableObject> pair
                in m_AssetsById)
            {
                if (
                    !idsByAsset.TryAdd(
                        pair.Value,
                        pair.Key))
                {
                    throw new InvalidOperationException(
                        $"Asset '{pair.Value.name}' is registered more than once.");
                }
            }

            m_IdsByAsset =
                idsByAsset;
        }

        /// <summary>
        /// Builds the runtime asset lookup and validates every serialized entry.
        /// </summary>
        private void BuildLookup()
        {
            if (m_Entries == null)
            {
                throw new InvalidOperationException(
                    "Asset registry entries are missing.");
            }

            Dictionary<
                AssetId,
                ScriptableObject> assetsById =
                    new(m_Entries.Length);

            for (
                int index = 0;
                index < m_Entries.Length;
                index++)
            {
                Entry entry =
                    m_Entries[index] ?? throw new InvalidOperationException(
                        $"Asset registry entry at index {index} is null.");

                if (entry.Asset == null)
                {
                    throw new InvalidOperationException(
                        $"Asset registry entry at index {index} has no asset.");
                }

                AssetId id;

                try
                {
                    id =
                        entry.Id;
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidOperationException(
                        $"Asset registry entry at index {index} has an invalid ID.",
                        exception);
                }

                if (
                    !assetsById.TryAdd(
                        id,
                        entry.Asset))
                {
                    throw new InvalidOperationException(
                        $"Asset registry contains duplicate ID '{id}'.");
                }
            }

            m_AssetsById =
                assetsById;
        }

#if UNITY_EDITOR

        /// <summary>
        /// Rebuilds this registry from supported gameplay assets.
        /// </summary>
        [Button(
            "REBUILD ASSETS",
            Expanded = true,
            Spacing = ButtonSpacing.Before)]
        private void Rebuild()
        {
            AssetRegistryGenerator.Rebuild();
        }

#endif
    }
}
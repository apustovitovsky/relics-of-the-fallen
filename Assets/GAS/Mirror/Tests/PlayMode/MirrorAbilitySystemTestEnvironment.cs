using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace GAS.Mirror.Tests
{
    internal sealed class MirrorAbilitySystemTestEnvironment :
        IDisposable
    {
        private readonly List<UnityObject> m_Objects = new();

        public MirrorAbilitySystemTestEnvironment()
        {
            ResetNetworking();
            StartHostRuntime();
        }

        /// <summary>
        /// Starts the Mirror host runtime required by synchronized collections.
        /// </summary>
        private void StartHostRuntime()
        {
            GameObject transportObject =
                new("Mirror Test Transport");

            m_Objects.Add(
                transportObject);

            HostTestTransport transport =
                transportObject.AddComponent<HostTestTransport>();

            Transport.active =
                transport;

            NetworkServer.listen =
                false;

            NetworkServer.Listen(
                1);

            NetworkClient.ConnectHost();
        }

        /// <summary>
        /// Creates an inactive network object owned by this test environment.
        /// </summary>
        public GameObject CreateNetworkObject(
            string name)
        {
            GameObject networkObject = new(name);

            networkObject.SetActive(
                false);

            networkObject.AddComponent<NetworkIdentity>();

            m_Objects.Add(
                networkObject);

            return networkObject;
        }

        /// <summary>
        /// Creates a transient attribute identifier owned by this test environment.
        /// </summary>
        public AttributeName CreateAttributeName(
            string name)
        {
            AttributeName attributeName =
                ScriptableObject.CreateInstance<AttributeName>();

            attributeName.name =
                name;

            m_Objects.Add(
                attributeName);

            return attributeName;
        }

        /// <summary>
        /// Creates an isolated registry containing one asset under the supplied identity.
        /// </summary>
        public AssetRegistry CreateAssetRegistry(
            AssetId assetId,
            ScriptableObject asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(
                    nameof(asset));
            }

            AssetRegistry.Entry entry =
                new();

            SetField(
                entry,
                "m_AssetName",
                asset.name);

            SetField(
                entry,
                "m_Asset",
                asset);

            SetField(
                entry,
                "m_Id",
                assetId.ToString());

            AssetRegistry assetRegistry =
                ScriptableObject.CreateInstance<AssetRegistry>();

            SetField(
                assetRegistry,
                "m_Entries",
                new[]
                {
                    entry
                });

            m_Objects.Add(
                assetRegistry);

            return assetRegistry;
        }

        /// <summary>
        /// Creates and starts one isolated server or client ability-system endpoint.
        /// </summary>
        public (
            AbilitySystemComponent AbilitySystem,
            NetworkAbilitySystemObserverComponent Observer)
            CreateAbilitySystemEndpoint(
                string name,
                AssetRegistry assetRegistry,
                AttributeName attributeName,
                float baseValue,
                bool isServer,
                bool isOwned)
        {
            GameObject networkObject =
                CreateNetworkObject(
                    name);

            AbilitySystemComponent abilitySystem =
                networkObject.AddComponent<AbilitySystemComponent>();

            abilitySystem.InitAbilityActorInfo(
                networkObject,
                networkObject);

            Attribute attribute =
                new(
                    attributeName,
                    baseValue);

            abilitySystem.attributes.Add(
                attribute);

            abilitySystem.AttributesDictionary.Add(
                attributeName.name,
                attribute);

            NetworkAbilitySystemObserverComponent observer =
                networkObject.AddComponent<
                    NetworkAbilitySystemObserverComponent>();

            SetField(
                observer,
                "m_AssetRegistry",
                assetRegistry);

            SetField(
                observer,
                "m_AbilitySystem",
                abilitySystem);

            NetworkIdentity identity =
                networkObject.GetComponent<NetworkIdentity>();

            SetProperty(
                identity,
                nameof(NetworkIdentity.isServer),
                isServer);

            SetProperty(
                identity,
                nameof(NetworkIdentity.isClient),
                !isServer);

            SetProperty(
                identity,
                nameof(NetworkIdentity.isOwned),
                isOwned);

            InvokeMethod(
                identity,
                "Awake");

            if (isServer)
            {
                observer.OnStartServer();
            }

            return (
                abilitySystem,
                observer);
        }

        /// <summary>
        /// Invokes a non-public lifecycle method required by the isolated test endpoint.
        /// </summary>
        private static void InvokeMethod(
            object target,
            string methodName)
        {
            MethodInfo method =
                target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            if (method == null)
            {
                throw new InvalidOperationException(
                    $"Method '{methodName}' was not found on '{target.GetType().Name}'.");
            }

            method.Invoke(
                target,
                null);
        }

        /// <summary>
        /// Replicates the initial observer state through the Mirror serialization lifecycle.
        /// </summary>
        public void ReplicateInitialState(
            NetworkAbilitySystemObserverComponent server,
            NetworkAbilitySystemObserverComponent client)
        {
            using NetworkWriterPooled writer =
                NetworkWriterPool.Get();

            server.OnSerialize(
                writer,
                true);

            using NetworkReaderPooled reader =
                NetworkReaderPool.Get(
                    writer.ToArraySegment());

            client.OnDeserialize(
                reader,
                true);

            client.OnStartClient();
            server.ClearAllDirtyBits();
        }

        /// <summary>
        /// Assigns a private serialized dependency while constructing a test endpoint.
        /// </summary>
        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            if (field == null)
            {
                throw new InvalidOperationException(
                    $"Field '{fieldName}' was not found on '{target.GetType().Name}'.");
            }

            field.SetValue(
                target,
                value);
        }

        /// <summary>
        /// Assigns a Mirror identity role that normally comes from the spawn pipeline.
        /// </summary>
        private static void SetProperty(
            object target,
            string propertyName,
            object value)
        {
            PropertyInfo property =
                target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public);

            MethodInfo setter =
                (property?.GetSetMethod(
                    true)) ?? throw new InvalidOperationException(
                    $"Property '{propertyName}' has no setter on '{target.GetType().Name}'.");

            setter.Invoke(
                target,
                new[]
                {
                    value
                });
        }

        /// <summary>
        /// Clears synthetic server roles before destroying unspawned test identities.
        /// </summary>
        private void ClearNetworkIdentityRoles()
        {
            foreach (
                UnityObject trackedObject
                in m_Objects)
            {
                if (trackedObject is not GameObject networkObject)
                {
                    continue;
                }

                NetworkIdentity identity =
                    networkObject.GetComponent<NetworkIdentity>();

                if (identity == null)
                {
                    continue;
                }

                SetProperty(
                    identity,
                    nameof(NetworkIdentity.isServer),
                    false);
            }
        }

        /// <summary>
        /// Shuts down Mirror and destroys every transient test object.
        /// </summary>
        public void Dispose()
        {
            ClearNetworkIdentityRoles();
            ResetNetworking();

            for (
                int index = m_Objects.Count - 1;
                index >= 0;
                index--)
            {
                UnityObject.DestroyImmediate(
                    m_Objects[index]);
            }

            m_Objects.Clear();
        }

        /// <summary>
        /// Resets the static Mirror runtime state between isolated tests.
        /// </summary>
        private static void ResetNetworking()
        {
            NetworkManager.ResetStatics();
            NetworkClient.Shutdown();
            NetworkServer.Shutdown();

            if (Transport.active == null)
            {
                return;
            }

            Transport.active.Shutdown();
            Transport.active = null;
        }
    }
}
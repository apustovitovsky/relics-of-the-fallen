using System;
using NUnit.Framework;

namespace GAS.Mirror.Tests
{
    public sealed class AttributeReplicationScenarioTests
    {
        [Test]
        public void InitialServerAttribute_IsReplicatedToSimulatedProxy()
        {
            using MirrorAbilitySystemTestEnvironment environment =
                new();

            AssetId healthId =
                new(
                    Guid.NewGuid());

            AttributeName serverHealth =
                environment.CreateAttributeName(
                    "Health");

            AttributeName clientHealth =
                environment.CreateAttributeName(
                    "Health");

            AssetRegistry serverRegistry =
                environment.CreateAssetRegistry(
                    healthId,
                    serverHealth);

            AssetRegistry clientRegistry =
                environment.CreateAssetRegistry(
                    healthId,
                    clientHealth);

            (
                AbilitySystemComponent AbilitySystem,
                NetworkAbilitySystemObserverComponent Observer) server =
                    environment.CreateAbilitySystemEndpoint(
                        "Server",
                        serverRegistry,
                        serverHealth,
                        75f,
                        true,
                        false);

            (
                AbilitySystemComponent AbilitySystem,
                NetworkAbilitySystemObserverComponent Observer) client =
                    environment.CreateAbilitySystemEndpoint(
                        "Simulated Proxy",
                        clientRegistry,
                        clientHealth,
                        0f,
                        false,
                        false);

            environment.ReplicateInitialState(
                server.Observer,
                client.Observer);

            Assert.That(
                client.AbilitySystem.GetAttributeValue(
                    clientHealth),
                Is.EqualTo(
                    75f));
        }
    }
}
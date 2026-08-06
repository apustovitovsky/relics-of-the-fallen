using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    public sealed class GameplayAbilityTargetData_LocationInfo :
        GameplayAbilityTargetData
    {
        private static readonly IReadOnlyList<GameObject>
            k_EmptyActors = Array.Empty<GameObject>();

        public GameplayAbilityTargetingLocationInfo SourceLocation
        {
            get;
        }

        public GameplayAbilityTargetingLocationInfo TargetLocation
        {
            get;
        }

        /// <summary>
        /// Creates location target data from source and target world transforms.
        /// </summary>
        public GameplayAbilityTargetData_LocationInfo(
            GameplayAbilityTargetingLocationInfo sourceLocation,
            GameplayAbilityTargetingLocationInfo targetLocation)
        {
            SourceLocation = sourceLocation;
            TargetLocation = targetLocation;
        }

        /// <summary>
        /// Returns the empty actor collection represented by location-only target data.
        /// </summary>
        public override IReadOnlyList<GameObject> GetActors()
        {
            return k_EmptyActors;
        }

        /// <summary>
        /// Returns whether this location payload provides a usable origin.
        /// </summary>
        public override bool HasOrigin()
        {
            return true;
        }

        /// <summary>
        /// Returns the source world transform represented by this payload.
        /// </summary>
        public override Pose GetOrigin()
        {
            return SourceLocation.GetTargetingTransform();
        }

        /// <summary>
        /// Returns whether this location payload provides a usable endpoint.
        /// </summary>
        public override bool HasEndPoint()
        {
            return true;
        }

        /// <summary>
        /// Returns the target world transform represented by this payload.
        /// </summary>
        public override Pose GetEndPointTransform()
        {
            return TargetLocation.GetTargetingTransform();
        }
    }
}
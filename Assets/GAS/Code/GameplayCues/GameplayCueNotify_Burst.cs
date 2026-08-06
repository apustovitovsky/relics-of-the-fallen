using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Spawns one local prefab when an executed gameplay cue is received.
    /// </summary>
    [CreateAssetMenu(
        menuName = "GAS/Gameplay Cue Notify/Burst",
        fileName = "GCN_Burst")]
    public class GameplayCueNotify_Burst :
        GameplayCueNotify_Static
    {
        [field: SerializeField]
        public GameObject BurstPrefab
        {
            get;
            private set;
        }

        /// <summary>
        /// Spawns the configured burst prefab at the cue location and surface orientation.
        /// </summary>
        protected override bool OnExecute(
            GameObject target,
            GameplayCueParameters parameters)
        {
            if (BurstPrefab == null)
            {
                Debug.LogWarning(
                    $"{name} cannot execute without a burst prefab.",
                    this);

                return false;
            }

            Quaternion rotation =
                parameters.Normal.sqrMagnitude > Mathf.Epsilon
                    ? Quaternion.FromToRotation(
                        Vector3.up,
                        parameters.Normal.normalized)
                    : Quaternion.identity;

            Instantiate(
                BurstPrefab,
                parameters.Location,
                rotation);

            return true;
        }
    }
}
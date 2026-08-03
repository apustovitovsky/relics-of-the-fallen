using UnityEngine;

namespace GAS
{
    [CreateAssetMenu(
        menuName = "GAS/AttributeName",
        fileName = "AttributeName")]
    public sealed class AttributeName :
        ScriptableObject
    {
        [field: SerializeField]
        public string Description
        {
            get;
            private set;
        }
    }
}
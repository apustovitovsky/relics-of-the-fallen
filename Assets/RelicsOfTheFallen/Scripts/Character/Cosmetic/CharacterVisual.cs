using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    public enum CharacterColor
    {
        Black,
        Brown,
        White
    }

    public sealed class CharacterVisual : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _rightHand;
        [SerializeField] private Transform _leftHand;

        [Header("Materials")]
        [SerializeField] private Renderer[] _renderers;

        [SerializeField] private Material _blackMaterial;
        [SerializeField] private Material _brownMaterial;
        [SerializeField] private Material _whiteMaterial;

        [SerializeField] private CharacterColor _color;

        public Animator Animator => _animator;
        public Transform Head => _head;
        public Transform RightHand => _rightHand;
        public Transform LeftHand => _leftHand;

        private void Awake()
        {
            ApplyColor();
        }

        [ContextMenu("Apply Color")]
        private void ApplyColor()
        {
            Material material = _color switch
            {
                CharacterColor.Black => _blackMaterial,
                CharacterColor.Brown => _brownMaterial,
                CharacterColor.White => _whiteMaterial,
                _ => _brownMaterial
            };

            foreach (Renderer targetRenderer in _renderers)
            {
                if (targetRenderer != null)
                    targetRenderer.sharedMaterial = material;
            }
        }
    }
}
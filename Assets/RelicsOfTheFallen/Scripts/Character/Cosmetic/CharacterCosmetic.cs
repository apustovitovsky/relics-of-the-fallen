using System;
using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    public enum CharacterCosmeticSlot
    {
        Sword,
        Dagger,
        Axe,
        Shield,
        CoinBag,
        Horn,
        Pouch,
        SwordHolder,
        SwordSheath,
        ShoulderArmorLeft,
        ShoulderArmorRight,
        WaterBladder,
        WaterCanteen
    }

    public sealed class CharacterCosmetic : MonoBehaviour
    {
        [Serializable]
        private sealed class Item
        {
            [SerializeField] private CharacterCosmeticSlot _slot;
            [SerializeField] private GameObject _object;
            [SerializeField] private bool _visible;

            public void Apply(CharacterCosmeticPreset preset)
            {
                bool visible = preset != null
                    ? preset.IsVisible(_slot)
                    : _visible;

                if (_object != null)
                    _object.SetActive(visible);
            }
        }

        [SerializeField] private CharacterCosmeticPreset _preset;
        [SerializeField] private Item[] _cosmetics;

        private void Awake()
        {
            Apply();
        }

        [ContextMenu("Apply Appearance")]
        private void Apply()
        {
            if (_cosmetics == null)
                return;

            foreach (Item item in _cosmetics)
                item?.Apply(_preset);
        }
    }
}
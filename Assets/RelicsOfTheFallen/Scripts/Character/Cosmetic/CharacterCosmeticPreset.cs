using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    [CreateAssetMenu(
        fileName = "CharacterCosmeticPreset",
        menuName = "Character/Character Cosmetic Preset")]
    public sealed class CharacterCosmeticPreset : ScriptableObject
    {
        [field: SerializeField] public bool Sword { get; private set; }
        [field: SerializeField] public bool Dagger { get; private set; }
        [field: SerializeField] public bool Axe { get; private set; }
        [field: SerializeField] public bool Shield { get; private set; }
        [field: SerializeField] public bool CoinBag { get; private set; }
        [field: SerializeField] public bool Horn { get; private set; }
        [field: SerializeField] public bool Pouch { get; private set; }
        [field: SerializeField] public bool SwordHolder { get; private set; }
        [field: SerializeField] public bool SwordSheath { get; private set; }
        [field: SerializeField] public bool ShoulderArmorLeft { get; private set; }
        [field: SerializeField] public bool ShoulderArmorRight { get; private set; }
        [field: SerializeField] public bool WaterBladder { get; private set; }
        [field: SerializeField] public bool WaterCanteen { get; private set; }

        public bool IsVisible(CharacterCosmeticSlot slot)
        {
            return slot switch
            {
                CharacterCosmeticSlot.Sword => Sword,
                CharacterCosmeticSlot.Dagger => Dagger,
                CharacterCosmeticSlot.Axe => Axe,
                CharacterCosmeticSlot.Shield => Shield,
                CharacterCosmeticSlot.CoinBag => CoinBag,
                CharacterCosmeticSlot.Horn => Horn,
                CharacterCosmeticSlot.Pouch => Pouch,
                CharacterCosmeticSlot.SwordHolder => SwordHolder,
                CharacterCosmeticSlot.SwordSheath => SwordSheath,
                CharacterCosmeticSlot.ShoulderArmorLeft => ShoulderArmorLeft,
                CharacterCosmeticSlot.ShoulderArmorRight => ShoulderArmorRight,
                CharacterCosmeticSlot.WaterBladder => WaterBladder,
                CharacterCosmeticSlot.WaterCanteen => WaterCanteen,
                _ => false
            };
        }
    }
}
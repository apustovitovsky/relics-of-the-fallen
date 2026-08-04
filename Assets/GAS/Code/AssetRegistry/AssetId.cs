using System;

namespace GAS
{
    public readonly struct AssetId :
        IEquatable<AssetId>
    {
        private readonly Guid m_Value;

        public bool IsValid =>
            m_Value != Guid.Empty;

        public Guid Value =>
            m_Value;

        /// <summary>
        /// Creates an asset identity from its serialized GUID value.
        /// </summary>
        public AssetId(
            Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException(
                    "Asset ID must be a non-empty GUID.",
                    nameof(value));
            }

            m_Value =
                value;
        }

        /// <summary>
        /// Creates an asset identity from a Unity GUID in compact format.
        /// </summary>
        public AssetId(
            string value)
        {
            if (
                !Guid.TryParseExact(
                    value,
                    "N",
                    out Guid parsedValue) ||
                parsedValue == Guid.Empty)
            {
                throw new ArgumentException(
                    "Asset ID must be a non-empty Unity GUID.",
                    nameof(value));
            }

            m_Value =
                parsedValue;
        }

        public bool Equals(
            AssetId other)
        {
            return m_Value.Equals(
                other.m_Value);
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is AssetId other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Value.GetHashCode();
        }

        public override string ToString()
        {
            return m_Value.ToString(
                "N");
        }

        public static bool operator ==(
            AssetId left,
            AssetId right)
        {
            return left.Equals(
                right);
        }

        public static bool operator !=(
            AssetId left,
            AssetId right)
        {
            return !left.Equals(
                right);
        }
    }
}
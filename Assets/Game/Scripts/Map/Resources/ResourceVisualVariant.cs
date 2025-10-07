using System;
using UnityEngine;

namespace SevenCrowns.Map.Resources
{
    /// <summary>
    /// Runtime representation of a resource node visual variant.
    /// </summary>
    public readonly struct ResourceVisualVariant : IEquatable<ResourceVisualVariant>
    {
        public ResourceVisualVariant(string variantId, Sprite sprite, Vector3 localOffset, bool includeInRandomSelection)
        {
            VariantId = string.IsNullOrWhiteSpace(variantId) ? string.Empty : variantId.Trim();
            Sprite = sprite;
            LocalOffset = localOffset;
            IncludeInRandomSelection = includeInRandomSelection;
        }

        /// <summary>Stable identifier for the variant (e.g. "pile.small").</summary>
        public string VariantId { get; }

        /// <summary>Sprite used to render the resource node.</summary>
        public Sprite Sprite { get; }

        /// <summary>Local offset applied on top of the snapped tile/world position.</summary>
        public Vector3 LocalOffset { get; }

        /// <summary>True if the variant should be considered when picking a random visual.</summary>
        public bool IncludeInRandomSelection { get; }

        /// <summary>True when the variant references a valid sprite.</summary>
        public bool IsValid => Sprite != null;

        public bool Equals(ResourceVisualVariant other)
        {
            return string.Equals(VariantId, other.VariantId, StringComparison.Ordinal)
                   && ReferenceEquals(Sprite, other.Sprite)
                   && LocalOffset.Equals(other.LocalOffset)
                   && IncludeInRandomSelection == other.IncludeInRandomSelection;
        }

        public override bool Equals(object obj) => obj is ResourceVisualVariant other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = VariantId != null ? StringComparer.Ordinal.GetHashCode(VariantId) : 0;
                hash = (hash * 397) ^ (Sprite != null ? Sprite.GetHashCode() : 0);
                hash = (hash * 397) ^ LocalOffset.GetHashCode();
                hash = (hash * 397) ^ IncludeInRandomSelection.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(ResourceVisualVariant left, ResourceVisualVariant right) => left.Equals(right);
        public static bool operator !=(ResourceVisualVariant left, ResourceVisualVariant right) => !left.Equals(right);
    }
}

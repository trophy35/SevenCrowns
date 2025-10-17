using System;
using SevenCrowns.Map.Resources;
using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Identifies the type of tooltip currently exposed by the map.
    /// </summary>
    public enum WorldTooltipKind
    {
        None = 0,
        Resource = 1,
        Farm = 2,
        Mine = 3
    }

    /// <summary>
    /// Resource-specific tooltip content exposed to the UI layer.
    /// </summary>
    public readonly struct ResourceTooltipContent : IEquatable<ResourceTooltipContent>
    {
        public ResourceTooltipContent(ResourceNodeDescriptor descriptor, int amount)
        {
            Descriptor = descriptor;
            Amount = amount;
        }

        /// <summary>Descriptor for the resource node under the pointer.</summary>
        public ResourceNodeDescriptor Descriptor { get; }

        /// <summary>Amount of resource that will be collected.</summary>
        public int Amount { get; }

        /// <summary>Convenience access to the sprite associated with the hovered variant.</summary>
        public Sprite IconSprite => Descriptor.Variant.Sprite;

        public bool Equals(ResourceTooltipContent other)
        {
            return Descriptor.Equals(other.Descriptor) && Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceTooltipContent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Descriptor.GetHashCode();
                hash = (hash * 397) ^ Amount;
                return hash;
            }
        }

        public static bool operator ==(ResourceTooltipContent left, ResourceTooltipContent right) => left.Equals(right);
        public static bool operator !=(ResourceTooltipContent left, ResourceTooltipContent right) => !left.Equals(right);
    }

    /// <summary>
    /// Farm-specific tooltip content exposed to the UI layer.
    /// </summary>
    public readonly struct FarmTooltipContent : IEquatable<FarmTooltipContent>
    {
        public FarmTooltipContent(Farms.FarmNodeDescriptor descriptor, int weeklyPopulation)
        {
            Descriptor = descriptor;
            WeeklyPopulation = weeklyPopulation;
        }

        public Farms.FarmNodeDescriptor Descriptor { get; }
        public int WeeklyPopulation { get; }

        public bool Equals(FarmTooltipContent other)
        {
            return Descriptor.Equals(other.Descriptor) && WeeklyPopulation == other.WeeklyPopulation;
        }

        public override bool Equals(object obj)
        {
            return obj is FarmTooltipContent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Descriptor.GetHashCode();
                hash = (hash * 397) ^ WeeklyPopulation;
                return hash;
            }
        }

        public static bool operator ==(FarmTooltipContent left, FarmTooltipContent right) => left.Equals(right);
        public static bool operator !=(FarmTooltipContent left, FarmTooltipContent right) => !left.Equals(right);
    }

    /// <summary>
    /// Mine-specific tooltip content exposed to the UI layer.
    /// </summary>
    public readonly struct MineTooltipContent : IEquatable<MineTooltipContent>
    {
        public MineTooltipContent(Mines.MineNodeDescriptor descriptor, int dailyYield)
        {
            Descriptor = descriptor;
            DailyYield = dailyYield;
        }

        public Mines.MineNodeDescriptor Descriptor { get; }
        public int DailyYield { get; }

        public bool Equals(MineTooltipContent other)
        {
            return Descriptor.Equals(other.Descriptor) && DailyYield == other.DailyYield;
        }

        public override bool Equals(object obj)
        {
            return obj is MineTooltipContent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Descriptor.GetHashCode();
                hash = (hash * 397) ^ DailyYield;
                return hash;
            }
        }

        public static bool operator ==(MineTooltipContent left, MineTooltipContent right) => left.Equals(right);
        public static bool operator !=(MineTooltipContent left, MineTooltipContent right) => !left.Equals(right);
    }

    /// <summary>
    /// Snapshot of a tooltip request emitted by the map layer.
    /// </summary>
    public readonly struct WorldTooltipHint : IEquatable<WorldTooltipHint>
    {
        public static WorldTooltipHint None => default;

        private WorldTooltipHint(WorldTooltipKind kind, ResourceTooltipContent resource)
        {
            Kind = kind;
            Resource = resource;
            Farm = default;
            Mine = default;
        }

        private WorldTooltipHint(WorldTooltipKind kind, FarmTooltipContent farm)
        {
            Kind = kind;
            Resource = default;
            Farm = farm;
            Mine = default;
        }

        private WorldTooltipHint(WorldTooltipKind kind, MineTooltipContent mine)
        {
            Kind = kind;
            Resource = default;
            Farm = default;
            Mine = mine;
        }

        public WorldTooltipKind Kind { get; }
        public ResourceTooltipContent Resource { get; }
        public FarmTooltipContent Farm { get; }
        public MineTooltipContent Mine { get; }
        public bool HasTooltip => Kind != WorldTooltipKind.None;

        public static WorldTooltipHint ForResource(ResourceTooltipContent content)
        {
            return new WorldTooltipHint(WorldTooltipKind.Resource, content);
        }

        public static WorldTooltipHint ForFarm(FarmTooltipContent content)
        {
            return new WorldTooltipHint(WorldTooltipKind.Farm, content);
        }

        public static WorldTooltipHint ForMine(MineTooltipContent content)
        {
            return new WorldTooltipHint(WorldTooltipKind.Mine, content);
        }

        public bool Equals(WorldTooltipHint other)
        {
            return Kind == other.Kind && Resource.Equals(other.Resource) && Farm.Equals(other.Farm) && Mine.Equals(other.Mine);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldTooltipHint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ Resource.GetHashCode();
                hash = (hash * 397) ^ Farm.GetHashCode();
                hash = (hash * 397) ^ Mine.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(WorldTooltipHint left, WorldTooltipHint right) => left.Equals(right);
        public static bool operator !=(WorldTooltipHint left, WorldTooltipHint right) => !left.Equals(right);
    }
}

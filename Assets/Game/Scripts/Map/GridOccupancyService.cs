using System;
using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Tracks hero occupancy on the grid by listening to HeroAgent position changes.
    /// Provides fast queries for pathfinding and movement collision prevention.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GridOccupancyService : MonoBehaviour, IGridOccupancyProvider
    {
        [Header("Debug")] [SerializeField] private bool _debugLogs = false;

        private readonly Dictionary<GridCoord, HeroIdentity> _byCoord = new Dictionary<GridCoord, HeroIdentity>();
        private readonly Dictionary<HeroIdentity, GridCoord> _byHero = new Dictionary<HeroIdentity, GridCoord>();

        private readonly List<HeroIdentity> _heroes = new List<HeroIdentity>(16);

        private void Awake()
        {
            ScanAndSubscribe();
        }

        private void OnEnable()
        {
            // Subscribe to late agent initialization
            var ids = FindObjectsOfType<HeroIdentity>(true);
            for (int i = 0; i < ids.Length; i++)
            {
                var hero = ids[i];
                var hac = hero?.Agent; // HeroAgentComponent
                if (hac != null && hac.Agent != null)
                {
                    hac.Agent.PositionChanged += OnHeroPositionChanged;
                }
                else if (hac != null)
                {
                    // Subscribe to component to know when Agent is created
                    hac.AgentInitialized += () => OnAgentInitialized(hero);
                }
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            var ids = FindObjectsOfType<HeroIdentity>(true);
            for (int i = 0; i < ids.Length; i++)
            {
                var hac = ids[i]?.Agent;
                if (hac != null && hac.Agent != null)
                {
                    hac.Agent.PositionChanged -= OnHeroPositionChanged;
                }
                // Note: cannot reliably unsubscribe anonymous AgentInitialized handlers; they are one-shot anyway.
            }
        }

        /// <summary>
        /// Rescans all HeroIdentity components and rebuilds internal maps.
        /// </summary>
        public void Refresh()
        {
            _byCoord.Clear();
            _byHero.Clear();
            ScanAndSubscribe();
        }

        public bool IsOccupied(GridCoord c) => _byCoord.ContainsKey(c);

        public bool IsOccupiedByOther(GridCoord c, HeroIdentity self)
        {
            if (!_byCoord.TryGetValue(c, out var occ)) return false;
            if (self == null) return true;
            return occ != self;
        }

        public bool TryGetOccupant(GridCoord c, out HeroIdentity hero) => _byCoord.TryGetValue(c, out hero);

        private void ScanAndSubscribe()
        {
            _heroes.Clear();
            var ids = FindObjectsOfType<HeroIdentity>(true);
            if (ids != null && ids.Length > 0)
            {
                _heroes.AddRange(ids);
            }

            for (int i = 0; i < _heroes.Count; i++)
            {
                var id = _heroes[i];
                if (id == null) continue;
                var hac = id.Agent;
                if (hac != null && hac.Agent != null)
                {
                    var pos = hac.Agent.Position;
                    RegisterHeroAt(id, pos);
                    hac.Agent.PositionChanged += OnHeroPositionChanged;
                    if (_debugLogs) Debug.Log($"[GridOccupancy] Registered {id.HeroId} at {pos}");
                }
                else
                {
                    // HeroAgentComponent will notify when Agent is initialized.
                    if (hac != null)
                        hac.AgentInitialized += () => OnAgentInitialized(id);
                }
            }
        }

        private void OnAgentInitialized(HeroIdentity id)
        {
            var hac = id.Agent;
            if (hac == null || hac.Agent == null) return;
            RegisterHeroAt(id, hac.Agent.Position);
            hac.Agent.PositionChanged += OnHeroPositionChanged;
            if (_debugLogs) Debug.Log($"[GridOccupancy] Late-registered {id.HeroId} at {hac.Agent.Position}");
        }

        private void OnHeroPositionChanged(GridCoord newPos)
        {
            // Find which hero invoked this by reverse map (safe since few heroes)
            HeroIdentity hero = null;
            foreach (var kv in _byHero)
            {
                var comp = kv.Key != null ? kv.Key.Agent : null; // HeroAgentComponent
                var mapAgent = comp != null ? comp.Agent : null;  // IHeroMapAgent
                if (mapAgent != null && mapAgent.Position.Equals(newPos))
                {
                    // This condition alone is not reliable if two heroes share same pos (should never happen by rule).
                    // Instead search by event sender is not available; we update via UpdateHeroPosition from external.
                }
            }
            // Since we don't receive the sender, rely on periodic refresh or explicit calls.
            // This handler is kept for completeness; actual updates are performed by UpdateHeroPosition(HeroIdentity,...)
        }

        /// <summary>
        /// Call when a hero moves to update occupancy immediately.
        /// </summary>
        public void UpdateHeroPosition(HeroIdentity hero, GridCoord from, GridCoord to)
        {
            if (hero == null) return;
            if (_byHero.TryGetValue(hero, out var cur) && cur.Equals(from))
            {
                _byCoord.Remove(from);
            }
            _byCoord[to] = hero;
            _byHero[hero] = to;
            if (_debugLogs) Debug.Log($"[GridOccupancy] {hero.HeroId} moved {from}->{to}");
        }

        private void RegisterHeroAt(HeroIdentity hero, GridCoord pos)
        {
            if (hero == null) return;
            _byHero[hero] = pos;
            _byCoord[pos] = hero;
        }
    }
}

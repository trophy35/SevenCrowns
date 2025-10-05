using System;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.Map;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Tracks selected hero agent and mirrors the selection to CurrentHeroService
    /// so UI (HeroPortraitView) updates automatically.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SelectedHeroService : MonoBehaviour, SevenCrowns.Map.ISelectedHeroAgentProvider
    {
        [Header("Wiring")]
        [SerializeField] private CurrentHeroService _portraitService; // required
        [Header("Debug")]
        [SerializeField] private bool _debugLogs = false;
        // Heroes are always discovered from the scene to avoid duplicating configuration.
        private List<HeroIdentity> _heroes = new();

        private readonly Dictionary<string, HeroAgentComponent> _byId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HeroIdentity> _identityById = new(StringComparer.Ordinal);
        private HeroAgentComponent _current;
        private HeroIdentity _currentIdentity;

        public HeroAgentComponent CurrentHero => _current;
        public event Action<HeroAgentComponent> SelectedHeroChanged;

        private void Awake()
        {
            RefreshHeroes();

            // Reflect any default hero selection from portrait service.
            if (_portraitService != null && !string.IsNullOrEmpty(_portraitService.CurrentHeroId))
            {
                if (_debugLogs) Debug.Log($"[SelectedHeroService] Using default id from portrait service: {_portraitService.CurrentHeroId}");
                SelectById(_portraitService.CurrentHeroId);
            }
            else if (_debugLogs)
            {
                Debug.Log("[SelectedHeroService] No default id available on portrait service at Awake.");
            }
        }

        private void Start()
        {
            // Run another pass once all other Awake methods have executed.
            RefreshHeroes();
            if (_portraitService != null && !string.IsNullOrEmpty(_portraitService.CurrentHeroId))
            {
                if (_debugLogs) Debug.Log($"[SelectedHeroService] Start refresh: selecting id={_portraitService.CurrentHeroId}");
                // Avoid loops: this will set portrait id again (idempotent) then set selection if agent exists.
                SelectById(_portraitService.CurrentHeroId);
            }
        }

        private void OnEnable()
        {
            if (_portraitService != null)
            {
                _portraitService.CurrentHeroChanged += OnPortraitServiceHeroChanged;
            }
        }

        private void OnDisable()
        {
            if (_portraitService != null)
            {
                _portraitService.CurrentHeroChanged -= OnPortraitServiceHeroChanged;
            }

            SetCurrentIdentity(null);
        }

        /// <summary>
        /// Rescans the scene for HeroIdentity components and rebuilds the id->agent map.
        /// Call this if heroes are spawned or destroyed at runtime.
        /// </summary>
        public void RefreshHeroes()
        {
            var found = FindObjectsOfType<HeroIdentity>(true);
            string previousHeroId = _currentIdentity != null ? _currentIdentity.HeroId : null;
            SetCurrentIdentity(null);

            _heroes = (found != null && found.Length > 0)
                ? new List<HeroIdentity>(found)
                : new List<HeroIdentity>();

            _byId.Clear();
            _identityById.Clear();
            for (int i = 0; i < _heroes.Count; i++)
            {
                var h = _heroes[i];
                if (h == null || string.IsNullOrWhiteSpace(h.HeroId)) continue;

                if (!_identityById.ContainsKey(h.HeroId))
                {
                    _identityById.Add(h.HeroId, h);
                }

                if (!_byId.ContainsKey(h.HeroId) && h.Agent != null)
                {
                    _byId.Add(h.HeroId, h.Agent);
                }

                _portraitService?.SetHeroLevelById(h.HeroId, h.Level);
            }

            if (!string.IsNullOrEmpty(previousHeroId) && _identityById.TryGetValue(previousHeroId, out var refreshedIdentity))
            {
                SetCurrentIdentity(refreshedIdentity);
            }

            if (_debugLogs)
            {
                Debug.Log($"[SelectedHeroService] Refreshed heroes. Count={_heroes.Count} mapCount={_byId.Count}");
            }
        }

        public void SelectById(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId)) return;

            _portraitService?.SetCurrentHeroById(heroId);

            if (!_byId.TryGetValue(heroId, out var agent))
            {
                // Lazy refresh in case heroes were not available previously
                if (_debugLogs) Debug.Log($"[SelectedHeroService] Id not found in map. Refreshing heroes for id={heroId}");
                RefreshHeroes();
                _byId.TryGetValue(heroId, out agent);
            }

            if (_identityById.TryGetValue(heroId, out var identity))
            {
                SetCurrentIdentity(identity);
                _portraitService?.SetCurrentHeroLevel(identity.Level);
            }
            else
            {
                SetCurrentIdentity(null);
            }

            if (agent != null && agent != _current)
            {
                _current = agent;
                SelectedHeroChanged?.Invoke(_current);
                if (_debugLogs)
                {
                    Debug.Log($"[SelectedHeroService] Selected hero id={heroId}, agentGO={_current.gameObject.name}");
                }
            }
            else if (_debugLogs)
            {
                Debug.Log($"[SelectedHeroService] SelectById could not resolve id={heroId} to agent. MapHas={_byId.ContainsKey(heroId)}");
            }
        }

        // Keep selection in sync when CurrentHeroService changes (e.g., default selection set later or UI changes).
        private void OnPortraitServiceHeroChanged(string heroId, string _)
        {
            if (string.IsNullOrWhiteSpace(heroId)) return;

            if (_identityById.TryGetValue(heroId, out var identity))
            {
                SetCurrentIdentity(identity);
                _portraitService?.SetCurrentHeroLevel(identity.Level);
            }
            else
            {
                SetCurrentIdentity(null);
            }

            if (_byId.TryGetValue(heroId, out var agent) && agent != _current)
            {
                _current = agent;
                SelectedHeroChanged?.Invoke(_current);
                if (_debugLogs)
                {
                    Debug.Log($"[SelectedHeroService] PortraitService changed current hero to id={heroId}, agentGO={_current.gameObject.name}");
                }
            }
            else if (_debugLogs)
            {
                Debug.Log($"[SelectedHeroService] PortraitService change ignored or unresolved for id={heroId} (same as current or not found).");
            }
        }

        private void SetCurrentIdentity(HeroIdentity identity)
        {
            if (_currentIdentity == identity) return;

            if (_currentIdentity != null)
            {
                _currentIdentity.LevelChanged -= OnCurrentHeroLevelChanged;
            }

            _currentIdentity = identity;

            if (_currentIdentity != null)
            {
                _portraitService?.SetHeroLevelById(_currentIdentity.HeroId, _currentIdentity.Level);
                _currentIdentity.LevelChanged += OnCurrentHeroLevelChanged;
            }
        }

        private void OnCurrentHeroLevelChanged(int level)
        {
            _portraitService?.SetCurrentHeroLevel(level);
        }
    }
}


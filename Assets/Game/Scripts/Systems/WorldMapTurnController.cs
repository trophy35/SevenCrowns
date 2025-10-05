using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SevenCrowns.Map;
using SevenCrowns.Map.FogOfWar;
using SevenCrowns.UI;
using SevenCrowns.UI.Popups;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Simple world-map turn controller that reacts to the End Turn UI event.
    /// Resets movement points for all known heroes and clears any pending paths.
    /// Attach this to a scene object and wire WorldMapRadialMenuController.OnEndTurnRequested to OnEndTurnRequested().
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapTurnController : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("Explicit heroes to affect. If empty, the scene will be scanned on Awake.")]
        [SerializeField] private List<HeroAgentComponent> _heroes = new();

        [Header("Fog of War")]
        [SerializeField] private MonoBehaviour _fogServiceBehaviour; // Optional; must implement IFogOfWarService

        [Header("Events")]
        [Tooltip("Raised after end turn processing completes.")]
        [SerializeField] private UnityEvent _onTurnEnded;

        [Header("UI")]
        [SerializeField] private WorldMapRadialMenuController _radialMenu;

        [Header("Time")]
        [SerializeField] private MonoBehaviour _timeServiceBehaviour; // Optional; must implement IWorldTimeService

        [Header("End Turn Confirmation")]
        [SerializeField, Tooltip("If true, show a confirmation popup when heroes still have movement points.")]
        private bool _requireConfirmationWhenMovementLeft = true;
        [SerializeField, Tooltip("Optional explicit popup service reference; auto-discovery is used when null.")]
        private MonoBehaviour _popupServiceBehaviour;
        [SerializeField, Tooltip("Optional CurrentHeroService reference used to restrict checks to player heroes.")]
        private MonoBehaviour _currentHeroServiceBehaviour;
        [SerializeField, Tooltip("Localization table for confirmation popup entries.")]
        private string _confirmationStringTable = "UI.Common";
        [SerializeField, Tooltip("Localized title entry for confirmation popup.")]
        private string _confirmationTitleEntry = "Popups.EndTurn.Title";
        [SerializeField, Tooltip("Localized body entry for confirmation popup (supports {0} for hero count).")]
        private string _confirmationBodyEntry = "Popups.EndTurn.Body";
        [SerializeField, Tooltip("Localized entry for the confirm button label.")]
        private string _confirmationConfirmEntry = "Popup.Confirm";
        [SerializeField, Tooltip("Localized entry for the cancel button label.")]
        private string _confirmationCancelEntry = "Popup.Cancel";

        [Header("Debug")]
        [SerializeField, Tooltip("If true, logs hero MP breakdown before showing confirmation.")]
        private bool _debugConfirmationLogs = false;


        private IFogOfWarService _fog;
        private IWorldTimeService _timeService;
        private IPopupService _popupService;
        private CurrentHeroService _currentHeroService;
        private HashSet<string> _allowedHeroIds;
        private readonly object[] _endTurnPopupArgs = new object[1];

        private void Awake()
        {
            if (_heroes == null || _heroes.Count == 0)
            {
                var found = FindObjectsOfType<HeroAgentComponent>(true);
                if (found != null && found.Length > 0)
                    _heroes = new List<HeroAgentComponent>(found);
            }

            if (_fogServiceBehaviour != null)
            {
                if (_fogServiceBehaviour is IFogOfWarService fogService)
                {
                    _fog = fogService;
                }
                else
                {
                    Debug.LogError("Assigned fog service must implement IFogOfWarService.", this);
                }
            }
            else
            {
                _fog = FindObjectOfType<FogOfWarService>(true);
            }

            ResolveTimeService();
            ResolvePopupService();
            ResolveCurrentHeroService();
            RefreshAllowedHeroIds();
        }

        private void Start()
        {
            RefreshAllowedHeroIds();
        }

        /// <summary>
        /// Invoked by UI when the user clicks End Turn.
        /// Resets MP to max and clears queued paths for each hero.
        /// </summary>
        public void OnEndTurnRequested()
        {
            if (_heroes == null)
                return;

            if (_requireConfirmationWhenMovementLeft)
            {
                int pending = CountHeroesWithMovement();
                if (pending > 0 && TryShowConfirmation(pending))
                {
                    return;
                }
            }

            ExecuteEndTurn();
        }

        private void ExecuteEndTurn()
        {
            if (_heroes == null)
                return;

            Debug.Log($"OnEndTurnRequested {_heroes.Count}");

            _fog?.ClearTransientVisibility();

            for (int i = 0; i < _heroes.Count; i++)
            {
                var hero = _heroes[i];
                if (hero == null)
                    continue;

                var movement = hero.Movement;
                movement?.ResetDaily();

                var agent = hero.Agent;
                agent?.ClearPath();
            }

            _timeService?.AdvanceDay();

            if (_onTurnEnded != null)
            {
                _onTurnEnded.Invoke();
            }
            else
            {
                Debug.Log("_onTurnEnded is null");
            }
            PlayTurnEndSfx();
        }

        private int CountHeroesWithMovement()
        {
            if (_heroes == null)
                return 0;

            int count = 0;
            bool debug = _debugConfirmationLogs;

            if (debug)
            {
                Debug.Log("[WorldMapTurnController] Checking hero movement pools before confirmation.", this);
            }

            for (int i = 0; i < _heroes.Count; i++)
            {
                var hero = _heroes[i];
                if (hero == null || !hero.isActiveAndEnabled)
                {
                    continue;
                }

                HeroIdentity identity = null;
                if (_allowedHeroIds != null && _allowedHeroIds.Count > 0)
                {
                    if (!hero.TryGetComponent(out identity) || string.IsNullOrWhiteSpace(identity.HeroId) || !_allowedHeroIds.Contains(identity.HeroId))
                    {
                        continue;
                    }
                }
                else
                {
                    hero.TryGetComponent(out identity);
                }

                var movement = hero.Movement;
                if (movement == null)
                {
                    continue;
                }

                int currentMp = movement.Current;
                if (currentMp <= 0)
                {
                    if (debug)
                    {
                        string heroId = identity != null ? identity.HeroId : hero.name;
                        Debug.Log($"[WorldMapTurnController] Hero '{heroId}' MP={currentMp}/{movement.Max} -> spent", this);
                    }
                    continue;
                }

                bool hasCheapestStep = hero.TryGetCheapestStepCost(out int cheapestCost);
                bool canAffordStep = hasCheapestStep && cheapestCost > 0 && currentMp >= cheapestCost;

                if (debug)
                {
                    string heroId = identity != null ? identity.HeroId : hero.name;
                    if (!hasCheapestStep)
                    {
                        Debug.Log($"[WorldMapTurnController] Hero '{heroId}' MP={currentMp}/{movement.Max} -> no legal steps.", this);
                    }
                    else
                    {
                        Debug.Log($"[WorldMapTurnController] Hero '{heroId}' MP={currentMp}/{movement.Max}, cheapestStep={cheapestCost} -> {(canAffordStep ? "pending" : "spent")}", this);
                    }
                }

                if (canAffordStep)
                {
                    count++;
                }
            }

            if (debug)
            {
                Debug.Log($"[WorldMapTurnController] Heroes with movement remaining: {count}.", this);
            }

            return count;
        }
        private bool TryShowConfirmation(int pendingHeroes)
        {
            if (_popupService == null)
            {
                Debug.LogWarning("[WorldMapTurnController] Popup service missing, skipping confirmation.", this);
                return false;
            }

            _endTurnPopupArgs[0] = pendingHeroes;

            PopupRequest request;
            try
            {
                request = PopupRequest.CreateConfirmation(
                    _confirmationStringTable,
                    _confirmationTitleEntry,
                    _confirmationBodyEntry,
                    _confirmationConfirmEntry,
                    _confirmationCancelEntry,
                    _endTurnPopupArgs);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WorldMapTurnController] Failed to create confirmation popup: {ex.Message}", this);
                return false;
            }

            _popupService.RequestPopup(request, OnConfirmationFinished);
            return true;
        }

        private void OnConfirmationFinished(PopupResult result)
        {
            if (result.Is(PopupOptionIds.Confirm))
            {
                ExecuteEndTurn();
            }
        }

        private void ResolveCurrentHeroService()
        {
            if (_currentHeroService != null)
            {
                return;
            }

            if (_currentHeroServiceBehaviour != null)
            {
                if (_currentHeroServiceBehaviour is CurrentHeroService service)
                {
                    _currentHeroService = service;
                }
                else
                {
                    Debug.LogError("Assigned current hero service must be a CurrentHeroService.", this);
                }
            }

            if (_currentHeroService == null)
            {
                _currentHeroService = FindObjectOfType<CurrentHeroService>(true);
            }
        }

        private void RefreshAllowedHeroIds()
        {
            if (_currentHeroService == null)
            {
                ResolveCurrentHeroService();
            }

            if (_currentHeroService == null)
            {
                _allowedHeroIds = null;
                return;
            }

            var known = _currentHeroService.KnownHeroIds;
            if (known == null)
            {
                _allowedHeroIds = null;
                return;
            }

            _allowedHeroIds ??= new HashSet<string>(StringComparer.Ordinal);
            _allowedHeroIds.Clear();

            foreach (var id in known)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    _allowedHeroIds.Add(id);
                }
            }

            if (_debugConfirmationLogs)
            {
                Debug.Log($"[WorldMapTurnController] Allowed hero ids refreshed: {_allowedHeroIds.Count}.", this);
            }
        }
        private void ResolveTimeService()
        {
            if (_timeServiceBehaviour != null)
            {
                if (_timeServiceBehaviour is IWorldTimeService service)
                {
                    _timeService = service;
                }
                else
                {
                    Debug.LogError("Assigned time service must implement IWorldTimeService.", this);
                }
            }

            if (_timeService != null)
                return;

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IWorldTimeService candidate)
                {
                    _timeService = candidate;
                    break;
                }
            }
        }

        private void ResolvePopupService()
        {
            if (_popupServiceBehaviour != null)
            {
                if (_popupServiceBehaviour is IPopupService service)
                {
                    _popupService = service;
                }
                else
                {
                    Debug.LogError("Assigned popup service must implement IPopupService.", this);
                }
            }

            if (_popupService != null)
                return;

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPopupService candidate)
                {
                    _popupService = candidate;
                    break;
                }
            }
        }
        private void PlayTurnEndSfx()
        {
            if (_radialMenu == null)
            {
                _radialMenu = FindObjectOfType<WorldMapRadialMenuController>(true);
            }

            _radialMenu?.PlayEndTurnSfx();
        }

    }
}







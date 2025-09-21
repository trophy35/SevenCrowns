using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SevenCrowns.Map;

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

        [Header("Events")]
        [Tooltip("Raised after end turn processing completes.")]
        [SerializeField] private UnityEvent _onTurnEnded;

        private void Awake()
        {
            if (_heroes == null || _heroes.Count == 0)
            {
                // Auto-populate from scene if not explicitly wired.
                var found = FindObjectsOfType<HeroAgentComponent>(true);
                if (found != null && found.Length > 0)
                    _heroes = new List<HeroAgentComponent>(found);
            }
        }

        /// <summary>
        /// Invoked by UI when the user clicks End Turn.
        /// Resets MP to max and clears queued paths for each hero.
        /// </summary>
        public void OnEndTurnRequested()
        {
            Debug.Log($"OnEndTurnRequested {_heroes.Count}");
            if (_heroes == null) return;

            for (int i = 0; i < _heroes.Count; i++)
            {
                var h = _heroes[i];
                if (h == null) continue;

                var mv = h.Movement;
                if (mv != null)
                {
                    mv.ResetDaily();
                }

                var agent = h.Agent;
                if (agent != null)
                {
                    agent.ClearPath();
                }
            }

            if (_onTurnEnded==null)
            {
                Debug.Log("_onTurnEnded is null");
                return;
            }
            _onTurnEnded?.Invoke();
        }
    }
}


using System.Collections.Generic;
using UnityEngine;

namespace SevenCrowns.Map.FogOfWar
{
    /// <summary>
    /// Subscribes to hero movement and updates fog-of-war visibility around the hero's current cell.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HeroAgentComponent))]
    public sealed class HeroFogVisionDriver : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _fogServiceBehaviour; // Optional; must implement IFogOfWarService
        [SerializeField, Min(0)] private int _visionRadius = 3;

        private HeroAgentComponent _hero;
        private IFogOfWarService _fog;
        private readonly Queue<GridCoord> _pending = new Queue<GridCoord>(8);
        private Coroutine _flushRoutine;

        private void Awake()
        {
            _hero = GetComponent<HeroAgentComponent>();
            _fog = ResolveFogService();
        }

        private void OnEnable()
        {
            if (_hero != null)
            {
                _hero.AgentInitialized += OnAgentInitialized;
                _hero.VisualStepCompleted += OnVisualStepCompleted;
                if (_hero.Agent != null)
                {
                    BindAgent();
                }
            }

            if (_fog != null)
            {
                _fog.VisibilityCleared += OnVisibilityCleared;
            }

            if (_hero != null && _hero.Agent != null)
            {
                RequestReveal(_hero.Agent.Position);
            }
        }

        private void OnDisable()
        {
            if (_hero != null)
            {
                _hero.AgentInitialized -= OnAgentInitialized;
                _hero.VisualStepCompleted -= OnVisualStepCompleted;
            }

            if (_fog != null)
            {
                _fog.VisibilityCleared -= OnVisibilityCleared;
            }

            if (_flushRoutine != null)
            {
                StopCoroutine(_flushRoutine);
                _flushRoutine = null;
            }
            _pending.Clear();
        }

        private IFogOfWarService ResolveFogService()
        {
            if (_fogServiceBehaviour is IFogOfWarService explicitService)
            {
                return explicitService;
            }
            return FindObjectOfType<FogOfWarService>(true);
        }

        private void OnAgentInitialized()
        {
            BindAgent();
            if (_hero != null && _hero.Agent != null)
            {
                RequestReveal(_hero.Agent.Position);
            }
        }

        private void BindAgent()
        {
            if (_hero == null || _hero.Agent == null)
                return;
            RequestReveal(_hero.Agent.Position);
        }

        private void OnVisibilityCleared()
        {
            if (_hero != null && _hero.Agent != null)
            {
                RequestReveal(_hero.Agent.Position);
            }
        }

        private void OnVisualStepCompleted(GridCoord coord)
        {
            RequestReveal(coord);
        }

        private void RequestReveal(GridCoord coord)
        {
            if (_fog == null)
            {
                _fog = ResolveFogService();
                if (_fog == null)
                    return;
                _fog.VisibilityCleared += OnVisibilityCleared;
            }

            if (_fog.Bounds.IsEmpty)
            {
                if (!_pending.Contains(coord))
                {
                    _pending.Enqueue(coord);
                }
                if (_flushRoutine == null)
                {
                    _flushRoutine = StartCoroutine(FlushWhenReady());
                }
                return;
            }

            _fog.RevealArea(coord, _visionRadius);
        }

        private System.Collections.IEnumerator FlushWhenReady()
        {
            while (_fog != null && _fog.Bounds.IsEmpty)
            {
                if (_pending.Count > 0)
                {
                    _fog.RevealArea(_pending.Peek(), _visionRadius);
                }
                yield return null;
            }

            if (_fog != null && !_fog.Bounds.IsEmpty)
            {
                while (_pending.Count > 0)
                {
                    var coord = _pending.Dequeue();
                    _fog.RevealArea(coord, _visionRadius);
                }
            }

            _flushRoutine = null;
        }
    }
}

using SevenCrowns.Map.Mines;
using UnityEngine;

namespace SevenCrowns.Map
{
    /// <summary>
    /// Tracks hover duration over mine entries and exposes tooltip hints after a configurable delay.
    /// </summary>
    internal sealed class MineTooltipState
    {
        private readonly float _delaySeconds;
        private string _activeNodeId = string.Empty;
        private bool _tooltipActive;
        private float _hoverTimer;
        private WorldTooltipHint _currentHint;

        public MineTooltipState(float delaySeconds)
        {
            _delaySeconds = Mathf.Max(0f, delaySeconds);
            _currentHint = WorldTooltipHint.None;
        }

        public bool Update(MineNodeDescriptor? descriptor, float deltaTime, out WorldTooltipHint hint)
        {
            if (!descriptor.HasValue || !descriptor.Value.IsValid)
            {
                if (_tooltipActive)
                {
                    ResetInternal();
                    hint = WorldTooltipHint.None;
                    return true;
                }

                ResetTracking();
                hint = WorldTooltipHint.None;
                return false;
            }

            var value = descriptor.Value;
            var nodeId = value.NodeId;

            if (!string.Equals(_activeNodeId, nodeId, System.StringComparison.Ordinal))
            {
                bool wasActive = _tooltipActive;
                ResetTracking();
                _activeNodeId = nodeId;
                if (wasActive)
                {
                    hint = WorldTooltipHint.None;
                    return true;
                }
            }

            _hoverTimer += Mathf.Max(0f, deltaTime);

            var desiredContent = new MineTooltipContent(value, Mathf.Max(0, value.DailyYield));

            if (_tooltipActive)
            {
                if (!_currentHint.Mine.Equals(desiredContent))
                {
                    _currentHint = WorldTooltipHint.ForMine(desiredContent);
                    hint = _currentHint;
                    return true;
                }

                hint = _currentHint;
                return false;
            }

            if (_hoverTimer >= _delaySeconds)
            {
                _tooltipActive = true;
                _currentHint = WorldTooltipHint.ForMine(desiredContent);
                hint = _currentHint;
                return true;
            }

            hint = WorldTooltipHint.None;
            return false;
        }

        public bool ForceHide(out WorldTooltipHint hint)
        {
            if (_tooltipActive)
            {
                ResetInternal();
                hint = WorldTooltipHint.None;
                return true;
            }

            ResetTracking();
            hint = WorldTooltipHint.None;
            return false;
        }

        private void ResetInternal()
        {
            ResetTracking();
        }

        private void ResetTracking()
        {
            _activeNodeId = string.Empty;
            _hoverTimer = 0f;
            _tooltipActive = false;
            _currentHint = WorldTooltipHint.None;
        }
    }
}


using UnityEngine;
using SevenCrowns.UI;
using SevenCrowns.Map;

namespace SevenCrowns.Systems
{
    /// <summary>
    /// Binds the WorldMapRadialMenuController Move toggle event to ClickToMoveController.SetMoveModeEnabled.
    /// Lives in Game.Core to avoid assembly cycles: Core references UI and Map; UI does not reference Map.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapMoveModeBinder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WorldMapRadialMenuController _menu;
        [SerializeField] private ClickToMoveController _clickToMove;
        private ISelectedHeroAgentProvider _selection;

        private void Awake()
        {
            if (_menu == null)
                _menu = FindObjectOfType<WorldMapRadialMenuController>(true);
            if (_clickToMove == null)
                _clickToMove = FindObjectOfType<ClickToMoveController>(true);
            if (_selection == null)
            {
                var behaviours = FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is ISelectedHeroAgentProvider p)
                    {
                        _selection = p;
                        break;
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (_menu != null && _clickToMove != null)
            {
                _menu.AddMoveModeChangedListener(_clickToMove.SetMoveModeEnabled);
            }
            if (_selection != null)
            {
                _selection.SelectedHeroChanged += OnSelectedHeroChanged;
            }
        }

        private void OnDisable()
        {
            if (_menu != null && _clickToMove != null)
            {
                _menu.RemoveMoveModeChangedListener(_clickToMove.SetMoveModeEnabled);
            }
            if (_selection != null)
            {
                _selection.SelectedHeroChanged -= OnSelectedHeroChanged;
            }
        }

        private void OnSelectedHeroChanged(HeroAgentComponent _)
        {
            // Disable move mode on hero change and sync UI state.
            if (_clickToMove != null)
                _clickToMove.SetMoveModeEnabled(false);
            if (_menu != null)
                _menu.SetMoveActive(false);
        }
    }
}

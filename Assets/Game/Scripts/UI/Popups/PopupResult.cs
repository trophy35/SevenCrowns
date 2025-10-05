using System;

namespace SevenCrowns.UI.Popups
{
    /// <summary>
    /// Result returned when a popup option is selected.
    /// </summary>
    public readonly struct PopupResult
    {
        public PopupResult(string optionId)
        {
            OptionId = optionId ?? string.Empty;
        }

        public string OptionId { get; }

        public bool Is(string optionId)
        {
            return string.Equals(OptionId, optionId, StringComparison.Ordinal);
        }
    }
}

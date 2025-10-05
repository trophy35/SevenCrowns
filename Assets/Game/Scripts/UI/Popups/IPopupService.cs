using System;

namespace SevenCrowns.UI.Popups
{
    /// <summary>
    /// Dispatches modal popup requests and reports the selected option.
    /// </summary>
    public interface IPopupService
    {
        bool IsShowing { get; }
        void RequestPopup(PopupRequest request, Action<PopupResult> onCompleted);
    }
}

using System;

namespace SevenCrowns.Systems
{
    public interface IWorldTimeService
    {
        WorldDate CurrentDate { get; }
        event Action<WorldDate> DateChanged;
        void AdvanceDay();
        void ResetTo(WorldDate date);
    }
}

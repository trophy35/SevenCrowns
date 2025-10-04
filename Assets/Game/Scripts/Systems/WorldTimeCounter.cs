using System;

namespace SevenCrowns.Systems
{
    public sealed class WorldTimeCounter
    {
        private int _daysPerWeek;
        private int _weeksPerMonth;

        public WorldDate CurrentDate { get; private set; }

        public WorldTimeCounter(WorldDate startDate, int daysPerWeek, int weeksPerMonth)
        {
            SetRules(daysPerWeek, weeksPerMonth);
            CurrentDate = startDate;
        }

        public WorldDate AdvanceDay()
        {
            int day = CurrentDate.Day + 1;
            int week = CurrentDate.Week;
            int month = CurrentDate.Month;

            if (day > _daysPerWeek)
            {
                day = 1;
                week++;
                if (week > _weeksPerMonth)
                {
                    week = 1;
                    month++;
                }
            }

            CurrentDate = new WorldDate(day, week, month);
            return CurrentDate;
        }

        public void Reset(WorldDate date)
        {
            CurrentDate = date;
        }

        public void SetRules(int daysPerWeek, int weeksPerMonth)
        {
            if (daysPerWeek < 1) throw new ArgumentOutOfRangeException(nameof(daysPerWeek));
            if (weeksPerMonth < 1) throw new ArgumentOutOfRangeException(nameof(weeksPerMonth));

            _daysPerWeek = daysPerWeek;
            _weeksPerMonth = weeksPerMonth;
        }
    }
}

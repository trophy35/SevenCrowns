using NUnit.Framework;
using SevenCrowns.Systems;

namespace SevenCrowns.Tests.EditMode.Systems
{
    public sealed class WorldTimeCounterTests
    {
        [Test]
        public void AdvanceDay_IncrementsDay()
        {
            var counter = new WorldTimeCounter(new WorldDate(1, 1, 1), 7, 4);

            var next = counter.AdvanceDay();

            Assert.That(next.Day, Is.EqualTo(2));
            Assert.That(next.Week, Is.EqualTo(1));
            Assert.That(next.Month, Is.EqualTo(1));
        }

        [Test]
        public void AdvanceDay_RollsWeekAfterConfiguredDays()
        {
            var counter = new WorldTimeCounter(new WorldDate(1, 1, 1), 7, 4);

            for (int i = 0; i < 6; i++)
            {
                counter.AdvanceDay();
            }

            var rolled = counter.AdvanceDay();

            Assert.That(rolled, Is.EqualTo(new WorldDate(1, 2, 1)));
        }

        [Test]
        public void AdvanceDay_RollsMonthAfterConfiguredWeeks()
        {
            var counter = new WorldTimeCounter(new WorldDate(1, 1, 1), 7, 4);

            for (int i = 0; i < 28; i++)
            {
                counter.AdvanceDay();
            }

            Assert.That(counter.CurrentDate, Is.EqualTo(new WorldDate(1, 1, 2)));
        }
    }
}

using NUnit.Framework;
using SevenCrowns.Systems;
using UnityEngine;

namespace SevenCrowns.Tests.EditMode.Systems
{
    public sealed class CurrentHeroServiceTests
    {
        [Test]
        public void SetCurrentHeroDirect_WithLevel_SetsStateAndRaisesEvents()
        {
            var go = new GameObject(nameof(CurrentHeroServiceTests));
            var service = go.AddComponent<CurrentHeroService>();

            try
            {
                string heroChangedId = null;
                string heroChangedPortrait = null;
                string levelChangedId = null;
                int levelChangedValue = 0;
                int heroChangedCount = 0;
                int levelChangedCount = 0;

                service.CurrentHeroChanged += (heroId, portraitKey) =>
                {
                    heroChangedCount++;
                    heroChangedId = heroId;
                    heroChangedPortrait = portraitKey;
                };
                service.CurrentHeroLevelChanged += (heroId, level) =>
                {
                    levelChangedCount++;
                    levelChangedId = heroId;
                    levelChangedValue = level;
                };

                service.SetCurrentHeroDirect("hero.test", "portrait.key", 5);

                Assert.That(service.CurrentHeroId, Is.EqualTo("hero.test"));
                Assert.That(service.CurrentPortraitKey, Is.EqualTo("portrait.key"));
                Assert.That(service.CurrentLevel, Is.EqualTo(5));
                Assert.That(heroChangedCount, Is.EqualTo(1));
                Assert.That(heroChangedId, Is.EqualTo("hero.test"));
                Assert.That(heroChangedPortrait, Is.EqualTo("portrait.key"));
                Assert.That(levelChangedCount, Is.EqualTo(1));
                Assert.That(levelChangedId, Is.EqualTo("hero.test"));
                Assert.That(levelChangedValue, Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TryGetPortraitKey_ReturnsMappedKey_WithoutChangingCurrent()
        {
            var go = new GameObject(nameof(CurrentHeroServiceTests));
            var service = go.AddComponent<CurrentHeroService>();

            try
            {
                // Seed two heroes via direct set
                service.SetCurrentHeroDirect("hero.alpha", "portrait.alpha", 2);
                service.SetCurrentHeroDirect("hero.beta", "portrait.beta", 4);

                // Current is beta now; query alpha
                bool foundAlpha = service.TryGetPortraitKey("hero.alpha", out var keyAlpha);
                Assert.That(foundAlpha, Is.True);
                Assert.That(keyAlpha, Is.EqualTo("portrait.alpha"));

                // Unknown id
                bool foundUnknown = service.TryGetPortraitKey("hero.unknown", out var keyUnknown);
                Assert.That(foundUnknown, Is.False);
                Assert.That(keyUnknown, Is.Null.Or.Empty);

                // Ensure current hero state unchanged by lookups
                Assert.That(service.CurrentHeroId, Is.EqualTo("hero.beta"));
                Assert.That(service.CurrentPortraitKey, Is.EqualTo("portrait.beta"));
                Assert.That(service.CurrentLevel, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
        [Test]
        public void SetCurrentHeroLevel_RaisesOnlyLevelEvent_WhenPortraitUnchanged()
        {
            var go = new GameObject(nameof(CurrentHeroServiceTests));
            var service = go.AddComponent<CurrentHeroService>();

            try
            {
                service.SetCurrentHeroDirect("hero.test", "portrait.key", 3);

                int heroChangedCount = 0;
                int levelChangedCount = 0;

                service.CurrentHeroChanged += (heroId, portraitKey) => heroChangedCount++;
                service.CurrentHeroLevelChanged += (heroId, level) => levelChangedCount++;

                service.SetCurrentHeroLevel(8);

                Assert.That(service.CurrentLevel, Is.EqualTo(8));
                Assert.That(heroChangedCount, Is.EqualTo(0));
                Assert.That(levelChangedCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}


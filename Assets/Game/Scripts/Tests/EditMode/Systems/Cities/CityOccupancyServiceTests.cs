using NUnit.Framework;
using SevenCrowns.Systems.Cities;
using UnityEngine;

namespace SevenCrowns.Tests.EditMode.Systems.Cities
{
    public sealed class CityOccupancyServiceTests
    {
        [Test]
        public void TryEnter_AllowsSingleHero_PerCity()
        {
            // Arrange
            var go = new GameObject("CityOcc");
            var svc = go.AddComponent<CityOccupancyService>();
            string cityId = "city.alpha";
            string heroA = "hero.a";
            string heroB = "hero.b";

            // Act
            bool first = svc.TryEnter(cityId, heroA);
            bool secondDifferentHero = svc.TryEnter(cityId, heroB);

            // Assert
            Assert.That(first, Is.True);
            Assert.That(secondDifferentHero, Is.False);
            Assert.That(svc.IsOccupied(cityId), Is.True);
            Assert.That(svc.GetOccupant(cityId), Is.EqualTo(heroA));
        }

        [Test]
        public void TryLeaveByHero_ClearsOccupancy()
        {
            // Arrange
            var go = new GameObject("CityOcc");
            var svc = go.AddComponent<CityOccupancyService>();
            string cityId = "city.beta";
            string heroA = "hero.a";
            svc.TryEnter(cityId, heroA);

            // Act
            bool left = svc.TryLeaveByHero(heroA);

            // Assert
            Assert.That(left, Is.True);
            Assert.That(svc.IsOccupied(cityId), Is.False);
            Assert.That(svc.GetOccupant(cityId), Is.EqualTo(string.Empty));
        }
    }
}


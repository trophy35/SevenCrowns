using NUnit.Framework;
using UnityEngine;
using TMPro;
using SevenCrowns.UI.Cities;

namespace SevenCrowns.Tests.EditMode.UI.Cities
{
    public sealed class CityNameViewTests
    {
        [Test]
        public void Apply_UsesFallback_WhenNoLocalization()
        {
            var go = new GameObject("CityNameTest");
            try
            {
                var text = go.AddComponent<TextMeshProUGUI>();
                var view = go.AddComponent<CityNameView>();
                // Provide a fake provider so UI remains decoupled from Core
                var providerGo = new GameObject("Provider");
                var fake = providerGo.AddComponent<FakeProvider>();
                fake.CityId = "city.knights.town";
                // Act
                view.Apply();
                // Assert: Title-cased fallback
                Assert.That(text.text, Is.EqualTo("Knights Town"));
            }
            finally
            {
                Object.DestroyImmediate(go);
                foreach (var o in Object.FindObjectsOfType<FakeProvider>()) Object.DestroyImmediate(o.gameObject);
            }
        }

        private sealed class FakeProvider : MonoBehaviour, ICityNameKeyProvider
        {
            public string CityId;
            public string CityNameKey;
            public bool TryGetCityNameKey(out string cityNameKey)
            {
                cityNameKey = CityNameKey;
                return !string.IsNullOrEmpty(cityNameKey);
            }
            public bool TryGetCityId(out string cityId)
            {
                cityId = CityId;
                return !string.IsNullOrEmpty(cityId);
            }
        }
    }
}

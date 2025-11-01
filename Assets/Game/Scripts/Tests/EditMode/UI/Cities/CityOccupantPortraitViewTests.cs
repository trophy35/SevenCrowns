using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using SevenCrowns.UI;
using SevenCrowns.UI.Cities;

namespace SevenCrowns.Tests.EditMode.UI.Cities
{
    public sealed class CityOccupantPortraitViewTests
    {
        private sealed class FakeAssets : MonoBehaviour, IUiAssetProvider
        {
            public string Key;
            public Sprite Sprite;
            public bool TryGetSprite(string key, out Sprite sprite)
            {
                if (key == Key) { sprite = Sprite; return true; }
                sprite = null; return false;
            }
            public bool TryGetAudioClip(string key, out AudioClip clip) { clip = null; return false; }
        }

        private sealed class FakeOccupantProvider : MonoBehaviour, ICityOccupantHeroProvider
        {
            public string HeroId;
            public string PortraitKey;
            public bool TryGetOccupantHero(out string heroId, out string portraitKey)
            {
                heroId = HeroId; portraitKey = PortraitKey; return !string.IsNullOrEmpty(heroId);
            }
        }

        [Test]
        public void ShowsImage_WhenOccupantProvidedAndSpriteResolves()
        {
            var root = new GameObject("Root");
            try
            {
                // Create a test sprite
                var tex = new Texture2D(2, 2);
                tex.SetPixel(0, 0, Color.green); tex.Apply();
                var sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

                // Fake services
                var assetsGo = new GameObject("Assets");
                var assets = assetsGo.AddComponent<FakeAssets>();
                var providerGo = new GameObject("Provider");
                var provider = providerGo.AddComponent<FakeOccupantProvider>();
                provider.HeroId = "hero.knight";
                provider.PortraitKey = "UI/Heroes/Knight01[Knight01_0]";

                assets.Key = provider.PortraitKey;
                assets.Sprite = sprite;

                // View
                var img = root.AddComponent<Image>();
                img.enabled = false; // default
                var view = root.AddComponent<CityOccupantPortraitView>();

                // Invoke lifecycle
                var awake = typeof(CityOccupantPortraitView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var onEnable = typeof(CityOccupantPortraitView).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awake!.Invoke(view, null);
                onEnable!.Invoke(view, null);

                // Assert: image enabled and sprite set
                Assert.That(img.enabled, Is.True);
                Assert.That(img.sprite, Is.Not.Null);
                Assert.That(img.sprite.texture.GetPixel(0, 0), Is.EqualTo(Color.green).Using(new ColorComparer()));
            }
            finally
            {
                Object.DestroyImmediate(root);
                foreach (var go in Object.FindObjectsOfType<GameObject>())
                {
                    if (go.name == "Assets" || go.name == "Provider")
                    {
                        Object.DestroyImmediate(go);
                    }
                }
            }
        }

        [Test]
        public void HidesImage_WhenNoOccupantProvided()
        {
            var root = new GameObject("Root");
            try
            {
                // Provider with no occupant
                var providerGo = new GameObject("Provider");
                var provider = providerGo.AddComponent<FakeOccupantProvider>();
                provider.HeroId = string.Empty;
                provider.PortraitKey = string.Empty;

                // View
                var img = root.AddComponent<Image>();
                img.enabled = true; // will be turned off
                var view = root.AddComponent<CityOccupantPortraitView>();

                var awake = typeof(CityOccupantPortraitView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var onEnable = typeof(CityOccupantPortraitView).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awake!.Invoke(view, null);
                onEnable!.Invoke(view, null);

                Assert.That(img.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                foreach (var go in Object.FindObjectsOfType<GameObject>())
                {
                    if (go.name == "Provider")
                    {
                        Object.DestroyImmediate(go);
                    }
                }
            }
        }

        private sealed class ColorComparer : System.Collections.IEqualityComparer
        {
            public new bool Equals(object x, object y)
            {
                if (x is Color a && y is Color b)
                {
                    return Mathf.Approximately(a.r, b.r) && Mathf.Approximately(a.g, b.g) && Mathf.Approximately(a.b, b.b) && Mathf.Approximately(a.a, b.a);
                }
                return Equals(x, y);
            }
            public int GetHashCode(object obj) => obj?.GetHashCode() ?? 0;
        }
    }
}


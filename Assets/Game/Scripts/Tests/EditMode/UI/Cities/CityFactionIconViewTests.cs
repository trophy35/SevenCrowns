using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using SevenCrowns.UI;
using SevenCrowns.UI.Cities;

namespace SevenCrowns.Tests.EditMode.UI.Cities
{
    public sealed class CityFactionIconViewTests
    {
        private sealed class FakeAssets : MonoBehaviour, IUiAssetProvider
        {
            public string Key;
            public Sprite Sprite;
            public bool TryGetSprite(string key, out Sprite sprite)
            {
                if (key == Key)
                {
                    sprite = Sprite; return true;
                }
                sprite = null; return false;
            }
            public bool TryGetAudioClip(string key, out AudioClip clip) { clip = null; return false; }
        }

        private sealed class FakeFactionProvider : MonoBehaviour, ICityFactionIdProvider
        {
            public string FactionId;
            public bool TryGetFactionId(out string factionId)
            {
                factionId = FactionId; return !string.IsNullOrEmpty(factionId);
            }
        }

        [Test]
        public void BindsSprite_FromProviderAndAssets()
        {
            var root = new GameObject("Root");
            try
            {
                // Create a small sprite
                var tex = new Texture2D(2, 2);
                tex.SetPixel(0, 0, Color.red); tex.Apply();
                var sprite = Sprite.Create(tex, new Rect(0,0,2,2), new Vector2(0.5f,0.5f));

                // Fake services
                var assetsGo = new GameObject("Assets");
                var assets = assetsGo.AddComponent<FakeAssets>();
                var providerGo = new GameObject("Provider");
                var provider = providerGo.AddComponent<FakeFactionProvider>();
                provider.FactionId = "faction.knight";

                // Key mapping
                assets.Key = "UI/Factions/faction.knight";
                assets.Sprite = sprite;

                // View
                var img = root.AddComponent<Image>();
                var view = root.AddComponent<CityFactionIconView>();

                // Act: invoke lifecycle via reflection to avoid Unity editor assertions on SendMessage
                var awake = typeof(CityFactionIconView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var onEnable = typeof(CityFactionIconView).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awake!.Invoke(view, null);
                onEnable!.Invoke(view, null);

                // Assert
                Assert.That(img.sprite, Is.Not.Null);
            }
            finally
            {
                // Cleanup all objects created by the test to avoid cross-test leakage
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
        public void LocalFallbackMapping_Binds_WhenNoAssetsProvider()
        {
            var root = new GameObject("Root");
            try
            {
                // Create a small sprite
                var tex = new Texture2D(2, 2);
                tex.SetPixel(0, 0, Color.blue); tex.Apply();
                var sprite = Sprite.Create(tex, new Rect(0,0,2,2), new Vector2(0.5f,0.5f));

                // Only faction provider (no assets provider in scene)
                var providerGo = new GameObject("Provider");
                var provider = providerGo.AddComponent<FakeFactionProvider>();
                provider.FactionId = "faction.knight";

                // View and local mapping
                var img = root.AddComponent<Image>();
                var view = root.AddComponent<CityFactionIconView>();
                // Use serialized mapping through reflection since it's private
                var field = typeof(CityFactionIconView).GetField("_fallbackSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var elemType = field.FieldType.GetElementType();
                var arr = System.Array.CreateInstance(elemType, 1);
                var entry = System.Activator.CreateInstance(elemType);
                elemType.GetField("factionId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).SetValue(entry, "faction.knight");
                elemType.GetField("sprite", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).SetValue(entry, sprite);
                arr.SetValue(entry, 0);
                field.SetValue(view, arr);

                // Act: invoke lifecycle via reflection to avoid Unity editor assertions on SendMessage
                var awake = typeof(CityFactionIconView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var onEnable = typeof(CityFactionIconView).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awake!.Invoke(view, null);
                onEnable!.Invoke(view, null);

                // Assert: sprite applied from local mapping
                Assert.That(img.sprite, Is.Not.Null);
                Assert.That(img.sprite.texture.GetPixel(0,0), Is.EqualTo(Color.blue).Using(new ColorComparer()));
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

        private sealed class ColorComparer : System.Collections.IEqualityComparer
        {
            public new bool Equals(object x, object y)
            {
                if (x is Color a && y is Color b)
                {
                    return Mathf.Approximately(a.r,b.r) && Mathf.Approximately(a.g,b.g) && Mathf.Approximately(a.b,b.b) && Mathf.Approximately(a.a,b.a);
                }
                return Equals(x,y);
            }
            public int GetHashCode(object obj) => obj?.GetHashCode() ?? 0;
        }
    }
}

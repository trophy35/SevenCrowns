using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SevenCrowns.UI;

namespace SevenCrowns.Tests.EditMode.UI
{
    public sealed class ButtonHoverColorTintTests
    {
        [Test]
        public void Hover_ChangesColor_And_RevertsOnExit()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            try
            {
                var go = new GameObject("Btn", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root.transform);
                var img = go.GetComponent<Image>();
                img.color = Color.gray;

                var tint = go.AddComponent<ButtonHoverColorTint>();
                tint.NormalColor = Color.gray;
                tint.HoverColor = Color.red;
                tint.FadeDuration = 0f; // instant for deterministic test

                // Simulate hover
                tint.OnPointerEnter(new PointerEventData(null));
                Assert.That(img.color, Is.EqualTo(Color.red));

                // Simulate exit
                tint.OnPointerExit(new PointerEventData(null));
                Assert.That(img.color, Is.EqualTo(Color.gray));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}


using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Systems.Cities;

namespace SevenCrowns.Tests.EditMode.Systems.Cities
{
    public sealed class CityExitFlowServiceTests
    {
        [Test]
        public void ExitToWorldMap_DoesNotThrow()
        {
            var go = new GameObject("CityExitTester");
            try
            {
                var svc = go.AddComponent<CityExitFlowService>();
                Assert.DoesNotThrow(() => svc.ExitToWorldMap());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExitToWorldMap_AllowsEmptySceneName()
        {
            var go = new GameObject("CityExitTester_Empty");
            try
            {
                var svc = go.AddComponent<CityExitFlowService>();
                // Overwrite via reflection since the field is private; we only assert call path robustness.
                var field = typeof(CityExitFlowService).GetField("_worldMapSceneName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.SetValue(svc, string.Empty);

                Assert.DoesNotThrow(() => svc.ExitToWorldMap());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}


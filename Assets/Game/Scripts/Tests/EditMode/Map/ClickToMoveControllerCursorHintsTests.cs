using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SevenCrowns.Map;

namespace SevenCrowns.Tests.EditMode.Map
{
    public sealed class ClickToMoveControllerCursorHintsTests
    {
        private static MethodInfo GetNotifyMethod()
        {
            var method = typeof(ClickToMoveController).GetMethod("NotifyCursorHints", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "NotifyCursorHints method is required for cursor hint signalling tests.");
            return method;
        }

        private static void InvokeNotify(ClickToMoveController controller, bool hover, bool move, bool collect)
        {
            GetNotifyMethod().Invoke(controller, new object[] { hover, move, collect });
        }

        [Test]
        public void NotifyCursorHints_RaisesEventWithCollectFlag()
        {
            var go = new GameObject("ClickToMoveController_Test");
            try
            {
                var controller = go.AddComponent<ClickToMoveController>();
                bool? receivedHover = null;
                bool? receivedMove = null;
                bool? receivedCollect = null;

                controller.CursorHintsChanged += (hover, move, collect) =>
                {
                    receivedHover = hover;
                    receivedMove = move;
                    receivedCollect = collect;
                };

                InvokeNotify(controller, true, false, true);

                Assert.That(receivedHover, Is.True);
                Assert.That(receivedMove, Is.False);
                Assert.That(receivedCollect, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void NotifyCursorHints_DoesNotInvokeWhenValuesUnchanged()
        {
            var go = new GameObject("ClickToMoveController_NoRepeat");
            try
            {
                var controller = go.AddComponent<ClickToMoveController>();
                int invocationCount = 0;
                controller.CursorHintsChanged += (_, _, _) => invocationCount++;

                InvokeNotify(controller, true, false, false);
                InvokeNotify(controller, true, false, false);

                Assert.That(invocationCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}

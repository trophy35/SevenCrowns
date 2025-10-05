using NUnit.Framework;
using SevenCrowns.UI.Popups;

namespace SevenCrowns.Tests.UI
{
    public sealed class PopupRequestTests
    {
        [Test]
        public void CreateConfirmation_ProducesExpectedOptions()
        {
            var args = new object[] { 3 };

            var request = PopupRequest.CreateConfirmation(
                "UI.Common",
                "Popups.EndTurn.Title",
                "Popups.EndTurn.Body",
                "Popup.Confirm",
                "Popup.Cancel",
                args);

            Assert.That(request, Is.Not.Null);
            Assert.That(request.HasTitle, Is.True);
            Assert.That(request.Options.Count, Is.EqualTo(2));
            Assert.That(request.Options[0].Id, Is.EqualTo(PopupOptionIds.Confirm));
            Assert.That(request.Options[1].Id, Is.EqualTo(PopupOptionIds.Cancel));
            Assert.That(request.Message.Arguments, Is.SameAs(args));
        }

        [Test]
        public void PopupResult_IsMatchesOptionIds()
        {
            var result = new PopupResult(PopupOptionIds.Confirm);

            Assert.That(result.Is(PopupOptionIds.Confirm), Is.True);
            Assert.That(result.Is(PopupOptionIds.Cancel), Is.False);
        }
    }
}

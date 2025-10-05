using System;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace SevenCrowns.UI.Popups
{
    /// <summary>
    /// Immutable description of a popup request (title, message, option set).
    /// </summary>
    public sealed class PopupRequest
    {
        public PopupRequest(LocalizedString message, IReadOnlyList<PopupOptionDefinition> options, LocalizedString title = null)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            if (options == null || options.Count == 0)
                throw new ArgumentException("At least one option is required.", nameof(options));
            Options = options;
            Title = title;
        }

        public LocalizedString Title { get; }
        public bool HasTitle => Title != null;
        public LocalizedString Message { get; }
        public IReadOnlyList<PopupOptionDefinition> Options { get; }

        public static PopupRequest CreateConfirmation(
            string table,
            string titleEntry,
            string bodyEntry,
            string confirmEntry,
            string cancelEntry,
            object[] bodyArguments = null)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentException("String table is required.", nameof(table));
            if (string.IsNullOrEmpty(bodyEntry)) throw new ArgumentException("Body entry is required.", nameof(bodyEntry));
            if (string.IsNullOrEmpty(confirmEntry)) throw new ArgumentException("Confirm entry is required.", nameof(confirmEntry));
            if (string.IsNullOrEmpty(cancelEntry)) throw new ArgumentException("Cancel entry is required.", nameof(cancelEntry));

            var title = string.IsNullOrEmpty(titleEntry) ? null : CreateLocalized(table, titleEntry, null);
            var body = CreateLocalized(table, bodyEntry, bodyArguments);

            var options = new[]
            {
                PopupOptionDefinition.Create(PopupOptionIds.Confirm, table, confirmEntry, true),
                PopupOptionDefinition.Create(PopupOptionIds.Cancel, table, cancelEntry)
            };

            return new PopupRequest(body, options, title);
        }

        public static LocalizedString CreateLocalized(string table, string entry, object[] arguments)
        {
            var localized = new LocalizedString
            {
                TableReference = table,
                TableEntryReference = entry
            };

            if (arguments != null && arguments.Length > 0)
            {
                localized.Arguments = arguments;
            }

            return localized;
        }
    }
}

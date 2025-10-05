using System;
using UnityEngine.Localization;

namespace SevenCrowns.UI.Popups
{
    /// <summary>
    /// Describes a popup option including localization and focus metadata.
    /// </summary>
    public readonly struct PopupOptionDefinition
    {
        public PopupOptionDefinition(string id, LocalizedString label, bool isDefaultFocus, bool closeOnSelect = true)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            Id = string.IsNullOrEmpty(id) ? PopupOptionIds.Ok : id;
            Label = label;
            IsDefaultFocus = isDefaultFocus;
            CloseOnSelect = closeOnSelect;
        }

        public string Id { get; }
        public LocalizedString Label { get; }
        public bool IsDefaultFocus { get; }
        public bool CloseOnSelect { get; }

        public static PopupOptionDefinition Create(string id, string table, string entry, bool isDefaultFocus = false, bool closeOnSelect = true)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentException("Table is required.", nameof(table));
            if (string.IsNullOrEmpty(entry)) throw new ArgumentException("Entry is required.", nameof(entry));

            var localized = new LocalizedString
            {
                TableReference = table,
                TableEntryReference = entry
            };

            return new PopupOptionDefinition(id, localized, isDefaultFocus, closeOnSelect);
        }
    }
}

﻿namespace FreeAndWithBeer.Text
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// StringSchema is a class to manage a Schema's StringSchemaEntries while validating that they are logically correct.
    /// A valid StringSchema has no Entry with a Header that StartWiths any other Entry's Header.
    /// <see cref="FreeAndWithBeer.Text.StringSchemaExtensions">StringSchemaExtensions</see> explains a "schema'd" sequence of characters.
    /// </summary>
    public sealed class StringSchema
    {
        /// <summary>
        /// Creates a new StringSchema without any Entries.
        /// </summary>
        public StringSchema() { }

        private readonly Dictionary<string, StringSchemaEntry> _entries = new Dictionary<string, StringSchemaEntry>();

        /// <summary>
        /// Validates and adds a new StringSchemaEntry to the Schema.
        /// </summary>
        public void AddEntry(StringSchemaEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            VerifyEntry(entry);
            _entries.Add(entry.Header, entry);
        }

        public StringSchemaEntry this[string header] { get { return _entries[header]; } }

        internal StringSchemaEntry GetEntryForRow(string row)
        {
            foreach (StringSchemaEntry entry in _entries.Values)
                if (row.StartsWith(entry.Header, StringComparison.Ordinal))
                    return entry;

            throw new ArgumentOutOfRangeException("row", row, "No matching schema definition.");
        }

        private void VerifyEntry(StringSchemaEntry entry)
        {
            foreach (StringSchemaEntry existingEntry in _entries.Values)
                if (existingEntry.Header.StartsWith(entry.Header, StringComparison.Ordinal) || entry.Header.StartsWith(existingEntry.Header, StringComparison.Ordinal))
                    throw new ArgumentOutOfRangeException("entry", entry.Header, "No entry.Header may StartWith any other existing entry.Header.");
        }
    }
}
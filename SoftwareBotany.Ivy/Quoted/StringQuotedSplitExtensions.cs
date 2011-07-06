﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SoftwareBotany.Ivy
{
    public static class StringQuotedSplitExtensions
    {
        /// <summary>
        /// Splits a delimited string while allowing for instances of the delimiter to occur within individual
        /// columns.  Such columns must be quoted to allow for this behavior. Newlines outside of quotes will
        /// cause an exception because multiple lines are not allowed.
        /// </summary>
        /// <param name="chars">The quoted delimited string to split.</param>
        /// <param name="signals">The signals that dictate the delimiter, the quote, the newline, etc.</param>
        /// <returns>The string columns after the split.</returns>
        public static string[] SplitQuoted(this IEnumerable<char> chars, StringQuotedSignals signals)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");

            SplitQuotedProcessor processor = new SplitQuotedProcessor(signals);
            string[] split = processor.Process(chars).SingleOrDefault();
            return split == null ? new string[0] : split;
        }

        /// <summary>
        /// Splits a delimited string while allowing for instances of the delimiter to occur within individual
        /// columns.  Such columns must be quoted to allow for this behavior. Newlines outside of quotes are
        /// allowed and signal a new split array to begin.
        /// </summary>
        /// <param name="chars">The quoted delimited string to split.</param>
        /// <param name="signals">The signals that dictate the delimiter, the quote, the newline, etc.</param>
        /// <returns>An enumeration of sets of string columns after each line in the delimited string has been split.</returns>
        public static IEnumerable<string[]> SplitQuotedMultiline(this IEnumerable<char> chars, StringQuotedSignals signals)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");

            SplitQuotedProcessor processor = new SplitQuotedProcessor(signals);
            return processor.Process(chars);
        }

        /// <summary>
        /// Splits a delimited string while allowing for instances of the delimiter to occur within individual
        /// columns.  Such columns must be quoted to allow for this behavior. Newlines outside of quotes will
        /// cause an exception because multiple lines are not allowed in an individual string; i.e. multiple
        /// lines are determined by the strings parameter and not by each individual string.
        /// </summary>
        /// <param name="strings">The quoted delimited strings to split.</param>
        /// <param name="signals">The signals that dictate the delimiter, the quote, the newline, etc.</param>
        /// <returns>An enumeration of sets of string columns after each line in the delimited string has been split.</returns>
        public static IEnumerable<string[]> SplitQuotedMultiline(this IEnumerable<IEnumerable<char>> strings, StringQuotedSignals signals)
        {
            if (strings == null)
                throw new ArgumentNullException("strings");

            SplitQuotedProcessor processor = new SplitQuotedProcessor(signals);

            foreach (IEnumerable<char> str in strings)
            {
                if (str == null)
                    throw new ArgumentException("strings");

                string[] split = processor.Process(str).SingleOrDefault();
                yield return split == null ? new string[0] : split;
            }
        }

        /// <summary>
        /// Resuable logic for processing the SplitQuoted functions.
        /// </summary>
        private class SplitQuotedProcessor
        {
            public SplitQuotedProcessor(StringQuotedSignals signals)
            {
                _signals = signals;

                _delimiterTracker = new StringSignalTracker(_signals.Delimiter);
                _quoteTracker = new StringSignalTracker(_signals.Quote);
                _newlineTracker = new StringSignalTracker(_signals.NewLine);
            }

            public IEnumerable<string[]> Process(IEnumerable<char> chars)
            {
                List<string> results = new List<string>();

                foreach (char c in chars)
                {
                    bool triggered = ProcessChar(c);

                    if (triggered)
                    {
                        if (IsNewline && !InQuotes())
                            yield return ProduceLineResults();

                        ResetTrackers();
                    }
                }

                if (_buffer.Length > 0)
                    yield return ProduceLineResults();

                // This will ensure the Processor is in a reusable state.
                ResetTrackers();
            }

            private bool ProcessChar(char c)
            {
                _buffer.Append(c);
                _categories.Add(CharacterCategory.Char);

                _delimiterTracker.ProcessChar(c);
                _quoteTracker.ProcessChar(c);
                _newlineTracker.ProcessChar(c);

                ReviseCategories();

                return IsDelimiter || IsQuote || IsNewline;
            }

            private void ReviseCategories()
            {
                if (IsDelimiter)
                {
                    ReviseCategories(CharacterCategory.Delimiter, _signals.Delimiter.Length);
                }
                else if (IsQuote)
                {
                    bool isEndQuote = InQuotes();

                    ReviseCategories(isEndQuote ? CharacterCategory.EndQuote : CharacterCategory.StartQuote, _signals.Quote.Length);

                    int potentialStartQuoteIndex = _categories.Count - (_signals.Quote.Length * 2);

                    if (isEndQuote
                        && potentialStartQuoteIndex >= 0
                        && _categories[potentialStartQuoteIndex] == CharacterCategory.StartQuote)
                    {
                        ReviseCategories(CharacterCategory.EscapedQuote, _signals.Quote.Length * 2);
                    }
                }
                else if (IsNewline)
                {
                    ReviseCategories(CharacterCategory.Newline, _signals.NewLine.Length);
                }
            }

            private void ReviseCategories(CharacterCategory category, int length)
            {
                for (int i = length; i > 0; i--)
                    _categories[_categories.Count - i] = i == length ? category : CharacterCategory.Noop;
            }

            private bool InQuotes()
            {
                return _categories.AsEnumerable()
                    .Reverse()
                    .TakeWhile(category => category != CharacterCategory.EndQuote)
                    .Any(category => category == CharacterCategory.StartQuote);
            }

            private string[] ProduceLineResults()
            {
                List<string> results = new List<string>();
                StringBuilder result = new StringBuilder();

                bool inQuotes = false;
                int index = 0;

                foreach (CharacterCategory category in _categories)
                {
                    switch (category)
                    {
                        case CharacterCategory.Char:
                            result.Append(_buffer[index]);
                            break;

                        case CharacterCategory.Delimiter:
                            if (inQuotes)
                                result.Append(_signals.Delimiter);
                            else
                            {
                                results.Add(result.ToString());
                                result.Clear();
                                inQuotes = false;
                            }
                            break;

                        case CharacterCategory.StartQuote:
                            inQuotes = true;
                            break;

                        case CharacterCategory.EndQuote:
                            inQuotes = false;
                            break;

                        case CharacterCategory.EscapedQuote:
                            result.Append(_signals.Quote);
                            break;

                        case CharacterCategory.Newline:
                            if (inQuotes)
                                result.Append(_signals.NewLine);
                            break;

                        case CharacterCategory.Noop:
                            break;

                        default:
                            throw new NotSupportedException(category.ToString());
                    }

                    index++;
                }

                results.Add(result.ToString());

                _buffer.Clear();
                _categories.Clear();

                return results.ToArray();
            }

            private void ResetTrackers()
            {
                _delimiterTracker.Reset();
                _quoteTracker.Reset();
                _newlineTracker.Reset();
            }

            private StringQuotedSignals _signals;
            private StringBuilder _buffer = new StringBuilder();
            private List<CharacterCategory> _categories = new List<CharacterCategory>();

            private StringSignalTracker _delimiterTracker;
            private StringSignalTracker _quoteTracker;
            private StringSignalTracker _newlineTracker;

            private bool IsDelimiter { get { return _delimiterTracker.IsTriggered; } }
            private bool IsQuote { get { return _quoteTracker.IsTriggered; } }
            private bool IsNewline { get { return _newlineTracker.IsTriggered; } }

            private enum CharacterCategory : byte
            {
                Char,
                Delimiter,
                StartQuote,
                EndQuote,
                EscapedQuote,
                Newline,
                Noop
            }
        }
    }
}
﻿namespace FreeAndWithBeer.Text
{
    using System.Collections.Generic;
    using System.Linq;

    public static partial class CharExtensions
    {
        /// <summary>
        /// Creates and returns a new string containing all of the characters in the provided enumeration.
        /// </summary>
        public static string ToNewString(this IEnumerable<char> chars) => new string(chars.ToArray());
    }
}
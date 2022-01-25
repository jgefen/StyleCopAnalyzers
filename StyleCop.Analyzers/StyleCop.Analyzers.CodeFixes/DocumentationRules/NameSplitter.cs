// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.DocumentationRules
{
    using System.Collections.Generic;

    /// <summary>
    /// The name splitter.
    /// </summary>
    internal class NameSplitter
    {
        /// <summary>
        /// Splits name by upper character.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A list of words.</returns>
        public static List<string> Split(string name)
        {
            List<string> words = new List<string>();
            List<char> singleWord = new List<char>();

            foreach (char c in name)
            {
                if (char.IsUpper(c) && singleWord.Count > 0)
                {
                    words.Add(new string(singleWord.ToArray()));
                    singleWord.Clear();
                    singleWord.Add(c);
                }
                else
                {
                    singleWord.Add(c);
                }
            }

            words.Add(new string(singleWord.ToArray()));

            return words;
        }
    }
}

namespace StyleCop.Analyzers.DocumentationRules
{
    using System.Collections.Generic;

    public static class CommentHelper
    {
        /// <summary>
        /// Creates method comment.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The method comment.</returns>
        public static string CreateMethodComment(string name)
        {
            List<string> parts = SplitNameAndToLower(name, false);
            // parts[0] = Pluralizer.Pluralize(parts[0]);
            parts.Insert(1, "the");
            return string.Join(" ", parts) + ".";
        }

        private static List<string> SplitNameAndToLower(string name, bool isFirstCharacterLower)
        {
            List<string> parts = NameSplitter.Split(name);

            int i = isFirstCharacterLower ? 0 : 1;
            for (; i < parts.Count; i++)
            {
                parts[i] = parts[i].ToLower();
            }

            return parts;
        }
    }
}

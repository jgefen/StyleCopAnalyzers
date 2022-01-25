namespace StyleCop.Analyzers.DocumentationRules
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            // parts[0] = Pluralizer.Pluralize(parts[0]); //TODO: add pluralize
            return string.Join(" ", parts) + ".";
        }

        /// <summary>
        /// Creates parameter comment.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The parameter comment.</returns>
        public static string CreateParameterComment(ParameterSyntax parameter)
        {
            bool isBoolean = false;
            if (parameter.Type.IsKind(SyntaxKind.PredefinedType))
            {
                isBoolean = (parameter.Type as PredefinedTypeSyntax).Keyword.IsKind(SyntaxKind.BoolKeyword);
            }
            else if (parameter.Type.IsKind(SyntaxKind.NullableType))
            {
                var type = (parameter.Type as NullableTypeSyntax).ElementType as PredefinedTypeSyntax;

                // If it is not predefined type syntax, it should be IdentifierNameSyntax.
                if (type != null)
                {
                    isBoolean = type.Keyword.IsKind(SyntaxKind.BoolKeyword);
                }
            }

            if (isBoolean)
            {
                return "If true, " + string.Join(" ", SplitNameAndToLower(parameter.Identifier.ValueText, true)) + ".";
            }
            else
            {
                return CreateCommonComment(parameter.Identifier.ValueText);
            }
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

        private static string CreateCommonComment(string name)
        {
            return $"The {string.Join(" ", SplitNameAndToLower(name, true))}.";
        }
    }
}

namespace StyleCop.Analyzers.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using StyleCop.Analyzers.DocumentationRules;

    internal static class CommentContentHelper
    {
        /// <summary>
        /// Creates method comment.
        /// </summary>
        /// <param name="name">The method name.</param>
        /// <returns>The method comment.</returns>
        public static string CreateMethodSummeryText(string name)
        {
            return string.Join(" ", SplitNameAndToLower(name, false)) + ".";
        }

        /// <summary>
        /// Creates parameter comment.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>The parameter comment.</returns>
        public static string CreateParameterSummeryText(ParameterSyntax parameter)
        {
            if (IsBooleanParameter(parameter))
            {
                return "If true, " + string.Join(" ", SplitNameAndToLower(parameter.Identifier.ValueText, true)) + ".";
            }
            else
            {
                return CreateCommonComment(parameter.Identifier.ValueText);
            }
        }

        /// <summary>
        /// Creates type parameter comment.
        /// </summary>
        /// <param name="parameter">The type parameter.</param>
        /// <returns>The parameter comment.</returns>
        public static string CreateTypeParameterComment(TypeParameterSyntax parameter)
        {
            // TODO: check where type.
            var typeParamName = SplitNameAndToLower(parameter.Identifier.Text, true)
                .SkipWhile((s, i) => i == 0 && s.Length == 1);

            return $"The {string.Join(" ", typeParamName)} parameter type.";
        }

        /// <summary>
        /// Create return element documentation content
        /// </summary>
        /// <param name="returnType">The return type</param>
        /// <returns>The parameter comment</returns>
        public static XmlElementSyntax CreateReturnElementSyntax(TypeSyntax returnType)
        {
            var returnComment = ReturnCommentConstruction.GetReturnCommentConstruction(returnType);
            XmlNameSyntax xmlReturnsText = SyntaxFactory.XmlName("returns");
            XmlElementStartTagSyntax startTag = SyntaxFactory.XmlElementStartTag(xmlReturnsText);
            XmlTextSyntax contentText = SyntaxFactory.XmlText(returnComment);
            XmlElementEndTagSyntax endTag = SyntaxFactory.XmlElementEndTag(xmlReturnsText);
            return SyntaxFactory.XmlElement(startTag, SyntaxFactory.SingletonList<XmlNodeSyntax>(contentText), endTag);
        }

        private static bool IsBooleanParameter(ParameterSyntax parameter)
        {
            if (parameter.Type.IsKind(SyntaxKind.PredefinedType))
            {
                return ((PredefinedTypeSyntax)parameter.Type).Keyword.IsKind(SyntaxKind.BoolKeyword);
            }
            else if (parameter.Type.IsKind(SyntaxKind.NullableType))
            {
                // If it is not predefined type syntax, it should be IdentifierNameSyntax.
                if (((NullableTypeSyntax)parameter.Type).ElementType is PredefinedTypeSyntax type)
                {
                    return type.Keyword.IsKind(SyntaxKind.BoolKeyword);
                }
            }

            return false;
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

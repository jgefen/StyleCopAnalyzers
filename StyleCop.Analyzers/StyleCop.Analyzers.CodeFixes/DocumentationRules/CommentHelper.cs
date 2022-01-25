namespace StyleCop.Analyzers.DocumentationRules
{
    using System;
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
        /// <param name="parameter">The parameter.</param>
        /// <returns>The parameter comment.</returns>
        public static string CreateParameterComment(ParameterSyntax parameter)
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
        /// Creates return part nodes.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>An array of XmlNodeSyntaxes.</returns>
        public static XmlNodeSyntax[] CreateReturnPartNodes(string content)
        {
            ///[0] <returns></returns>[1][2]

            XmlTextSyntax lineStartText = CreateLineStartTextSyntax();

            XmlElementSyntax returnElement = CreateReturnElementSyntax(content);

            XmlTextSyntax lineEndText = CreateLineEndTextSyntax();

            return new XmlNodeSyntax[] { lineStartText, returnElement, lineEndText };
        }

        private static XmlTextSyntax CreateLineEndTextSyntax()
        {
            /*
                /// <summary>
                /// The code fix provider.
                /// </summary>[0]
            */

            // [0] end line token.
            SyntaxToken xmlTextNewLineToken = CreateNewLineToken();
            XmlTextSyntax xmlText = SyntaxFactory.XmlText(xmlTextNewLineToken);
            return xmlText;
        }

        private static SyntaxToken CreateNewLineToken()
        {
            return SyntaxFactory.XmlTextNewLine(Environment.NewLine, false);
        }

        private static XmlTextSyntax CreateLineStartTextSyntax()
        {
            /*
                ///[0] <summary>
                /// The code fix provider.
                /// </summary>
            */

            // [0] " " + leading comment exterior trivia
            SyntaxTriviaList xmlText0Leading = CreateCommentExterior();
            SyntaxToken xmlText0LiteralToken = SyntaxFactory.XmlTextLiteral(xmlText0Leading, " ", " ", SyntaxFactory.TriviaList());
            XmlTextSyntax xmlText0 = SyntaxFactory.XmlText(xmlText0LiteralToken);
            return xmlText0;
        }

        private static XmlElementSyntax CreateReturnElementSyntax(string content)
        {
            XmlNameSyntax xmlName = SyntaxFactory.XmlName("returns");
            /// <returns>[0]xxx[1]</returns>[2]

            XmlElementStartTagSyntax startTag = SyntaxFactory.XmlElementStartTag(xmlName);

            XmlTextSyntax contentText = SyntaxFactory.XmlText(content);

            XmlElementEndTagSyntax endTag = SyntaxFactory.XmlElementEndTag(xmlName);
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

        private static SyntaxTriviaList CreateCommentExterior()
        {
            return SyntaxFactory.TriviaList(SyntaxFactory.DocumentationCommentExterior("///"));
        }
    }
}

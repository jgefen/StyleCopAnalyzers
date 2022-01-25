// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using StyleCop.Analyzers.DocumentationRules;

    internal static class CommonDocumentationHelper
    {
        public static List<string> SplitNameAndToLower(string name, bool isFirstCharacterLower, bool skipSingleCharIfFirst = false)
        {
            return NameSplitter.Split(name)
                .Select((n, i) => !isFirstCharacterLower && i == 0 ? n : n.ToLower())
                .SkipWhile((n, i) => skipSingleCharIfFirst && i == 0 && n.Length == 1)
                .ToList();
        }

        public static bool IsBooleanParameter(TypeSyntax type)
        {
            if (type.IsKind(SyntaxKind.PredefinedType))
            {
                return ((PredefinedTypeSyntax)type).Keyword.IsKind(SyntaxKind.BoolKeyword);
            }
            else if (type.IsKind(SyntaxKind.NullableType))
            {
                // If it is not predefined type syntax, it should be IdentifierNameSyntax.
                if (((NullableTypeSyntax)type).ElementType is PredefinedTypeSyntax predefinedType)
                {
                    return predefinedType.Keyword.IsKind(SyntaxKind.BoolKeyword);
                }
            }

            return false;
        }

        public static string CreateCommonComment(string name, bool skipSingleCharIfFirst = false)
        {
            return $"The {string.Join(" ", SplitNameAndToLower(name, true, skipSingleCharIfFirst))}.";
        }
    }
}

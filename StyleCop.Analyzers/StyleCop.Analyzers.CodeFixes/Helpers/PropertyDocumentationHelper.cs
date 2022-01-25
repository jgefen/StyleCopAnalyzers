// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.Helpers
{
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class PropertyDocumentationHelper
    {
        public static string CreatePropertyComment(
            PropertyDeclarationSyntax propertyDeclaration,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var propertyName = propertyDeclaration.Identifier.ValueText;
            string comment = "Gets";
            if (PropertyAnalyzerHelper.AnalyzePropertyAccessors(propertyDeclaration, semanticModel, cancellationToken).SetterVisible)
            {
                comment += " or sets";
            }

            if (CommonDocumentationHelper.IsBooleanParameter(propertyDeclaration.Type))
            {
                comment += CreatePropertyBooleanPart(propertyName);
            }
            else
            {
                comment += " the " + string.Join(" ", CommonDocumentationHelper.SplitNameAndToLower(propertyName, true));
            }

            return comment + ".";
        }

        private static string CreatePropertyBooleanPart(string name)
        {
            string booleanPart = " a value indicating whether ";

            var parts = CommonDocumentationHelper.SplitNameAndToLower(name, true).ToList();

            string isWord = parts.FirstOrDefault(o => o == "is");
            if (isWord != null)
            {
                parts.Remove(isWord);
                parts.Insert(parts.Count - 1, isWord);
            }

            booleanPart += string.Join(" ", parts);
            return booleanPart;
        }
    }
}

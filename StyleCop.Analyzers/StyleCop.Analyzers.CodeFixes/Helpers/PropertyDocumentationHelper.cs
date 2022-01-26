// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.Helpers
{
    using System;
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
                comment += " the " + CommonDocumentationHelper.SplitNameAndToLower(propertyName, true);
            }

            return comment + ".";
        }

        private static string CreatePropertyBooleanPart(string name)
        {
            string booleanPart = " a value indicating whether ";

            var nameDocumentation = CommonDocumentationHelper.SplitNameAndToLower(name, true);

            var isWord = nameDocumentation.IndexOf("is", StringComparison.OrdinalIgnoreCase);
            if (isWord != -1)
            {
                nameDocumentation = nameDocumentation.Remove(isWord, 2) + " is";
            }

            return booleanPart + nameDocumentation;
        }
    }
}

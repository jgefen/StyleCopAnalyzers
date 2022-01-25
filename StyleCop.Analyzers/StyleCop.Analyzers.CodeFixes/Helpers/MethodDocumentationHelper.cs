// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.Helpers
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class MethodDocumentationHelper
    {
        /// <summary>
        /// Creates method comment.
        /// </summary>
        /// <param name="name">The method name.</param>
        /// <returns>The method comment.</returns>
        public static string CreateMethodSummeryText(string name)
        {
            return string.Join(" ", CommonDocumentationHelper.SplitNameAndToLower(name, false)) + ".";
        }

        /// <summary>
        /// Creates parameter comment.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>The parameter comment.</returns>
        public static string CreateParameterSummeryText(ParameterSyntax parameter)
        {
            if (CommonDocumentationHelper.IsBooleanParameter(parameter.Type))
            {
                return "If true, " + string.Join(" ", CommonDocumentationHelper.SplitNameAndToLower(parameter.Identifier.ValueText, true)) + ".";
            }
            else
            {
                return CommonDocumentationHelper.CreateCommonComment(parameter.Identifier.ValueText);
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
            var typeParamName = CommonDocumentationHelper.SplitNameAndToLower(parameter.Identifier.Text, true, true);
            return $"The {string.Join(" ", typeParamName)} parameter type.";
        }

        /// <summary>
        /// Create return element documentation content.
        /// </summary>
        /// <param name="returnType">The return type.</param>
        /// <returns>The parameter comment.</returns>
        public static XmlTextSyntax CreateReturnElementSyntax(TypeSyntax returnType)
        {
            return XmlSyntaxFactory.Text(GetReturnDocumentationText(returnType));
        }

        private static string GetReturnDocumentationText(TypeSyntax returnType)
        {
            return returnType switch
            {
                PredefinedTypeSyntax predefinedType => GeneratePredefinedTypeComment(predefinedType),
                IdentifierNameSyntax identifierNameSyntax => GenerateIdentifierNameTypeComment(identifierNameSyntax),
                QualifiedNameSyntax qualifiedNameSyntax => GenerateQualifiedNameTypeComment(qualifiedNameSyntax),
                GenericNameSyntax genericNameSyntax => GenerateGenericTypeComment(genericNameSyntax),
                ArrayTypeSyntax arrayTypeSyntax => GenerateArrayTypeComment(arrayTypeSyntax),
                _ => GenerateGeneralComment(returnType.ToFullString()),
            };
        }

        private static string GeneratePredefinedTypeComment(PredefinedTypeSyntax returnType)
        {
            return DetermineStartedWord(returnType.Keyword.ValueText) + " " + returnType.Keyword.ValueText + ".";
        }

        private static string GenerateIdentifierNameTypeComment(IdentifierNameSyntax returnType)
        {
            return GenerateGeneralComment(returnType.Identifier.ValueText);
        }

        private static string GenerateQualifiedNameTypeComment(QualifiedNameSyntax returnType)
        {
            return GenerateGeneralComment(returnType.ToString());
        }

        private static string GenerateArrayTypeComment(ArrayTypeSyntax arrayTypeSyntax)
        {
            return "An array of " + DetermineSpecificObjectName(arrayTypeSyntax.ElementType);
        }

        private static string GenerateGenericTypeComment(GenericNameSyntax returnType)
        {
            string genericTypeStr = returnType.Identifier.ValueText;
            if (genericTypeStr.Contains("ReadOnlyCollection"))
            {
                return "A read only collection of " + DetermineSpecificObjectName(returnType.TypeArgumentList.Arguments.First());
            }

            // IEnumerable IList List
            if (genericTypeStr == "IEnumerable" || genericTypeStr.Contains("List"))
            {
                return "A list of " + DetermineSpecificObjectName(returnType.TypeArgumentList.Arguments.First());
            }

            if (genericTypeStr.Contains("Dictionary"))
            {
                return GenerateGeneralComment(genericTypeStr);
            }

            return GenerateGeneralComment(genericTypeStr);
        }

        private static string GenerateGeneralComment(string returnType)
        {
            return DetermineStartedWord(returnType) + " " + returnType + ".";
        }

        private static string DetermineSpecificObjectName(TypeSyntax specificType)
        {
            string result = null;
            if (specificType is IdentifierNameSyntax)
            {
                // result = Pluralizer.Pluralize(((IdentifierNameSyntax)specificType).Identifier.ValueText);
            }
            else if (specificType is PredefinedTypeSyntax predefinedTypeSyntax)
            {
                result = predefinedTypeSyntax.Keyword.ValueText;
            }
            else if (specificType is GenericNameSyntax genericNameSyntax)
            {
                result = genericNameSyntax.Identifier.ValueText;
            }
            else
            {
                result = specificType.ToFullString();
            }

            return result + ".";
        }

        private static string DetermineStartedWord(string returnType)
        {
            var vowelChars = new List<char>() { 'a', 'e', 'i', 'o', 'u' };
            if (vowelChars.Contains(char.ToLower(returnType[0])))
            {
                return "An";
            }
            else
            {
                return "A";
            }
        }
    }
}

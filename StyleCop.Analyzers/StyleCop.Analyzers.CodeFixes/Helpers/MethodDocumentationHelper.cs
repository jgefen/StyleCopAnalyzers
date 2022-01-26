﻿// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Simplification;
    using StyleCop.Analyzers.DocumentationRules;

    internal static class MethodDocumentationHelper
    {
        /// <summary>
        /// Creates method comment.
        /// </summary>
        /// <param name="name">The method name.</param>
        /// <param name="newLineText">The new line text.</param>
        /// <returns>The method comment.</returns>
        public static XmlNodeSyntax CreateMethodSummeryText(string name, string newLineText)
        {
            return CommonDocumentationHelper.CreateSummeryNode(CommonDocumentationHelper.GetNameDocumentation(name, false), newLineText);
        }

        /// <summary>
        /// Creates the throw documentation.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="newLine">The new line.</param>
        /// <returns>The xml node syntax list.</returns>
        public static IEnumerable<XmlNodeSyntax> CreateThrowDocumentation(SyntaxNode expression, string newLine)
        {
            var throwStatements = expression.DescendantNodes().OfType<ThrowStatementSyntax>();
            foreach (var throwStatement in throwStatements)
            {
                if (throwStatement.Expression is not ObjectCreationExpressionSyntax objectCreationExpression)
                {
                    continue;
                }

                var exceptionType = objectCreationExpression.Type;
                yield return XmlSyntaxFactory.NewLine(newLine);
                yield return XmlSyntaxFactory.ExceptionElement(SyntaxFactory.TypeCref(exceptionType));
            }
        }

        /// <summary>
        /// Creates the return documentation.
        /// </summary>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="returnType">Type of the return.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="newLineText">The new line text.</param>
        /// <returns>The list of xml node syntax for the return documentation.</returns>
        public static IEnumerable<XmlNodeSyntax> CreateReturnDocumentation(
            SemanticModel semanticModel,
            TypeSyntax returnType,
            CancellationToken cancellationToken,
            string newLineText)
        {
            if (semanticModel.GetSymbolInfo(returnType, cancellationToken).Symbol is not ITypeSymbol typeSymbol)
            {
                return Enumerable.Empty<XmlNodeSyntax>();
            }

            if (TaskHelper.IsTaskReturningType(semanticModel, returnType, cancellationToken))
            {
                TypeSyntax typeName;

                if (((INamedTypeSymbol)typeSymbol).IsGenericType)
                {
                    typeName = SyntaxFactory.ParseTypeName("global::System.Threading.Tasks.Task<TResult>");
                }
                else
                {
                    typeName = SyntaxFactory.ParseTypeName("global::System.Threading.Tasks.Task");
                }

                XmlNodeSyntax[] returnContent =
                {
                    XmlSyntaxFactory.Text(DocumentationResources.TaskReturnElementFirstPart),
                    XmlSyntaxFactory.SeeElement(SyntaxFactory.TypeCref(typeName)).WithAdditionalAnnotations(Simplifier.Annotation),
                    XmlSyntaxFactory.Text(DocumentationResources.TaskReturnElementSecondPart),
                };

                return CreateReturnDocumentation(newLineText, returnContent);
            }
            else if (typeSymbol.SpecialType != SpecialType.System_Void)
            {
                var returnDocumentationContent = CreateReturnDocumentationContent(returnType);
                return CreateReturnDocumentation(newLineText, returnDocumentationContent);
            }

            return Enumerable.Empty<XmlNodeSyntax>();
        }

        /// <summary>
        /// Creates the return documentation.
        /// </summary>
        /// <param name="newLineText">The new line text.</param>
        /// <param name="returnContent">Content of the return.</param>
        /// <returns>Create return documentation.</returns>
        public static IEnumerable<XmlNodeSyntax> CreateReturnDocumentation(
            string newLineText,
            params XmlNodeSyntax[] returnContent)
        {
            yield return XmlSyntaxFactory.NewLine(newLineText);
            yield return XmlSyntaxFactory.ReturnsElement(returnContent);
        }

        /// <summary>
        /// Creates the type parameters documentation.
        /// </summary>
        /// <param name="typeParametersList">The type parameters list.</param>
        /// <param name="newLineText">The new line text.</param>
        /// <returns>The list of xml node syntax for the type parameters documentation.</returns>
        public static IEnumerable<XmlNodeSyntax> CreateTypeParametersDocumentation(
            TypeParameterListSyntax typeParametersList,
            string newLineText)
        {
            if (typeParametersList == null)
            {
                yield break;
            }

            foreach (var typeParameter in typeParametersList.Parameters)
            {
                yield return XmlSyntaxFactory.NewLine(newLineText);
                var paramDocumentation = XmlSyntaxFactory.Text(CreateTypeParameterComment(typeParameter));
                yield return XmlSyntaxFactory.TypeParamElement(typeParameter.Identifier.ValueText, paramDocumentation);
            }
        }

        /// <summary>
        /// Creates the parameters documentation.
        /// </summary>
        /// <param name="parametersList">The parameters list.</param>
        /// <param name="newLineText">The new line text.</param>
        /// <returns>The list of xml node syntax for the parameters documentation.</returns>
        public static IEnumerable<XmlNodeSyntax> CreateParametersDocumentation(IEnumerable<ParameterSyntax> parametersList, string newLineText)
        {
            if (parametersList == null)
            {
                yield break;
            }

            foreach (var parameter in parametersList)
            {
                yield return XmlSyntaxFactory.NewLine(newLineText);
                var paramDocumentation = XmlSyntaxFactory.Text(MethodDocumentationHelper.CreateParameterSummeryText(parameter));
                yield return XmlSyntaxFactory.ParamElement(parameter.Identifier.ValueText, paramDocumentation);
            }
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
            var objectName = specificType switch
            {
                IdentifierNameSyntax identifierNameSyntax => identifierNameSyntax.Identifier.ValueText, // TODO: Pluralizer.Pluralize
                PredefinedTypeSyntax predefinedTypeSyntax => predefinedTypeSyntax.Keyword.ValueText,
                GenericNameSyntax genericNameSyntax => genericNameSyntax.Identifier.ValueText,
                _ => specificType.ToFullString(),
            };

            return objectName + ".";
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

        private static string CreateParameterSummeryText(ParameterSyntax parameter)
        {
            if (CommonDocumentationHelper.IsBooleanParameter(parameter.Type))
            {
                return "If true, " + CommonDocumentationHelper.GetNameDocumentation(parameter.Identifier.ValueText);
            }
            else
            {
                return CommonDocumentationHelper.CreateCommonComment(parameter.Identifier.ValueText);
            }
        }

        private static string CreateTypeParameterComment(TypeParameterSyntax parameter)
        {
            var typeParamName = CommonDocumentationHelper.SplitNameAndToLower(parameter.Identifier.Text, true, true);
            var prefix = "The type of ";
            if (typeParamName.Length == 1)
            {
                return prefix + typeParamName.ToUpper() + ".";
            }
            else
            {
                return prefix + "the " + typeParamName + ".";
            }
        }

        private static XmlTextSyntax CreateReturnDocumentationContent(TypeSyntax returnType)
        {
            return XmlSyntaxFactory.Text(GetReturnDocumentationText(returnType));
        }
    }
}

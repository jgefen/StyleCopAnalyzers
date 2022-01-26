// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.DocumentationRules
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Simplification;
    using StyleCop.Analyzers.Helpers;

    /// <summary>
    /// Implements a code fix that will generate a documentation comment comprised of an empty
    /// <c>&lt;inheritdoc/&gt;</c> element.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SA1600CodeFixProvider))]
    [Shared]
    internal class SA1600CodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                "CS1591",
                SA1600ElementsMustBeDocumented.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider()
        {
            return CustomFixAllProviders.BatchFixer;
        }

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            SemanticModel semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                SyntaxToken identifierToken = root.FindToken(diagnostic.Location.SourceSpan.Start);
                if (identifierToken.IsMissingOrDefault())
                {
                    continue;
                }

                switch (identifierToken.Parent.Kind())
                {
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.ConstructorDocumentationCodeFix,
                            cancellationToken =>
                                GetConstructorOrDestructorDocumentationTransformedDocumentAsync(
                                    context.Document,
                                    root,
                                    (BaseMethodDeclarationSyntax)identifierToken.Parent,
                                    cancellationToken),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);
                    break;

                case SyntaxKind.MethodDeclaration:
                    MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax)identifierToken.Parent;
                    if (!IsCoveredByInheritDoc(semanticModel, methodDeclaration, context.CancellationToken))
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                DocumentationResources.MethodDocumentationCodeFix,
                                cancellationToken => GetMethodDocumentationTransformedDocumentAsync(
                                    context.Document,
                                    root,
                                    semanticModel,
                                    (MethodDeclarationSyntax)identifierToken.Parent,
                                    cancellationToken),
                                nameof(SA1600CodeFixProvider)),
                            diagnostic);
                    }

                    break;

                case SyntaxKind.DelegateDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.DelegateDocumentationCodeFix,
                            cancellationToken => GetDelegateDocumentationTransformedDocumentAsync(
                                context.Document,
                                root,
                                semanticModel,
                                (DelegateDeclarationSyntax)identifierToken.Parent,
                                cancellationToken),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);

                    break;

                case SyntaxKind.PropertyDeclaration:
                    var propertyDeclaration = (PropertyDeclarationSyntax)identifierToken.Parent;
                    if (!IsCoveredByInheritDoc(semanticModel, propertyDeclaration, context.CancellationToken))
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                DocumentationResources.PropertyDocumentationCodeFix,
                                cancellationToken => GetPropertyDocumentationTransformedDocumentAsync(
                                    context.Document,
                                    root,
                                    semanticModel,
                                    (PropertyDeclarationSyntax)identifierToken.Parent,
                                    cancellationToken),
                                nameof(SA1600CodeFixProvider)),
                            diagnostic);
                    }

                    break;

                case SyntaxKind.ClassDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.ClassDocumentationCodeFix,
                            _ => GetCommonGenericTypeDocumentationTransformedDocumentAsync(
                                context.Document,
                                root,
                                (TypeDeclarationSyntax)identifierToken.Parent,
                                ((BaseTypeDeclarationSyntax)identifierToken.Parent).Identifier),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);

                    break;

                case SyntaxKind.InterfaceDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.InterfaceDocumentationCodeFix,
                            _ => GetCommonGenericTypeDocumentationTransformedDocumentAsync(
                                context.Document,
                                root,
                                (TypeDeclarationSyntax)identifierToken.Parent,
                                ((BaseTypeDeclarationSyntax)identifierToken.Parent).Identifier),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);

                    break;

                case SyntaxKind.StructDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.StructDocumentationCodeFix,
                            _ => GetCommonGenericTypeDocumentationTransformedDocumentAsync(
                                context.Document,
                                root,
                                (TypeDeclarationSyntax)identifierToken.Parent,
                                ((BaseTypeDeclarationSyntax)identifierToken.Parent).Identifier),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);

                    break;

                case SyntaxKind.EnumDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.EnumDocumentationCodeFix,
                            _ => GetCommonTypeDocumentationTransformedDocumentAsync(
                                context.Document,
                                root,
                                identifierToken.Parent,
                                ((BaseTypeDeclarationSyntax)identifierToken.Parent).Identifier),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);

                    break;

                case SyntaxKind.VariableDeclarator:
                    var fieldDeclaration = identifierToken.Parent.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();
                    if (fieldDeclaration != null)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                DocumentationResources.FieldDocumentationCodeFix,
                                _ => GetCommonTypeDocumentationTransformedDocumentAsync(
                                    context.Document,
                                    root,
                                    identifierToken.Parent.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault(),
                                    identifierToken.Parent.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().First().Identifier),
                                nameof(SA1600CodeFixProvider)),
                            diagnostic);
                    }

                    var eventDeclaration = identifierToken.Parent.Ancestors().OfType<EventFieldDeclarationSyntax>().FirstOrDefault();
                    if (eventDeclaration != null)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                DocumentationResources.EventDocumentationCodeFix,
                                _ => GetEventFieldDocumentationTransformedDocumentAsync(
                                    context.Document,
                                    root,
                                    identifierToken.Parent.Ancestors().OfType<EventFieldDeclarationSyntax>().FirstOrDefault()),
                                nameof(SA1600CodeFixProvider)),
                            diagnostic);
                    }

                    break;

                case SyntaxKind.IndexerDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.IndexerDocumentationCodeFix,
                            cancellationToken => GetIndexerDocumentationTransformedDocumentAsync(
                                context.Document,
                                root,
                                semanticModel,
                                (IndexerDeclarationSyntax)identifierToken.Parent,
                                cancellationToken),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);

                    break;

                case SyntaxKind.EventDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.EventDocumentationCodeFix,
                            _ => GetEventDocumentationTransformedDocumentAsync(
                                context.Document,
                                root,
                                (EventDeclarationSyntax)identifierToken.Parent),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);

                    break;
                }
            }
        }

        private static Task<Document> GetEventDocumentationTransformedDocumentAsync(
            Document document,
            SyntaxNode root,
            EventDeclarationSyntax eventDeclaration)
        {
            return GetEventDocumentationTransformedDocument(
                document,
                root,
                eventDeclaration,
                eventDeclaration.Identifier);
        }

        private static Task<Document> GetEventFieldDocumentationTransformedDocumentAsync(
            Document document,
            SyntaxNode root,
            EventFieldDeclarationSyntax eventDeclaration)
        {
            return GetEventDocumentationTransformedDocument(
                document,
                root,
                eventDeclaration,
                eventDeclaration.Declaration.Variables.First().Identifier);
        }

        private static Task<Document> GetEventDocumentationTransformedDocument(
            Document document,
            SyntaxNode root,
            SyntaxNode eventDeclaration,
            SyntaxToken identifier)
        {
            string newLineText = GetNewLineText(document);

            var documentationText = EventDocumentationHelper.CreateEventDocumentation(identifier);
            var documentationNode = CreateSummeryNode(documentationText, newLineText);

            return Task.FromResult(
                CreateCommentAndReplaceInDocument(document, root, eventDeclaration, newLineText, documentationNode));
        }

        private static Task<Document> GetIndexerDocumentationTransformedDocumentAsync(
            Document document,
            SyntaxNode root,
            SemanticModel semanticModel,
            IndexerDeclarationSyntax indexerDeclaration,
            CancellationToken cancellationToken)
        {
            string newLineText = GetNewLineText(document);

            var documentationNodes = new List<XmlNodeSyntax>();
            var indexerSummeryText = PropertyDocumentationHelper.CreateIndexerSummeryComment(indexerDeclaration, semanticModel, cancellationToken);
            documentationNodes.Add(CreateSummeryNode(indexerSummeryText, newLineText));
            documentationNodes.AddRange(CreateParametersDocumentation(indexerDeclaration.ParameterList?.Parameters, newLineText));
            documentationNodes.Add(XmlSyntaxFactory.NewLine(newLineText));
            documentationNodes.Add(XmlSyntaxFactory.ReturnsElement(XmlSyntaxFactory.Text(DocumentationResources.IndexerReturnDocumentation)));

            return Task.FromResult(CreateCommentAndReplaceInDocument(document, root, indexerDeclaration, newLineText, documentationNodes.ToArray()));
        }

        private static Task<Document> GetCommonTypeDocumentationTransformedDocumentAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode declaration,
            SyntaxToken declarationIdentifier)
        {
            string newLineText = GetNewLineText(document);

            var documentationText = CommonDocumentationHelper.CreateCommonComment(declarationIdentifier.ValueText, declaration.Kind() == SyntaxKind.InterfaceDeclaration);
            var documentationNode = CreateSummeryNode(documentationText, newLineText);

            return Task.FromResult(CreateCommentAndReplaceInDocument(document, root, declaration, newLineText, documentationNode));
        }

        private static Task<Document> GetCommonGenericTypeDocumentationTransformedDocumentAsync(
            Document document,
            SyntaxNode root,
            TypeDeclarationSyntax declaration,
            SyntaxToken declarationIdentifier)
        {
            string newLineText = GetNewLineText(document);

            var documentationList = new List<XmlNodeSyntax>();
            var documentationText = CommonDocumentationHelper.CreateCommonComment(declarationIdentifier.ValueText, declaration.Kind() == SyntaxKind.InterfaceDeclaration);
            documentationList.Add(CreateSummeryNode(documentationText, newLineText));
            documentationList.AddRange(CreateTypeParametersDocumentation(declaration.TypeParameterList, newLineText));

            return Task.FromResult(CreateCommentAndReplaceInDocument(document, root, declaration, newLineText, documentationList.ToArray()));
        }

        private static Task<Document> GetPropertyDocumentationTransformedDocumentAsync(
            Document document,
            SyntaxNode root,
            SemanticModel semanticModel,
            PropertyDeclarationSyntax propertyDeclaration,
            CancellationToken cancellationToken)
        {
            string newLineText = GetNewLineText(document);

            var propertyDocumentationText = PropertyDocumentationHelper.CreatePropertySummeryComment(propertyDeclaration, semanticModel, cancellationToken);
            var propertyDocumentationNode = CreateSummeryNode(propertyDocumentationText, newLineText);

            return Task.FromResult(CreateCommentAndReplaceInDocument(document, root, propertyDeclaration, newLineText, propertyDocumentationNode));
        }

        private static Document CreateCommentAndReplaceInDocument(
            Document document,
            SyntaxNode root,
            SyntaxNode declarationNode,
            string newLineText,
            params XmlNodeSyntax[] documentationNodes)
        {
            var leadingTrivia = declarationNode.GetLeadingTrivia();
            int insertionIndex = GetInsertionIndex(ref leadingTrivia);

            var documentationComment = XmlSyntaxFactory.DocumentationComment(newLineText, documentationNodes);
            var trivia = SyntaxFactory.Trivia(documentationComment);

            SyntaxTriviaList newLeadingTrivia = leadingTrivia.Insert(insertionIndex, trivia);
            SyntaxNode newElement = declarationNode.WithLeadingTrivia(newLeadingTrivia);
            return document.WithSyntaxRoot(root.ReplaceNode(declarationNode, newElement));
        }

        private static bool IsCoveredByInheritDoc(SemanticModel semanticModel, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            if (methodDeclaration.ExplicitInterfaceSpecifier != null)
            {
                return true;
            }

            if (methodDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword))
            {
                return true;
            }

            ISymbol declaredSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
            return (declaredSymbol != null) && NamedTypeHelpers.IsImplementingAnInterfaceMember(declaredSymbol);
        }

        private static bool IsCoveredByInheritDoc(SemanticModel semanticModel, PropertyDeclarationSyntax propertyDeclaration, CancellationToken cancellationToken)
        {
            if (propertyDeclaration.ExplicitInterfaceSpecifier != null)
            {
                return true;
            }

            if (propertyDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword))
            {
                return true;
            }

            ISymbol declaredSymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken);
            return (declaredSymbol != null) && NamedTypeHelpers.IsImplementingAnInterfaceMember(declaredSymbol);
        }

        private static Task<Document> GetConstructorOrDestructorDocumentationTransformedDocumentAsync(Document document, SyntaxNode root, BaseMethodDeclarationSyntax declaration, CancellationToken cancellationToken)
        {
            SyntaxTriviaList leadingTrivia = declaration.GetLeadingTrivia();
            int insertionIndex = GetInsertionIndex(ref leadingTrivia);

            string newLineText = GetNewLineText(document);

            var documentationNodes = new List<XmlNodeSyntax>();

            var typeDeclaration = declaration.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>();
            var standardText = SA1642SA1643CodeFixProvider.GenerateStandardText(document, declaration, typeDeclaration, cancellationToken);
            var standardTextSyntaxList = SA1642SA1643CodeFixProvider.BuildStandardTextSyntaxList(typeDeclaration, newLineText, standardText[0], standardText[1]);

            // Remove the empty line generated by build standard text, as this is not needed with constructing a new summary element.
            standardTextSyntaxList = standardTextSyntaxList.RemoveAt(0);

            documentationNodes.Add(XmlSyntaxFactory.SummaryElement(newLineText, standardTextSyntaxList));

            var parametersDocumentation = CreateParametersDocumentation(declaration.ParameterList?.Parameters, newLineText);
            documentationNodes.AddRange(parametersDocumentation);

            var documentationComment =
                XmlSyntaxFactory.DocumentationComment(
                    newLineText,
                    documentationNodes.ToArray());
            var trivia = SyntaxFactory.Trivia(documentationComment);

            SyntaxTriviaList newLeadingTrivia = leadingTrivia.Insert(insertionIndex, trivia);
            SyntaxNode newElement = declaration.WithLeadingTrivia(newLeadingTrivia);
            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(declaration, newElement)));
        }

        private static Task<Document> GetMethodDocumentationTransformedDocumentAsync(
            Document document,
            SyntaxNode root,
            SemanticModel semanticModel,
            MethodDeclarationSyntax methodDeclaration,
            CancellationToken cancellationToken)
        {
            var throwStatements = MethodDocumentationHelper.CreateThrowDocumentation(methodDeclaration.Body, GetNewLineText(document)).ToArray();
            return GetMethodDocumentationTransformedDocumentAsync(
                document,
                root,
                semanticModel,
                methodDeclaration,
                methodDeclaration.Identifier,
                methodDeclaration.TypeParameterList,
                methodDeclaration.ParameterList,
                methodDeclaration.ReturnType,
                cancellationToken,
                throwStatements);
        }

        private static Task<Document> GetDelegateDocumentationTransformedDocumentAsync(
            Document document,
            SyntaxNode root,
            SemanticModel semanticModel,
            DelegateDeclarationSyntax delegateDeclaration,
            CancellationToken cancellationToken)
        {
            return GetMethodDocumentationTransformedDocumentAsync(
                document,
                root,
                semanticModel,
                delegateDeclaration,
                delegateDeclaration.Identifier,
                delegateDeclaration.TypeParameterList,
                delegateDeclaration.ParameterList,
                delegateDeclaration.ReturnType,
                cancellationToken);
        }

        private static Task<Document> GetMethodDocumentationTransformedDocumentAsync(
            Document document,
            SyntaxNode root,
            SemanticModel semanticModel,
            SyntaxNode declaration,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            ParameterListSyntax parameterList,
            TypeSyntax returnType,
            CancellationToken cancellationToken,
            params XmlNodeSyntax[] additionalDocumentation)
        {
            string newLineText = GetNewLineText(document);

            var documentationNodes = new List<XmlNodeSyntax>();

            var summeryContent = MethodDocumentationHelper.CreateMethodSummeryText(identifier.ValueText);
            documentationNodes.Add(CreateSummeryNode(summeryContent, newLineText));

            documentationNodes.AddRange(CreateTypeParametersDocumentation(typeParameterList, newLineText));

            documentationNodes.AddRange(CreateParametersDocumentation(parameterList?.Parameters, newLineText));

            documentationNodes.AddRange(CreateReturnDocumentation(semanticModel, returnType, cancellationToken, newLineText));

            documentationNodes.AddRange(additionalDocumentation);

            return Task.FromResult(CreateCommentAndReplaceInDocument(document, root, declaration, newLineText, documentationNodes.ToArray()));
        }

        private static XmlNodeSyntax CreateSummeryNode(string summeryContent, string newLineText)
        {
            var summerySyntax = XmlSyntaxFactory.Text(summeryContent);
            return XmlSyntaxFactory.SummaryElement(newLineText, summerySyntax);
        }

        private static IEnumerable<XmlNodeSyntax> CreateReturnDocumentation(
            SemanticModel semanticModel,
            TypeSyntax returnType,
            CancellationToken cancellationToken,
            string newLineText)
        {
            if (semanticModel.GetSymbolInfo(returnType, cancellationToken).Symbol is not ITypeSymbol typeSymbol)
            {
                yield break;
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

                yield return XmlSyntaxFactory.NewLine(newLineText);
                yield return XmlSyntaxFactory.ReturnsElement(returnContent);
            }
            else if (typeSymbol.SpecialType != SpecialType.System_Void)
            {
                yield return XmlSyntaxFactory.NewLine(newLineText);
                var returnDocumentationContent = MethodDocumentationHelper.CreateReturnElementSyntax(returnType);
                yield return XmlSyntaxFactory.ReturnsElement(returnDocumentationContent);
            }
        }

        private static IEnumerable<XmlNodeSyntax> CreateTypeParametersDocumentation(
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
                var paramDocumentation = XmlSyntaxFactory.Text(MethodDocumentationHelper.CreateTypeParameterComment(typeParameter));
                yield return XmlSyntaxFactory.TypeParamElement(typeParameter.Identifier.ValueText, paramDocumentation);
            }
        }

        private static int GetInsertionIndex(ref SyntaxTriviaList leadingTrivia)
        {
            int insertionIndex = leadingTrivia.Count;
            while (insertionIndex > 0 && !leadingTrivia[insertionIndex - 1].HasBuiltinEndLine())
            {
                insertionIndex--;
            }

            return insertionIndex;
        }

        private static IEnumerable<XmlNodeSyntax> CreateParametersDocumentation(IEnumerable<ParameterSyntax> parametersList, string newLineText)
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

        private static string GetNewLineText(Document document)
        {
            return document.Project.Solution.Workspace.Options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);
        }
    }
}

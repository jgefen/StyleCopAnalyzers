// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.DocumentationRules
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;
    using StyleCop.Analyzers.Helpers;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SA1611CodeFixProvider))]
    [Shared]
    internal class SA1611CodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                SA1611ElementParametersMustBeDocumented.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider()
        {
            return CustomFixAllProviders.BatchFixer;
        }

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
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
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.ConstructorDocumentationCodeFix,
                            cancellationToken => GetConstructorDocumentationTransformedDocumentAsync(context.Document, root, (BaseMethodDeclarationSyntax)identifierToken.Parent, cancellationToken),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);
                    break;

                case SyntaxKind.MethodDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.MethodDocumentationCodeFix,
                            cancellationToken => GetMethodDocumentationTransformedDocumentAsync(context.Document, root, (BaseMethodDeclarationSyntax)identifierToken.Parent, cancellationToken),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);
                    break;

                case SyntaxKind.DelegateDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.DelegateDocumentationCodeFix,
                            cancellationToken => GetDelegateDocumentationTransformedDocumentAsync(context.Document, root, (DelegateDeclarationSyntax)identifierToken.Parent, cancellationToken),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);
                    break;
                case SyntaxKind.IndexerDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.IndexerDocumentationCodeFix,
                            cancellationToken => GetIndexerDocumentationTransformedDocumentAsync(context.Document, root, (IndexerDeclarationSyntax)identifierToken.Parent, cancellationToken),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);
                    break;
                }
            }
        }

        private Task<Document> GetIndexerDocumentationTransformedDocumentAsync(Document document, SyntaxNode root, IndexerDeclarationSyntax indexerDeclaration, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private Task<Document> GetDelegateDocumentationTransformedDocumentAsync(Document document, SyntaxNode root, DelegateDeclarationSyntax delegateDeclaration, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private Task<Document> GetMethodDocumentationTransformedDocumentAsync(Document document, SyntaxNode root, BaseMethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            SyntaxTriviaList leadingTrivia = methodDeclaration.GetLeadingTrivia();
            int insertionIndex = GetInsertionIndex(ref leadingTrivia);

            string newLineText = document.Project.Solution.Workspace.Options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);

            throw new NotImplementedException();
        }

        private Task<Document> GetConstructorDocumentationTransformedDocumentAsync(Document document, SyntaxNode root, BaseMethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
    }
}

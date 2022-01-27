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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SA1602CodeFixProvider))]
    [Shared]
    internal class SA1602CodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                SA1602EnumerationItemsMustBeDocumented.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider()
        {
            return CustomFixAllProviders.BatchFixer;
        }

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                SyntaxToken identifier = root.FindToken(diagnostic.Location.SourceSpan.Start);
                if (identifier.IsMissingOrDefault())
                {
                    continue;
                }

                EnumMemberDeclarationSyntax declaration = identifier.Parent.FirstAncestorOrSelf<EnumMemberDeclarationSyntax>();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        DocumentationResources.EnumDocumentationCodeFix,
                        cancellationToken => GetEnumDocumentationTransformedDocumentAsync(
                            context.Document,
                            root,
                            declaration,
                            identifier,
                            cancellationToken),
                        nameof(SA1600CodeFixProvider)),
                    diagnostic);
            }
        }

        private static Task<Document> GetEnumDocumentationTransformedDocumentAsync(Document document, SyntaxNode root, EnumMemberDeclarationSyntax declaration, SyntaxToken identifier, CancellationToken cancellationToken)
        {
            string newLineText = GetNewLineText(document);

            var documentationNodes = new List<XmlNodeSyntax>();

            documentationNodes.Add(MethodDocumentationHelper.CreateMethodSummeryText(identifier.ValueText, newLineText));

            return Task.FromResult(CreateCommentAndReplaceInDocument(document, root, declaration, newLineText, documentationNodes.ToArray()));
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

        private static int GetInsertionIndex(ref SyntaxTriviaList leadingTrivia)
        {
            int insertionIndex = leadingTrivia.Count;
            while (insertionIndex > 0 && !leadingTrivia[insertionIndex - 1].HasBuiltinEndLine())
            {
                insertionIndex--;
            }

            return insertionIndex;
        }

        private static string GetNewLineText(Document document)
        {
            return document.Project.Solution.Workspace.Options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);
        }
    }
}

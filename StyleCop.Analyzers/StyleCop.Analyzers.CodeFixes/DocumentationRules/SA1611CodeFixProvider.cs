// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.DocumentationRules
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
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

                var parmaterSyntax = (ParameterSyntax)identifierToken.Parent;

                // Declaration --> ParameterList --> Parameter
                var parentDeclaration = parmaterSyntax.Parent.Parent;
                switch (parentDeclaration.Kind())
                {
                //case SyntaxKind.ConstructorDeclaration:
                //    context.RegisterCodeFix(
                //        CodeAction.Create(
                //            DocumentationResources.ConstructorDocumentationCodeFix,
                //            cancellationToken => GetConstructorDocumentationTransformedDocumentAsync(context.Document, root, (BaseMethodDeclarationSyntax)identifierToken.Parent, cancellationToken),
                //            nameof(SA1600CodeFixProvider)),
                //        diagnostic);
                //    break;

                case SyntaxKind.MethodDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.MethodDocumentationCodeFix,
                            cancellationToken => GetMethodDocumentationTransformedDocumentAsync(context.Document, root, parentDeclaration, parmaterSyntax, cancellationToken),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);
                    break;

                //case SyntaxKind.DelegateDeclaration:
                //    context.RegisterCodeFix(
                //        CodeAction.Create(
                //            DocumentationResources.DelegateDocumentationCodeFix,
                //            cancellationToken => GetDelegateDocumentationTransformedDocumentAsync(context.Document, root, (DelegateDeclarationSyntax)identifierToken.Parent, cancellationToken),
                //            nameof(SA1600CodeFixProvider)),
                //        diagnostic);
                //    break;
                //case SyntaxKind.IndexerDeclaration:
                //    context.RegisterCodeFix(
                //        CodeAction.Create(
                //            DocumentationResources.IndexerDocumentationCodeFix,
                //            cancellationToken => GetIndexerDocumentationTransformedDocumentAsync(context.Document, root, (IndexerDeclarationSyntax)identifierToken.Parent, cancellationToken),
                //            nameof(SA1600CodeFixProvider)),
                //        diagnostic);
                //    break;
                }
            }
        }

        private Task<Document> GetMethodDocumentationTransformedDocumentAsync(Document document, SyntaxNode root, SyntaxNode parent, ParameterSyntax parmaterSyntax, CancellationToken cancellationToken)
        {
            string newLineText = document.Project.Solution.Workspace.Options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);
            var documentation = parent.GetDocumentationCommentTriviaSyntax();

            var paramNodesDocumentation = documentation.Content
                .GetXmlElements(XmlCommentHelper.ParamXmlTag)
                .ToList();
            SeparatedSyntaxList<ParameterSyntax> parameters = ((ParameterListSyntax)parmaterSyntax.Parent).Parameters;
            var parameterIndex = parameters.IndexOf(parmaterSyntax);
            SyntaxNode prevNode = null;

            if (parameterIndex != 0)
            {
                var count = 0;
                foreach (XmlNodeSyntax paramXmlNode in paramNodesDocumentation)
                {
                    if (count > parameterIndex)
                    {
                        prevNode = paramXmlNode;
                        break;
                    }

                    var name = XmlCommentHelper.GetFirstAttributeOrDefault<XmlNameAttributeSyntax>(paramXmlNode);
                    if (name != null)
                    {
                        var nameValue = name.Identifier.Identifier.ValueText;
                        if (parameters[count].Identifier.ValueText == nameValue)
                        {
                            count++;
                            continue;
                        }

                        prevNode = paramXmlNode;
                        break;
                    }
                }
            }

            if (prevNode == null)
            {
                prevNode = documentation.Content.GetXmlElements(XmlCommentHelper.TypeParamXmlTag).LastOrDefault();
            }

            // no
            if (prevNode == null)
            {
                prevNode = documentation.Content.GetXmlElements(XmlCommentHelper.SummaryXmlTag).FirstOrDefault() ?? documentation.Content.First();
            }

            var parmeterDocumentation = GetParameterDocumentation(newLineText, parmaterSyntax);
            var newDocumentation = documentation.InsertNodesAfter(prevNode, parmeterDocumentation);
            var newTriva = SyntaxFactory.Trivia(newDocumentation);
            var newElement = parent.ReplaceTrivia(documentation.ParentTrivia, newTriva);
            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(parent, newElement)));
        }

        private static IEnumerable<XmlNodeSyntax> GetParameterDocumentation(string newLineText, ParameterSyntax parameter)
        {
            yield return XmlSyntaxFactory.NewLine(newLineText);
            var paramDocumentation = XmlSyntaxFactory.Text(CommentContentHelper.CreateParameterSummeryText(parameter));
            yield return XmlSyntaxFactory.ParamElement(parameter.Identifier.ValueText, paramDocumentation);
        }
    }
}

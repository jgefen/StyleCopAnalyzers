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

                var parmaterSyntax = (ParameterSyntax)identifierToken.Parent;

                // Declaration --> ParameterList --> Parameter
                var parentDeclaration = parmaterSyntax.Parent.Parent;
                switch (parentDeclaration.Kind())
                {
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            DocumentationResources.ParameterDocumentationCodeFix,
                            cancellationToken => GetParmeterDocumentationTransformedDocumentAsync(context.Document, root, parentDeclaration, parmaterSyntax, cancellationToken),
                            nameof(SA1600CodeFixProvider)),
                        diagnostic);
                    break;
                }
            }
        }

        private static Task<Document> GetParmeterDocumentationTransformedDocumentAsync(Document document, SyntaxNode root, SyntaxNode parent, ParameterSyntax parmaterSyntax, CancellationToken cancellationToken)
        {
            string newLineText = document.Project.Solution.Workspace.Options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);
            var documentation = parent.GetDocumentationCommentTriviaSyntax();

            var paramNodesDocumentation = documentation.Content
                .GetXmlElements(XmlCommentHelper.ParamXmlTag)
                .ToList();
            IList<ParameterSyntax> parameters = GetParentDeclerationParameters(parmaterSyntax).ToList();
            var parameterIndex = parameters.IndexOf(parmaterSyntax);
            SyntaxNode prevNode = null;

            if (parameterIndex != 0)
            {
                var count = 0;
                foreach (XmlNodeSyntax paramXmlNode in paramNodesDocumentation)
                {
                    var name = XmlCommentHelper.GetFirstAttributeOrDefault<XmlNameAttributeSyntax>(paramXmlNode);
                    if (name != null)
                    {
                        var nameValue = name.Identifier.Identifier.ValueText;
                        if (parameters[count].Identifier.ValueText == nameValue)
                        {
                            count++;
                            if (count == parameterIndex)
                            {
                                prevNode = paramXmlNode;
                                break;
                            }

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

            var parmeterDocumentation = MethodDocumentationHelper.CreateParametersDocumentation(newLineText, parmaterSyntax);
            var newDocumentation = documentation.InsertNodesAfter(prevNode, parmeterDocumentation);
            var newTriva = SyntaxFactory.Trivia(newDocumentation);
            var newElement = parent.ReplaceTrivia(documentation.ParentTrivia, newTriva);
            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(parent, newElement)));
        }

        private static IEnumerable<ParameterSyntax> GetParentDeclerationParameters(ParameterSyntax parmaterSyntax)
        {
            return (parmaterSyntax.Parent as ParameterListSyntax)?.Parameters
                ?? (parmaterSyntax.Parent as BracketedParameterListSyntax)?.Parameters;
        }
    }
}

namespace StyleCop.Analyzers.Helpers
{
    using Microsoft.CodeAnalysis;
    using StyleCop.Analyzers.DocumentationRules;

    internal static class EventDocumentationHelper
    {
        public static string CreateEventDocumentation(SyntaxToken identifier)
        {
            return DocumentationResources.EventDocumentationPrefix + CommonDocumentationHelper.GetNameDocumentation(identifier.ValueText);
        }
    }
}

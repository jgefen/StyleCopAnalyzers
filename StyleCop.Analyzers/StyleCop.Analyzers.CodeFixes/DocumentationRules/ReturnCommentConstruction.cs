namespace StyleCop.Analyzers.DocumentationRules;

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// The return comment construction.
/// </summary>
public static class ReturnCommentConstruction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReturnCommentConstruction"/> class.
    /// </summary>
    /// <param name="returnType">The return type.</param>
    /// <returns>The return comment</returns>
    public static string GetReturnCommentConstruction(TypeSyntax returnType)
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

    /// <summary>
    /// Generates predefined type comment.
    /// </summary>
    /// <param name="returnType">The return type.</param>
    /// <returns>The comment.</returns>
    private static string GeneratePredefinedTypeComment(PredefinedTypeSyntax returnType)
    {
        return DetermineStartedWord(returnType.Keyword.ValueText) + " " + returnType.Keyword.ValueText + ".";
    }

    /// <summary>
    /// Generates identifier name type comment.
    /// </summary>
    /// <param name="returnType">The return type.</param>
    /// <returns>The comment.</returns>
    private static string GenerateIdentifierNameTypeComment(IdentifierNameSyntax returnType)
    {
        return GenerateGeneralComment(returnType.Identifier.ValueText);
    }

    /// <summary>
    /// Generates qualified name type comment.
    /// </summary>
    /// <param name="returnType">The return type.</param>
    /// <returns>The comment.</returns>
    private static string GenerateQualifiedNameTypeComment(QualifiedNameSyntax returnType)
    {
        return GenerateGeneralComment(returnType.ToString());
    }

    /// <summary>
    /// Generates array type comment.
    /// </summary>
    /// <param name="arrayTypeSyntax">The array type syntax.</param>
    /// <returns>The comment.</returns>
    private static string GenerateArrayTypeComment(ArrayTypeSyntax arrayTypeSyntax)
    {
        return "An array of " + DetermineSpecificObjectName(arrayTypeSyntax.ElementType);
    }

    /// <summary>
    /// Generates generic type comment.
    /// </summary>
    /// <param name="returnType">The return type.</param>
    /// <returns>The comment.</returns>
    private static string GenerateGenericTypeComment(GenericNameSyntax returnType)
    {
        // ReadOnlyCollection IReadOnlyCollection 
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

    /// <summary>
    /// Generates general comment.
    /// </summary>
    /// <param name="returnType">The return type.</param>
    /// <returns>The comment.</returns>
    private static string GenerateGeneralComment(string returnType)
    {
        return DetermineStartedWord(returnType) + " " + returnType + ".";
    }

    /// <summary>
    /// Determines specific object name.
    /// </summary>
    /// <param name="specificType">The specific type.</param>
    /// <returns>The comment.</returns>
    private static string DetermineSpecificObjectName(TypeSyntax specificType)
    {
        string result = null;
        if (specificType is IdentifierNameSyntax)
        {
            // result = Pluralizer.Pluralize(((IdentifierNameSyntax)specificType).Identifier.ValueText);
        }
        else if (specificType is PredefinedTypeSyntax)
        {
            result = (specificType as PredefinedTypeSyntax).Keyword.ValueText;
        }
        else if (specificType is GenericNameSyntax)
        {
            result = (specificType as GenericNameSyntax).Identifier.ValueText;
        }
        else
        {
            result = specificType.ToFullString();
        }
        return result + ".";
    }

    /// <summary>
    /// Determines started word.
    /// </summary>
    /// <param name="returnType">The return type.</param>
    /// <returns>The comment.</returns>
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

using Avroify.Internals;
using Microsoft.CodeAnalysis;

namespace Avroify.Diagnostics;

internal static class NonPartialClassDiagnostic
{
    private const string Id = "AVROIFY00001";
    private const string Title = "Not A Partial Class";
    private const string Message = "Classes targeted by Avroify must be declared as partials to allow Avroify to extend them.";

    public static DiagnosticInfo Create(SyntaxNode node)
    {
        return new DiagnosticInfo(
            new DiagnosticDescriptor(Id, Title, Message, Constants.CategoryUsage, DiagnosticSeverity.Warning,
                isEnabledByDefault: true),
            node.GetLocation()
        );
    }
}
using Avroify.Internals;
using Microsoft.CodeAnalysis;

namespace Avroify.Diagnostics;

internal static class UnmarkedClassDiagnostic
{
    private const string Id = "AVROIFY00002";
    private const string Title = "Class Not Avroified";

    private const string Message =
        "Class {0} referenced by property {1} does not support Avro Serialization/Deserialization.";

    public static DiagnosticInfo Create(IPropertySymbol node, string className)
        => new(
            new DiagnosticDescriptor(Id, Title, Message, Constants.CategorySerialization, DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            node.Locations[0],
            className, node.Name
        );
}
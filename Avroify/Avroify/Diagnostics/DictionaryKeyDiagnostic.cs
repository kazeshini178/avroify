using Avroify.Internals;
using Microsoft.CodeAnalysis;

namespace Avroify.Diagnostics;

internal static class DictionaryKeyDiagnostic
{
    private const string Id = "AVROIFY00003";
    private const string Title = "String Key Required";

    private const string Message =
        "Only string keyed Dictionaries are support with Avro Map Schema.";

    public static DiagnosticInfo Create(ITypeSymbol node)
        => new(
            new DiagnosticDescriptor(Id, Title, Message, Constants.CategorySerialization, DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            node.Locations[0]
        );
}
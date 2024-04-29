using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Avroify;

[Generator]
public class AvcsSchemaSourceGenerator : ISourceGenerator
{
    private ClassBuilder _classBuilder = null!;

    public void Initialize(GeneratorInitializationContext context)
    {
        _classBuilder = new();
    }

    public void Execute(GeneratorExecutionContext context)
    {
        _ = context.AdditionalFiles
            .Where(file => _classBuilder.IsAvroSchemaFile(file))
            .Select(file => file.GetText())
            .Where(text => text is not null)
            .Select((text, _) => _classBuilder.RegisterSchema(text!))
            .ToImmutableArray();

        var files = _classBuilder.GenerateAvroClass();
        foreach (var file in files)
        {
            context.AddSource($"{file.Name}.g.cs", file.Source);
        }
    }
}
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Avroify;

[Generator]
public class AvcsSchemaSourceGenerator : IIncrementalGenerator
{ 
    private ClassBuilder _classBuilder = null!;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        _classBuilder = new();

        var fileInfo = context.AdditionalTextsProvider
            .Where(file => _classBuilder.IsAvroSchemaFile(file))
            .Select((file, _) => _classBuilder.GetFileInfo(file))
            .Where(file => !string.IsNullOrWhiteSpace(file.Content))
            .WithTrackingName("Avcs File Gather");
        
        var outputFiles = fileInfo
            .Select((file, _) => _classBuilder.TransformAvcsFile(file))
            .WithTrackingName("Avcs File Transformation");
        
        context.RegisterSourceOutput(outputFiles,
            (productionContext, result) =>
            {
                foreach (var (name, source) in result.Outputs)
                {
                    productionContext.AddSource($"{name}.g.cs", source);      
                }
            });
    }
}
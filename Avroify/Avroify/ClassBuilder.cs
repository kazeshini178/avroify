using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avro;
using Avroify.Internals;
using Microsoft.CodeAnalysis;

namespace Avroify;

internal class ClassBuilder
{
    private const string AvroSchemaExtension = ".avcs";
    private const string Delimiter = "//-------------------";

    internal bool IsAvroSchemaFile(AdditionalText file)
    {
        return Path.GetFileName(file.Path).EndsWith(AvroSchemaExtension);
    }

    internal AvcsFileInfo GetFileInfo(AdditionalText file)
    {
        var fileText = file.GetText()?.ToString() ?? string.Empty;
        return new AvcsFileInfo(file.Path, fileText);
    }

    internal AvcsGenerationResult TransformAvcsFile(AvcsFileInfo file)
    {
        CodeGen codeGen = new();
        codeGen.AddSchema(file.Content);
        var codeUnit = codeGen.GenerateCode();
        var generatedClasses = GenerateCodeOutput(codeUnit);
        return new AvcsGenerationResult(file.FileName, new EquatableArray<GenerationOutput>(generatedClasses));
    }

    private GenerationOutput[] GenerateCodeOutput(CodeCompileUnit compileUnit)
    {
        var positionalNames = new List<string>();
        var generatorOptions = new CodeGeneratorOptions()
        {
            BracingStyle = "C"
        };

        using var writer = new StringWriter();
        // Create a C# code provider
        using var codeProvider = CodeDomProvider.CreateProvider("CSharp");

        var imports = string.Empty;
        foreach (CodeNamespace codeNamespace in compileUnit.Namespaces)
        {
            if (imports == string.Empty)
            {
                var builder = new StringBuilder();
                foreach (CodeNamespaceImport codeNamespaceImport in codeNamespace.Imports)
                {
                    builder.AppendLine($"using {codeNamespaceImport.Namespace};");
                }

                imports = builder.ToString();
            }

            foreach (CodeTypeDeclaration codeType in codeNamespace.Types)
            {
                writer.WriteLine(Delimiter);

                writer.WriteLine(imports);
                writer.WriteLine($"namespace {codeNamespace.Name};");
                writer.WriteLine();
                codeProvider.GenerateCodeFromType(codeType, writer, generatorOptions);
                positionalNames.Add(codeType.Name);
            }
        }

        // Get the generated code as a string
        var generatedCode = writer.ToString();

        return generatedCode.Split([Delimiter], StringSplitOptions.RemoveEmptyEntries)
            .Select((code, index) => new GenerationOutput(positionalNames[index], code))
            .ToArray();
    }
}
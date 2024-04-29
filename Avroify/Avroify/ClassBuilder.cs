using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Avro;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Avroify;

internal class ClassBuilder
{
    private const string AvroSchemaExtension = ".avcs";
    private const string Delimiter = "//-------------------";

    private CodeGen _avroCodeGen = new();

    internal bool IsAvroSchemaFile(AdditionalText file)
    {
        return Path.GetFileName(file.Path).EndsWith(AvroSchemaExtension);
    }

    internal SourceText RegisterSchema(SourceText avroText)
    {
        var text = avroText.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return avroText;

        _avroCodeGen.AddSchema(text);
        return avroText;
    }

    internal List<GenerationOutput> GenerateAvroClass(CancellationToken token = default)
    {
        var codeUnit = _avroCodeGen.GenerateCode();
        var generatedClasses = GenerateCodeOutput(codeUnit);
        return generatedClasses;
    }

    private List<GenerationOutput> GenerateCodeOutput(CodeCompileUnit compileUnit)
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
            .ToList();
    }
}
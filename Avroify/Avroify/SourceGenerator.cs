using System.Collections.Generic;
using System.Text;
using System.Threading;
using Avroify.Diagnostics;
using Avroify.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Avroify;

[Generator]
public class SourceGenerator : IIncrementalGenerator
{
    private readonly bool _isTestContext;
    private readonly SchemaBuilder _schemaBuilder = new();

    private const string AvroifyAttributeName = "Avroify.AvroifyAttribute";

    // Only used in tests, Attributes project has "real" attribute
    private const string AttributeSourceCode = @"// <auto-generated/>
using System;

namespace Avroify
{
    [AttributeUsage(System.AttributeTargets.Class)]
    public class AvroifyAttribute : Attribute
    {
        public string? Name { get; set; }
        public string? Namespace { get; set; }
    }
}";

    public SourceGenerator()
    {
    }

    // Dont really like this but couldnt figure out why test wouldnt use marker attribute
    public SourceGenerator(bool isTestContext)
    {
        _isTestContext = isTestContext;
    }
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        if (_isTestContext)
        {
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "AvroifyAttribute.g.cs",
                SourceText.From(AttributeSourceCode, Encoding.UTF8)));
        }

        var attributeGeneration = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AvroifyAttributeName,
                (node, _) => node is ClassDeclarationSyntax,
                CreateAvroDetails
            )
            .Where(c => c is not null)
            .Select((c, _) => c!.Value)
            .WithTrackingName("AvroPartialGenerator");
        
        var generationDiagnostics = attributeGeneration
            .Where(s => s.Diagnostics is not null)
            .SelectMany((s, _) => s.Diagnostics!.Value);
        
        context.RegisterSourceOutput(generationDiagnostics,
            (productionContext, diagnostics) =>
            {
                productionContext.ReportDiagnostic(Diagnostic.Create(diagnostics.Descriptor,
                    diagnostics.Location?.ToLocation(), properties: null, messageArgs: diagnostics.MessageParams));
            }
        );

        context.RegisterSourceOutput(attributeGeneration, (productionContext, s) =>
        {
            var file = CreateAvroRecordPartial(s);
            productionContext.AddSource($"{s.Name}.g.cs", file);
        });
    }

    private SourceText CreateAvroRecordPartial(AvroRecordDetails details)
    {
        var fileTemplate = @$"// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by Avroify
//    Changes to this file will be lost when code is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using global::Avro;
using global::Avro.Specific;

namespace {details.Namespace};

[global::System.CodeDom.Compiler.GeneratedCodeAttribute(""Avroify"", ""{Constants.Version}"")]
public partial class {details.Name} : global::Avro.Specific.ISpecificRecord
{{
	public static global::Avro.Schema _SCHEMA = global::Avro.Schema.Parse(""{details.Schema}"");
    public virtual global::Avro.Schema Schema
	{{
		get
		{{
			return {details.Name}._SCHEMA;
		}}
	}}

    private AvroDecimal ToScaledAvroDecimal(decimal value, int targetScale = 14)
    {{
        var result = Math.Round(value * (decimal)Math.Pow(10, targetScale), targetScale);
        return new AvroDecimal(new System.Numerics.BigInteger(result), targetScale);
    }}

    public virtual object Get(int fieldPos)
	{{
		switch (fieldPos)
		{{
{details.Getters}
			default: throw new global::Avro.AvroRuntimeException(""Bad index "" + fieldPos + "" in Get()"");
		}}
	}}

	public virtual void Put(int fieldPos, object fieldValue)
	{{
		switch (fieldPos)
		{{
{details.Setters}
			default: throw new global::Avro.AvroRuntimeException(""Bad index "" + fieldPos + "" in Put()"");
		}}
	}}
}}
";

        return SourceText.From(fileTemplate, Encoding.UTF8);
    }

    private AvroRecordDetails? CreateAvroDetails(GeneratorAttributeSyntaxContext ctx, CancellationToken token)
    {
        // Symbols allow us to get the compile-time information.
        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.TargetNode) is not INamedTypeSymbol classSymbol)
            return null;

        List<DiagnosticInfo>? diagnostics = null;
        var classSyntax = ctx.TargetNode as ClassDeclarationSyntax;
        if (!IsPartialClass(classSyntax!))
        {
            diagnostics ??= new List<DiagnosticInfo>();
            diagnostics.Add(NonPartialClassDiagnostic.Create(ctx.TargetNode));
        }

        var @namespace = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        var getStringBuilder = new StringBuilder();
        var setStringBuilder = new StringBuilder();
        var properties = Util.GetSettableProperties(classSymbol);
        for (var index = 0; index < properties.Count; index++)
        {
            var property = properties[index];
            getStringBuilder.Append($"\t\t\tcase {index}: return ");
            setStringBuilder.Append(
                $"\t\t\tcase {index}: this.{property.Name} = ");

            if (property.Type is IArrayTypeSymbol arrayProp)
            {
                getStringBuilder.AppendLine($"this.{property.Name};");
                setStringBuilder.AppendLine($"((List<{arrayProp.ElementType.Name}>)fieldValue).ToArray(); break;");
            }
            else
            {
                var typeString = property.Type.ToDisplayString();
                switch (typeString.ToLower())
                {
                    case "byte":
                        getStringBuilder.AppendLine($"(int) this.{property.Name};");
                        setStringBuilder.AppendLine("Convert.ToByte((int)fieldValue); break;");
                        break;
                    case "short":
                        getStringBuilder.AppendLine($"(int) this.{property.Name};");
                        setStringBuilder.AppendLine("Convert.ToInt16((int)fieldValue); break;");
                        break;
                    case "char":
                        getStringBuilder.AppendLine($"(int) this.{property.Name};");
                        setStringBuilder.AppendLine("Convert.ToChar((int)fieldValue); break;");
                        break;
                    case "decimal":
                        getStringBuilder.AppendLine($"(Avro.AvroDecimal) ToScaledAvroDecimal(this.{property.Name});");
                        setStringBuilder.AppendLine($"({typeString})(Avro.AvroDecimal)fieldValue; break;");
                        break;
                    case "dateonly" or "system.dateonly":
                        getStringBuilder.AppendLine($"this.{property.Name}.ToDateTime(TimeOnly.MinValue);");
                        setStringBuilder.AppendLine("DateOnly.FromDateTime((DateTime)fieldValue); break;");
                        break;
                    case "timeonly" or "system.timeonly":
                        getStringBuilder.AppendLine($"this.{property.Name}.ToTimeSpan();");
                        setStringBuilder.AppendLine("TimeOnly.FromTimeSpan((TimeSpan)fieldValue); break;");
                        break;
                    default:
                        getStringBuilder.AppendLine($"this.{property.Name};");
                        setStringBuilder.AppendLine($"({typeString})fieldValue; break;");
                        break;
                }
            }
        }

        var nullableContextEnabled = ctx.SemanticModel.Compilation.Options.NullableContextOptions !=
                                     NullableContextOptions.Disable;
        _schemaBuilder.SetNullableEnabled(nullableContextEnabled);
        var (schema, schemaDiagnostics) = _schemaBuilder.GenerateSchemaForClass(classSymbol, properties, token);
        if (schemaDiagnostics is not null)
        {
            diagnostics ??= new List<DiagnosticInfo>();
            diagnostics.AddRange(schemaDiagnostics);
        }

        return new AvroRecordDetails(@namespace, className,
            schema.ToString().Replace("\"", "\\\""),
            setStringBuilder.ToString(), getStringBuilder.ToString(),
            diagnostics is not null ? new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()) : null);
    }

    private bool IsPartialClass(ClassDeclarationSyntax classSyntax)
    {
        var isPartialClass = false;
        foreach (var classSyntaxModifier in classSyntax.Modifiers)
        {
            if (classSyntaxModifier.IsKind(SyntaxKind.PartialKeyword))
                isPartialClass = true;
        }

        return isPartialClass;
    }
}
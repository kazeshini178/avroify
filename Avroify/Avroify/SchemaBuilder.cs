using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avro;
using Avroify.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;

namespace Avroify;

internal class SchemaBuilder
{
    private readonly string[] BaseTypes =
    [
        "String", "Byte", "Char", "Int16", "Int32", "Int64", "Boolean", "Single", "Double", "Decimal", "DateTime",
        "DateOnly", "TimeOnly", "List", "Dictionary", "Nullable"
    ];

    internal SchemaBuilder()
    {
    }

    internal (Schema schema, List<DiagnosticInfo>? diagnostics) GenerateSchemaForClass(INamedTypeSymbol classSymbol,
        List<IPropertySymbol> properties,
        CancellationToken token)
    {
        var diagnostics = new List<DiagnosticInfo>();

        var schemaFields = new List<Field>();

        for (var index = 0; index < properties.Count; index++)
        {
            var property = properties[index];

            if (property.Type.TypeKind != TypeKind.Enum)
            {
                if (property.Type is not IArrayTypeSymbol &&
                    !IsSupportedBaseType(property.Type.Name))
                {
                    if (!property.Type.GetAttributes().Any(s => s.AttributeClass?.Name == nameof(AvroifyAttribute)))
                    {
                        diagnostics.Add(NonAvroifiedClassDiagnostic.Create(property, property.Type.Name));
                    }
                }
                else if (property.Type is IArrayTypeSymbol arrayType &&
                         !IsSupportedBaseType(arrayType.ElementType.Name))
                {
                    if (!arrayType.ElementType.GetAttributes()
                            .Any(s => s.AttributeClass?.Name == nameof(AvroifyAttribute)))
                    {
                        diagnostics.Add(NonAvroifiedClassDiagnostic.Create(property, arrayType.ElementType.Name));
                    }
                }
                else if (property.Type is INamedTypeSymbol {IsGenericType: true} namedSymbol)
                {
                    foreach (var typeArgument in namedSymbol.TypeArguments)
                    {
                        if (!IsSupportedBaseType(typeArgument.Name) &&
                            !typeArgument.GetAttributes().Any(s => s.AttributeClass?.Name == nameof(AvroifyAttribute)))
                        {
                            diagnostics.Add(NonAvroifiedClassDiagnostic.Create(property, typeArgument.Name));
                        }
                    }
                }
            }

            var (fieldSchema, fieldDiagnostics) = CreateFieldSchema(property.Type);
            if (fieldDiagnostics is not null)
                diagnostics.AddRange(fieldDiagnostics);

            var defaultValue = GetPropertyDefaultValue(property);
            var field = new Field(schema: fieldSchema,
                name: property.Name,
                pos: index,
                defaultValue: defaultValue);
            schemaFields.Add(field);
        }

        return (
            RecordSchema.Create(classSymbol.Name, schemaFields, classSymbol.ContainingNamespace.ToDisplayString()),
            diagnostics
        );
    }

    private bool IsSupportedBaseType(string typeName) => BaseTypes.Contains(typeName);

    private JToken? GetPropertyDefaultValue(IPropertySymbol property)
    {
        var reference = property.DeclaringSyntaxReferences.FirstOrDefault();
        if (reference is null) return null;

        var node = (PropertyDeclarationSyntax) reference.GetSyntax();
        return node switch
        {
            {Initializer.Value: MemberAccessExpressionSyntax member} =>
                JValue.CreateString(member.Name.ToString()),
            // {Initializer.Value: LiteralExpressionSyntax literal} =>
            //     JValue.CreateString(literal.GetText().ToString().Trim()),
            {Initializer.Value: LiteralExpressionSyntax literal} =>
                JValue.CreateString(literal.Token.ValueText),
            _ => null
        };
    }

    private (Schema schema, List<DiagnosticInfo>? diagnostics) CreateFieldSchema(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is IArrayTypeSymbol arraySymbol)
        {
            var arrayTypeSymbol = (INamedTypeSymbol) arraySymbol.ElementType;
            // arrayTypeSymbol.IsGenericType
            var listType = arrayTypeSymbol.Name;
            var (schema, diagnostics) = CreateSchemaForType(listType, arrayTypeSymbol);
            return (ArraySchema.Create(schema), diagnostics);
        }

        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            var enumValues = typeSymbol.GetMembers()
                .Where(m => m.Kind == SymbolKind.Field)
                .Select(m => m.Name)
                .ToArray();
            return (EnumSchema.Create(typeSymbol.Name, enumValues, typeSymbol.ContainingNamespace.ToDisplayString()),
                null);
        }

        return CreateSchemaForType(typeSymbol.Name, (INamedTypeSymbol) typeSymbol);
    }

    private (Schema schema, List<DiagnosticInfo>? diagnostics) CreateListSchema(INamedTypeSymbol typeSymbol)
    {
        var listSymbol = (INamedTypeSymbol) typeSymbol.TypeArguments[0];
        var listType = listSymbol.Name;
        var (schema, diagnostics) = CreateSchemaForType(listType, listSymbol);
        return (ArraySchema.Create(schema), diagnostics);
    }

    private (Schema schema, List<DiagnosticInfo>? diagnostics) CreateMapSchema(INamedTypeSymbol mapSymbol)
    {
        // TODO: Add Diagnostic to check users are using complex map keys
        // Currently unused on Avro map type
        var mapKeySymbol = (INamedTypeSymbol) mapSymbol.TypeArguments[0];
        var mapValueSymbol = (INamedTypeSymbol) mapSymbol.TypeArguments[1];
        var valueType = mapValueSymbol.Name;
        var (schema, diagnostics) = CreateSchemaForType(valueType, mapValueSymbol);
        return (MapSchema.CreateMap(schema), diagnostics);
    }

    private (Schema schema, List<DiagnosticInfo>? diagnostics) CreateSchemaForType(string symbolType,
        INamedTypeSymbol symbol)
    {
        return symbolType switch
        {
            "String" => (PrimitiveSchema.Create(Schema.Type.String), null),
            "Byte" => (PrimitiveSchema.Create(Schema.Type.Bytes), null),
            "Char" or "Int16" or "Int32" => (PrimitiveSchema.Create(Schema.Type.Int), null),
            "Int64" => (PrimitiveSchema.Create(Schema.Type.Long), null),
            "Boolean" => (PrimitiveSchema.Create(Schema.Type.Boolean), null),
            "Single" => (PrimitiveSchema.Create(Schema.Type.Float), null),
            "Double" => (PrimitiveSchema.Create(Schema.Type.Double), null),
            "Decimal" => (LogicalSchema.Parse(
                "{\"type\":\"bytes\",\"logicalType\":\"decimal\",\"precision\": 29,\"scale\": 14}"), null),
            "DateTime" => (LogicalSchema.Parse("{\"type\":\"long\",\"logicalType\":\"timestamp-millis\"}"), null),
            "DateOnly" => (LogicalSchema.Parse("{\"type\":\"int\",\"logicalType\":\"date\"}"), null),
            "TimeOnly" => (LogicalSchema.Parse("{\"type\":\"int\",\"logicalType\":\"time-millis\"}"), null),
            "List" => CreateListSchema(symbol),
            "Dictionary" => CreateMapSchema(symbol),
            "Nullable" => CreateNullUnionSchema(symbol),
            _ => GenerateSchemaForClass(symbol, Util.GetSettableProperties(symbol), default)
        };
    }

    private (Schema schema, List<DiagnosticInfo>? diagnostics) CreateNullUnionSchema(INamedTypeSymbol symbol)
    {
        var (schema, diagnostics) = CreateFieldSchema((INamedTypeSymbol) symbol.TypeArguments[0]);
        return (UnionSchema.Create([PrimitiveSchema.Create(Schema.Type.Null), schema]), diagnostics);
    }
}
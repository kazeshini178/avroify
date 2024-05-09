using System;
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
    private readonly string[] _baseTypes =
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

            AddMarkedClassDiagnostics(property, diagnostics);

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

    private void AddMarkedClassDiagnostics(IPropertySymbol property, List<DiagnosticInfo> diagnostics)
    {
        if (property.Type.TypeKind == TypeKind.Enum) return;

        if (property.Type is IArrayTypeSymbol arrayType &&
            !IsSupportedBaseType(arrayType.ElementType.Name) &&
            !HasAvroifyAttribute(arrayType.ElementType))
        {
            diagnostics.Add(UnmarkedClassDiagnostic.Create(property, arrayType.ElementType.Name));
            return;
        }

        if (property.Type is not IArrayTypeSymbol &&
            !IsSupportedBaseType(property.Type.Name) &&
            !HasAvroifyAttribute(property.Type))
        {
            diagnostics.Add(UnmarkedClassDiagnostic.Create(property, property.Type.Name));
            return;
        }

        if (property.Type is not INamedTypeSymbol {IsGenericType: true} namedSymbol) return;
        
        foreach (var typeArgument in namedSymbol.TypeArguments)
        {
            if (IsSupportedBaseType(typeArgument.Name) || HasAvroifyAttribute(typeArgument)) continue;
            diagnostics.Add(UnmarkedClassDiagnostic.Create(property, typeArgument.Name));
        }
    }

    private static bool HasAvroifyAttribute(ITypeSymbol typeSymbol) =>
        typeSymbol.GetAttributes().Any(s => s.AttributeClass?.Name == nameof(AvroifyAttribute));

    private bool IsSupportedBaseType(string typeName) => _baseTypes.Contains(typeName);

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
        DiagnosticInfo? keyDiagnostic = null;
        if (mapKeySymbol.Name != nameof(String))
        {
            keyDiagnostic = DictionaryKeyDiagnostic.Create(mapKeySymbol);
        }
        var mapValueSymbol = (INamedTypeSymbol) mapSymbol.TypeArguments[1];
        var valueType = mapValueSymbol.Name;
        var (schema, diagnostics) = CreateSchemaForType(valueType, mapValueSymbol);
        if (keyDiagnostic is not null)
        {
            diagnostics ??= [];
            diagnostics.Add(keyDiagnostic);
        }
        return (MapSchema.CreateMap(schema), diagnostics);
    }

    private (Schema schema, List<DiagnosticInfo>? diagnostics) CreateSchemaForType(string symbolType,
        INamedTypeSymbol symbol)
    {
        return symbolType switch
        {
            "String" => (PrimitiveSchema.Create(Schema.Type.String), null),
            "Byte" or "Char" or "Int16" or "Int32" => (PrimitiveSchema.Create(Schema.Type.Int), null),
            "Int64" => (PrimitiveSchema.Create(Schema.Type.Long), null),
            "Boolean" => (PrimitiveSchema.Create(Schema.Type.Boolean), null),
            "Single" => (PrimitiveSchema.Create(Schema.Type.Float), null),
            "Double" => (PrimitiveSchema.Create(Schema.Type.Double), null),
            "Decimal" => (Schema.Parse(
                "{\"type\":\"bytes\",\"logicalType\":\"decimal\",\"precision\": 29,\"scale\": 14}"), null),
            "DateTime" => (Schema.Parse("{\"type\":\"long\",\"logicalType\":\"timestamp-millis\"}"), null),
            "DateOnly" => (Schema.Parse("{\"type\":\"int\",\"logicalType\":\"date\"}"), null),
            "TimeOnly" => (Schema.Parse("{\"type\":\"int\",\"logicalType\":\"time-millis\"}"), null),
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
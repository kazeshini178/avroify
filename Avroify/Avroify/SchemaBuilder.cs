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

    private bool _explicitNullableReferenceTypes { get; set; }

    internal SchemaBuilder()
    {
    }

    internal void SetNullableEnabled(bool nullableContextEnabled)
    {
        _explicitNullableReferenceTypes = nullableContextEnabled;
    }

    internal (Schema schema, List<DiagnosticInfo>? diagnostics) GenerateSchemaForClass(INamedTypeSymbol classSymbol,
        List<IPropertySymbol> properties, CancellationToken token)
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

        var schemaNamingInfo = GetNamingInfo(classSymbol);
        return (
            RecordSchema.Create(schemaNamingInfo.Name, schemaFields, schemaNamingInfo.Namespace),
            diagnostics
        );
    }

    internal record SchemaNamingInfo(string Name, string Namespace);

    private SchemaNamingInfo GetNamingInfo(INamedTypeSymbol classSymbol)
    {
        var schemaName = classSymbol.Name;
        var schemaNamespace = classSymbol.ContainingNamespace.ToDisplayString();
        if (!HasAvroifyAttribute(classSymbol))
        {
            return new SchemaNamingInfo(schemaName, schemaNamespace);
        }

        var avroifyAttributeDetails = classSymbol.GetAttributes()
            .First(a => a.AttributeClass!.Name == nameof(AvroifyAttribute));
        foreach (var args in avroifyAttributeDetails.NamedArguments)
        {
            switch (args.Key)
            {
                case "Name":
                    schemaName = args.Value.Value!.ToString();
                    break;
                case "Namespace":
                    schemaNamespace = args.Value.Value!.ToString();
                    break;
            }
        }

        return new SchemaNamingInfo(schemaName, schemaNamespace);
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
            if (typeArgument.TypeKind == TypeKind.Enum ||
                IsSupportedBaseType(typeArgument.Name) ||
                HasAvroifyAttribute(typeArgument)) continue;
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
                new JValue(literal.Token.Value),
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
        Schema resultSchema =
            _explicitNullableReferenceTypes && typeSymbol.NullableAnnotation != NullableAnnotation.Annotated
                ? ArraySchema.Create(schema)
                : UnionSchema.Create([
                    PrimitiveSchema.Create(Schema.Type.Null),
                    ArraySchema.Create(schema)
                ]);
        return (resultSchema, diagnostics);
    }

    private (Schema schema, List<DiagnosticInfo>? diagnostics) CreateMapSchema(INamedTypeSymbol mapSymbol)
    {
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

        Schema resultSchema =
            _explicitNullableReferenceTypes && mapSymbol.NullableAnnotation != NullableAnnotation.Annotated
                ? MapSchema.CreateMap(schema)
                : UnionSchema.Create([
                    PrimitiveSchema.Create(Schema.Type.Null),
                    MapSchema.CreateMap(schema)
                ]);
        return (resultSchema, diagnostics);
    }

    private (Schema schema, List<DiagnosticInfo>? diagnostics) CreateSchemaForType(string symbolType,
        INamedTypeSymbol symbol)
    {
        return symbolType switch
        {
            "String" => (
                _explicitNullableReferenceTypes && symbol.NullableAnnotation != NullableAnnotation.Annotated
                    ? PrimitiveSchema.Create(Schema.Type.String)
                    : UnionSchema.Create([
                        PrimitiveSchema.Create(Schema.Type.Null),
                        PrimitiveSchema.Create(Schema.Type.String)
                    ]), null),
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
            _ => CreateClassSchema(symbol)
        };
    }

    private (Schema schema, List<DiagnosticInfo>? diagnostics) CreateClassSchema(INamedTypeSymbol symbol)
    {
        var (schema, diagnostics) = GenerateSchemaForClass(symbol, Util.GetSettableProperties(symbol), default);
        return (_explicitNullableReferenceTypes && symbol.NullableAnnotation != NullableAnnotation.Annotated
                ? schema
                : UnionSchema.Create([
                    PrimitiveSchema.Create(Schema.Type.Null),
                    schema
                ])
            , diagnostics);
    }

    private (Schema schema, List<DiagnosticInfo>? diagnostics) CreateNullUnionSchema(INamedTypeSymbol symbol)
    {
        var (schema, diagnostics) = CreateFieldSchema((INamedTypeSymbol) symbol.TypeArguments[0]);
        return (UnionSchema.Create([PrimitiveSchema.Create(Schema.Type.Null), schema]), diagnostics);
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avro;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;

namespace Avroify;

internal class SchemaBuilder
{
    internal SchemaBuilder()
    { }

    internal Schema GenerateSchemaForClass(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties,
        CancellationToken token)
    {
        var schemaFields = new List<Field>();

        for (var index = 0; index < properties.Count; index++)
        {
            var property = properties[index];
            var fieldSchema = CreateFieldSchema(property.Type);
            var defaultValue = GetPropertyDefaultValue(property);

            var field = new Field(schema: fieldSchema,
                name: property.Name,
                pos: index,
                defaultValue: defaultValue);
            schemaFields.Add(field);
        }

        return RecordSchema.Create(classSymbol.Name, schemaFields, classSymbol.ContainingNamespace.ToDisplayString());
    }

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

    private Schema CreateFieldSchema(ITypeSymbol typeSymbol)
    {
        Schema fieldSchema;

        if (typeSymbol is IArrayTypeSymbol arraySymbol)
        {
            var arrayTypeSymbol = (INamedTypeSymbol) arraySymbol.ElementType;
            var listType = arrayTypeSymbol.Name;
            var schema = CreateSchemaForType(listType, arrayTypeSymbol);
            fieldSchema = ArraySchema.Create(schema);
        }
        else if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            var enumValues = typeSymbol.GetMembers()
                .Where(m => m.Kind == SymbolKind.Field)
                .Select(m => m.Name)
                .ToArray();
            fieldSchema = EnumSchema.Create(typeSymbol.Name, enumValues,
                typeSymbol.ContainingNamespace.ToDisplayString());
        }
        else
        {
            fieldSchema = CreateSchemaForType(typeSymbol.Name, (INamedTypeSymbol) typeSymbol);
        }

        return fieldSchema;
    }

    private Schema CreateListSchema(INamedTypeSymbol typeSymbol)
    {
        var listSymbol = (INamedTypeSymbol) typeSymbol.TypeArguments[0];
        var listType = listSymbol.Name;
        var schema = CreateSchemaForType(listType, listSymbol);
        return ArraySchema.Create(schema);
    }

    private Schema CreateMapSchema(INamedTypeSymbol mapSymbol)
    {
        // Currently unused on Avro map type
        var mapKeySymbol = (INamedTypeSymbol) mapSymbol.TypeArguments[0];
        var mapValueSymbol = (INamedTypeSymbol) mapSymbol.TypeArguments[1];
        var valueType = mapValueSymbol.Name;
        var schema = CreateSchemaForType(valueType, mapValueSymbol);
        return MapSchema.CreateMap(schema);
    }

    private Schema CreateSchemaForType(string listType, INamedTypeSymbol listSymbol)
    {
        return listType switch
        {
            "String" => PrimitiveSchema.Create(Schema.Type.String),
            "Byte" => PrimitiveSchema.Create(Schema.Type.Bytes),
            "Char" or "Int16" or "Int32" => PrimitiveSchema.Create(Schema.Type.Int),
            "Int64" => PrimitiveSchema.Create(Schema.Type.Long),
            "Boolean" => PrimitiveSchema.Create(Schema.Type.Boolean),
            "Single" => PrimitiveSchema.Create(Schema.Type.Float),
            "Double" => PrimitiveSchema.Create(Schema.Type.Double),
            "Decimal" => LogicalSchema.Parse(
                "{\"type\":\"bytes\",\"logicalType\":\"decimal\",\"precision\": 29,\"scale\": 14}"),
            "DateTime" => LogicalSchema.Parse("{\"type\":\"long\",\"logicalType\":\"timestamp-millis\"}"),
            "DateOnly" => LogicalSchema.Parse("{\"type\":\"int\",\"logicalType\":\"date\"}"),
            "TimeOnly" => LogicalSchema.Parse("{\"type\":\"int\",\"logicalType\":\"time-millis\"}"),
            "List" => CreateListSchema(listSymbol),
            "Dictionary" => CreateMapSchema(listSymbol),
            "Nullable" => UnionSchema.Create([
                PrimitiveSchema.Create(Schema.Type.Null),
                CreateFieldSchema((INamedTypeSymbol) listSymbol.TypeArguments[0])
            ]),
            _ => GenerateSchemaForClass(listSymbol, Util.GetSettableProperties(listSymbol), default)
        };
    }
}
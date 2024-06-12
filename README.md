# Avroify

![Build](https://github.com/kazeshini178/avroify/actions/workflows/dotnet-ci.yml/badge.svg)
![Build](https://github.com/kazeshini178/avroify/actions/workflows/release-cd.yml/badge.svg)

Source Generator that allows you to extend your data models to be used for Avro Serialization and Deserialization. 

Removes the need to run external steps during your development process to add Avro support or even generate classes from a given Schema.

Currently supports extending existing classes as well as generating classes for `.avcs` schema files.

[toc]

## Usage

 Install the `Avroify` Package

```bash
dotnet add package Avroify 
```

Once installed you can either use `avcs` schema files or annotating classes for avrofication.
> **NOTE**: Application will need to reference `Apache.Avro` 

## Features

- Simplifies creation of Schemas for your classes
- Generate usable classes from `avcs` files
- Respects Nullable Annotation settings
- Override namings to help with refactorings

### .Net to Avro Mappings

| .NET Type | Avro Type |
| --------- | --------- |
| Boolean | Boolean |
| Char | Int |
| String | String |
| Byte | Int |
| Short | Int |
| Int | Int |
| Long | Long |
| Single | Float |
| Double | Double |
| Decimal | AvroDecimal* |
| DateTime | long (Logical Type: timestamp-millis) |
| DateOnly | int (Logical Type: date) |
| TimeOnly | int (Logical Type: time-millis) |
| List | Array |
| Array | Array |
| Dictionary | Map** |
| Enum | Enum |
| Class | Record |


\* `AvroDecimal`s are represented as bytes types and logical types of decimal with a precision of 29 and scale of 14

**  `Map`s expect the key to be a string type, currently there is not way to change the key type on the schema

## Examples

### AVCS Files

Add the file to your project, then **right click** select **properties** and set the build action to `AdditionalFiles` 

Below sample Schema will generate 3 files (2 Classes and 1 Enum)

```json
{
  "name": "SampleAvro",
  "namespace": "Avroify.Sample.Models",
  "type": "record",
  "fields": [
    {
      "name": "Id",
      "type": "long"
    },
    {
      "name": "Age",
      "type": "int"
    },
    {
      "name": "FullName",
      "type": [
        "null",
        "string"
      ]
    },
    {
      "name": "Status",
      "type": {
        "type": "enum",
        "name": "StatusEnum",
        "namespace": "Avroify.Sample.Enums",
        "symbols": [
          "Active",
          "Inactive",
          "Unknown"
        ]
      }
    },
    {
      "name": "Address",
      "type": [
        "null",
        {
          "name": "AddressAvro",
          "namespace": "Avroify.Sample.Models",
          "type": "record",
          "fields": [
            {
              "name": "Street",
              "type": "string"
            },
            {
              "name": "Country",
              "type": [
                "null",
                "string"
              ]
            },
            {
              "name": "PostalCode",
              "type": [
                "null",
                "string"
              ]
            }
          ]
        }
      ]
    }
  ]
}
```

### Attribute Classes

To extend existing classes to support Avro, mark the class as `partial` and add the `[Avroify]` attribute to the class.

```csharp
using System;
using System.Collections.Generic;

namespace Avroify.Sample;

[Avroify]
public partial class AllBaseTypes
{
    public string StringType { get; set; }
    public char CharType { get; set; }
    public byte ByteType { get; set; }
    public short ShortType { get; set; }
    public int IntType { get; set; }
    public long LongType { get; set; }
    public float FloatType { get; set; }
    public double DoubleType { get; set; }
    public decimal DecimalType { get; set; }
    public int[] ArrayType  { get; set; }
    public List<string> ListType { get; set; }
    public Dictionary<string, NestedClass> MapType { get; set; }
    public NestedClass ComplexType { get; set; }
    public DateTime DateTimeType { get; set; }
    public DateOnly DateType { get; set; }
    public TimeOnly TimeType { get; set; }
    public SimpleEnum EnumType { get; set; }
}

[Avroify]
public partial class NestedClass
{
    public string NextStringType { get; set; }
}

public enum SimpleEnum
{
     Active,
     Inactive,
     Deleted
}
```


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

Once installed you can either use `avcs` schema files or marking classes to avrofication.

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


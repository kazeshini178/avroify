using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using System.Linq;
using Avroify.Internals;
using Avroify.Tests.Utils;
using FluentAssertions;

namespace Avroify.Tests;

public class SourceGeneratorTests
{
    private const string CSharpClass = """
                                       using Avroify;
                                       using System.Collections.Generic;

                                       namespace Avroify.Sample
                                       {
                                           using Enums;
                                       
                                           [Avroify]
                                           public partial class SampleAvroModel
                                           {
                                               public string Name { get; set; }
                                               public FontSizeEnum FontSize { get; set; } = FontSize.Large;
                                               public BasicModel Basic { get; set; }
                                               public DateTime DateCreated { get; set; }
                                               public List<BasicModel> BasicList { get; set; }
                                       
                                               public List<string> Colours { get; set; }
                                               public int Age { get; set; } = 18;
                                               public int? Money { get; set; }
                                           }
                                           
                                           [Avroify]
                                           public partial class BasicModel
                                           {
                                               public int Id { get; set; }
                                               public string Avroname { get; set; } = "Bob"
                                               public bool IsDeleted { get; set; }
                                           }
                                       }

                                       namespace Avroify.Sample.Enums
                                       {
                                           public enum FontSizeEnum
                                           {
                                               Small,
                                               Medium,
                                               Large
                                           }
                                       }
                                       """;

    [Fact]
    public void Given_Classes_Avroify_Attributes_All_Class_Generate()
    {
        var generator = new SourceGenerator(true);

        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(nameof(SourceGeneratorTests), new[]
        {
            CSharpSyntaxTree.ParseText(CSharpClass)
        }, new[]
        {
            // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        });

        var results = driver.RunGenerators(compilation).GetRunResult();

        results.GeneratedTrees
            .Count(t => !t.FilePath.EndsWith("AvroifyAttribute.g.cs"))
            .Should()
            .Be(2);
    }

    [Theory]
    [ClassData(typeof(GenerationTestCases))]
    public void Given_Attribute_Configuration_Correct_Schema_Is_Generated(GenerationTestCase testCase)
    {
        var generator = new SourceGenerator(true);

        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(nameof(SourceGeneratorTests), new[]
        {
            CSharpSyntaxTree.ParseText(testCase.ClassDefinition)
        }, new[]
        {
            // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        });

        var results = driver.RunGenerators(compilation).GetRunResult();

        var generateFileSyntax = results.GeneratedTrees.Single(t => t.FilePath.EndsWith("SampleAvroModel.g.cs"));

        Assert.Equal(testCase.GenerationResult, generateFileSyntax.GetText().ToString(),
            ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }

    private const string AvroSchema = """
                                      {
                                        "name": "NonBusinessPartnerPurposeAvro",
                                        "namespace": "Avroify.Sample.Models",
                                        "type": "record",
                                        "fields": [
                                          {
                                            "name": "NonbusinessPartnerId",
                                            "type": "long"
                                          },
                                          {
                                            "name": "NonbusinessPartnerAuditStat",
                                            "type": "int"
                                          },
                                          {
                                            "name": "NonbusinessPartnerName",
                                            "type": [ "null", "string" ]
                                          },
                                          {
                                            "name": "NonbusinessPartnerType",
                                            "type": {
                                              "type": "enum",
                                              "name": "PartnerTypeAvro",
                                              "namespace": "Avroify.Sample.Enums",
                                              "symbols": [ "CustomsAuthority", "PortAuthority", "CustomsBroker" ]
                                            }
                                          },
                                          {
                                            "name": "CountryCode",
                                            "type": [ "null", "string" ]
                                          },
                                          {
                                            "name": "PortCodes",
                                            "type": {
                                              "type": "array",
                                              "items": "string"
                                            }
                                          },
                                          {
                                            "name": "ContactPurposeId",
                                            "type": "long"
                                          },
                                          {
                                            "name": "ContactPurposeAuditStat",
                                            "type": "int"
                                          },
                                          {
                                            "name": "PurposeType",
                                            "type": "string"
                                          },
                                          {
                                            "name": "SubPurposeType",
                                            "type": "string"
                                          },
                                          {
                                            "name": "CommsType",
                                            "type": {
                                              "type": "enum",
                                              "name": "CommunicationTypeAvro",
                                              "namespace": "Avroify.Sample.Enums",
                                              "symbols": [ "Email", "API", "FTP" ]
                                            }
                                          },
                                          {
                                            "name": "FileConfigurations",
                                            "type": [
                                              "null",
                                              {
                                                "type": "array",
                                                "items": {
                                                  "name": "FileConfigurationAvro",
                                                  "namespace": "Avroify.Sample.Models",
                                                  "type": "record",
                                                  "fields": [
                                                    {
                                                      "name": "Type",
                                                      "type": "string"
                                                    },
                                                    {
                                                      "name": "ExportAs",
                                                      "type": [ "null", "string" ]
                                                    },
                                                    {
                                                      "name": "SenderId",
                                                      "type": [ "null", "string" ]
                                                    },
                                                    {
                                                      "name": "SenderName",
                                                      "type": [ "null", "string" ]
                                                    },
                                                    {
                                                      "name": "ReceiverId",
                                                      "type": [ "null", "string" ]
                                                    },
                                                    {
                                                      "name": "CallReference",
                                                      "type": [ "null", "string" ]
                                                    },
                                                    {
                                                      "name": "IsForDischarge",
                                                      "type": "boolean"
                                                    },
                                                    {
                                                      "name": "SplitAs",
                                                      "type": [ "null", "string" ]
                                                    },
                                                    {
                                                      "name": "FilterContainerType",
                                                      "type": {
                                                        "type": "array",
                                                        "items": "string"
                                                      }
                                                    },
                                                    {
                                                      "name": "FilterCustomUnionStatus",
                                                      "type": {
                                                        "type": "array",
                                                        "items": "string"
                                                      }
                                                    },
                                                    {
                                                      "name": "FilterWebuyAccounts",
                                                      "type": {
                                                        "type": "array",
                                                        "items": "string"
                                                      }
                                                    },
                                                    {
                                                      "name": "WebuyHideAccountNames",
                                                      "type": "boolean"
                                                    },
                                                    {
                                                      "name": "FilterWesellAccounts",
                                                      "type": {
                                                        "type": "array",
                                                        "items": "string"
                                                      }
                                                    },
                                                    {
                                                      "name": "WesellHideAccountNames",
                                                      "type": "boolean"
                                                    },
                                                    {
                                                      "name": "FilterMapping",
                                                      "type": [ "null", "string" ]
                                                    },
                                                    {
                                                      "name": "FilterHideAccountNames",
                                                      "type": "boolean"
                                                    }
                                                  ]
                                                }
                                              }
                                            ]
                                          },
                                          {
                                            "name": "Emails",
                                            "type": {
                                              "type": "array",
                                              "items": "string"
                                            }
                                          },
                                          {
                                            "name": "CCEmails",
                                            "type": {
                                              "type": "array",
                                              "items": "string"
                                            }
                                          }
                                        ]
                                      }
                                      """;

    [Fact]
    public void Given_Avcs_Avro_Schema_File()
    {
        var generator = new AvcsSchemaSourceGenerator();

        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([
                new TestAdditionalFile("./PartnerPurposeAvro.avcs", AvroSchema),
                // new TestAdditionalFile("./PartnerPurposeAvro2.avcs", AvroSchema)
            ]);

        var compilation = CSharpCompilation.Create(nameof(SourceGeneratorTests));
        var results = driver.RunGenerators(compilation).GetRunResult();

        results.GeneratedTrees.Length.Should().Be(4);
    }

    private const string CSharpClassAllBaseType = """
                                                  using System.Collections.Generic;

                                                  namespace Avroify.Sample;

                                                  [Avroify]
                                                  public partial class AllBaseTypes
                                                  {
                                                      public string StringType { get; set; }
                                                      public char CharType { get; set; }
                                                      public short ShortType { get; set; }
                                                      public int IntType { get; set; }
                                                      public long LongType { get; set; }
                                                      public float FloatType { get; set; }
                                                      public double DoubleType { get; set; }
                                                      public decimal DecimalType { get; set; }
                                                      public int[] ArrayType  { get; set; }
                                                      public List<NestedClass> ListType { get; set; }
                                                      public Dictionary<string, NestedClass> MapType { get; set; }
                                                      public NestedClass ComplexType { get; set; }
                                                      public DateTime DateTimeType { get; set; }
                                                      public DateOnly DateType { get; set; }
                                                      public TimeOnly TimeType { get; set; }
                                                  }

                                                  public partial class NestedClass
                                                  {
                                                      public string NextStringType { get; set; }
                                                  } 
                                                  """;

    private const string CSharpResultClassAllBaseType =
        $$$$"""
            // ------------------------------------------------------------------------------
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

            namespace Avroify.Sample;

            [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Avroify", "{{{{Constants.Version}}}}")]
            public partial class AllBaseTypes : global::Avro.Specific.ISpecificRecord
            {
              public static global::Avro.Schema _SCHEMA = global::Avro.Schema.Parse("{\"type\":\"record\",\"name\":\"AllBaseTypes\",\"namespace\":\"Avroify.Sample\",\"fields\":[{\"name\":\"StringType\",\"type\":\"string\"},{\"name\":\"CharType\",\"type\":\"int\"},{\"name\":\"ShortType\",\"type\":\"int\"},{\"name\":\"IntType\",\"type\":\"int\"},{\"name\":\"LongType\",\"type\":\"long\"},{\"name\":\"FloatType\",\"type\":\"float\"},{\"name\":\"DoubleType\",\"type\":\"double\"},{\"name\":\"DecimalType\",\"type\":{\"type\":\"bytes\",\"logicalType\":\"decimal\",\"precision\":29,\"scale\":14}},{\"name\":\"ArrayType\",\"type\":{\"type\":\"array\",\"items\":\"int\"}},{\"name\":\"ListType\",\"type\":{\"type\":\"array\",\"items\":{\"type\":\"record\",\"name\":\"NestedClass\",\"namespace\":\"Avroify.Sample\",\"fields\":[{\"name\":\"NextStringType\",\"type\":\"string\"}]}}},{\"name\":\"MapType\",\"type\":{\"type\":\"map\",\"values\":\"NestedClass\"}},{\"name\":\"ComplexType\",\"type\":\"NestedClass\"},{\"name\":\"DateTimeType\",\"type\":{\"type\":\"long\",\"logicalType\":\"timestamp-millis\"}},{\"name\":\"DateType\",\"type\":{\"type\":\"int\",\"logicalType\":\"date\"}},{\"name\":\"TimeType\",\"type\":{\"type\":\"int\",\"logicalType\":\"time-millis\"}}]}");
              public virtual global::Avro.Schema Schema
              {
                get
                {
            			return AllBaseTypes._SCHEMA;
            		}
            	}
            
                private AvroDecimal ToScaledAvroDecimal(decimal value, int targetScale = 14)
                {
                    var result = Math.Round(value * (decimal)Math.Pow(10, targetScale), targetScale);
                    return new AvroDecimal(new System.Numerics.BigInteger(result), targetScale);
                }
            
              public virtual object Get(int fieldPos)
            	{
            		  switch (fieldPos)
            		  {
            			    case 0: return this.StringType;
                      case 1: return (int) this.CharType;
                      case 2: return (int) this.ShortType;
                      case 3: return this.IntType;
                      case 4: return this.LongType;
                      case 5: return this.FloatType;
                      case 6: return this.DoubleType;
                      case 7: return (Avro.AvroDecimal) ToScaledAvroDecimal(this.DecimalType);
                      case 8: return this.ArrayType;
                      case 9: return this.ListType;
                      case 10: return this.MapType;
                      case 11: return this.ComplexType;
                      case 12: return this.DateTimeType;
                      case 13: return this.DateType.ToDateTime(TimeOnly.MinValue);
                      case 14: return this.TimeType.ToTimeSpan();
            
            			    default: throw new global::Avro.AvroRuntimeException("Bad index " + fieldPos + " in Get()");
            		  }
            	}
            
            	public virtual void Put(int fieldPos, object fieldValue)
            	{
            		  switch (fieldPos)
            		  {
                      case 0: this.StringType = (string)fieldValue; break;
                      case 1: this.CharType = Convert.ToChar((int)fieldValue); break;
                      case 2: this.ShortType = Convert.ToInt16((int)fieldValue); break;
                      case 3: this.IntType = (int)fieldValue; break;
                      case 4: this.LongType = (long)fieldValue; break;
                      case 5: this.FloatType = (float)fieldValue; break;
                      case 6: this.DoubleType = (double)fieldValue; break;
                      case 7: this.DecimalType = (decimal)(Avro.AvroDecimal)fieldValue; break;
                      case 8: this.ArrayType = ((List<Int32>)fieldValue).ToArray(); break;
                      case 9: this.ListType = (System.Collections.Generic.List<Avroify.Sample.NestedClass>)fieldValue; break;
                      case 10: this.MapType = (System.Collections.Generic.Dictionary<string, Avroify.Sample.NestedClass>)fieldValue; break;
                      case 11: this.ComplexType = (Avroify.Sample.NestedClass)fieldValue; break;
                      case 12: this.DateTimeType = (DateTime)fieldValue; break;
                      case 13: this.DateType = DateOnly.FromDateTime((DateTime)fieldValue); break;
                      case 14: this.TimeType = TimeOnly.FromTimeSpan((TimeSpan)fieldValue); break;
            
            			    default: throw new global::Avro.AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            		  }
            	}
            }

            """;

    [Fact]
    public void Given_Class_With_Most_Common_Base_Types()
    {
        var generator = new SourceGenerator(true);

        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(nameof(SourceGeneratorTests), new[]
        {
            CSharpSyntaxTree.ParseText(CSharpClassAllBaseType)
        }, new[]
        {
            // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        });

        var results = driver.RunGenerators(compilation).GetRunResult();

        var generateFileSyntax = results.GeneratedTrees.Single(t => t.FilePath.EndsWith("AllBaseTypes.g.cs"));
        Assert.Equal(CSharpResultClassAllBaseType, generateFileSyntax.GetText().ToString(),
            ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }
}
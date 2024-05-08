using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Avroify.Tests;

public class DiagnosticOutputTests
{
    private const string NonPartialClass = @"using Avroify;

namespace Avroify.Sample
{
    [Avroify]
    public class SampleAvroModel
    {
        public string Name { get; set; } 
    }
}";

    [Fact]
    public void Given_NonPartial_Class_Report_Warning_AVROIFY00001()
    {
        var generator = new SourceGenerator(true);

        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(nameof(SourceGeneratorTests), new[]
        {
            CSharpSyntaxTree.ParseText(NonPartialClass)
        }, new[]
        {
            // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        });

        var results = driver.RunGenerators(compilation).GetRunResult();
        Assert.False(results.Diagnostics.IsEmpty);
        Assert.Contains(results.Diagnostics, d => d.Id == "AVROIFY00001");
    }

    [Theory]
    [ClassData(typeof(UnmarkedPropertyClassTestCases))]
    public void Given_NonMarked_Property_Class_Report_Error_AVROIFY00002(DiagnosticTestCase obj)
    {
        var generator = new SourceGenerator(true);

        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(nameof(SourceGeneratorTests), new[]
        {
            CSharpSyntaxTree.ParseText(obj.ClassDefinition)
        }, new[]
        {
            // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        });

        var results = driver.RunGenerators(compilation).GetRunResult();
        Assert.False(results.Diagnostics.IsEmpty);
        Assert.Single(results.Diagnostics);
        Assert.Contains(results.Diagnostics, d => d.Id == "AVROIFY00002");
        Assert.Contains(results.Diagnostics, d => d.GetMessage() == obj.DiagnosticResultMessage);
    }
    
    private const string IntKeyDictionaryClass = @"using System.Collections.Generic;

namespace Avroify.Sample;

[Avroify]
public partial class DictionaryAvro
{
    public Dictionary<int, List<string>> MapType { get; set; }
}
";
    [Fact]
    public void Given_NonString_Key_Dictionary_Property_Report_Error_AVROIFY00003()
    {
        var generator = new SourceGenerator(true);

        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(nameof(SourceGeneratorTests), new[]
        {
            CSharpSyntaxTree.ParseText(IntKeyDictionaryClass)
        }, new[]
        {
            // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        });

        var results = driver.RunGenerators(compilation).GetRunResult();
        Assert.False(results.Diagnostics.IsEmpty);
        Assert.Contains(results.Diagnostics, d => d.Id == "AVROIFY00003");
    }
    
    private const string ValidClass = @"using System.Collections.Generic;

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
    public NestedClass[] ArrayType  { get; set; }
    public List<NestedClass> ListType { get; set; }
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
    ValueOne,
    ValueTwo,
    ValueThree
}
";
    
    [Fact]
    public void Given_Correctly_Setup_Class_Report_Nothing()
    {
        var generator = new SourceGenerator(true);

        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(nameof(SourceGeneratorTests), new[]
        {
            CSharpSyntaxTree.ParseText(ValidClass)
        }, new[]
        {
            // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        });

        var results = driver.RunGenerators(compilation).GetRunResult();
        Assert.True(results.Diagnostics.IsEmpty);
    }
}
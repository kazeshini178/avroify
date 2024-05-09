using Avro.IO;
using Avro.Specific;
using Avroify.Tests.Models;
using Bogus;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace Avroify.Nuget.Tests;

public class AvroSerializationTests
{
    private readonly SupportedBaseTypes _testModel;

    public AvroSerializationTests()
    {
        Randomizer.Seed = new Random(42069);
        var nestFaker = new Faker<NestedClass>("en")
            .RuleFor(m => m.NestedProp, f => f.Name.FullName());
        _testModel = new Faker<SupportedBaseTypes>("en")
            .RuleFor(m => m.CharType, f => f.Random.Char())
            .RuleFor(m => m.StringType, f => f.Lorem.Text())
            // .RuleFor(m => m.ByteType, f => f.Random.Byte())
            .RuleFor(m => m.ShortType, f => f.Random.Short())
            .RuleFor(m => m.IntType, f => f.Random.Int())
            .RuleFor(m => m.LongType, f => f.Random.Long())
            .RuleFor(m => m.FloatType, f => f.Random.Float())
            .RuleFor(m => m.DoubleType, f => f.Random.Double())
            .RuleFor(m => m.DecimalType, f => f.Random.Decimal(60, 900))
            .RuleFor(m => m.ArrayType, f => f.Make(4, () => f.Random.Int()).ToArray())
            .RuleFor(m => m.ListType, setter: f => f.Make(2, () => f.Name.JobType()))
            .RuleFor(m => m.MapType,
                f => new Dictionary<string, NestedClass>(f.Make(2,
                    () => new KeyValuePair<string, NestedClass>(f.Name.JobTitle(), nestFaker.Generate(1)[0]))))
            .RuleFor(m => m.ComplexType, _ => nestFaker.Generate(1)[0])
            .RuleFor(m => m.DateTimeType, f => f.Date.Soon().ToUniversalTime())
            .RuleFor(m => m.DateType, f => f.Date.SoonDateOnly())
            .RuleFor(m => m.TimeType, f => f.Date.SoonTimeOnly())
            .RuleFor(m => m.EnumType, f => f.PickRandom<SimpleEnum>())
            .Generate(1)[0];
    }

    [Fact(Skip = "Latest version not released")]
    public void Given_A_Avroified_Class_It_Should_Serialize_With_Apache_Avro()
    {
        var memStream = new MemoryStream();
        var encoder = new BinaryEncoder(memStream);
        var defaultWriter = new SpecificWriter<SupportedBaseTypes>(SupportedBaseTypes._SCHEMA);
        FluentActions.Invoking(() => defaultWriter.Write(_testModel, encoder))
            .Should()
            .NotThrow("Unable to Serialize from Avro Model");
    }

    [Fact(Skip = "Latest version not released")]
    public void Given_A_Serialised_Class_It_Should_Deserialize_With_Apache_Avro()
    {
        var memStream = new MemoryStream();
        var encoder = new BinaryEncoder(memStream);
        var defaultWriter = new SpecificWriter<SupportedBaseTypes>(SupportedBaseTypes._SCHEMA);
        defaultWriter.Write(_testModel, encoder);
        memStream.Position = 0;
        
        var newSample = new SupportedBaseTypes();
        var decoder = new BinaryDecoder(memStream); 
        var defaultReader = new SpecificReader<SupportedBaseTypes>(SupportedBaseTypes._SCHEMA, SupportedBaseTypes._SCHEMA);
        FluentActions.Invoking(() => defaultReader.Read(newSample, decoder))
            .Should()
            .NotThrow("Unable to Deserialize to Avro Model");
        newSample.Should().BeEquivalentTo(_testModel, 
            opt=> opt 
                .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Seconds()))
                .WhenTypeIs<DateTime>()
                .Using<TimeOnly>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Seconds()))
                .WhenTypeIs<TimeOnly>());
    }
}
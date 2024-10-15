using Xunit;

namespace Avroify.Tests;

public class DiagnosticTestCase
{
    public string ClassDefinition { get; init; } = null!;
    public string DiagnosticResultMessage { get; init; } = null!;
}

public class UnmarkedPropertyClassTestCases : TheoryData<DiagnosticTestCase>
{
    public UnmarkedPropertyClassTestCases()
    {
        Add(new DiagnosticTestCase()
        {
            ClassDefinition = @"using Avroify;

namespace Avroify.Sample
{
    [Avroify]
    public partial class SampleAvroModel
    {
        public SubAvroModel SubModel { get; set; }  
    }

    public class SubAvroModel
    {
        public string SubProp { get; set; } 
    }
}",
            DiagnosticResultMessage =
                "Class SubAvroModel referenced by property SubModel does not support Avro Serialization/Deserialization."
        });
        Add(new DiagnosticTestCase()
        {
            ClassDefinition = @"using Avroify;

namespace Avroify.Sample
{
    [Avroify]
    public partial class SampleAvroModel
    { 
        public SubAvroModel? NullableSubModel { get; set; }
        public int? NullableBaseType { get; set; }
    }

    public class SubAvroModel
    {
        public string SubProp { get; set; } 
    }
}",
            DiagnosticResultMessage =
                "Class SubAvroModel referenced by property NullableSubModel does not support Avro Serialization/Deserialization."
        });
        Add(new DiagnosticTestCase()
        {
            ClassDefinition = @"using Avroify;

namespace Avroify.Sample
{
    [Avroify]
    public partial class SampleAvroModel
    { 
        public List<SubAvroModel> ListOfSubModel { get; set; }
        public List<int> ListOfBaseType { get; set; }
    }

    public class SubAvroModel
    {
        public string SubProp { get; set; } 
    }
}",
            DiagnosticResultMessage =
                "Class SubAvroModel referenced by property ListOfSubModel does not support Avro Serialization/Deserialization."
        });
        Add(new DiagnosticTestCase()
        {
            ClassDefinition = @"using Avroify;

namespace Avroify.Sample
{
    [Avroify]
    public partial class SampleAvroModel
    { 
        public Dictionary<string, SubAvroModel> DictOfSubModel { get; set; }  
        public Dictionary<string, string> DictOfBaseType { get; set; }
    }

    public class SubAvroModel
    {
        public string SubProp { get; set; } 
    }
}",
            DiagnosticResultMessage =
                "Class SubAvroModel referenced by property DictOfSubModel does not support Avro Serialization/Deserialization."
        });
        Add(new DiagnosticTestCase()
        {
            ClassDefinition = @"using Avroify;

namespace Avroify.Sample
{
    [Avroify]
    public partial class SampleAvroModel
    { 
        public SubAvroModel[] ArrayOfSubModel { get; set; }  
        public int[] ArrayOfBaseType { get; set; }
    }

    public class SubAvroModel
    {
        public string SubProp { get; set; } 
    }
}",
            DiagnosticResultMessage =
                "Class SubAvroModel referenced by property ArrayOfSubModel does not support Avro Serialization/Deserialization."
        });
    }
}
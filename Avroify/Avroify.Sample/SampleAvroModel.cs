using System;
using System.Collections.Generic;

namespace Avroify.Sample
{
    using Enums;
    [Avroify]
    public partial class SampleAvroModel
    {
        public string? Name { get; set; }
        public FontSize FontSize { get; set; } = FontSize.Large;
        public List<BasicModel>? Basic { get; set; }
        public DateTime DateCreated { get; set; }

        public string ColoursFlattened
        {
            get => string.Join(',', Colours);
        } 
        
        public List<string> Colours { get; set; }
        public int Age { get; set; } = 18;
        public int? Money { get; set; }
    }

    [Avroify]
    public partial class BasicModel
    {
        public int Id { get; set; }
        public string Avroname { get; set; }
    }
}

namespace Avroify.Sample.Enums
{
    public enum FontSize
    {
        Small,
        Medium,
        Large
    }
}
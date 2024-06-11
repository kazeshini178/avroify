using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avroify.Sample.Models;

namespace Avroify.Sample
{
    using Enums;
    [Avroify(Name = "AvroModel")]
    public partial class SampleOverrideNameAvroMode
    {
        public string Name { get; set; }
        public FontSize FontSize { get; set; } = FontSize.Large;
        public List<BasicNamespaceOverrideModel> Basic { get; set; }
        public DateTime DateCreated { get; set; }

        public string ColoursFlattened
        {
            get => string.Join(',', Colours);
        } 
        
        public List<string> Colours { get; set; }
        public int Age { get; set; } = 18;
        public int? Money { get; set; }
    }

    [Avroify(Namespace = "Avroify.Override.NS")]
    public partial class BasicNamespaceOverrideModel
    {
        public int Id { get; set; }
        public string Avroname { get; set; }
    }
}
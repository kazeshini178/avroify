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
}

public partial class NestedClass
{
    public string NextStringType { get; set; }
} 
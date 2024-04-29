using System;

namespace Avroify;

[AttributeUsage(System.AttributeTargets.Class)]
public class AvroifyAttribute : Attribute
{
    public AvroifyAttribute()
    {
    }
}
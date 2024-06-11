using System;

namespace Avroify;

[AttributeUsage(System.AttributeTargets.Class)]
public class AvroifyAttribute : Attribute
{
    /// <summary>
    /// Allow schema to use a different name from the class, helpful for refactoring and migration scenarios 
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Allow schema to use a different namespace for the declared class, helpful for refactoring and migration scenarios 
    /// </summary>
    public string? Namespace { get; set; }
    public AvroifyAttribute()
    {
    }
}
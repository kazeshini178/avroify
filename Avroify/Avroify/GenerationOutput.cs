namespace Avroify;

internal class GenerationOutput(string name, string source)
{
    internal string Name { get; } = name;
    internal string Source { get; } = source;
}
using Avroify.Internals;

namespace Avroify;

internal record struct GenerationOutput(string Name, string Source);

internal record struct AvcsGenerationResult(string FileName, EquatableArray<GenerationOutput> Outputs);
internal record struct AvcsFileInfo(string FileName, string Content);
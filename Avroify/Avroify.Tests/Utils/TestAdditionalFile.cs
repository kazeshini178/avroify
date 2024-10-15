using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Avroify.Tests.Utils;

internal class TestAdditionalFile(string path, string text) 
    : AdditionalText
{
    private readonly SourceText _text = SourceText.From(text);

    public override SourceText GetText(CancellationToken cancellationToken = new()) => _text;

    public override string Path { get; } = path;
}
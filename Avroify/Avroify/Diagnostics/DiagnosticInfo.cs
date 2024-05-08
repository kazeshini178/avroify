using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Avroify.Diagnostics;

internal sealed record DiagnosticInfo
{
    // Explicit constructor to convert Location into LocationInfo
    public DiagnosticInfo(DiagnosticDescriptor descriptor, Location? location)
    {
        Descriptor = descriptor;
        Location = location is not null ? LocationInfo.CreateFrom(location) : null;
    }

    public DiagnosticInfo(DiagnosticDescriptor descriptor, Location? location, params string[] messageParams)
        : this(descriptor, location)
    {
        MessageParams = messageParams;
    }

    public DiagnosticDescriptor Descriptor { get; }
    public string[]? MessageParams { get; }
    public LocationInfo? Location { get; }
}

internal record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    public Location ToLocation()
        => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? CreateFrom(SyntaxNode node)
        => CreateFrom(node.GetLocation());

    public static LocationInfo? CreateFrom(Location location)
    {
        return location.SourceTree is null
            ? null
            : new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }
};
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Avroify;

internal static class Util
{
    internal static List<IPropertySymbol> GetSettableProperties(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(s => s.SetMethod is not null)
            .ToList();
    }
}
// Needed to support records, has the compiler needs this class
// Yay duck typing :D - https://stackoverflow.com/questions/64749385/predefined-type-system-runtime-compilerservices-isexternalinit-is-not-defined
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}
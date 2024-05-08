using Avroify.Diagnostics;
using Avroify.Internals;

namespace Avroify;

internal readonly record struct AvroRecordDetails(string Namespace, string Name, string Schema, string Setters, string Getters, EquatableArray<DiagnosticInfo>? Diagnostics);
using System.Text.Json.Serialization;

namespace Shiny.Impl;


/// <summary>
/// Source-generated type info for primitives and common BCL value types. <see cref="DefaultJsonSerializer"/>
/// falls back to this when no registered context/resolver supplies type info, so storing a plain
/// <c>string</c>/<c>int</c>/<c>bool</c> (e.g. a token in the secure store) works without the app
/// having to register a context first.
/// </summary>
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(char))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(sbyte))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(ushort))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(ulong))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(DateOnly))]
[JsonSerializable(typeof(TimeOnly))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(Uri))]
[JsonSerializable(typeof(byte[]))]
[JsonSerializable(typeof(bool?))]
[JsonSerializable(typeof(char?))]
[JsonSerializable(typeof(byte?))]
[JsonSerializable(typeof(sbyte?))]
[JsonSerializable(typeof(short?))]
[JsonSerializable(typeof(ushort?))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(uint?))]
[JsonSerializable(typeof(long?))]
[JsonSerializable(typeof(ulong?))]
[JsonSerializable(typeof(float?))]
[JsonSerializable(typeof(double?))]
[JsonSerializable(typeof(decimal?))]
[JsonSerializable(typeof(DateTime?))]
[JsonSerializable(typeof(DateTimeOffset?))]
[JsonSerializable(typeof(DateOnly?))]
[JsonSerializable(typeof(TimeOnly?))]
[JsonSerializable(typeof(TimeSpan?))]
[JsonSerializable(typeof(Guid?))]
internal partial class BuiltInJsonContext : JsonSerializerContext;

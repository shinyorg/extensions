﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shiny.Extensions.Stores.Impl;


public class TypeJsonConverter : JsonConverter<Type>
{
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var typeName = reader.GetString();
        return Type.GetType(typeName!);
    }


    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.AssemblyQualifiedName);
}


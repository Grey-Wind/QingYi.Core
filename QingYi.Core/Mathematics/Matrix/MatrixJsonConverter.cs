#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QingYi.Core.Mathematics.Matrix
{
    /// <summary>
    /// JSON serialization converter
    /// </summary>
    /// <typeparam name="T">Numeric type</typeparam>
    internal class MatrixJsonConverter<T> : JsonConverter<Matrix<T>> where T : unmanaged, INumber<T>
    {
        public override Matrix<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            int rows = 0, cols = 0;
            T[] data = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "Rows":
                            rows = reader.GetInt32();
                            break;
                        case "Cols":
                            cols = reader.GetInt32();
                            break;
                        case "Data":
                            data = JsonSerializer.Deserialize<T[]>(ref reader, options);
                            break;
                    }
                }
            }

            if (rows <= 0 || cols <= 0 || data == null || data.Length != rows * cols)
                throw new JsonException("Invalid matrix data");

            var memory = new Memory<T>(data);
            return new Matrix<T>(memory, rows, cols, false);
        }

        public override void Write(Utf8JsonWriter writer, Matrix<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Rows", value.Rows);
            writer.WriteNumber("Cols", value.Cols);
            writer.WritePropertyName("Data");
            JsonSerializer.Serialize(writer, value.ToArray().Cast<T>().ToArray(), options);
            writer.WriteEndObject();
        }
    }
}
#endif

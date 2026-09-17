using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brigade.Net.Core.Results;

/// <summary>Serializes a Result as its inner payload without a union wrapper.</summary>
public sealed class ResultJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => !typeToConvert.ContainsGenericParameters
        && FindResultType(typeToConvert) is not null;

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (!CanConvert(typeToConvert))
        {
            throw new ArgumentException("Expected a closed Result<T> type.", nameof(typeToConvert));
        }

        return (JsonConverter)Activator.CreateInstance(
            typeof(ResultJsonConverter<,>).MakeGenericType(FindResultType(typeToConvert)!.GetGenericArguments()[0], typeToConvert)
        )!;
    }

    private static Type? FindResultType(Type type)
    {
        for (var candidate = type; candidate is not null; candidate = candidate.BaseType)
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(Result<>))
            {
                return candidate;
            }
        }

        return null;
    }

    private sealed class ResultJsonConverter<T, TResult> : JsonConverter<TResult> where TResult : Result<T>
    {
        public override TResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Result<T> JSON is write-only because unwrapped payloads do not identify the result case.");
        }

        public override void Write(Utf8JsonWriter writer, TResult value, JsonSerializerOptions options)
        {
            value.Map(
                success: payload => WritePayload(writer, payload, options),
                failure: payload => WritePayload(writer, payload, options)
            );
        }

        private static Unit WritePayload(Utf8JsonWriter writer, object? payload, JsonSerializerOptions options)
        {
            var payloadType = payload?.GetType() ?? typeof(object);
            for (var candidate = payloadType; candidate is not null; candidate = candidate.BaseType)
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(Result<>))
                {
                    payloadType = candidate;
                    break;
                }
            }

            JsonSerializer.Serialize(writer, payload, payloadType, options);
            return Unit.Default;
        }
    }
}
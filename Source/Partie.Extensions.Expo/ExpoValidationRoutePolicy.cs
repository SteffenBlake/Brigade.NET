using System.Text.Json.Nodes;
using System.Text.Json;
using Brigade.Net.Expo;
using Brigade.Net.Partie.Engines.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi;

namespace Brigade.Net.Partie.Extensions.Expo;

/// <summary>Adds generated Expo validation constraints to route OpenAPI schemas.</summary>
public sealed class ExpoValidationRoutePolicy<TRequest>
    : IQueryRoutePolicy<TRequest>, ICommandRoutePolicy<TRequest>
    where TRequest : IExpoValidatable
{
    /// <summary>Adds Expo metadata to a query route.</summary>
    public static void Query(RouteHandlerBuilder route)
    {
        route.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            ApplyQuery(operation);
            return Task.CompletedTask;
        });
    }

    /// <summary>Adds Expo metadata to a command route.</summary>
    public static void Command(RouteHandlerBuilder route)
    {
        route.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            ApplyCommand(operation);
            return Task.CompletedTask;
        });
    }

    /// <summary>Applies Expo metadata to an object schema.</summary>
    public static void Apply(OpenApiSchema schema)
    {
        foreach (var property in TRequest.Metadata.Properties)
        {
            if (schema.Properties is null
                || !schema.Properties.TryGetValue(property.Name, out var rawPropertySchema)
                || rawPropertySchema is not OpenApiSchema propertySchema)
            {
                continue;
            }

            foreach (var rule in property.Rules)
            {
                ApplyRule(schema, propertySchema, property, rule);
            }
        }
    }

    private static void ApplyQuery(OpenApiOperation operation)
    {
        foreach (var property in TRequest.Metadata.Properties)
        {
            var parameter = operation.Parameters?.OfType<OpenApiParameter>().FirstOrDefault(item =>
                string.Equals(item.Name, property.Name, StringComparison.OrdinalIgnoreCase));
            if (parameter?.Schema is not OpenApiSchema schema)
            {
                continue;
            }

            foreach (var rule in property.Rules)
            {
                ApplyRule(null, schema, property, rule);
                if (rule.Kind == ExpoRuleKind.Required)
                {
                    parameter.Required = true;
                }
            }
        }
    }

    private static void ApplyCommand(OpenApiOperation operation)
    {
        if (operation.RequestBody?.Content is null)
        {
            return;
        }

        foreach (var mediaType in operation.RequestBody.Content.Values)
        {
            if (mediaType.Schema is OpenApiSchema schema)
            {
                Apply(schema);
            }
        }
    }

    private static void ApplyRule(
        OpenApiSchema? parent,
        OpenApiSchema schema,
        ExpoPropertyMetadata property,
        ExpoRuleMetadata rule
    )
    {
        switch (rule.Kind)
        {
            case ExpoRuleKind.Required:
                if (parent is not null)
                {
                    parent.Required ??= new HashSet<string>();
                    parent.Required.Add(property.Name);
                }
                break;
            case ExpoRuleKind.Minimum:
                schema.Minimum = Json(rule.ConstantValue);
                break;
            case ExpoRuleKind.ExclusiveMinimum:
                schema.ExclusiveMinimum = Json(rule.ConstantValue);
                break;
            case ExpoRuleKind.Maximum:
                schema.Maximum = Json(rule.ConstantValue);
                break;
            case ExpoRuleKind.ExclusiveMaximum:
                schema.ExclusiveMaximum = Json(rule.ConstantValue);
                break;
            case ExpoRuleKind.Equal:
                schema.Const = Json(rule.ConstantValue);
                break;
            case ExpoRuleKind.NotEqual:
                schema.Not = new OpenApiSchema { Const = Json(rule.ConstantValue) };
                break;
            default:
                AddInexactRule(schema, rule);
                break;
        }
    }

    private static string? Json(object? value) => JsonSerializer.Serialize(value);

    private static void AddInexactRule(OpenApiSchema schema, ExpoRuleMetadata rule)
    {
        var text = rule.ComparedPropertyName is null
            ? $"Expo validation rule: {rule.Kind}."
            : $"Expo validation rule: {rule.Kind} {rule.ComparedPropertyName}.";
        schema.Description = string.IsNullOrWhiteSpace(schema.Description)
            ? text
            : schema.Description + " " + text;
        schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        schema.Extensions["x-expo-validation"] = new JsonNodeExtension(JsonValue.Create(text));
    }
}

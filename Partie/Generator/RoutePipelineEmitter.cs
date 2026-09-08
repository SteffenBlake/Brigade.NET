using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Brigade.Net.Partie.Generator;

public static class RoutePipelineEmitter
{
    public static string Emit(RouteGraph graph, string methodName = "Execute")
    {
        var resultType = "global::Brigade.Net.Core.Results.Result<" + TypeName(graph.ResultType) + ">";
        var returnType = ParseTypeName("global::System.Threading.Tasks.ValueTask<" + resultType + ">");
        var body = Block(ReturnStatement(Invoke(graph.Calls[graph.Calls.Length - 1])));

        for (var index = graph.Calls.Length - 2; index >= 0; index--)
        {
            var call = graph.Calls[index];
            var output = call.ProvidedValue!;
            var continuationName = "Next" + output.Id;
            var continuation = LocalFunctionStatement(returnType, continuationName)
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier(ValueName(output))).WithType(ParseTypeName(TypeName(output.Type)))
                )))
                .WithBody(body);
            body = Block(ReturnStatement(Invoke(call, continuationName)), continuation);
        }

        return MethodDeclaration(returnType, methodName)
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(SeparatedList(graph.ExternalValues.Select(value =>
                Parameter(Identifier(ValueName(value))).WithType(ParseTypeName(TypeName(value.Type)))
            ))))
            .WithBody(body)
            .NormalizeWhitespace()
            .ToFullString();

        ExpressionSyntax Invoke(RouteCall call, string? continuationName = null)
        {
            SimpleNameSyntax name = IdentifierName("@" + call.Method.Name);
            if (call.Method.Arity != 0)
            {
                name = GenericName(Identifier("@" + call.Method.Name))
                    .WithTypeArgumentList(TypeArgumentList(SeparatedList(call.Method.TypeArguments.Select(type => ParseTypeName(TypeName(type))))));
            }

            var arguments = new List<ArgumentSyntax>();
            var valueIndex = 0;
            foreach (var parameter in call.Method.Parameters)
            {
                arguments.Add(Argument(IdentifierName(parameter.Ordinal == call.ContinuationParameterIndex
                    ? continuationName!
                    : ValueName(call.Arguments[valueIndex++])
                )));
            }

            ExpressionSyntax invocation = InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ParseName(TypeName(call.Method.ContainingType)), name),
                ArgumentList(SeparatedList(arguments))
            );

            if (continuationName is null && call.Method.ReturnType is INamedTypeSymbol named
                && (named.MetadataName == "Result`1"
                    || named.MetadataName == "Task`1"))
            {
                invocation = ObjectCreationExpression(returnType)
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(invocation))));
            }

            return invocation;
        }
    }

    private static string TypeName(ITypeSymbol type) => type.ToDisplayString(
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier)
    );

    private static string ValueName(RouteValue value) => "value" + value.Id;
}
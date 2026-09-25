using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Brigade.Net.Mise.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MiseJoinCapabilityAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MiseDiagnostics.UnsupportedJoin);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics
        );
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax member ||
            member.Name.Identifier.ValueText is not (
                "InnerJoin" or "LeftJoin" or "RightJoin" or "FullJoin" or "CrossJoin"
            ))
        {
            return;
        }
        var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;
        if (symbol is not IMethodSymbol method ||
            method.ContainingType.ToDisplayString() != "Brigade.Net.Mise.QueryBuilder")
        {
            return;
        }

        if (member.Name.Identifier.ValueText != "FullJoin")
        {
            return;
        }
        var engine = FindEngine(member.Expression, context.SemanticModel, context.CancellationToken);
        if (engine is "MySQL" or "MariaDB")
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    MiseDiagnostics.UnsupportedJoin,
                    member.Name.GetLocation(),
                    engine,
                    "FULL JOIN"
                )
            );
        }
    }

    private static string? FindEngine(
        ExpressionSyntax expression,
        SemanticModel model,
        System.Threading.CancellationToken cancellationToken
    )
    {
        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax member)
        {
            return FindEngine(member.Expression, model, cancellationToken);
        }
        if (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            return FindEngine(parenthesized.Expression, model, cancellationToken);
        }
        if (expression is IdentifierNameSyntax identifier &&
            model.GetSymbolInfo(identifier, cancellationToken).Symbol is ILocalSymbol local)
        {
            var declaration = local.DeclaringSyntaxReferences.First()
                .GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
            if (declaration?.Initializer?.Value is ExpressionSyntax initializer)
            {
                return FindEngine(initializer, model, cancellationToken);
            }
        }
        if (expression is ObjectCreationExpressionSyntax creation &&
            model.GetTypeInfo(creation, cancellationToken).Type is INamedTypeSymbol type)
        {
            var name = type.ToDisplayString();
            if (name == "Brigade.Net.Mise.MySQL.MySqlQueryBuilder")
            {
                return "MySQL";
            }
            if (name == "Brigade.Net.Mise.MariaDb.MariaDbQueryBuilder")
            {
                return "MariaDB";
            }
            if (name == "Brigade.Net.Mise.QueryBuilder" && creation.ArgumentList?.Arguments.Count == 1)
            {
                var dialect = model.GetTypeInfo(
                    creation.ArgumentList.Arguments[0].Expression,
                    cancellationToken
                ).Type?.ToDisplayString();
                return dialect switch
                {
                    "Brigade.Net.Mise.MySQL.MySqlDialect" => "MySQL",
                    "Brigade.Net.Mise.MariaDb.MariaDbDialect" => "MariaDB",
                    _ => null
                };
            }
        }
        var staticType = model.GetTypeInfo(expression, cancellationToken).Type?.ToDisplayString();
        return staticType switch
        {
            "Brigade.Net.Mise.MySQL.MySqlQueryBuilder" => "MySQL",
            "Brigade.Net.Mise.MariaDb.MariaDbQueryBuilder" => "MariaDB",
            _ => null
        };
    }
}

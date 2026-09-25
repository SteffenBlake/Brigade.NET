using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Brigade.Net.Mise.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MiseRawInterpolationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MiseDiagnostics.UnsafeRawInterpolation);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics
        );
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInterpolatedString, SyntaxKind.InterpolatedStringExpression);
    }

    private static void AnalyzeInterpolatedString(SyntaxNodeAnalysisContext context)
    {
        var interpolation = (InterpolatedStringExpressionSyntax)context.Node;
        if (!IsFormattableString(context.SemanticModel.GetTypeInfo(interpolation, context.CancellationToken).ConvertedType))
        {
            return;
        }

        foreach (var hole in interpolation.Contents.OfType<InterpolationSyntax>())
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (hole.FormatClause?.FormatStringToken.ValueText != "raw")
            {
                continue;
            }

            var constant = context.SemanticModel.GetConstantValue(hole.Expression, context.CancellationToken);
            if (!constant.HasValue || constant.Value is not string)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        MiseDiagnostics.UnsafeRawInterpolation,
                        hole.Expression.GetLocation()
                    )
                );
            }
        }
    }

    private static bool IsFormattableString(ITypeSymbol? type)
    {
        return type?.ToDisplayString() == "System.FormattableString";
    }
}

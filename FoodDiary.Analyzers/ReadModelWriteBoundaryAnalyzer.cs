using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReadModelWriteBoundaryAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "FD0018";
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, "Keep composed projections read-only",
        "Composed reads cannot acquire '{0}'; perform mutations through the owning module",
        "Architecture", DiagnosticSeverity.Warning, isEnabledByDefault: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start => {
            if (!string.Equals(start.Compilation.AssemblyName, "FoodDiary.ReadModel.Composition", StringComparison.Ordinal)) {
                return;
            }
            start.RegisterOperationAction(AnalyzeMethod, OperationKind.Invocation, OperationKind.MethodReference);
            start.RegisterOperationAction(AnalyzeProperty, OperationKind.PropertyReference);
            start.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);
        });
    }

    private static void AnalyzeMethod(OperationAnalysisContext context) {
        IMethodSymbol method = context.Operation is IInvocationOperation invocation
            ? invocation.TargetMethod : ((IMethodReferenceOperation)context.Operation).Method;
        bool ef = method.ContainingNamespace.ToDisplayString().StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
            || DerivesFrom(method.ContainingType, "Microsoft.EntityFrameworkCore.DbContext");
        if (IsAdoCapability(method.ContainingType) || (ef && (method.Name is
                "Add" or "AddAsync" or "AddRange" or "AddRangeAsync" or "Attach" or "AttachRange" or
                "Update" or "UpdateRange" or "Remove" or "RemoveRange" or "Entry" or "Find" or "FindAsync" or "AsTracking"
                || method.Name.StartsWith("SaveChanges", StringComparison.Ordinal)
                || method.Name.StartsWith("ExecuteUpdate", StringComparison.Ordinal)
                || method.Name.StartsWith("ExecuteDelete", StringComparison.Ordinal)
                || method.Name.StartsWith("ExecuteSql", StringComparison.Ordinal)
                || method.Name.StartsWith("FromSql", StringComparison.Ordinal)))) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), method.Name));
        }
    }

    private static void AnalyzeProperty(OperationAnalysisContext context) {
        var property = (IPropertyReferenceOperation)context.Operation;
        if (DerivesFrom(property.Property.ContainingType, "Microsoft.EntityFrameworkCore.DbContext")
            && property.Property.Name is "Database" or "ChangeTracker") {
            context.ReportDiagnostic(Diagnostic.Create(Rule, property.Syntax.GetLocation(), property.Property.Name));
        }
    }

    private static void AnalyzeConversion(OperationAnalysisContext context) {
        var conversion = (IConversionOperation)context.Operation;
        if (!conversion.IsImplicit && conversion.Type is INamedTypeSymbol type
            && DerivesFrom(type, "Microsoft.EntityFrameworkCore.DbContext")) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, conversion.Syntax.GetLocation(), type.Name));
        }
    }

    internal static bool IsAdoCapability(INamedTypeSymbol type) =>
        DerivesFrom(type, "System.Data.Common.DbCommand") || DerivesFrom(type, "System.Data.Common.DbConnection")
        || DerivesFrom(type, "System.Data.Common.DbTransaction")
        || type.ToDisplayString() is "System.Data.IDbCommand" or "System.Data.IDbConnection" or "System.Data.IDbTransaction";

    private static bool DerivesFrom(INamedTypeSymbol type, string name) {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType) {
            if (string.Equals(current.ToDisplayString(), name, StringComparison.Ordinal)) { return true; }
        }
        return false;
    }
}

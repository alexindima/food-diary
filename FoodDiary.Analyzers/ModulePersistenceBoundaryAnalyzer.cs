using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ModulePersistenceBoundaryAnalyzer : DiagnosticAnalyzer {
    public const string OwnershipDiagnosticId = "FD0015";
    public const string TechnicalDiagnosticId = "FD0016";

    private static readonly DiagnosticDescriptor OwnershipRule = new(
        OwnershipDiagnosticId, "Keep EF writes and tracking within the owning module",
        "Module '{0}' cannot acquire tracking or write capability for '{1}'; use an owner port or an explicit AsNoTracking read",
        "Architecture", DiagnosticSeverity.Warning, isEnabledByDefault: false);
    private static readonly DiagnosticDescriptor TechnicalRule = new(
        TechnicalDiagnosticId, "Review shared persistence escape APIs",
        "'{0}' requires an exact reviewed source fingerprint in persistence-technical-sources.txt",
        "Architecture", DiagnosticSeverity.Warning, isEnabledByDefault: false);

    private static readonly ImmutableHashSet<string> WriteMethods = ImmutableHashSet.Create(StringComparer.Ordinal,
        "Add", "AddAsync", "AddRange", "AddRangeAsync", "Update", "UpdateRange", "Remove", "RemoveRange",
        "Attach", "AttachRange", "ExecuteUpdate", "ExecuteUpdateAsync", "ExecuteDelete", "ExecuteDeleteAsync",
        "Entry", "Find", "FindAsync", "AsTracking");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [OwnershipRule, TechnicalRule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start => {
            string? module = ModuleOwner(start.Compilation.AssemblyName);
            if (module is null || !start.Compilation.AssemblyName!.EndsWith(".Infrastructure", StringComparison.Ordinal)) {
                return;
            }

            ImmutableDictionary<string, string> reviewed = ReadFingerprints(start.Options.AdditionalFiles, start.CancellationToken);
            var verified = new ConcurrentDictionary<SyntaxTree, bool>();
            start.RegisterOperationAction(operation => AnalyzeInvocation(operation, module, reviewed, verified), OperationKind.Invocation);
            start.RegisterOperationAction(operation => AnalyzeMethodReference(operation, module, reviewed, verified), OperationKind.MethodReference);
            start.RegisterOperationAction(operation => AnalyzeReference(operation, module, reviewed, verified),
                OperationKind.PropertyReference, OperationKind.FieldReference, OperationKind.ParameterReference);
        });
    }

    private static void AnalyzeMethodReference(OperationAnalysisContext context, string module,
        ImmutableDictionary<string, string> reviewed, ConcurrentDictionary<SyntaxTree, bool> verified) {
        var reference = (IMethodReferenceOperation)context.Operation;
        IMethodSymbol method = reference.Method;
        if (!IsEfType(method.ContainingType)) { return; }

        if (method.Name.StartsWith("SaveChanges", StringComparison.Ordinal) ||
            method.ContainingType.Name.EndsWith("DatabaseFacadeExtensions", StringComparison.Ordinal) ||
            string.Equals(method.ContainingType.Name, "DatabaseFacade", StringComparison.Ordinal)) {
            ReportTechnicalUnlessReviewed(context, method.Name, reviewed, verified);
            return;
        }

        if (!WriteMethods.Contains(method.Name) && !string.Equals(method.Name, "Set", StringComparison.Ordinal)) { return; }
        ITypeSymbol? target = method.TypeArguments.FirstOrDefault()
            ?? SequenceElement(reference.Instance?.Type)
            ?? method.Parameters.FirstOrDefault()?.Type;
        if (!IsOwned(target, module) && !IsReviewedAudit(target, context, reviewed, verified)) {
            ReportOwnership(context, module, target);
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, string module, ImmutableDictionary<string, string> reviewed, ConcurrentDictionary<SyntaxTree, bool> verified) {
        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod;
        if (!IsEfType(method.ContainingType)) { return; }

        if (method.Name.StartsWith("SaveChanges", StringComparison.Ordinal) ||
            method.ContainingType.Name.EndsWith("DatabaseFacadeExtensions", StringComparison.Ordinal) ||
            string.Equals(method.ContainingType.Name, "DatabaseFacade", StringComparison.Ordinal)) {
            ReportTechnicalUnlessReviewed(context, method.Name, reviewed, verified);
            return;
        }

        if (string.Equals(method.Name, "Set", StringComparison.Ordinal) && IsDbContext(method.ContainingType)) {
            ITypeSymbol? entity = method.TypeArguments.FirstOrDefault();
            if (!IsOwned(entity, module) && !IsReviewedAudit(entity, context, reviewed, verified) && !IsNoTrackingReceiver(invocation)) {
                ReportOwnership(context, module, entity);
            }
            return;
        }

        if (!WriteMethods.Contains(method.Name)) { return; }
        ITypeSymbol? target = method.TypeArguments.FirstOrDefault()
            ?? SequenceElement(invocation.Instance?.Type)
            ?? SequenceElement(invocation.Arguments.FirstOrDefault()?.Value.Type);
        if (IsDbContext(method.ContainingType) && target is null) {
            target = Unwrap(invocation.Arguments.FirstOrDefault()?.Value)?.Type;
        }
        if (!IsOwned(target, module) && !IsReviewedAudit(target, context, reviewed, verified)) {
            ReportOwnership(context, module, target);
        }
    }

    private static void AnalyzeReference(OperationAnalysisContext context, string module, ImmutableDictionary<string, string> reviewed, ConcurrentDictionary<SyntaxTree, bool> verified) {
        IOperation operation = context.Operation;
        if (operation is IPropertyReferenceOperation property && IsDbContext(property.Property.ContainingType) &&
            property.Property.Name is "Database" or "ChangeTracker") {
            ReportTechnicalUnlessReviewed(context, property.Property.Name, reviewed, verified);
            return;
        }

        if (operation.Type is not INamedTypeSymbol type || !string.Equals(type.OriginalDefinition.ToDisplayString(), "Microsoft.EntityFrameworkCore.DbSet<TEntity>", StringComparison.Ordinal)) {
            return;
        }
        ITypeSymbol entity = type.TypeArguments[0];
        if (!IsOwned(entity, module) && !IsNoTrackingReceiver(operation)) {
            ReportOwnership(context, module, entity);
        }
    }

    private static bool IsNoTrackingReceiver(IOperation operation) {
        IOperation? parent = operation.Parent;
        while (parent is IConversionOperation or IArgumentOperation) { parent = parent.Parent; }
        return parent is IInvocationOperation invocation && IsEfType(invocation.TargetMethod.ContainingType) &&
            invocation.TargetMethod.Name is "AsNoTracking" or "AsNoTrackingWithIdentityResolution";
    }

    private static IOperation? Unwrap(IOperation? operation) {
        while (operation is IConversionOperation conversion) { operation = conversion.Operand; }
        return operation;
    }

    private static ITypeSymbol? SequenceElement(ITypeSymbol? type) {
        if (type is IArrayTypeSymbol array) { return array.ElementType; }
        if (type is not INamedTypeSymbol named) { return null; }
        if (named.IsGenericType && (named.Name is "DbSet" or "IQueryable" or "IEnumerable")) { return named.TypeArguments[0]; }
        return named.AllInterfaces.FirstOrDefault(candidate => string.Equals(candidate.Name, "IEnumerable", StringComparison.Ordinal) && candidate.IsGenericType)?.TypeArguments[0];
    }

    private static bool IsOwned(ITypeSymbol? entity, string module) =>
        entity is not null && string.Equals(ModuleOwner(entity.ContainingAssembly?.Name), module, StringComparison.Ordinal);

    private static string? ModuleOwner(string? assembly) {
        const string prefix = "FoodDiary.Modules.";
        if (assembly?.StartsWith(prefix, StringComparison.Ordinal) != true) { return null; }
        string remainder = assembly.Substring(prefix.Length);
        int separator = remainder.IndexOf('.');
        return separator > 0 ? remainder.Substring(0, separator) : null;
    }

    private static bool IsEfType(INamedTypeSymbol type) =>
        type.ContainingNamespace.ToDisplayString().StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) || IsDbContext(type);

    private static bool IsDbContext(INamedTypeSymbol type) {
        for (INamedTypeSymbol? candidate = type; candidate is not null; candidate = candidate.BaseType) {
            if (string.Equals(candidate.ToDisplayString(), "Microsoft.EntityFrameworkCore.DbContext", StringComparison.Ordinal)) { return true; }
        }
        return false;
    }

    private static void ReportOwnership(OperationAnalysisContext context, string module, ITypeSymbol? entity) =>
        context.ReportDiagnostic(Diagnostic.Create(OwnershipRule, context.Operation.Syntax.GetLocation(), module, entity?.Name ?? "untyped entity"));

    private static bool IsReviewedAudit(ITypeSymbol? entity, OperationAnalysisContext context,
        ImmutableDictionary<string, string> reviewed, ConcurrentDictionary<SyntaxTree, bool> verified) =>
        string.Equals(entity?.ContainingAssembly?.Name, "FoodDiary.Audit.PersistenceModel", StringComparison.Ordinal) &&
        IsReviewed(context, reviewed, verified);

    private static void ReportTechnicalUnlessReviewed(OperationAnalysisContext context, string member,
        ImmutableDictionary<string, string> reviewed, ConcurrentDictionary<SyntaxTree, bool> verified) {
        if (!IsReviewed(context, reviewed, verified)) {
            context.ReportDiagnostic(Diagnostic.Create(TechnicalRule, context.Operation.Syntax.GetLocation(), member));
        }
    }

    private static bool IsReviewed(OperationAnalysisContext context,
        ImmutableDictionary<string, string> reviewed, ConcurrentDictionary<SyntaxTree, bool> verified) =>
        verified.GetOrAdd(context.Operation.Syntax.SyntaxTree, tree => {
            string normalizedPath = tree.FilePath.Replace('\\', '/');
            int start = normalizedPath.IndexOf("/Modules/", StringComparison.Ordinal);
            string relativePath = start >= 0 ? normalizedPath.Substring(start + 1) : normalizedPath;
            if (!reviewed.TryGetValue(relativePath, out string? expected)) { return false; }
            string source = tree.GetText(context.CancellationToken).ToString().Replace("\r\n", "\n").Trim();
            using var sha = SHA256.Create();
            string actual = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(source))).Replace("-", "").ToLowerInvariant();
            return string.Equals(actual, expected, StringComparison.Ordinal);
        });

    private static ImmutableDictionary<string, string> ReadFingerprints(ImmutableArray<AdditionalText> files, System.Threading.CancellationToken cancellationToken) {
        ImmutableDictionary<string, string>.Builder result = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        AdditionalText? file = files.FirstOrDefault(candidate => candidate.Path.Replace('\\', '/').EndsWith("/persistence-technical-sources.txt", StringComparison.Ordinal));
        if (file?.GetText(cancellationToken) is not { } text) { return result.ToImmutable(); }
        foreach (string line in text.ToString().Split('\n')) {
            string trimmed = line.Trim();
            int separator = trimmed.IndexOf(' ');
            if (separator != 64) { continue; }
            string path = trimmed.Substring(separator + 1).Trim();
            if (!path.StartsWith("Modules/", StringComparison.Ordinal) || result.ContainsKey(path)) { continue; }
            result.Add(path, trimmed.Substring(0, separator));
        }
        return result.ToImmutable();
    }
}

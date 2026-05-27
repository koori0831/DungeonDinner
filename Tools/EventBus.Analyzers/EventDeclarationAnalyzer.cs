using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Work.Core.Utils.EventBus.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EventDeclarationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DDEVENT001";

        private static readonly DiagnosticDescriptor EventMustBeReadonlyRecordStruct = new DiagnosticDescriptor(
            DiagnosticId,
            "EventBus events must be immutable value records",
            "Event type '{0}' must be declared as a readonly record struct",
            "EventBus",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "All types implementing IEvent must be readonly record structs.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(EventMustBeReadonlyRecordStruct);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(startContext =>
            {
                var eventInterface = startContext.Compilation.GetTypeByMetadataName("Work.Core.Utils.EventBus.IEvent");
                if (eventInterface == null)
                {
                    return;
                }

                startContext.RegisterSymbolAction(
                    symbolContext => AnalyzeNamedType(symbolContext, eventInterface),
                    SymbolKind.NamedType);
            });
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol eventInterface)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind == TypeKind.Interface || !type.AllInterfaces.Contains(eventInterface, SymbolEqualityComparer.Default))
            {
                return;
            }

            if (type.TypeKind == TypeKind.Struct && type.IsReadOnly && type.IsRecord)
            {
                return;
            }

            var location = type.Locations.Length > 0 ? type.Locations[0] : Location.None;
            context.ReportDiagnostic(Diagnostic.Create(EventMustBeReadonlyRecordStruct, location, type.Name));
        }
    }
}

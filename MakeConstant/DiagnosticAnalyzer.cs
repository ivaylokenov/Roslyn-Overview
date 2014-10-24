namespace MakeConstant
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // KNOWN BUGS:
    // 1. The Diagnostic Analyzer’s AnalyzeNode method does not check to see if the constant value is actually 
    //  convertible to the variable type.So, the current implementation will happily convert an incorrect
    //  declaration such as ‘int i = "abc"’ to a local constant.
    // 2. Reference types are not handled properly. The only constant value allowed for a reference type is null,
    //  except in this case of System.String, which allows string literals. In other words, ‘const string s =
    //  "abc"’ is legal, but ‘const object s = "abc"’ is not.
    // 3. If a variable is declared with the “var” keyword, the Code Fix does the wrong thing and generates a 
    //  “const var” declaration, which is not supported by the C# language. To fix this bug, the code fix must 
    //  replace the “var” keyword with the inferred type’s name.
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(DiagnosticId, LanguageNames.CSharp)]
    public class DiagnosticAnalyzer : ISyntaxNodeAnalyzer<SyntaxKind>
    {
        internal const string DiagnosticId = "MakeConst";
        internal const string Description = "Make Constant";
        internal const string MessageFormat = "Can be made constant";
        internal const string Category = "Usage";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
        {
            get
            {
                return ImmutableArray.Create(SyntaxKind.LocalDeclarationStatement);
            }
        }

        public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            var localNode = (LocalDeclarationStatementSyntax)node;

            if (localNode.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                return;
            }

            foreach (var variable in localNode.Declaration.Variables)
            {
                var initializer = variable.Initializer;
                if (initializer == null)
                {
                    return;
                }

                var constantValue = semanticModel.GetConstantValue(initializer.Value);
                if (!constantValue.HasValue)
                {
                    return;
                }
            }

            var analysis = semanticModel.AnalyzeDataFlow(localNode);
            foreach (var variable in localNode.Declaration.Variables)
            {
                var symbol = semanticModel.GetDeclaredSymbol(variable);
                if (analysis.WrittenOutside.Contains(symbol))
                {
                    return;
                }
            }

            addDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
        }
    }
}

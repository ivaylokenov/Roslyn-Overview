namespace CodeExamine
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Reflection;
    using Microsoft.CodeAnalysis.Text;

    public class CodeExaminer
    {
        public static void Main()
        {
            var sourceCode = File.ReadAllText("Code.cs");
            var tree = SyntaxFactory.ParseSyntaxTree(sourceCode);

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var reference = new MetadataFileReference(typeof(object).Assembly.Location);

            var compilation = CreateCompilation(tree, options, reference);
            var model = compilation.GetSemanticModel(tree);

            ShowLocalDeclarations(tree, model);
            CheckDiagnostics(compilation);
            // Greet(compilation);

            GetCodeCompletion(tree, model, sourceCode);
        }

        private static void GetCodeCompletion(SyntaxTree tree, SemanticModel model, string code)
        {
            var dotTextSpan = new TextSpan(code.IndexOf("now.", StringComparison.Ordinal) + "now.".Length - 1, 1);
            var memberAccessNode =
                (MemberAccessExpressionSyntax)tree.GetRoot().DescendantNodes(dotTextSpan).Last();

            var lhsType = model.GetTypeInfo(memberAccessNode.Expression).Type;

            foreach (var symbol in lhsType.GetMembers())
            {
                if (!symbol.CanBeReferencedByName || symbol.DeclaredAccessibility != Accessibility.Public
                    || symbol.IsStatic)
                {
                    continue;
                }

                Console.WriteLine(symbol.Name);
            }
        }

        private static void Greet(CSharpCompilation compilation)
        {
            using (var memory = new MemoryStream())
            {
                compilation.Emit(memory);

                var assembly = Assembly.Load(memory.GetBuffer());
                var type = assembly.GetType("CodeExamine.Greeter");
                var method = type.GetMethod("Greet");
                var greeter = Activator.CreateInstance(type);
                method.Invoke(greeter, null);
            }
        }

        private static void CheckDiagnostics(CSharpCompilation compilation)
        {
            var diagnostics = compilation.GetDiagnostics();

            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine(diagnostic.ToString());
            }
        }

        private static void ShowLocalDeclarations(SyntaxTree tree, SemanticModel model)
        {
            var localDeclarationNodes = tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>();

            foreach (var node in localDeclarationNodes)
            {
                var typeInfo = model.GetTypeInfo(node.Declaration.Type);
                Console.WriteLine("{0} {1}", typeInfo.Type, node.Declaration);
            }
        }

        private static CSharpCompilation CreateCompilation(SyntaxTree tree, CSharpCompilationOptions options, MetadataFileReference reference)
        {
            return CSharpCompilation
                .Create("Test")
                .WithOptions(options)
                .AddSyntaxTrees(tree)
                .AddReferences(reference);
        }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace TestGeneratorLib
{
    class SyntaxAnalyzer
    {
        public NamespaceInfo GetNamespaceInfoForCode(string code)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

            List<string> usings = GetUsingsList(root.DescendantNodes());


            List<ClassInfo> classInfoList = new List<ClassInfo>();
            List<ClassDeclarationSyntax> classDeclarations = SelectClassDeclarationsFrom(root.DescendantNodes());
            foreach (ClassDeclarationSyntax classDeclaration in classDeclarations)
            {
                classInfoList.Add(CreateClassInfo(classDeclaration));
            }

            return new NamespaceInfo(usings, classInfoList);
        }

        private List<string> GetUsingsList(IEnumerable<SyntaxNode> members)
        {
            return members.OfType<UsingDirectiveSyntax>().Select(usingSyntax => usingSyntax.Name.ToString()).ToList();
        }

        private List<ClassDeclarationSyntax> SelectClassDeclarationsFrom(IEnumerable<SyntaxNode> syntaxNodes)
        {
            return syntaxNodes.OfType<ClassDeclarationSyntax>().ToList();
        }

        private ClassInfo CreateClassInfo(ClassDeclarationSyntax classDeclaration)
        {
            List<MethodDeclarationSyntax> publicMethods = SelectPublicNodes<MethodDeclarationSyntax>(classDeclaration.Members);
            List<ConstructorDeclarationSyntax> constructors = SelectPublicNodes<ConstructorDeclarationSyntax>(classDeclaration.Members);

            List<MethodInfo> methodInfos = new List<MethodInfo>();
            foreach (MethodDeclarationSyntax methodDeclaration in publicMethods)
            {
                methodInfos.Add(CreateMethodInfo(methodDeclaration));
            }

            List<ParameterSyntax> suitableConstructorParametres = GetSuitableConstructorParametres(constructors);

            return new ClassInfo(GetNamespaceName(classDeclaration), classDeclaration.Identifier.ValueText, 
                                                    suitableConstructorParametres, methodInfos);
        }

        private List<T> SelectPublicNodes<T>(IEnumerable<MemberDeclarationSyntax> members) where T : MemberDeclarationSyntax
        {
            return members.OfType<T>()
                .Where(methodDeclaration => methodDeclaration.Modifiers.Select(modifire =>
                                            modifire.IsKind(SyntaxKind.PublicKeyword)).Any())
                .ToList();
        }

        private MethodInfo CreateMethodInfo(MethodDeclarationSyntax methodDeclaration)
        {
            return new MethodInfo(methodDeclaration.Identifier.ValueText, methodDeclaration.ReturnType, 
                new List<ParameterSyntax>(methodDeclaration.ParameterList.Parameters));
        }
        private List<ParameterSyntax> GetSuitableConstructorParametres(List<ConstructorDeclarationSyntax> constructors)
        {
            List<ParameterSyntax> parameters = new List<ParameterSyntax>();
            if (constructors.Count == 0)
                return parameters;

            ConstructorDeclarationSyntax minParametresConstructor = constructors[0];
            foreach (var constructor in constructors.Skip(1))
            {
                if (constructor.ParameterList.Parameters.Count < minParametresConstructor.ParameterList.Parameters.Count)
                    minParametresConstructor = constructor;

            }

            parameters.AddRange(minParametresConstructor.ParameterList.Parameters);
            return parameters;
        }

        private string GetNamespaceName(SyntaxNode syntaxNode)
        {
            while (syntaxNode != null && !syntaxNode.IsKind(SyntaxKind.NamespaceDeclaration))
            {
                syntaxNode = syntaxNode.Parent;
            }

            return syntaxNode == null ? "" : ((NamespaceDeclarationSyntax) syntaxNode).Name.ToString();
        }
    }
}

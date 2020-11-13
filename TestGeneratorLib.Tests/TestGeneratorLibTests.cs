using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace TestGeneratorLib.Tests
{
    public class TestGeneratorLibTests
    {
        private static readonly string CLASS_FILE_PATH = "D:/!Университет/5 семестр/СПП/TestGenerator/AssemblyForTests/SomeClass.cs";
        private readonly List<string> DEFAULT_USINGS = new List<string>
        {
            "Xunit",
            "System",
            "System.Collections.Generic",
            "System.Text",
            "Moq"
        };

        private readonly List<TestGenerationResult> testGenerationResults;

        public TestGeneratorLibTests()
        {
            TestGenerator testGenerator = new TestGenerator();
            string code = new StreamReader(CLASS_FILE_PATH).ReadToEnd();

            testGenerationResults = testGenerator.GenerateTestsForCode(code);
        }
        
        [Fact]
        public void ShouldReturnTwoTestClasses()
        {
            Assert.NotNull(testGenerationResults);
            Assert.Equal(2, testGenerationResults.Count);
        }

        [Fact]
        public void ShouldContainTestMethods()
        {
            TestGenerationResult someClassResult = testGenerationResults[0];
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(someClassResult.TestClassCode).GetCompilationUnitRoot();

            List<MethodDeclarationSyntax> publicMethods = GetPublicMethods(root.DescendantNodes());
            List<string> methodNames = GetMethodNames(publicMethods);

            Assert.Contains("FirstMethodTest", methodNames);
            Assert.Contains("SecondMethodTest", methodNames);
        }

        [Fact]
        public void EveryMethodShouldContainActAttribute()
        {
            TestGenerationResult someClassResult = testGenerationResults[0];
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(someClassResult.TestClassCode).GetCompilationUnitRoot();

            List<MethodDeclarationSyntax> publicMethods = GetPublicMethods(root.DescendantNodes());
            List<string> methodNames = GetMethodNames(publicMethods);

            foreach (MethodDeclarationSyntax method in publicMethods)
            {
                List<string> methodAttributes = method.DescendantNodes().OfType<AttributeSyntax>().
                    Select(attribute => attribute.Name.ToString()).ToList();
                Assert.Contains("Fact", methodAttributes);
            }
        }

        [Fact]
        public void EveryMethodShouldContainAssert()
        {
            TestGenerationResult someClassResult = testGenerationResults[0];
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(someClassResult.TestClassCode).GetCompilationUnitRoot();

            List<MethodDeclarationSyntax> publicMethods = GetPublicMethods(root.DescendantNodes());
            List<string> methodNames = GetMethodNames(publicMethods);

            foreach (MethodDeclarationSyntax method in publicMethods)
            {
                List<string> identifireNmaes = method.DescendantNodes().OfType<IdentifierNameSyntax>().
                    Select(identifire => identifire.Identifier.ToString()).ToList();
                Assert.Contains("Assert", identifireNmaes);
            }
        }

        [Fact]
        public void ShouldContainDefaultUsings()
        {
            TestGenerationResult someClassResult = testGenerationResults[0];
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(someClassResult.TestClassCode).GetCompilationUnitRoot();

            List<UsingDirectiveSyntax> usings = GetUsings(root.DescendantNodes());
            List<string> usingNames = GetUsingNames(usings);

            foreach (string usingName in DEFAULT_USINGS)
            {
                Assert.Contains(usingName, usingNames);
            }
        }

        private List<MethodDeclarationSyntax> GetPublicMethods(IEnumerable<SyntaxNode> members)
        {
            return members.OfType<MethodDeclarationSyntax>()
                .Where(methodDeclaration => methodDeclaration.Modifiers.Select(modifire =>
                                            modifire.IsKind(SyntaxKind.PublicKeyword)).Any())
                .ToList();
        }

        private List<string> GetMethodNames(List<MethodDeclarationSyntax> methods)
        {
            return methods.Select(method => method.Identifier.ToString()).ToList();
        }

        private List<UsingDirectiveSyntax> GetUsings(IEnumerable<SyntaxNode> members)
        {
            return members.OfType<UsingDirectiveSyntax>().ToList();
        }

        private List<string> GetUsingNames(List<UsingDirectiveSyntax> usings)
        {
            return usings.Select(usingDeclaration => usingDeclaration.Name.ToString()).ToList();
        }
    }
}

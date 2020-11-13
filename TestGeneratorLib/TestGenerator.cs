using System.Collections.Generic;

namespace TestGeneratorLib
{
    public class TestGenerator
    {
        private readonly SyntaxAnalyzer syntaxAnalyzer;
        private readonly CodeGenerator codeGenerator;

        public TestGenerator()
        {
            syntaxAnalyzer = new SyntaxAnalyzer();
            codeGenerator = new CodeGenerator();
        }

        public List<TestGenerationResult> GenerateTestsForCode(string code)
        {
            NamespaceInfo namespaceInfo = syntaxAnalyzer.GetNamespaceInfoForCode(code);
            return codeGenerator.GenerateTestsForClasses(namespaceInfo);
        }
    }
}

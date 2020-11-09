using System.Threading.Tasks;
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

        public async Task<List<TestGenerationResult>> GenerateTestsForCode(string code)
        {
            List<ClassInfo> classInfos = syntaxAnalyzer.GetClassInfoListForCode(code);
            return codeGenerator.GenerateTestsForClasses(classInfos);
        }
    }
}

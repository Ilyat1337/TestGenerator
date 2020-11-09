namespace TestGeneratorLib
{
    public class TestGenerationResult
    { 
        public string TestClassName
        { get; }

        public string TestClassCode
        { get; }

        public TestGenerationResult(string testClassName, string testClassCode)
        {
            TestClassName = testClassName;
            TestClassCode = testClassCode;
        }
    }
}

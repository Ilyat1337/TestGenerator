using System;
using System.IO;
using System.Threading.Tasks.Dataflow;
using TestGeneratorLib;

namespace TestGeneratorDemo
{
    class Program
    {
        private static readonly string CSHARP_FILE_EXT = ".cs";

        private static readonly int ARGUMENT_COUNT = 3;
        private static readonly int SOURCE_DIRECTORY_ARGUMENT_INDEX = 0;
        private static readonly int OUTPUT_DIRECTORY_ARGUMENT_INDEX = 1;
        private static readonly int PARALLELISM_DEGREE_ARGUMENT_INDEX = 2;

        static void Main(string[] args)
        {
            if (args.Length != ARGUMENT_COUNT)
                throw new ArgumentException(string.Format("Wrong argument count. Expected: {0}, actual: {1}", ARGUMENT_COUNT, args.Length));

            string sourceDirectory = ValidateSourceDirectory(args[SOURCE_DIRECTORY_ARGUMENT_INDEX]);
            string outputDirectory = args[OUTPUT_DIRECTORY_ARGUMENT_INDEX];
            int degreeOfParallelism = ValidateParallelismDegreeParam(args[PARALLELISM_DEGREE_ARGUMENT_INDEX]);

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            TestGenerator testGenerator = new TestGenerator();

            ExecutionDataflowBlockOptions executionOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = degreeOfParallelism,
                EnsureOrdered = false
            };

            Program program = new Program();
            var fileReadBlock = program.GetFileReadBlock(executionOptions);
            var testGeneratorBlock = program.GetTestGeneratorBlock(executionOptions, testGenerator);
            var fileWriteBlock = program.GetFileWriteBlock(executionOptions, outputDirectory);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            fileReadBlock.LinkTo(testGeneratorBlock, linkOptions);
            testGeneratorBlock.LinkTo(fileWriteBlock, linkOptions);

            foreach (string fileName in Directory.EnumerateFiles(sourceDirectory, string.Format("*{0}", CSHARP_FILE_EXT)))
            {
                fileReadBlock.Post(fileName);
            }

            fileReadBlock.Complete();
            fileWriteBlock.Completion.Wait();
        }

        private static string ValidateSourceDirectory(string sourceDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                throw new ArgumentException("Directory with name {0} does not exist", sourceDirectory);
            }
            return sourceDirectory;
        }

        private static int ValidateParallelismDegreeParam(string degreeOfParallelismStr)
        {
            int degreeOfParallelism;
            try
            {
                degreeOfParallelism = int.Parse(degreeOfParallelismStr);
            }
            catch (FormatException)
            {
                throw new ArgumentException(string.Format("Argument with index {0} must be an integer number", PARALLELISM_DEGREE_ARGUMENT_INDEX));
            }
            if (degreeOfParallelism <= 0)
            {
                throw new ArgumentOutOfRangeException(string.Format("Argument with index {0} must be greater than 0", PARALLELISM_DEGREE_ARGUMENT_INDEX));
            }
            return degreeOfParallelism;
        }

        private TransformBlock<string, string> GetFileReadBlock(ExecutionDataflowBlockOptions options)
        {
            return new TransformBlock<string, string>(fileName =>
            {
                Console.WriteLine(string.Format("Reading file {0}", Path.GetFileName(fileName)));
                return new StreamReader(fileName).ReadToEnd();
            },             
            options);
        }

        private TransformManyBlock<string, TestGenerationResult> GetTestGeneratorBlock(ExecutionDataflowBlockOptions options, TestGenerator testGenerator)
        {
            return new TransformManyBlock<string, TestGenerationResult>(code =>
            {
                Console.WriteLine("Generating tests");
                return testGenerator.GenerateTestsForCode(code);
            }, 
            options);
        }

        private ActionBlock<TestGenerationResult> GetFileWriteBlock(ExecutionDataflowBlockOptions options, string outputDirectoryName)
        {
            return new ActionBlock<TestGenerationResult>(testGenerationResult =>
            {
                string outputFileName = testGenerationResult.TestClassName + CSHARP_FILE_EXT;
                Console.WriteLine(string.Format("Writing file {0}", outputFileName));
                File.WriteAllText(Path.Combine(outputDirectoryName, outputFileName), 
                    testGenerationResult.TestClassCode);
            },
            options); 
        }
    }
}

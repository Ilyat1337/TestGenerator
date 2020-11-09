using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace TestGeneratorLib
{
    class CodeGenerator
    {
        private readonly string[] DEFAULT_USINGS =
        {
            "Xunit",
            "System",
            "System.Collections.Generic",
            "System.Text",
            "using Moq"
        };
        private readonly string VOID_KEYWORD = "void";

        private readonly string CLASS_TEST_POSTFIX = "Tests";
        private readonly string METHOD_TEST_POSTFIX = "Test";

        private readonly string FACT_ANNOTATION = "Fact";
        private readonly string ARRANGE_COMMENT = "//Arrange";
        private readonly string ACT_COMMENT = "//Act";
        private readonly string ASSERT_COMMENT = "//Assert";

        private readonly string FAIL_ASSERT_STATEMENT = "Assert.True(false, \"This test needs an implementation\");";
        private readonly string EQUAL_ASSERT_STATEMENT_FORMAT = "Assert.Equal({0}, {1});";

        private readonly string ACTUAL_VARIABLE_NAME = "actual";
        private readonly string EXPECTED_VARIABLE_NAME = "expected";

        private readonly string MOCK_VARIABLE_FORMAT = "Mock<{0}>";
        private readonly string MOCK_CREATION_FORMAT = "new Mock<{0}>()";
        private readonly string MOCK_OBJECT_ACCESS_FORMAT = "{0}.Object";

        public List<TestGenerationResult> GenerateTestsForClasses(List<ClassInfo> classInfos)
        {
            List<TestGenerationResult> testGenerationResults = new List<TestGenerationResult>();
            foreach (ClassInfo classInfo in classInfos)
            {
                testGenerationResults.Add(GetTestGenerationResult(classInfo));
            }
            return testGenerationResults;
        }

        private TestGenerationResult GetTestGenerationResult(ClassInfo classInfo)
        {
            var compilationUnit = SF.CompilationUnit();
            AddDefaultUsings(ref compilationUnit);
            AddUsing(ref compilationUnit, classInfo.NamespaceName);

            NamespaceDeclarationSyntax namespaceDeclaration = SF.NamespaceDeclaration(
                        SF.ParseName(classInfo.NamespaceName + "." + CLASS_TEST_POSTFIX));

            string testClassName = classInfo.Name + CLASS_TEST_POSTFIX;
            ClassDeclarationSyntax classDeclaration = GetTestClassDeclaration(testClassName, classInfo);

            namespaceDeclaration = namespaceDeclaration.AddMembers(classDeclaration);
            compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);

            return new TestGenerationResult(testClassName, compilationUnit.NormalizeWhitespace().ToFullString());
        }

        private void AddDefaultUsings(ref CompilationUnitSyntax compilationUnit)
        {
            foreach (string defaultusing in DEFAULT_USINGS)
            {
                AddUsing(ref compilationUnit, defaultusing);
            }
        }

        private void AddUsing(ref CompilationUnitSyntax compilationUnit, string usingName)
        {
            compilationUnit = compilationUnit.AddUsings(SF.UsingDirective(SF.ParseName(usingName)));
        }

        private ClassDeclarationSyntax GetTestClassDeclaration(string testClassName, ClassInfo classInfo)
        {
            String testClassObjectName = GetObjectNameForClass(classInfo.Name);

            var classDeclaration = SF.ClassDeclaration(testClassName);

            FieldDeclarationSyntax testObjectDeclaration = GetPrivateFieldDeclaration(classInfo.Name, testClassObjectName);
            List<FieldDeclarationSyntax> interfaceDeclarations = GetInterfaceDeclarations(classInfo.ConstructorParametres);

            ConstructorDeclarationSyntax constructorDeclaration = GetSetupConstructorDeclaration(testClassName, classInfo.Name, classInfo.ConstructorParametres);

            List<MethodDeclarationSyntax> methodDeclarations = GetTestMethodDeclarations(classInfo.Methods, testClassObjectName);

            classDeclaration = classDeclaration.AddMembers(testObjectDeclaration).AddMembers(interfaceDeclarations.ToArray())
                .AddMembers(constructorDeclaration).AddMembers(methodDeclarations.ToArray());

            return classDeclaration;
        }

        private string GetObjectNameForClass(string className)
        {
            return Char.ToLower(className[0]) + className.Substring(1);
        }

        private FieldDeclarationSyntax GetPrivateFieldDeclaration(string className, string fieldName)
        {
            return SF.FieldDeclaration(SF.VariableDeclaration(SF.ParseTypeName(className))
                .AddVariables(SF.VariableDeclarator(fieldName))).
                AddModifiers(SF.Token(SyntaxKind.PrivateKeyword));
        }

        private List<FieldDeclarationSyntax> GetInterfaceDeclarations(List<ParameterSyntax> parameters)
        {
            List<FieldDeclarationSyntax> fieldDeclarations = new List<FieldDeclarationSyntax>();
            foreach (ParameterSyntax parameter in parameters)
            {
                if (IsInterfaceName(parameter.Type.ToString()))
                {
                    fieldDeclarations.Add(GetPrivateFieldDeclaration(
                        String.Format(MOCK_VARIABLE_FORMAT, parameter.Type.ToString()),
                        InterfaceToFieldName(parameter.Type.ToString())));
                }
            }
            return fieldDeclarations;
        }

        private bool IsInterfaceName(string className)
        {
            return className.Length > 1 && className[0] == 'I' && char.IsUpper(className[1]);
        }

        private string InterfaceToFieldName(string interfaceName)
        {
            return GetObjectNameForClass(interfaceName.Substring(1));
        }

        private ConstructorDeclarationSyntax GetSetupConstructorDeclaration(string testClassName, string className, List<ParameterSyntax> constructorParametres)
        {
            var methodBodyBlock = SF.Block();

            List<StatementSyntax> localDeclarations = new List<StatementSyntax>();
            foreach (ParameterSyntax parameter in constructorParametres)
            {
                localDeclarations.Add(GetSetupVariableDeclaration(parameter));
            }

            AssignmentExpressionSyntax constructorInvocation = GetConstructorInvokation(testClassName, constructorParametres);

            methodBodyBlock = methodBodyBlock.AddStatements(localDeclarations.ToArray())
                .AddStatements(SF.ExpressionStatement(constructorInvocation));

            var constructorDeclaration = SF.ConstructorDeclaration(testClassName)
                .AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
                .WithBody(methodBodyBlock);
            return constructorDeclaration;
        }

        private StatementSyntax GetSetupVariableDeclaration(ParameterSyntax parameter)
        {
            if (IsInterfaceName(parameter.Type.ToString()))
            {
                return SF.ExpressionStatement(SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, 
                    SF.IdentifierName(InterfaceToFieldName(parameter.Type.ToString())),
                    SF.ParseExpression(string.Format(MOCK_CREATION_FORMAT, parameter.Type.ToString()))));
            }
            else
            {
                return GetLocalVariableDeclaration(parameter.Type, parameter.Identifier,
                    SyntaxAnalysisUtils.TypeToDefaultValue(parameter.Type));
            }
        }

        private AssignmentExpressionSyntax GetConstructorInvokation(string className, List<ParameterSyntax> constructorParametres)
        {
            ArgumentListSyntax argumentList = SF.ArgumentList();
            foreach (ParameterSyntax parameter in constructorParametres)
            {
                if (IsInterfaceName(parameter.Type.ToString()))
                {
                    argumentList = argumentList.AddArguments(SF.Argument(SF.ParseExpression(
                        string.Format(MOCK_OBJECT_ACCESS_FORMAT, InterfaceToFieldName(parameter.Type.ToString())))));
                }
                else
                {
                    argumentList = argumentList.AddArguments(SF.Argument(SF.IdentifierName(parameter.Identifier.ToString())));
                }
            }

            return SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SF.IdentifierName(GetObjectNameForClass(className)),
                SF.ObjectCreationExpression(SF.IdentifierName(className)).WithArgumentList(argumentList));
        }

        private List<MethodDeclarationSyntax> GetTestMethodDeclarations(List<MethodInfo> methods, string testClassObjectName)
        {
            List<MethodDeclarationSyntax> methodDeclarations = new List<MethodDeclarationSyntax>();
            foreach (MethodInfo methodInfo in methods)
            {
                methodDeclarations.Add(GetTestMethodDeclaration(methodInfo, testClassObjectName));
            }
            return methodDeclarations;
        }

        private MethodDeclarationSyntax GetTestMethodDeclaration(MethodInfo methodInfo, string testClassObjectName)
        {           
            var methodBodyBlock = SF.Block();

            List<LocalDeclarationStatementSyntax> localDeclarations = GetLocalVariableDeclarations(methodInfo.Parameters);
            if (localDeclarations.Count != 0)
                localDeclarations[0] = localDeclarations[0].WithLeadingTrivia(SF.Comment(ARRANGE_COMMENT));
            methodBodyBlock = methodBodyBlock.AddStatements(localDeclarations.ToArray());

            var generatedAssertExpression = SF.ParseStatement(FAIL_ASSERT_STATEMENT);

            InvocationExpressionSyntax methodInvokation = GetMethodInvokation(methodInfo, testClassObjectName);

            if (methodInfo.ReturnType.ToString().Trim().Equals(VOID_KEYWORD))
            {
                var methodExpression = SF.ExpressionStatement(methodInvokation).WithLeadingTrivia(SF.Comment(ACT_COMMENT));
                var assertExpression = generatedAssertExpression.WithLeadingTrivia(SF.Comment(ASSERT_COMMENT));
                methodBodyBlock = methodBodyBlock.AddStatements(methodExpression, assertExpression);
            }
            else
            {
                var expectedAssignmentExpression = GetLocalVariableDeclaration(methodInfo.ReturnType,
                    SF.Identifier(ACTUAL_VARIABLE_NAME), methodInvokation)
                    .WithLeadingTrivia(SF.Comment(ACT_COMMENT)); ;
                var expectedVariableDeclaration = GetLocalVariableDeclaration(methodInfo.ReturnType,
                    SF.Identifier(EXPECTED_VARIABLE_NAME), SyntaxAnalysisUtils.TypeToDefaultValue(methodInfo.ReturnType))
                    .WithLeadingTrivia(SF.Comment(ASSERT_COMMENT));
                var assertEqualExpression = SF.ParseStatement(String.Format(EQUAL_ASSERT_STATEMENT_FORMAT, EXPECTED_VARIABLE_NAME,
                    ACTUAL_VARIABLE_NAME));
                methodBodyBlock = methodBodyBlock.AddStatements(expectedAssignmentExpression, expectedVariableDeclaration,
                    assertEqualExpression, generatedAssertExpression);
            }           

            var methodDeclaration = SF.MethodDeclaration(SF.PredefinedType(SF.Token(SyntaxKind.VoidKeyword)), methodInfo.Name + METHOD_TEST_POSTFIX)
                .AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
                .WithBody(methodBodyBlock);
            methodDeclaration = methodDeclaration.AddAttributeLists(SF.AttributeList(
                SF.SingletonSeparatedList(SF.Attribute(SF.IdentifierName(FACT_ANNOTATION)))));
            return methodDeclaration;
        }

        private List<LocalDeclarationStatementSyntax> GetLocalVariableDeclarations(List<ParameterSyntax> parameters)
        {
            List<LocalDeclarationStatementSyntax> localDeclarations = new List<LocalDeclarationStatementSyntax>();
            foreach (ParameterSyntax parameter in parameters)
            {
                localDeclarations.Add(GetLocalVariableDeclaration(parameter.Type, 
                    parameter.Identifier, SyntaxAnalysisUtils.TypeToDefaultValue(parameter.Type)));
            }
            return localDeclarations;
        }

        private LocalDeclarationStatementSyntax GetLocalVariableDeclaration(TypeSyntax type, SyntaxToken identifier, 
                                                                                    ExpressionSyntax initializer)
        {
            return SF.LocalDeclarationStatement(SF.VariableDeclaration(type)
                                        .WithVariables(
                                            SF.SingletonSeparatedList(
                                                SF.VariableDeclarator(identifier)
                                                .WithInitializer(SF.EqualsValueClause(initializer)))));
        }

        private InvocationExpressionSyntax GetMethodInvokation(MethodInfo methodInfo, string objectName)
        {
            ArgumentListSyntax argumentList = SF.ArgumentList();
            argumentList = argumentList.AddArguments(methodInfo.Parameters.Select(
                parameter => SF.Argument(SF.IdentifierName(parameter.Identifier.ValueText))).ToArray());
            return SF.InvocationExpression(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                SF.IdentifierName(objectName), SF.IdentifierName(methodInfo.Name)))
                .WithArgumentList(argumentList);
        }
    }
}

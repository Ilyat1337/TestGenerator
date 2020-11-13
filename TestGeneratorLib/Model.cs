using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace TestGeneratorLib
{
    internal class NamespaceInfo
    {
        public List<string> Usings
        { get; }

        public List<ClassInfo> Classes
        { get; }

        public NamespaceInfo(List<string> usings, List<ClassInfo> classes)
        {
            Usings = usings;
            Classes = classes;
        }
    }

    internal class ClassInfo
    { 
        public string NamespaceName
        { get; }

        public string Name
        { get; }

        public List<ParameterSyntax> ConstructorParametres
        { get; }

        public List<MethodInfo> Methods
        { get; }

        public ClassInfo(string namespaceName, string name, List<ParameterSyntax> constructorParametres, List<MethodInfo> methods)
        {
            NamespaceName = namespaceName;
            Name = name;
            ConstructorParametres = constructorParametres;
            Methods = methods;
        }

        public bool HasSpecialConstructor()
        {
            return ConstructorParametres.Count != 0;
        }
    }

    internal class MethodInfo
    {
        public string Name
        { get; }

        public TypeSyntax ReturnType
        { get; }

        public List<ParameterSyntax> Parameters
        { get; }

        public MethodInfo(string name, TypeSyntax returnType, List<ParameterSyntax> parameters)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
        }
    }
}

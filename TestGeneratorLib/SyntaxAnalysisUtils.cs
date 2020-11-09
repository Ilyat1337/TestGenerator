using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestGeneratorLib
{
    class SyntaxAnalysisUtils
    {
        public static ExpressionSyntax TypeToDefaultValue(TypeSyntax typeSyntax)
        {
            if (typeSyntax is PredefinedTypeSyntax)
            {
                switch (((PredefinedTypeSyntax) typeSyntax).Keyword.Kind())
                {
                    case SyntaxKind.IntKeyword:
                    case SyntaxKind.ShortKeyword:
                    case SyntaxKind.ByteKeyword:
                        return ParseExpression("0");
                    case SyntaxKind.UIntKeyword:
                    case SyntaxKind.UShortKeyword:
                        return ParseExpression("0u");
                    case SyntaxKind.LongKeyword:
                        return ParseExpression("0l");
                    case SyntaxKind.ULongKeyword:
                        return ParseExpression("0ul");
                    case SyntaxKind.DoubleKeyword:
                        return ParseExpression("0d");
                    case SyntaxKind.FloatKeyword:
                        return ParseExpression("0f");
                    case SyntaxKind.StringKeyword:
                        return ParseExpression("\"\"");
                    case SyntaxKind.CharKeyword:
                        return ParseExpression("'\0'");
                    case SyntaxKind.BoolKeyword:
                        return ParseExpression("false");
                    default:
                        return DefaultExpression(typeSyntax);
                }
            }
            else
            {
                return ParseExpression("null");
            }
        }
    }
}

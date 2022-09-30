using System.Collections.Generic;
using System.Globalization;

namespace FrogSharp.Tools
{
    public static class Calculator
    {
        private static readonly Dictionary<int, int> MultiplicationTokens = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> DivisionTokens = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> AdditionTokens = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> SubtractionTokens = new Dictionary<int, int>();

        public static CultureInfo CultureInfo = CultureInfo.CurrentCulture;

        public static float Evaluate(string expression)
        {
            var whitespaces = expression.Split(' ');
            for (var i = 0; i < whitespaces.Length / 2; i++)
                expression = EvaluateExpressionUnit(expression);
            
            return float.Parse(expression, NumberStyles.Any, CultureInfo);
        }

        private static string EvaluateExpressionUnit(string expression)
        {
            var whitespaces = expression.Split(' ');

            MultiplicationTokens.Clear();
            DivisionTokens.Clear();
            AdditionTokens.Clear();
            SubtractionTokens.Clear();

            var currentTokenId = 0;

            for (var i = 0; i < expression.Length; i++)
            {
                if (expression[i] == ' ')
                    currentTokenId++;

                switch (expression[i])
                {
                    case '*':
                        MultiplicationTokens.Add(currentTokenId, i);
                        break;
                    case '/':
                        DivisionTokens.Add(currentTokenId, i);
                        break;
                    case '+':
                        AdditionTokens.Add(currentTokenId, i);
                        break;
                    case '-':
                        SubtractionTokens.Add(currentTokenId, i);
                        break;
                }
            }

            var multiplicationCheck = CheckTokens(MathOperation.Multiplication, MultiplicationTokens, whitespaces, expression);
            if (multiplicationCheck != expression) return multiplicationCheck;

            var divisionCheck = CheckTokens(MathOperation.Division, DivisionTokens, whitespaces, expression);
            if (divisionCheck != expression) return divisionCheck;

            var additionCheck = CheckTokens(MathOperation.Addition, AdditionTokens, whitespaces, expression);
            if (additionCheck != expression) return additionCheck;

            var subtractionCheck = CheckTokens(MathOperation.Subtraction, SubtractionTokens, whitespaces, expression);
            return subtractionCheck != expression ? subtractionCheck : expression;
        }

        private static string CheckTokens(MathOperation operation, Dictionary<int, int> tokens, IReadOnlyList<string> whitespaces,
            string expression)
        {
            foreach (var token in tokens)
            {
                var a = whitespaces[token.Key - 1];
                var b = whitespaces[token.Key + 1];
                var len = a.Length + b.Length + 3;
                return expression.Remove(tokens[token.Key] - a.Length - 1, len).Insert(tokens[token.Key] - a.Length - 1,
                    EvaluateOperation(operation, 
                        float.Parse(a, NumberStyles.Any, CultureInfo),
                        float.Parse(b, NumberStyles.Any, CultureInfo))
                        .ToString(CultureInfo.CurrentCulture));
            }

            return expression;
        }

        private static float EvaluateOperation(MathOperation operation, float a, float b)
        {
            switch (operation)
            {
                case MathOperation.Multiplication:
                    return a * b;
                case MathOperation.Division:
                    return a / b;
                case MathOperation.Addition:
                    return a + b;
                case MathOperation.Subtraction:
                    return a - b;
                default:
                    return 0;
            }
        }

        private enum MathOperation
        {
            Multiplication,
            Division,
            Addition,
            Subtraction
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using FrogSharp.Common;
using FrogSharp.Tools;

namespace FrogSharp.Core
{
    public class FsCallable
    {
        protected FsCallable Parent;
        public readonly Dictionary<string, int> Arguments = new Dictionary<string, int>();
        protected readonly Dictionary<string, FsObject> Variables = new Dictionary<string, FsObject>();
        protected readonly List<FsCodeElement> Elements = new List<FsCodeElement>();
        protected readonly Dictionary<string, FsCallable> Methods = new Dictionary<string, FsCallable>();
        private readonly Dictionary<string, float> Temporary = new Dictionary<string, float>();
        protected ReturnEvent Returnable;

        public float GetAsSingle(string variable) => GetObject(variable).AsSingle;
        public string GetAsString(string variable) => GetObject(variable).AsString;
        public bool GetAsBoolean(string variable) => GetObject(variable).AsBoolean;
        public int GetAsInteger(string variable) => GetObject(variable).AsInteger;
        private static CultureInfo cultureInfo;

        public FsObject GetObject(string variable)
        {
            if (Variables.ContainsKey(variable)) return Variables[variable];
            if (Parent == this) return GetFromMethod(variable);
            
            var obj =  Parent.GetObject(variable);
            return obj ?? GetFromMethod(variable);
        }
        
        private FsObject GetFromMethod(string variable) => Parent.Methods.Values.Where
                (method => method.Variables.ContainsKey(variable))
            .Select(method => method.Variables[variable]).FirstOrDefault();
        
        public FsCallable GetMethod(string method)
        {
            if (Methods.ContainsKey(method)) return Methods[method];
            return Parent.Methods.ContainsKey(method) ? Parent.GetMethod(method) : null;
        }

        protected FsCallable()
        {
            if (!(cultureInfo is null)) return;

            cultureInfo = (CultureInfo) CultureInfo.CurrentCulture.Clone();
            cultureInfo.NumberFormat.CurrencyDecimalSeparator = Constants.Separator;
            Calculator.CultureInfo = cultureInfo;
        }

        public virtual FsObject Return()
        {
            var returnable = Returnable.Invoke();
            foreach (var temp in Temporary) GetObject(temp.Key).Set(temp.Value, 0);
            return returnable;
        }

        protected int Parse(FsCallable callable, string[] lines, int currentLine)
        {
            var i = currentLine;
            while (i < lines.Length - 1 && !lines[i].StartsWith(Constants.Return) &&
                   !lines[i].StartsWith(Constants.Break))
            {
                if (IsToParse(lines[i]))
                {
                    var whitespaces = lines[i].Split(' ');

                    if (lines[i].StartsWith(Constants.Variable))
                    {
                        var variable = new FsObject();

                        if (lines[i].Contains(Constants.Set))
                        {
                            var expression = DecodeExpression(callable, lines[i]
                                .Split(new[] {Constants.Set}, StringSplitOptions.None)[1]);

                            variable.Add(Calculator.Evaluate(expression));
                        }
                        else
                            for (var v = 2; v < whitespaces.Length; v++)
                                if (whitespaces[v] != Constants.OpenBracket && whitespaces[v] != Constants.CloseBracket)
                                    variable.Add(float.Parse(whitespaces[v], NumberStyles.Any, cultureInfo));

                        callable.Variables.Add(whitespaces[1], variable);
                    }
                    else if (lines[i].StartsWith(Constants.Function))
                    {
                        var func = new FsCallable {Parent = callable};
                        i = Parse(func, lines, i + 1);
                        callable.Methods.Add(whitespaces[1], func);
                    }
                    else if (lines[i].StartsWith(Constants.MethodCall))
                    {
                        var expression = lines[i].Remove(0, Constants.MethodCall.Length);
                        DecodeExpression(callable, expression, true);
                    }
                    else if (lines[i].StartsWith(Constants.If))
                    {
                        var condition = ParseCondition(callable, lines, i, out var j);
                        i = j;
                        callable.Elements.Add(condition);
                    }
                    else if (lines[i].StartsWith(Constants.For) && lines[i].Contains(Constants.In))
                    {
                        var loop = ParseLoop(callable, lines, i, out var j);
                        i = j;
                        callable.Elements.Add(loop);
                    }
                    else if (lines[i].StartsWith(Constants.Temporary))
                    {
                        var variable = new FsObject();
                        var name = whitespaces[1];

                        var expression = DecodeExpression(callable, lines[i]
                            .Split(new[] {Constants.Set}, StringSplitOptions.None)[1]);

                        var temp = Calculator.Evaluate(expression);
                        variable.Add(temp);

                        callable.Temporary.Add(name, temp);
                        callable.Variables.Add(name, variable);
                    }
                    else callable.Elements.Add(new FsOperation(callable, ParseOperation(lines[i], callable)));
                }

                i++;
            }

            if (lines[i].StartsWith(Constants.Return))
            {
                var lineNum = i;
                callable.Returnable = () =>
                {
                    var expression = DecodeExpression(callable,
                        lines[lineNum].Replace($"{Constants.Return} ", string.Empty));
                    var variable = new FsObject {Calculator.Evaluate(expression)};
                    return variable;
                };
            }
            else if (lines[i].StartsWith(Constants.Break)) callable.Returnable = () => new FsObject {0};

            return i;
        }

        private FsCondition ParseCondition(FsCallable callable, IReadOnlyList<string> lines, int i, out int j)
        {
            var whitespaces = lines[i].Split(' ');
            var condition = new FsCondition(callable, whitespaces[1], whitespaces[3], whitespaces[2]);
            Parse(condition, callable, lines, i, out j);
            return condition;
        }

        private FsLoop ParseLoop(FsCallable callable, IReadOnlyList<string> lines, int i, out int j)
        {
            var whitespaces = lines[i].Split(' ');
            var loop = new FsLoop(callable, whitespaces[1], whitespaces[3]);
            Parse(loop, callable, lines, i, out j);
            return loop;
        }

        private void Parse(FsCodeElement element, FsCallable callable, IReadOnlyList<string> lines, int i, out int j)
        {
            j = i + 1;
            while (!lines[j].StartsWith(Constants.Break))
            {
                if (IsToParse(lines[j]))
                {
                    if (lines[j].StartsWith(Constants.If))
                    {
                        var childCondition = ParseCondition(callable, lines, j, out int k);
                        element.AddEvent(childCondition);
                        j = k;
                    }

                    if (lines[j].StartsWith(Constants.For))
                    {
                        var childCondition = ParseLoop(callable, lines, j, out int k);
                        element.AddEvent(childCondition);
                        j = k;
                    }
                    else
                    {
                        element.AddEvent(new FsOperation(callable, ParseOperation(lines[j], callable)));
                    }
                }

                j++;
            }
        }

        protected static string[] LoadLines(string code, Dictionary<string, float> input = null)
        {
            if (input != null)
                code = input.Aggregate(code, (current, value) =>
                    current.Replace(value.Key, value.Value.ToString(CultureInfo.CurrentCulture)));

            return code.Replace(Constants.True, 1.ToString())
                .Replace(Constants.False, 0.ToString())
                .Split(new[] {"\r\n"}, StringSplitOptions.None);
        }

        private static bool IsToParse(string line) =>
            line.Any(char.IsLetterOrDigit) && !line.StartsWith(Constants.CommentPrefix);

        private static bool IsFunction(string variable) => variable.Contains(Constants.OpenArguments)
                                                           && variable.Contains(Constants.CloseArguments);

        private static bool IsVariable(FsCallable callable, string variable) =>
            ContainsVariable(callable, variable) || variable.Contains(Constants.Argument);

        private static bool ContainsVariable(FsCallable callable, string variableClean) =>
            callable.Variables.ContainsKey(variableClean) ||
            callable.Parent != callable && ContainsVariable(callable.Parent, variableClean);

        private string DecodeExpression(FsCallable callable, string expression, bool isRuntime = false)
        {
            var expressionWhitespaces = expression.Split(' ');
            foreach (var variable in expressionWhitespaces)
            {
                SplitArray(callable, variable, out var slot, out var variableClean);

                var isVar = IsVariable(callable, variableClean);
                var isFun = IsFunction(variable);

                switch (isVar)
                {
                    case false when !isFun:
                        continue;
                    case true:
                    {
                        if (ContainsVariable(callable, variableClean))
                        {
                            expression = expression.Replace(variable,
                                callable.GetObject(variableClean)[slot].ToString(CultureInfo.CurrentCulture));
                        }

                        else if (ContainsVariable(this, variableClean))
                        {
                            expression = expression.Replace(variable,
                                this.GetObject(variableClean)[slot].ToString(CultureInfo.CurrentCulture));
                        }

                        else if (variableClean == Constants.Argument)
                        {
                            var arg = callable.Arguments.ElementAt(slot);
                            var obj = callable.GetObject(arg.Key);
                            var argument = obj != null
                                ? obj[arg.Value]
                                : float.Parse(arg.Key, NumberStyles.Any, cultureInfo);
                            expression = expression.Replace(variable, argument.ToString(CultureInfo.CurrentCulture));
                        }

                        break;
                    }
                }

                if (!isFun) continue;
                expression = ParseMethod(callable, expression, variable, variableClean, slot, isRuntime);
            }

            return expression;
        }

        private string ParseMethod(FsCallable callable, string expression, string variable, string variableClean,
            int slot, bool isRunnable)
        {
            var indexOfBracket = variable.IndexOf(Convert.ToChar(Constants.OpenArguments));
            var argsAsString = StringTools.GetBetween(Constants.OpenArguments, Constants.CloseArguments, variable)
                .Split(',');

            var funLink = variable.Remove(indexOfBracket,
                variable.Length - indexOfBracket);

            if (!Methods.ContainsKey(funLink) && funLink.Contains(Constants.Separator))
                Methods.Add(funLink, new FsStatic(funLink, cultureInfo) {Parent = this});

            var fun = GetMethod(funLink);

            if (fun.Arguments.Count > 0) fun.Arguments.Clear();

            foreach (var t in argsAsString)
            {
                SplitArray(callable, t, out slot, out variableClean);
                if (!fun.Arguments.ContainsKey(variableClean))
                    fun.Arguments.Add(variableClean, slot);
            }

            foreach (var e in fun.Elements) e?.Invoke();

            return expression.Replace(variable, fun.Return().First().ToString(CultureInfo.CurrentCulture));
        }

        private void SplitArray(FsCallable callable, string variableInput, out int slot, out string variableClean)
        {
            variableClean = variableInput;
            slot = 0;

            if (!variableInput.Contains(Constants.OpenBracket) ||
                !variableInput.Contains(Constants.CloseBracket)) return;

            variableClean = variableInput.Remove(variableInput.IndexOf(Constants.OpenBracket,
                StringComparison.Ordinal), 3);

            var slotAsString =
                StringTools.GetBetween(Constants.OpenBracket, Constants.CloseBracket, variableInput);

            if (ContainsVariable(callable, slotAsString)) slot = (int) callable.GetObject(slotAsString)[0];
            else if (ContainsVariable(this, slotAsString)) slot = (int) GetObject(slotAsString)[0];
            else slot = int.Parse(slotAsString);
        }

        private OperationEvent ParseOperation(string line, FsCallable callable)
        {
            var operationParts = line.Split(new[]
            {
                Constants.Set, Constants.Multiply, Constants.Divide,
                Constants.Add, Constants.Subtract
            }, StringSplitOptions.None);

            if (line.Contains(Constants.Set))
                return () =>
                {
                    var expression = DecodeExpression(callable, operationParts[1]);
                    var evaluatedVariable = Calculator.Evaluate(expression);

                    SplitArray(callable, operationParts[0], out var slot, out var variableClean);
                    callable.GetObject(variableClean).Set(evaluatedVariable, slot);
                    return callable.GetObject(variableClean);
                };

            if (line.Contains(Constants.Multiply))
                return () =>
                {
                    var expression = DecodeExpression(callable, operationParts[1]);
                    var evaluatedVariable = Calculator.Evaluate(expression);

                    SplitArray(callable, operationParts[0], out var slot, out var variableClean);
                    callable.GetObject(variableClean).Mul(evaluatedVariable, slot);
                    return callable.GetObject(variableClean);
                };

            if (line.Contains(Constants.Divide))
                return () =>
                {
                    var expression = DecodeExpression(callable, operationParts[1]);
                    var evaluatedVariable = Calculator.Evaluate(expression);

                    SplitArray(callable, operationParts[0], out var slot, out var variableClean);
                    callable.GetObject(variableClean).Div(evaluatedVariable, slot);
                    return callable.GetObject(variableClean);
                };

            if (line.Contains(Constants.Add))
                return () =>
                {
                    var expression = DecodeExpression(callable, operationParts[1]);
                    var evaluatedVariable = Calculator.Evaluate(expression);

                    SplitArray(callable, operationParts[0], out var slot, out var variableClean);
                    callable.GetObject(variableClean).Add(evaluatedVariable, slot);
                    return callable.GetObject(variableClean);
                };

            if (line.Contains(Constants.Subtract))
                return () =>
                {
                    var expression = DecodeExpression(callable, operationParts[1]);
                    var evaluatedVariable = Calculator.Evaluate(expression);

                    SplitArray(callable, operationParts[0], out var slot, out var variableClean);
                    callable.GetObject(variableClean).Sub(evaluatedVariable, slot);
                    return callable.GetObject(variableClean);
                };

            return null;
        }
    }
}
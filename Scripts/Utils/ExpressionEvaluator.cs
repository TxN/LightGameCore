using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Utils {
    //
    // Сводка:
    //     Evaluates simple math expressions; supports int float and operators: + - * %
    //     ^ ( ).
    public class ExpressionEvaluator {
        internal class Expression {
            internal readonly string[] rpnTokens;

            internal readonly bool hasVariables;

            internal Expression(string expression) {
                expression = PreFormatExpression(expression);
                string[] tokens = ExpressionToTokens(expression, out hasVariables);
                tokens = FixUnaryOperators(tokens);
                rpnTokens = InfixToRPN(tokens);
            }

            public bool Evaluate<T>(ref T value, int index = 0, int count = 1) {
                return EvaluateTokens(rpnTokens, ref value, index, count);
            }
        }

        private struct PcgRandom {
            private readonly ulong increment;

            private ulong state;

            private const ulong Multiplier64 = 6364136223846793005uL;

            public PcgRandom(ulong state = 0uL, ulong sequence = 0uL) {
                increment = (sequence << 1) | 1;
                this.state = 0uL;
                Step();
                this.state += state;
                Step();
            }

            public uint GetUInt() {
                ulong s = state;
                Step();
                return XshRr(s);
            }

            private static uint RotateRight(uint v, int rot) {
                return (v >> rot) | (v << (-rot & 0x1F));
            }

            private static uint XshRr(ulong s) {
                return RotateRight((uint)(((s >> 18) ^ s) >> 27), (int)(s >> 59));
            }

            private void Step() {
                state = state * 6364136223846793005L + increment;
            }
        }

        private enum Op {
            Add,
            Sub,
            Mul,
            Div,
            Mod,
            Neg,
            Pow,
            Sqrt,
            Sin,
            Cos,
            Tan,
            Floor,
            Ceil,
            Round,
            Rand,
            Linear
        }

        private enum Associativity {
            Left,
            Right
        }

        private class Operator {
            public readonly Op op;

            public readonly int precedence;

            public readonly Associativity associativity;

            public readonly int inputs;

            public Operator(Op op, int precedence, int inputs, Associativity associativity) {
                this.op = op;
                this.precedence = precedence;
                this.inputs = inputs;
                this.associativity = associativity;
            }
        }

        private static PcgRandom s_Random = new PcgRandom(0uL, 0uL);

        private static Dictionary<string, Operator> s_Operators = new Dictionary<string, Operator>
        {
            {
                "-",
                new Operator(Op.Sub, 2, 2, Associativity.Left)
            },
            {
                "+",
                new Operator(Op.Add, 2, 2, Associativity.Left)
            },
            {
                "/",
                new Operator(Op.Div, 3, 2, Associativity.Left)
            },
            {
                "*",
                new Operator(Op.Mul, 3, 2, Associativity.Left)
            },
            {
                "%",
                new Operator(Op.Mod, 3, 2, Associativity.Left)
            },
            {
                "^",
                new Operator(Op.Pow, 5, 2, Associativity.Right)
            },
            {
                "_",
                new Operator(Op.Neg, 5, 1, Associativity.Left)
            },
            {
                "sqrt",
                new Operator(Op.Sqrt, 4, 1, Associativity.Left)
            },
            {
                "cos",
                new Operator(Op.Cos, 4, 1, Associativity.Left)
            },
            {
                "sin",
                new Operator(Op.Sin, 4, 1, Associativity.Left)
            },
            {
                "tan",
                new Operator(Op.Tan, 4, 1, Associativity.Left)
            },
            {
                "floor",
                new Operator(Op.Floor, 4, 1, Associativity.Left)
            },
            {
                "ceil",
                new Operator(Op.Ceil, 4, 1, Associativity.Left)
            },
            {
                "round",
                new Operator(Op.Round, 4, 1, Associativity.Left)
            },
            {
                "R",
                new Operator(Op.Rand, 4, 2, Associativity.Left)
            },
            {
                "L",
                new Operator(Op.Linear, 4, 2, Associativity.Left)
            }
        };

        public static bool Evaluate<T>(string expression, out T value) {
            Expression delayed;
            return Evaluate<T>(expression, out value, out delayed);
        }

        internal static bool Evaluate<T>(string expression, out T value, out Expression delayed) {
            value = default(T);
            delayed = null;
            if (TryParse<T>(expression, out value)) {
                return true;
            }

            Expression expression2 = new Expression(expression);
            if (expression2.hasVariables) {
                value = default(T);
                delayed = expression2;
                return false;
            }

            return EvaluateTokens(expression2.rpnTokens, ref value, 0, 1);
        }

        internal static void SetRandomState(uint state) {
            s_Random = new PcgRandom(state, 0uL);
        }

        private static bool EvaluateTokens<T>(string[] tokens, ref T value, int index, int count) {
            bool result = false;
            if (typeof(T) == typeof(float)) {
                double value2 = (float)(object)value;
                result = EvaluateDouble(tokens, ref value2, index, count);
                value = (T)(object)(float)value2;
            } else if (typeof(T) == typeof(int)) {
                double value3 = (int)(object)value;
                result = EvaluateDouble(tokens, ref value3, index, count);
                value = (T)(object)(int)value3;
            } else if (typeof(T) == typeof(long)) {
                double value4 = (long)(object)value;
                result = EvaluateDouble(tokens, ref value4, index, count);
                value = (T)(object)(long)value4;
            } else if (typeof(T) == typeof(double)) {
                double value5 = (double)(object)value;
                result = EvaluateDouble(tokens, ref value5, index, count);
                value = (T)(object)value5;
            }

            return result;
        }

        private static bool EvaluateDouble(string[] tokens, ref double value, int index, int count) {
            Stack<string> stack = new Stack<string>();
            foreach (string text in tokens) {
                if (IsOperator(text)) {
                    Operator @operator = TokenToOperator(text);
                    List<double> list = new List<double>();
                    bool flag = true;
                    while (stack.Count > 0 && !IsCommand(stack.Peek()) && list.Count < @operator.inputs) {
                        flag &= TryParse<double>(stack.Pop(), out var result);
                        list.Add(result);
                    }

                    list.Reverse();
                    if (!flag || list.Count != @operator.inputs) {
                        return false;
                    }

                    stack.Push(EvaluateOp(list.ToArray(), @operator.op, index, count).ToString(CultureInfo.InvariantCulture));
                } else if (IsVariable(text)) {
                    stack.Push((text == "#") ? index.ToString() : value.ToString(CultureInfo.InvariantCulture));
                } else {
                    stack.Push(text);
                }
            }

            if (stack.Count == 1) {
                if (TryParse<double>(stack.Pop(), out value)) {
                    return true;
                }
            } else if (tokens.Length == 0) {
                value = 0.0;
                return true;
            }

            return false;
        }

        private static string[] InfixToRPN(string[] tokens) {
            Stack<string> stack = new Stack<string>();
            Queue<string> queue = new Queue<string>();
            foreach (string text in tokens) {
                if (IsCommand(text)) {
                    char c = text[0];
                    if (c == '(') {
                        stack.Push(text);
                    } else if (c == ')') {
                        while (stack.Count > 0 && stack.Peek() != "(") {
                            queue.Enqueue(stack.Pop());
                        }

                        if (stack.Count > 0) {
                            stack.Pop();
                        }

                        if (stack.Count > 0 && IsDelayedFunction(stack.Peek())) {
                            queue.Enqueue(stack.Pop());
                        }
                    } else if (c == ',') {
                        while (stack.Count > 0 && stack.Peek() != "(") {
                            queue.Enqueue(stack.Pop());
                        }
                    } else {
                        Operator newOperator = TokenToOperator(text);
                        while (NeedToPop(stack, newOperator)) {
                            queue.Enqueue(stack.Pop());
                        }

                        stack.Push(text);
                    }
                } else if (IsDelayedFunction(text)) {
                    stack.Push(text);
                } else {
                    queue.Enqueue(text);
                }
            }

            while (stack.Count > 0) {
                queue.Enqueue(stack.Pop());
            }

            return queue.ToArray();
        }

        private static bool NeedToPop(Stack<string> operatorStack, Operator newOperator) {
            if (operatorStack.Count > 0 && newOperator != null) {
                Operator @operator = TokenToOperator(operatorStack.Peek());
                if (@operator != null && ((newOperator.associativity == Associativity.Left && newOperator.precedence <= @operator.precedence) || (newOperator.associativity == Associativity.Right && newOperator.precedence < @operator.precedence))) {
                    return true;
                }
            }

            return false;
        }

        private static string[] ExpressionToTokens(string expression, out bool hasVariables) {
            hasVariables = false;
            List<string> list = new List<string>();
            string text = "";
            for (int i = 0; i < expression.Length; i++) {
                char c = expression[i];
                if (IsCommand(c.ToString())) {
                    if (text.Length > 0) {
                        list.Add(text);
                    }

                    list.Add(c.ToString());
                    text = "";
                } else if (c != ' ') {
                    text += c;
                } else {
                    if (text.Length > 0) {
                        list.Add(text);
                    }

                    text = "";
                }
            }

            if (text.Length > 0) {
                list.Add(text);
            }

            hasVariables = list.Any((string f) => IsVariable(f) || IsDelayedFunction(f));
            return list.ToArray();
        }

        private static bool IsCommand(string token) {
            if (token.Length == 1) {
                char c = token[0];
                if (c == '(' || c == ')' || c == ',') {
                    return true;
                }
            }

            return IsOperator(token);
        }

        private static bool IsVariable(string token) {
            if (token.Length == 1) {
                char c = token[0];
                return c == 'x' || c == 'v' || c == 'f' || c == '#';
            }

            return false;
        }

        private static bool IsDelayedFunction(string token) {
            Operator @operator = TokenToOperator(token);
            if (@operator != null && (@operator.op == Op.Rand || @operator.op == Op.Linear)) {
                return true;
            }

            return false;
        }

        private static bool IsOperator(string token) {
            return s_Operators.ContainsKey(token);
        }

        private static Operator TokenToOperator(string token) {
            Operator value;
            return s_Operators.TryGetValue(token, out value) ? value : null;
        }

        private static string PreFormatExpression(string expression) {
            string text = expression;
            text = text.Trim();
            if (text.Length == 0) {
                return text;
            }

            char c = text[text.Length - 1];
            if (IsOperator(c.ToString())) {
                text = text.TrimEnd(new char[1] { c });
            }

            if (text.Length >= 2 && text[1] == '=') {
                char c2 = text[0];
                string text2 = text.Substring(2);
                if (c2 == '+') {
                    text = "x+(" + text2 + ")";
                }

                if (c2 == '-') {
                    text = "x-(" + text2 + ")";
                }

                if (c2 == '*') {
                    text = "x*(" + text2 + ")";
                }

                if (c2 == '/') {
                    text = "x/(" + text2 + ")";
                }
            }

            return text;
        }

        private static string[] FixUnaryOperators(string[] tokens) {
            if (tokens.Length == 0) {
                return tokens;
            }

            if (tokens[0] == "-") {
                tokens[0] = "_";
            }

            for (int i = 1; i < tokens.Length - 1; i++) {
                string text = tokens[i];
                string text2 = tokens[i - 1];
                if (text == "-" && IsCommand(text2) && text2 != ")") {
                    tokens[i] = "_";
                }
            }

            return tokens;
        }

        private static double EvaluateOp(double[] values, Op op, int index, int count) {
            double num = ((values.Length >= 1) ? values[0] : 0.0);
            double num2 = ((values.Length >= 2) ? values[1] : 0.0);
            switch (op) {
                case Op.Neg:
                    return 0.0 - num;
                case Op.Add:
                    return num + num2;
                case Op.Sub:
                    return num - num2;
                case Op.Mul:
                    return num * num2;
                case Op.Div:
                    return num / num2;
                case Op.Mod:
                    return num % num2;
                case Op.Pow:
                    return Math.Pow(num, num2);
                case Op.Sqrt:
                    return (num <= 0.0) ? 0.0 : Math.Sqrt(num);
                case Op.Floor:
                    return Math.Floor(num);
                case Op.Ceil:
                    return Math.Ceiling(num);
                case Op.Round:
                    return Math.Round(num);
                case Op.Cos:
                    return Math.Cos(num);
                case Op.Sin:
                    return Math.Sin(num);
                case Op.Tan:
                    return Math.Tan(num);
                case Op.Rand: {
                        uint num4 = s_Random.GetUInt() & 0xFFFFFFu;
                        double num5 = (double)num4 / 16777215.0;
                        return num + num5 * (num2 - num);
                    }
                case Op.Linear: {
                        if (count < 1) {
                            count = 1;
                        }

                        double num3 = ((count < 2) ? 0.5 : ((double)index / (double)(count - 1)));
                        return num + num3 * (num2 - num);
                    }
                default:
                    return 0.0;
            }
        }

        private static bool TryParse<T>(string expression, out T result) {
            expression = expression.Replace(',', '.');
            expression = expression.TrimEnd(new char[1] { 'f' });
            string text = expression.ToLowerInvariant();
            bool result2 = false;
            result = default(T);
            if (typeof(T) == typeof(float)) {
                if (text == "pi") {
                    result2 = true;
                    result = (T)(object)MathF.PI;
                } else {
                    result2 = float.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var result3);
                    result = (T)(object)result3;
                }
            } else if (typeof(T) == typeof(int)) {
                result2 = int.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var result4);
                result = (T)(object)result4;
            } else if (typeof(T) == typeof(double)) {
                if (text == "pi") {
                    result2 = true;
                    result = (T)(object)Math.PI;
                } else {
                    result2 = double.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var result5);
                    result = (T)(object)result5;
                }
            } else if (typeof(T) == typeof(long)) {
                result2 = long.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var result6);
                result = (T)(object)result6;
            }

            return result2;
        }
    }
}
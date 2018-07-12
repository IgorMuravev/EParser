using System;
using System.Collections.Generic;
using System.Linq;

namespace EParserLib
{
    /// <summary>
    /// Настройки для парсера
    /// </summary>
    public class EParserSettings
    {
        /// <summary>
        /// Словарь: имя функции - делегат
        /// </summary>
        public Dictionary<string, Func<double, double>> funcs;
        /// <summary>
        /// Словарь: бинарная операция - делегат
        /// </summary>
        public Dictionary<string, Func<double, double, double>> binOperators;
        /// <summary>
        /// Словарь: унарная - делегат
        /// </summary>
        public Dictionary<string, Func<double, double>> unOperators;

        /// <summary>
        /// Стандартные настройки
        /// </summary>
        public static EParserSettings Default
        {
            get
            {
                return new EParserSettings()
                {
                    funcs = new Dictionary<string, Func<double, double>>()
                    {
                        { "SIN", new Func<double, double>(x=>Math.Sin(x)) },
                        { "COS", new Func<double, double>(x=>Math.Cos(x)) },
                        { "LN", new Func<double, double>(x=>Math.Log(x)) },
                        { "EXP", new Func<double, double>(x=>Math.Exp(x)) },
                    },

                    binOperators = new Dictionary<string, Func<double, double, double>>()
                    {
                        {"+", new Func<double, double, double>((a,b)=> a + b)},
                        {"-", new Func<double, double, double>((a,b)=> a - b)},
                        {"*", new Func<double, double, double>((a,b)=> a * b)},
                        {"/", new Func<double, double, double>((a,b)=> a / b)},
                        {"^", new Func<double, double, double>((a,b)=> Math.Pow(a,b))},
                    },

                    unOperators = new Dictionary<string, Func<double, double>>()
                    {
                        { "-", new Func<double, double>(x=>-x) },
                        { "+", new Func<double, double>(x=>+x) },
                    }
                };
            }
        }
    }

    /// <summary>
    /// Класс для вычисления строкового выражения
    /// </summary>
    public class EParser
    {
        /// <summary>
        /// Настройки
        /// </summary>
        private EParserSettings settings;
        /// <summary>
        /// Специальные символы
        /// </summary>
        private List<string> special = new List<string>() { "(", ")" };
        /// <summary>
        /// Список переменых
        /// </summary>
        private List<string> variables = new List<string>();
        /// <summary>
        /// Исходное строковое выражение
        /// </summary>
        private string sourceExp;

        /// <summary>
        /// Полученное дерево выражения
        /// </summary>
        private List<Term> tree = new List<Term>();

        /// <summary>
        /// Словарь, хранящий список значений переменных
        /// </summary>
        private Dictionary<string, double> vars = new Dictionary<string, double>();

        /// <summary>
        /// Перечисление - тип терма
        /// </summary>
        private enum TermType { Variable, Function, OpenBracket, CloseBracket, Number, BinOperation, UnarOperation }
        /// <summary>
        /// СТруктура - терм и его тип
        /// </summary>
        private struct Term
        {
            public string Source;
            public TermType Type;
            public override string ToString()
            {
                return Source + " " + Type.ToString();
            }
        }
        /// <summary>
        /// Получить приоритет терма
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private int GetPriority(Term t)
        {
            if (t.Type == TermType.Function) return 100;
            if (t.Type == TermType.UnarOperation) return 90;
            if (t.Type == TermType.BinOperation)
            {
                switch (t.Source)
                {
                    case "^":
                        return 70;

                    case "*":
                    case "/":
                        return 60;

                }
                return 50;
            }
            return 0;
        }

        /// <summary>
        /// Является ли строка числов функцией или переменной
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private bool IsNumberOrFuncOrVar(string str)
        {
            double buf;
            if (Double.TryParse(str, out buf))
                return true;
            if (settings.funcs.ContainsKey(str))
                return true;
            if (variables.Contains(str))
                return true;

            return false;
        }
        /// <summary>
        /// ПОлучение лексемы из строки
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="index"></param>
        /// <param name="newIndex"></param>
        /// <returns></returns>
        private string GetLexem(string exp, int index, out int newIndex)
        {
            var readedLexem = String.Empty;
            for (int i = index; i < exp.Length; i++)
            {
                readedLexem += exp[i];
                if (settings.binOperators.ContainsKey(readedLexem) || settings.unOperators.ContainsKey(readedLexem) || special.Contains(readedLexem))
                {
                    newIndex = i + 1;
                    return readedLexem;
                }
                if (!IsNumberOrFuncOrVar(readedLexem))
                {
                    var flag = true;
                    foreach (var f in settings.funcs.Keys)
                    {
                        if (f.Contains(readedLexem))
                            flag = false;
                    }
                    if (flag)
                    {
                        newIndex = i;
                        var lex = readedLexem.Substring(0, readedLexem.Length - 1);
                        if (!IsNumberOrFuncOrVar(lex)) throw new Exception();
                        return lex;
                    }
                }
            }
            if (IsNumberOrFuncOrVar(readedLexem))
            {
                newIndex = exp.Length;
                return readedLexem;
            }

            newIndex = -1;
            return String.Empty;
        }
        /// <summary>
        /// Получение всех лексем
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        private List<string> GetLexems(string exp)
        {
            var result = new List<string>();
            var builder = new StringBuilder(exp);
            builder = builder.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
            builder = builder.Replace(",", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
            builder = builder.Replace("\t", "").Replace("\n", "").Replace(" ", "").Replace("\r", "");
            var clearExp = builder.ToString().ToUpper();
            var index = 0;
            while (index != clearExp.Length && index >= 0)
            {
                if (index >= 0)
                    result.Add(GetLexem(clearExp, index, out index));
            }
            return result;
        }
        /// <summary>
        /// Перевод лексем в термы
        /// </summary>
        /// <param name="lexems"></param>
        /// <returns></returns>
        private List<Term> GetTerms(List<string> lexems)
        {
            var result = new List<Term>();
            double buf;
            for (int i = 0; i < lexems.Count; i++)
            {
                if (settings.funcs.ContainsKey(lexems[i]))
                {
                    result.Add(new Term() { Source = lexems[i], Type = TermType.Function });
                }
                else if (Double.TryParse(lexems[i], out buf))
                {
                    result.Add(new Term() { Source = lexems[i], Type = TermType.Number });
                }
                else if (variables.Contains(lexems[i]))
                {
                    result.Add(new Term() { Source = lexems[i], Type = TermType.Variable });
                }
                else if (lexems[i] == "(")
                {
                    result.Add(new Term() { Source = lexems[i], Type = TermType.OpenBracket });
                }
                else if (lexems[i] == ")")
                {
                    result.Add(new Term() { Source = lexems[i], Type = TermType.CloseBracket });
                }
                else if (settings.binOperators.ContainsKey(lexems[i]))
                {
                    if (i == 0)
                        result.Add(new Term() { Source = lexems[i], Type = TermType.UnarOperation });
                    else if (result.Last().Type == TermType.OpenBracket || result.Last().Type == TermType.BinOperation)
                        result.Add(new Term() { Source = lexems[i], Type = TermType.UnarOperation });
                    else result.Add(new Term() { Source = lexems[i], Type = TermType.BinOperation });
                }
                else
                {
                    throw new Exception();
                }
            }

            return result;
        }
        /// <summary>
        /// Разбор строки
        /// </summary>
        private void Parse()
        {
            var lexems = GetLexems(sourceExp);
            var terms = GetTerms(lexems);
            var stack = new Stack<Term>();

            var result = new List<Term>();
            for (int i = 0; i < terms.Count; i++)
            {
                if (terms[i].Type == TermType.Number || terms[i].Type == TermType.Variable)
                {
                    result.Add(terms[i]);
                }
                else if (terms[i].Type == TermType.Function || terms[i].Type == TermType.OpenBracket)
                {
                    stack.Push(terms[i]);
                }
                else if (terms[i].Type == TermType.CloseBracket)
                {
                    while (stack.Peek().Type != TermType.OpenBracket)
                    {
                        result.Add(stack.Pop());
                    }
                    stack.Pop();
                }
                else if (terms[i].Type == TermType.UnarOperation)
                {
                    var prior = GetPriority(terms[i]);
                    while (stack.Count > 0 && prior < GetPriority(stack.Peek()))
                    {
                        result.Add(stack.Pop());
                    }
                    stack.Push(terms[i]);
                }
                else if (terms[i].Type == TermType.BinOperation)
                {
                    var prior = GetPriority(terms[i]);
                    while (stack.Count > 0 && prior <= GetPriority(stack.Peek()))
                    {
                        result.Add(stack.Pop());
                    }
                    stack.Push(terms[i]);
                }
                else
                {
                    throw new Exception();
                }

            }
            while (stack.Count > 0)
                result.Add(stack.Pop());

            tree = result;
        }
        /// <summary>
        /// Присвоение значений переменной
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        public double this[string var]
        {
            get
            {
                if (vars.ContainsKey(var))
                    return vars[var];
                else
                    return Double.NaN;
            }
            set
            {
                if (vars.ContainsKey(var))
                    vars[var] = value;
                else
                    vars.Add(var, value);
            }
        }
        /// <summary>
        /// Исходная строка
        /// </summary>
        public string SourceExpression
        {
            get
            {
                return sourceExp;
            }
        }
        /// <summary>
        /// Вывод в виде обратной польской нотации
        /// </summary>
        public string RPN
        {
            get { return String.Join(" ", tree.Select(x => x.Type == TermType.UnarOperation ? "'"+x.Source : x.Source)); }
        }
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="s"></param>
        public EParser(string exp, EParserSettings s = null)
        {
            for (int i = 65; i < 91; i++)
                variables.Add(((char)i).ToString());
            sourceExp = exp;
            if (s == null)
                settings = EParserSettings.Default;
            else
                settings = s;
            Parse();
        }
        /// <summary>
        /// Расчет выражения
        /// </summary>
        /// <returns></returns>
        public double Calculate()
        {
            var stack = new Stack<double>();
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree[i].Type == TermType.Number)
                {
                    stack.Push(Convert.ToDouble(tree[i].Source));
                }
                else if (tree[i].Type == TermType.Variable)
                {
                    stack.Push(this[tree[i].Source]);
                }
                else if (tree[i].Type == TermType.BinOperation)
                {
                    var p1 = stack.Pop();
                    var p2 = stack.Pop();
                    stack.Push(settings.binOperators[tree[i].Source](p2, p1));
                }
                else if (tree[i].Type == TermType.UnarOperation)
                {
                    var p1 = stack.Pop();
                    stack.Push(settings.unOperators[tree[i].Source](p1));
                }
                else if (tree[i].Type == TermType.Function)
                {
                    var p1 = stack.Pop();
                    stack.Push(settings.funcs[tree[i].Source](p1));
                }
            }
            return stack.Pop();
        }

    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace EParserLib
{
    public class EParserSettings
    {
        public Dictionary<string, Func<double, double>> funcs;
        public Dictionary<string, Func<double, double, double>> binOperators;
        public Dictionary<string, Func<double, double>> unOperators;

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

    public class EParser
    {
        private EParserSettings settings;
        private List<string> special = new List<string>() { "(", ")" };
        private List<string> variables = new List<string>();

        private EParser()
        {
            for (int i = 65; i < 91; i++)
                variables.Add(((char)i).ToString());
        }
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
        private enum TermType { Variable, Function, OpenBracket, CloseBracket, Number, BinOperation, UnarOperation }
        private struct Term
        {
            public string Source;
            public TermType Type;
            public override string ToString()
            {
                return Source + " " + Type.ToString();
            }
        }
        private string sourceExp;
        private List<Term> tree = new List<Term>();
        private Dictionary<string, double> vars = new Dictionary<string, double>();
        private bool IsLexem(string str)
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
                if (!IsLexem(readedLexem))
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
                        return readedLexem.Substring(0, readedLexem.Length - 1);
                    }
                }
            }
            if (IsLexem(readedLexem))
            {
                newIndex = exp.Length;
                return readedLexem;
            }

            newIndex = -1;
            return String.Empty;
        }
        private List<string> GetLexems(string exp)
        {
            var result = new List<string>();
            var clearExp = exp.Replace('.', ',').Trim().Replace('\t', '\0').Replace('\n', '\0').Replace(' ', '\0').Replace('\r', '\0').ToUpper();
            var index = 0;
            while (index != clearExp.Length && index >= 0)
            {
                if (index >= 0)
                    result.Add(GetLexem(clearExp, index, out index));
            }
            return result;
        }
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
        public string SourceExpression
        {
            get
            {
                return sourceExp;
            }
        }
        public string RPN
        {
            get { return String.Join(" ", tree.Select(x => x.Source)); }
        }

        public EParser(string exp, EParserSettings s = null) : this()
        {
            sourceExp = exp;
            if (s == null)
                settings = EParserSettings.Default;
            else
                settings = s;
            Parse();
        }
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

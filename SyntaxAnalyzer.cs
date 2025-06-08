using System;
using System.Collections.Generic;
using System.Linq;

namespace PascalCompiler
{
    public static class SyntaxAnalyzer
    {
        private static HashSet<string> declaredVars = new HashSet<string>();
        private static HashSet<string> declaredConsts = new HashSet<string>();
        private static HashSet<string> declaredTypes = new HashSet<string> { "integer", "real", "char", "boolean" };
        private static bool inVarSection = false;
        private static bool inConstSection = false;
        private static bool inBeginSection = false;
        private static Stack<string> controlStructures = new Stack<string>();

        public static void Analyze()
        {
            var lines = InputOutput.GetSourceLines();
            declaredVars.Clear();
            declaredConsts.Clear();
            inVarSection = false;
            inConstSection = false;
            inBeginSection = false;
            controlStructures.Clear();

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                uint lineNum = (uint)(i + 1);
                string trimmed = line.Trim().ToLower();

                if (trimmed.StartsWith("//") || trimmed.StartsWith("{") || string.IsNullOrWhiteSpace(trimmed))
                    continue;

                CheckSections(line, lineNum, ref i);

                if (inVarSection)
                    AnalyzeVarDeclaration(line, lineNum);
                else if (inConstSection)
                    AnalyzeConstDeclaration(line, lineNum);
                else if (inBeginSection)
                    AnalyzeStatement(line, lineNum);
            }

            if (controlStructures.Count > 0)
            {
                InputOutput.AddError(110, new TextPosition((uint)lines.Count, 0));
            }
        }

        private static void CheckSections(string line, uint lineNum, ref int currentLine)
        {
            string trimmed = line.Trim().ToLower();

            if (trimmed == "var")
            {
                inVarSection = true;
                inConstSection = false;
                inBeginSection = false;
                return;
            }

            if (trimmed == "const")
            {
                inConstSection = true;
                inVarSection = false;
                inBeginSection = false;
                return;
            }

            if (trimmed == "begin")
            {
                inBeginSection = true;
                inVarSection = false;
                inConstSection = false;
                controlStructures.Push("begin");
                return;
            }

            if (trimmed.StartsWith("end"))
            {
                if (controlStructures.Count > 0 && controlStructures.Peek() == "begin")
                {
                    controlStructures.Pop();
                }
                else
                {
                    InputOutput.AddError(111, new TextPosition(lineNum, (byte)line.IndexOf("end")));
                }

                if (trimmed.EndsWith("."))
                {
                    inBeginSection = false;
                }
                return;
            }
        }

        private static void AnalyzeVarDeclaration(string line, uint lineNum)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) return;


            if (trimmed.StartsWith("//") || trimmed.StartsWith("{")) return;

            if (trimmed.Contains(":"))
            {
                string[] parts = trimmed.Split(':');
                if (parts.Length != 2)
                {
                    InputOutput.AddError(120, new TextPosition(lineNum, (byte)line.IndexOf(':')));
                    return;
                }

                string varPart = parts[0].Trim();
                string typePart = parts[1].Trim().TrimEnd(';').Trim();

                string[] vars = varPart.Split(',');
                foreach (string varName in vars.Select(v => v.Trim()))
                {
                    if (string.IsNullOrEmpty(varName))
                    {
                        InputOutput.AddError(121, new TextPosition(lineNum, (byte)line.IndexOf(varPart)));
                        continue;
                    }

                    if (!char.IsLetter(varName[0]))
                    {
                        InputOutput.AddError(122, new TextPosition(lineNum, (byte)line.IndexOf(varName)));
                        continue;
                    }

                    declaredVars.Add(varName.ToLower());
                }

                if (!declaredTypes.Contains(typePart.ToLower()) && !declaredVars.Contains(typePart.ToLower()))
                {
                    InputOutput.AddError(123, new TextPosition(lineNum, (byte)line.IndexOf(typePart)));
                }
            }
            else if (!trimmed.StartsWith("begin"))
            {
                InputOutput.AddError(124, new TextPosition(lineNum, 0));
            }
        }

        private static void AnalyzeConstDeclaration(string line, uint lineNum)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) return;

            if (trimmed.StartsWith("//") || trimmed.StartsWith("{")) return;

            if (trimmed.Contains("="))
            {
                string[] parts = trimmed.Split('=');
                if (parts.Length != 2)
                {
                    InputOutput.AddError(130, new TextPosition(lineNum, (byte)line.IndexOf('=')));
                    return;
                }

                string constName = parts[0].Trim();
                string constValue = parts[1].Trim().TrimEnd(';').Trim();

                if (string.IsNullOrEmpty(constName))
                {
                    InputOutput.AddError(131, new TextPosition(lineNum, (byte)line.IndexOf(parts[0])));
                    return;
                }

                if (!char.IsLetter(constName[0]))
                {
                    InputOutput.AddError(132, new TextPosition(lineNum, (byte)line.IndexOf(constName)));
                    return;
                }

                if (string.IsNullOrEmpty(constValue))
                {
                    InputOutput.AddError(133, new TextPosition(lineNum, (byte)line.IndexOf(parts[1])));
                    return;
                }

                declaredConsts.Add(constName.ToLower());
            }
            else if (!trimmed.StartsWith("var") && !trimmed.StartsWith("begin"))
            {
                InputOutput.AddError(134, new TextPosition(lineNum, 0));
            }
        }

        private static void AnalyzeStatement(string line, uint lineNum)
        {
            string trimmed = line.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(trimmed)) return;

            if (trimmed.StartsWith("//") || trimmed.StartsWith("{")) return;

            if (trimmed.StartsWith("begin"))
            {
                controlStructures.Push("begin");
                return;
            }

            if (trimmed.StartsWith("if"))
            {
                AnalyzeIfStatement(line, lineNum);
                return;
            }

            if (trimmed.StartsWith("case"))
            {
                AnalyzeCaseStatement(line, lineNum);
                return;
            }

            if (trimmed.StartsWith("for"))
            {
                AnalyzeForStatement(line, lineNum);
                return;
            }

            if (trimmed.Contains(":="))
            {
                AnalyzeAssignment(line, lineNum);
                return;
            }

            if (ContainsOperators(line))
            {
                AnalyzeExpression(line, lineNum);
                return;
            }
        }

        private static void AnalyzeIfStatement(string line, uint lineNum)
        {
            controlStructures.Push("if");
            string trimmed = line.Trim();
            int ifPos = trimmed.IndexOf("if", StringComparison.OrdinalIgnoreCase);

            int thenPos = trimmed.IndexOf("then", StringComparison.OrdinalIgnoreCase);
            if (thenPos == -1)
            {
                InputOutput.AddError(140, new TextPosition(lineNum, (byte)(ifPos + 2)));
                return;
            }

            string condition = trimmed.Substring(ifPos + 2, thenPos - (ifPos + 2)).Trim();
            if (string.IsNullOrWhiteSpace(condition))
            {
                InputOutput.AddError(141, new TextPosition(lineNum, (byte)(ifPos + 2)));
            }
            else
            {
                AnalyzeExpression(condition, lineNum);
            }

            string afterThen = trimmed.Substring(thenPos + 4).Trim();
            if (string.IsNullOrWhiteSpace(afterThen))
            {
                InputOutput.AddError(142, new TextPosition(lineNum, (byte)(thenPos + 4)));
            }

            int elsePos = trimmed.IndexOf("else", StringComparison.OrdinalIgnoreCase);
            if (elsePos != -1 && string.IsNullOrWhiteSpace(trimmed.Substring(elsePos + 4).Trim()))
            {
                InputOutput.AddError(143, new TextPosition(lineNum, (byte)(elsePos + 4)));
            }
        }

        private static void AnalyzeCaseStatement(string line, uint lineNum)
        {
            controlStructures.Push("case");
            string trimmed = line.Trim();
            int casePos = trimmed.IndexOf("case", StringComparison.OrdinalIgnoreCase);

            // Проверяем наличие выражения выбора
            int ofPos = trimmed.IndexOf("of", StringComparison.OrdinalIgnoreCase);
            if (ofPos == -1)
            {
                InputOutput.AddError(150, new TextPosition(lineNum, (byte)(casePos + 4)));
                return;
            }

            string selector = trimmed.Substring(casePos + 4, ofPos - (casePos + 4)).Trim();
            if (string.IsNullOrWhiteSpace(selector))
            {
                InputOutput.AddError(151, new TextPosition(lineNum, (byte)(casePos + 4)));
            }
            else
            {
                AnalyzeExpression(selector, lineNum);
            }

            string[] caseLabels = trimmed.Substring(ofPos + 2).Split(':');
            foreach (string label in caseLabels)
            {
                string cleanLabel = label.Trim();
                if (!string.IsNullOrEmpty(cleanLabel))
                {
                    if (!cleanLabel.StartsWith("'") && !int.TryParse(cleanLabel, out _) &&
                        !cleanLabel.Contains("..") && !declaredConsts.Contains(cleanLabel.ToLower()))
                    {
                        InputOutput.AddError(152, new TextPosition(lineNum, (byte)line.IndexOf(cleanLabel)));
                    }
                }
            }
        }

        private static void AnalyzeForStatement(string line, uint lineNum)
        {
            controlStructures.Push("for");
            string trimmed = line.Trim();
            int forPos = trimmed.IndexOf("for", StringComparison.OrdinalIgnoreCase);

            int assignPos = trimmed.IndexOf(":=", StringComparison.OrdinalIgnoreCase);
            if (assignPos == -1)
            {
                InputOutput.AddError(160, new TextPosition(lineNum, (byte)(forPos + 3)));
                return;
            }

            string counterVar = trimmed.Substring(forPos + 3, assignPos - (forPos + 3)).Trim();
            if (string.IsNullOrWhiteSpace(counterVar))
            {
                InputOutput.AddError(161, new TextPosition(lineNum, (byte)(forPos + 3)));
            }
            else if (!declaredVars.Contains(counterVar.ToLower()))
            {
                InputOutput.AddError(162, new TextPosition(lineNum, (byte)line.IndexOf(counterVar)));
            }

            int toDowntoPos = trimmed.IndexOf("to", StringComparison.OrdinalIgnoreCase);
            if (toDowntoPos == -1)
            {
                toDowntoPos = trimmed.IndexOf("downto", StringComparison.OrdinalIgnoreCase);
                if (toDowntoPos == -1)
                {
                    InputOutput.AddError(163, new TextPosition(lineNum, (byte)(assignPos + 2)));
                    return;
                }
            }

            string initialValue = trimmed.Substring(assignPos + 2, toDowntoPos - (assignPos + 2)).Trim();
            if (string.IsNullOrWhiteSpace(initialValue))
            {
                InputOutput.AddError(164, new TextPosition(lineNum, (byte)(assignPos + 2)));
            }
            else
            {
                AnalyzeExpression(initialValue, lineNum);
            }

            int doPos = trimmed.IndexOf("do", StringComparison.OrdinalIgnoreCase);
            if (doPos == -1)
            {
                InputOutput.AddError(165, new TextPosition(lineNum, (byte)(toDowntoPos + 2)));
                return;
            }

            string finalValue = trimmed.Substring(toDowntoPos + (trimmed.Substring(toDowntoPos).StartsWith("downto") ? 6 : 2),
                                                doPos - (toDowntoPos + (trimmed.Substring(toDowntoPos).StartsWith("downto") ? 6 : 2))).Trim();
            if (string.IsNullOrWhiteSpace(finalValue))
            {
                InputOutput.AddError(166, new TextPosition(lineNum, (byte)(toDowntoPos + 2)));
            }
            else
            {
                AnalyzeExpression(finalValue, lineNum);
            }

            string afterDo = trimmed.Substring(doPos + 2).Trim();
            if (string.IsNullOrWhiteSpace(afterDo))
            {
                InputOutput.AddError(167, new TextPosition(lineNum, (byte)(doPos + 2)));
            }
        }

        private static void AnalyzeAssignment(string line, uint lineNum)
        {
            string trimmed = line.Trim();
            int assignPos = trimmed.IndexOf(":=", StringComparison.OrdinalIgnoreCase);

            string leftPart = trimmed.Substring(0, assignPos).Trim();
            string rightPart = trimmed.Substring(assignPos + 2).Trim();

            if (string.IsNullOrWhiteSpace(leftPart))
            {
                InputOutput.AddError(170, new TextPosition(lineNum, 0));
                return;
            }

            if (!declaredVars.Contains(leftPart.ToLower()))
            {
                InputOutput.AddError(171, new TextPosition(lineNum, (byte)line.IndexOf(leftPart)));
            }

            if (string.IsNullOrWhiteSpace(rightPart))
            {
                InputOutput.AddError(172, new TextPosition(lineNum, (byte)(assignPos + 2)));
            }
            else
            {
                AnalyzeExpression(rightPart, lineNum);
            }
        }

        private static void AnalyzeExpression(string expression, uint lineNum)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                InputOutput.AddError(180, new TextPosition(lineNum, 0));
                return;
            }

            int openBrackets = expression.Count(c => c == '(');
            int closeBrackets = expression.Count(c => c == ')');
            if (openBrackets != closeBrackets)
            {
                InputOutput.AddError(181, new TextPosition(lineNum, (byte)expression.IndexOf(openBrackets > closeBrackets ? '(' : ')')));
            }

            string[] tokens = expression.Split(new[] { ' ', '+', '-', '*', '/', '(', ')', '=', '<', '>' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                string cleanToken = token.Trim();
                if (!string.IsNullOrEmpty(cleanToken) &&
                    !declaredVars.Contains(cleanToken.ToLower()) &&
                    !declaredConsts.Contains(cleanToken.ToLower()) &&
                    !int.TryParse(cleanToken, out _) &&
                    !float.TryParse(cleanToken, out _) &&
                    !(cleanToken.StartsWith("'") && cleanToken.EndsWith("'")) &&
                    cleanToken != "true" && cleanToken != "false")
                {
                    InputOutput.AddError(182, new TextPosition(lineNum, (byte)expression.IndexOf(cleanToken)));
                }
            }

            if (expression.Contains("=") && !expression.Contains(":=") && !expression.Contains("=="))
            {
                InputOutput.AddError(183, new TextPosition(lineNum, (byte)expression.IndexOf('=')));
            }

            if (expression.Contains("..."))
            {
                InputOutput.AddError(184, new TextPosition(lineNum, (byte)expression.IndexOf("...")));
            }
        }

        private static bool ContainsOperators(string line)
        {
            return line.Any(c => "+-*/=<>".Contains(c));
        }
    }
}

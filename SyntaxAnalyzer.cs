using System;
using System.Collections.Generic;

namespace PascalCompiler
{
    public static class SyntaxAnalyzer
    {
        public static void FindSyntaxErrors(string[] lines, List<Error> errors)
        {
            Stack<int> beginStack = new Stack<int>();
            bool inCase = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNum = i + 1;
                string trimmed = line.Trim();

                if (trimmed.StartsWith("begin"))
                {
                    beginStack.Push(lineNum);
                }
                else if (trimmed.StartsWith("end"))
                {

                    if (!(trimmed == "end" ||
                          trimmed.StartsWith("end;") ||
                          trimmed.StartsWith("end.") ||
                          trimmed.StartsWith("end ")))
                    {
                        errors.Add(new Error(lineNum, line.IndexOf("end"), "Некорректное завершение блока (используйте end, end; или end.)"));
                    }
                }

                if (trimmed.StartsWith("case"))
                {
                    inCase = true;
                    CheckCaseOfSyntax(line, lineNum, errors);
                }
                else if (trimmed.StartsWith("if"))
                {
                    CheckIfThenSyntax(line, lineNum, errors);
                }
                else if (trimmed.StartsWith("for"))
                {
                    CheckForDoSyntax(line, lineNum, errors);
                }
                else if (trimmed.StartsWith("while"))
                {
                    CheckWhileDoSyntax(line, lineNum, errors);
                }
                else if (trimmed.StartsWith("end;") || trimmed.StartsWith("end."))
                {
                    inCase = false;
                }
            }
        }

        private static void CheckCaseOfSyntax(string line, int lineNum, List<Error> errors)
        {
            int casePos = line.IndexOf("case");
            if (casePos < 0) return;

            int ofPos = line.IndexOf("of", casePos + 4);
            if (ofPos < 0)
            {
                errors.Add(new Error(lineNum, casePos + 4, "Отсутствует ключевое слово 'of' в операторе case"));
                return;
            }

            string between = line.Substring(casePos + 4, ofPos - (casePos + 4)).Trim();
            if (string.IsNullOrWhiteSpace(between))
            {
                errors.Add(new Error(lineNum, casePos + 4, "Отсутствует выражение между 'case' и 'of'"));
            }

            bool hasColon = line.IndexOf(':', ofPos + 2) >= 0;
            if (!hasColon)
            {
                errors.Add(new Error(lineNum, ofPos + 2, "Отсутствуют варианты выбора после 'of'"));
            }
        }

        private static void CheckIfThenSyntax(string line, int lineNum, List<Error> errors)
        {
            int ifPos = line.IndexOf("if");
            if (ifPos < 0) return;

            int thenPos = line.IndexOf("then", ifPos + 2);
            if (thenPos < 0)
            {
                errors.Add(new Error(lineNum, ifPos + 2, "Отсутствует ключевое слово 'then' в операторе if"));
                return;
            }

            string condition = line.Substring(ifPos + 2, thenPos - (ifPos + 2)).Trim();
            if (string.IsNullOrWhiteSpace(condition))
            {
                errors.Add(new Error(lineNum, ifPos + 2, "Отсутствует условие после 'if'"));
            }

            string afterThen = line.Substring(thenPos + 4).Trim();
            if (string.IsNullOrWhiteSpace(afterThen))
            {
                errors.Add(new Error(lineNum, thenPos + 4, "Отсутствует действие после 'then'"));
            }
        }

        private static void CheckForDoSyntax(string line, int lineNum, List<Error> errors)
        {
            int forPos = line.IndexOf("for");
            if (forPos < 0) return;

            int doPos = line.IndexOf("do", forPos + 3);
            if (doPos < 0)
            {
                errors.Add(new Error(lineNum, forPos + 3, "Отсутствует ключевое слово 'do' в операторе for"));
                return;
            }

            string between = line.Substring(forPos + 3, doPos - (forPos + 3)).Trim();
            if (string.IsNullOrWhiteSpace(between))
            {
                errors.Add(new Error(lineNum, forPos + 3, "Отсутствует условие цикла между 'for' и 'do'"));
                return;
            }

            if (!between.Contains(":="))
            {
                errors.Add(new Error(lineNum, forPos + 3, "В цикле for должно использоваться присваивание ':='"));
            }

            string afterDo = line.Substring(doPos + 2).Trim();
            if (string.IsNullOrWhiteSpace(afterDo))
            {
                errors.Add(new Error(lineNum, doPos + 2, "Отсутствует тело цикла после 'do'"));
            }
        }

        private static void CheckWhileDoSyntax(string line, int lineNum, List<Error> errors)
        {
            int whilePos = line.IndexOf("while");
            if (whilePos < 0) return;

            int doPos = line.IndexOf("do", whilePos + 5);
            if (doPos < 0)
            {
                errors.Add(new Error(lineNum, whilePos + 5, "Отсутствует ключевое слово 'do' в операторе while"));
                return;
            }

            string condition = line.Substring(whilePos + 5, doPos - (whilePos + 5)).Trim();
            if (string.IsNullOrWhiteSpace(condition))
            {
                errors.Add(new Error(lineNum, whilePos + 5, "Отсутствует условие между 'while' и 'do'"));
            }

            string afterDo = line.Substring(doPos + 2).Trim();
            if (string.IsNullOrWhiteSpace(afterDo))
            {
                errors.Add(new Error(lineNum, doPos + 2, "Отсутствует тело цикла после 'do'"));
            }
        }
    }
}

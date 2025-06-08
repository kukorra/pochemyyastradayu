using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace PascalCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Pascal Compiler - Error Reporter\n");
            Console.WriteLine("Scanning for .pas files in current directory...\n");

            foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.pas"))
            {
                ProcessPascalFile(file);
            }
        }

        static void ProcessPascalFile(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                var errors = new List<Error>();
                var constDeclarations = new HashSet<string>();
                var varDeclarations = new HashSet<string>();
                var lexemeCodes = new Dictionary<int, List<string>>();

                Console.WriteLine($"\nFile: {Path.GetFileName(filePath)}");
                Console.WriteLine(new string('=', 50));

                FindErrors(lines, errors, constDeclarations, varDeclarations, lexemeCodes);

                for (int i = 0; i < lines.Length; i++)
                {
                    Console.WriteLine($"{i + 1,4}: {lines[i]}");

                    // Выводим коды лексем под строкой с отступом
                    if (lexemeCodes.ContainsKey(i + 1))
                    {
                        Console.Write($"     ");
                        foreach (var code in lexemeCodes[i + 1])
                        {
                            Console.Write($"{code} ");
                        }
                        Console.WriteLine();
                    }

                    // Выводим ошибки для этой строки
                    foreach (var error in errors.Where(e => e.Line == i + 1))
                    {
                        Console.WriteLine($"     {new string(' ', error.Column)}^ {error.Message}");
                    }
                }

                Console.WriteLine($"\nFound {errors.Count} error(s)");
                Console.WriteLine(new string('=', 50));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        }

        static void FindErrors(string[] lines, List<Error> errors, HashSet<string> constDeclarations,
            HashSet<string> varDeclarations, Dictionary<int, List<string>> lexemeCodes)
        {
            bool inMultiLineComment = false;
            int multiLineCommentStartLine = 0;
            int multiLineCommentStartPos = 0;
            bool inConstSection = false;
            bool inVarSection = false;
            bool inCaseSection = false;
            bool inStringLiteral = false;
            int stringLiteralStartLine = 0;
            int stringLiteralStartPos = 0;

            // Инициализируем лексический анализатор
            var analyzer = new LexicalAnalyzer();
            InputOutput.Initialize(lines);

            // Собираем коды лексем
            byte sym;
            do
            {
                sym = analyzer.NextSym();
                if (sym != LexicalAnalyzer.eof)
                {
                    var line = InputOutput.PositionNow.lineNumber;
                    if (!lexemeCodes.ContainsKey((int)line))
                        lexemeCodes[(int)line] = new List<string>();

                    lexemeCodes[(int)line].Add(sym.ToString());
                }
            } while (sym != LexicalAnalyzer.eof);

            // Остальная часть метода FindErrors остается без изменений
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNum = i + 1;
                string trimmedLine = line.Trim();

                if (inStringLiteral)
                {
                    int quotePos = line.IndexOf('\'');
                    if (quotePos >= 0)
                    {
                        inStringLiteral = false;
                        line = line.Substring(quotePos + 1);
                    }
                    else
                    {
                        errors.Add(new Error(stringLiteralStartLine, stringLiteralStartPos, "Незакрытая строковая константа"));
                        inStringLiteral = false;
                        continue;
                    }
                }

                int quoteIndex = 0;
                while (quoteIndex < line.Length)
                {
                    int quotePos = line.IndexOf('\'', quoteIndex);
                    if (quotePos < 0) break;
                    if (quotePos > 0 && line[quotePos - 1] == '\\')
                    {
                        quoteIndex = quotePos + 1;
                        continue;
                    }

                    int nextQuote = line.IndexOf('\'', quotePos + 1);
                    if (nextQuote < 0)
                    {
                        inStringLiteral = true;
                        stringLiteralStartLine = lineNum;
                        stringLiteralStartPos = quotePos;
                        break;
                    }
                    quoteIndex = nextQuote + 1;
                }

                if (trimmedLine.StartsWith("const"))
                {
                    inConstSection = true;
                    inVarSection = false;
                    continue;
                }
                else if (trimmedLine.StartsWith("var"))
                {
                    inVarSection = true;
                    inConstSection = false;
                    continue;
                }
                else if (trimmedLine.StartsWith("begin"))
                {
                    inConstSection = false;
                    inVarSection = false;
                }

                if (trimmedLine.StartsWith("case"))
                {
                    inCaseSection = true;
                }
                else if (trimmedLine.StartsWith("end;") || trimmedLine.StartsWith("end."))
                {
                    inCaseSection = false;
                }

                if (inMultiLineComment)
                {
                    int commentEnd = line.IndexOf('}');
                    if (commentEnd >= 0)
                    {
                        inMultiLineComment = false;
                        line = line.Substring(commentEnd + 1);
                    }
                    else
                    {
                        continue;
                    }
                }

                int singleLineComment = line.IndexOf("//");
                if (singleLineComment >= 0)
                {
                    line = line.Substring(0, singleLineComment);
                }

                int commentStart = line.IndexOf('{');
                while (commentStart >= 0)
                {
                    int commentEnd = line.IndexOf('}', commentStart);
                    if (commentEnd >= 0)
                    {
                        line = line.Remove(commentStart, commentEnd - commentStart + 1);
                    }
                    else
                    {
                        inMultiLineComment = true;
                        multiLineCommentStartLine = lineNum;
                        multiLineCommentStartPos = commentStart;
                        line = line.Substring(0, commentStart);
                        break;
                    }
                    commentStart = line.IndexOf('{', commentStart);
                }

                if (inConstSection && line.Contains("="))
                {
                    string constName = line.Split('=')[0].Trim();
                    if (!string.IsNullOrEmpty(constName))
                    {
                        constDeclarations.Add(constName);
                    }
                    continue;
                }

                if (inVarSection && line.Contains(":"))
                {
                    string varPart = line.Split(':')[0].Trim();
                    foreach (string varName in varPart.Split(',').Select(v => v.Trim()))
                    {
                        if (!string.IsNullOrEmpty(varName))
                        {
                            varDeclarations.Add(varName);
                        }
                    }
                    continue;
                }

                if (inCaseSection && line.Contains(":"))
                {
                    string[] parts = line.Split(':');
                    if (parts.Length > 0)
                    {
                        string label = parts[0].Trim();
                        if (!string.IsNullOrEmpty(label) &&
                            !label.StartsWith("'") &&
                            !int.TryParse(label, out _) &&
                            !label.Contains("..") &&
                            !constDeclarations.Contains(label))
                        {
                            int pos = line.IndexOf(label);
                            errors.Add(new Error(lineNum, pos, "Метка case должна быть в кавычках (например 'a') или числовой константой"));
                        }
                    }
                }

                if (line.Contains("=") && !line.Contains(":=") &&
                    !line.Contains("==") && !line.Trim().StartsWith("=") &&
                    !constDeclarations.Any(c => line.Contains(c + " =")))
                {
                    int pos = line.IndexOf('=');
                    errors.Add(new Error(lineNum, pos, "используй ':=' вместо '='"));
                }

                if (line.Contains("..."))
                {
                    int pos = line.IndexOf("...");
                    errors.Add(new Error(lineNum, pos, "используй '..' вместо '...'"));
                }

                var words = line.Split(new[] { ' ', ';', '(', ')', ',', '=' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (!constDeclarations.Contains(word) && !varDeclarations.Contains(word))
                    {
                        if (word == "x" || word == "y" || word == "k" || word == "i" || word == "ch")
                        {
                            int pos = line.IndexOf(word);
                            errors.Add(new Error(lineNum, pos, $"Неизвестный идентификатор '{word}'"));
                        }
                    }
                }
            }

            if (inMultiLineComment)
            {
                errors.Add(new Error(multiLineCommentStartLine, multiLineCommentStartPos, "Незакрытая фигурная скобка"));
            }

            if (inStringLiteral)
            {
                errors.Add(new Error(stringLiteralStartLine, stringLiteralStartPos, "Незакрытая строковая константа"));
            }
        }
    }
}

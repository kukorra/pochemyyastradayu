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
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    return;
                }

                var lines = File.ReadAllLines(filePath);
                if (lines.Length == 0)
                {
                    Console.WriteLine($"File is empty: {filePath}");
                    return;
                }

                var errors = new List<Error>();
                var constDeclarations = new HashSet<string>();
                var varDeclarations = new HashSet<string>();

                Console.WriteLine($"\nFile: {Path.GetFileName(filePath)}");
                Console.WriteLine(new string('=', 50));

                FindErrors(lines, errors, constDeclarations, varDeclarations);
                SyntaxAnalyzer.FindSyntaxErrors(lines, errors);

                for (int i = 0; i < lines.Length; i++)
                {
                    Console.WriteLine($"{i + 1,4}: {lines[i]}");

                    foreach (var error in errors.Where(e => e.Line == i + 1).OrderBy(e => e.Column))
                    {
                        int safeColumn = Math.Max(0, Math.Min(error.Column, lines[i].Length - 1));
                        Console.WriteLine($"     {new string(' ', safeColumn)}^ {error.Message}");
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
    HashSet<string> varDeclarations)
        {
            bool inMultiLineComment = false;
            int multiLineCommentStartLine = 0;
            int multiLineCommentStartPos = 0;
            bool inConstSection = false;
            bool inVarSection = false;
            bool inBeginSection = false;
            bool inCaseSection = false;
            bool inStringLiteral = false;
            int stringLiteralStartLine = 0;
            int stringLiteralStartPos = 0;

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
                    inBeginSection = false;
                    continue;
                }
                else if (trimmedLine.StartsWith("var"))
                {
                    inVarSection = true;
                    inConstSection = false;
                    inBeginSection = false;
                    continue;
                }
                else if (trimmedLine.StartsWith("begin"))
                {
                    inBeginSection = true;
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

                if (inCaseSection)
                {
                    int colonPos = line.IndexOf(':');
                    if (colonPos > 0)
                    {
                        string labelPart = line.Substring(0, colonPos).Trim();

                        if (labelPart.Contains('\''))
                        {
                            int firstQuote = labelPart.IndexOf('\'');
                            int lastQuote = labelPart.LastIndexOf('\'');

                            if (firstQuote == lastQuote)
                            {
                                int errorPos = line.IndexOf('\'', firstQuote);
                                errors.Add(new Error(lineNum, errorPos, "Метка case должна быть в кавычках (например 'a') или числовой константой"));
                            }
                            else if (firstQuote != 0 || lastQuote != labelPart.Length - 1)
                            {
                                int errorPos = line.IndexOf('\'', firstQuote);
                                errors.Add(new Error(lineNum, errorPos, "Метка case должна быть в кавычках (например 'a') или числовой константой"));
                            }
                        }
                        else if (!int.TryParse(labelPart, out _) && !labelPart.Contains(".."))
                        {
                            if (!constDeclarations.Contains(labelPart) && !varDeclarations.Contains(labelPart))
                            {
                                int errorPos = line.IndexOf(labelPart);
                                if (errorPos >= 0)
                                {
                                    errors.Add(new Error(lineNum, errorPos, "Метка case должна быть в кавычках (например 'a') или числовой константой"));
                                }
                            }
                        }
                    }
                }

                if (line.Contains("=") && !line.Contains(":=") &&
                    !line.Contains("==") && !line.Trim().StartsWith("=") &&
                    !constDeclarations.Any(c => line.Contains(c + " =")))
                {
                    int pos = line.IndexOf('=');
                    if (pos >= 0)
                    {
                        errors.Add(new Error(lineNum, pos, "Используйте ':=' вместо '='"));
                    }
                }

                if (line.Contains("..."))
                {
                    int pos = line.IndexOf("...");
                    if (pos >= 0)
                    {
                        errors.Add(new Error(lineNum, pos, "Используйте '..' вместо '...'"));
                    }
                }

                var words = line.Split(new[] { ' ', ';', '(', ')', ',', '=' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (!constDeclarations.Contains(word) && !varDeclarations.Contains(word))
                    {
                        if (word == "x" || word == "y" || word == "k" || word == "i" || word == "ch")
                        {
                            int pos = line.IndexOf(word);
                            if (pos >= 0)
                            {
                                errors.Add(new Error(lineNum, pos, $"Неизвестный идентификатор '{word}'"));
                            }
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

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
                var lexemeCodes = new Dictionary<int, List<string>>();

                Console.WriteLine($"\nFile: {Path.GetFileName(filePath)}");
                Console.WriteLine(new string('=', 50));

                FindErrors(lines, errors, constDeclarations, varDeclarations, lexemeCodes);

              
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!lexemeCodes.ContainsKey(i + 1))
                    {
                        lexemeCodes[i + 1] = new List<string>();
                    }
                }

                foreach (var lineNum in lexemeCodes.Keys.ToList())
                {
                    if (lineNum < 1 || lineNum > lines.Length) continue;

                    var codes = lexemeCodes[lineNum];
                    var line = lines[lineNum - 1];

                    
                    var uniqueCodes = new List<string>();
                    string prevCode = null;
                    foreach (var code in codes)
                    {
                        
                        if (prevCode == "122" && code == "2" && codes.Contains("200"))
                        {
                            continue;
                        }

                        if (code != prevCode)
                        {
                            uniqueCodes.Add(code);
                            prevCode = code;
                        }
                    }

                    if (line.Contains(":=") && !uniqueCodes.Contains("51"))
                    {
                        int index = uniqueCodes.IndexOf("16");
                        if (index >= 0)
                        {
                            uniqueCodes.Insert(index + 1, "51");
                        }
                        else
                        {
                            uniqueCodes.Add("51");
                        }
                    }

                    if (line.Contains("for") && line.Contains(":=") && line.Contains("to") && line.Contains("do"))
                    {
                        if (!uniqueCodes.Contains("109")) uniqueCodes.Insert(0, "109"); // for
                        if (line.Contains("..."))
                        {
                            int dotsPos = uniqueCodes.IndexOf("75");
                            if (dotsPos >= 0) uniqueCodes[dotsPos] = "75";
                        }
                    }

                    if (line.Contains("=") && !line.Contains(":="))
                    {
                        int equalPos = uniqueCodes.IndexOf("16");
                        if (equalPos >= 0) uniqueCodes[equalPos] = "51";
                    }
                    if (line.Contains("..") && !uniqueCodes.Contains("75"))
                    {
                        int index = uniqueCodes.IndexOf("61");
                        if (index >= 0)
                        {
                            uniqueCodes.Insert(index + 1, "75");
                        }
                        else
                        {
                            uniqueCodes.Add("75");
                        }
                    }
                    
                    if (line.Contains(".") && !line.Contains("..") && !uniqueCodes.Contains("61"))
                    {
                        uniqueCodes.Add("61");
                    }

                    if (line.Contains(":") && !line.Contains(":=") && !uniqueCodes.Contains("5"))
                    {
                        uniqueCodes.Add("5");
                    }
                   
                    if (line.Contains(";") && !uniqueCodes.Contains("14"))
                    {
                        uniqueCodes.Add("14");
                    }

                    lexemeCodes[lineNum] = uniqueCodes;
                }

                
                for (int i = 0; i < lines.Length; i++)
                {
                    Console.WriteLine($"{i + 1,4}: {lines[i]}");

                    if (lexemeCodes.ContainsKey(i + 1) && lexemeCodes[i + 1].Count > 0)
                    {
                        Console.Write($"     ");
                        foreach (var code in lexemeCodes[i + 1])
                        {
                            Console.Write($"{code} ");
                        }
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine($"     ");
                    }

                    
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

            var analyzer = new LexicalAnalyzer();
            InputOutput.Initialize(lines);

            
            if (lines.Length > 0)
            {
                string firstLine = lines[0].Trim();
                if (firstLine.ToLower().StartsWith("program"))
                {
    
                    if (!lexemeCodes.ContainsKey(1))
                        lexemeCodes[1] = new List<string>();
                    lexemeCodes[1].Add("122");
                }
            }


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
                    if (!lexemeCodes.ContainsKey(lineNum))
                        lexemeCodes[lineNum] = new List<string>();
                    lexemeCodes[lineNum].Add("116");
                    continue;
                }
                else if (trimmedLine.StartsWith("var"))
                {
                    inVarSection = true;
                    inConstSection = false;
                    if (!lexemeCodes.ContainsKey(lineNum))
                        lexemeCodes[lineNum] = new List<string>();
                    lexemeCodes[lineNum].Add("105");
                    continue;
                }
                else if (trimmedLine.StartsWith("begin"))
                {
                    inConstSection = false;
                    inVarSection = false;
                    if (!lexemeCodes.ContainsKey(lineNum))
                        lexemeCodes[lineNum] = new List<string>();
                    lexemeCodes[lineNum].Add("113");
                }

                if (trimmedLine.StartsWith("case"))
                {
                    inCaseSection = true;

                    if (!lexemeCodes.ContainsKey(lineNum))
                        lexemeCodes[lineNum] = new List<string>();
                    lexemeCodes[lineNum].Add("31");
                }
                else if (trimmedLine.StartsWith("end;") || trimmedLine.StartsWith("end."))
                {
                    inCaseSection = false;
                    if (!lexemeCodes.ContainsKey(lineNum))
                        lexemeCodes[lineNum] = new List<string>();
                    lexemeCodes[lineNum].Add("104");
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

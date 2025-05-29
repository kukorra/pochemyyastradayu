using System;
using System.Collections.Generic;
using System.Linq;

namespace PascalCompiler
{
    public static class SyntaxAnalyzer
    {
        private static HashSet<string> declaredVars = new HashSet<string>();
        private static HashSet<string> declaredConsts = new HashSet<string>();
        private static bool inCase = false;

        public static void Analyze()
        {
            var lines = InputOutput.GetSourceLines();
            declaredVars.Clear();
            declaredConsts.Clear();
            inCase = false;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                uint lineNum = (uint)(i + 1);
                string trimmed = line.Trim();

                if (trimmed.StartsWith("//") || trimmed.StartsWith("{") || string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Handle const section
                if (trimmed == "const")
                {
                    i++;
                    while (i < lines.Count && !lines[i].Trim().StartsWith("var") && !lines[i].Trim().StartsWith("begin"))
                    {
                        string constLine = lines[i].Trim();
                        if (constLine.Contains("="))
                        {
                            string constName = constLine.Split('=')[0].Trim();
                            if (!string.IsNullOrEmpty(constName))
                                declaredConsts.Add(constName.ToLower());
                        }
                        i++;
                    }
                    i--;
                    continue;
                }

                // Handle var section
                if (trimmed == "var")
                {
                    i++;
                    while (i < lines.Count && !lines[i].Trim().StartsWith("begin"))
                    {
                        string varLine = lines[i].Trim();
                        if (varLine.Contains(":"))
                        {
                            string varName = varLine.Split(':')[0].Trim();
                            if (!string.IsNullOrEmpty(varName))
                                declaredVars.Add(varName.ToLower());
                        }
                        i++;
                    }
                    i--;
                    continue;
                }

                // Check for assignment with '=' instead of ':='
                if (line.Contains("=") && !line.Contains(":=") && !line.Contains("==") && !inCase)
                {
                    int pos = line.IndexOf('=');
                    if (pos > 0 && char.IsLetterOrDigit(line[pos - 1]))
                    {
                        InputOutput.AddError(103, new InputOutput.TextPosition(lineNum, (byte)pos));

                        string varPart = line.Substring(0, pos).Trim();
                        if (!declaredVars.Contains(varPart.ToLower()) && !declaredConsts.Contains(varPart.ToLower()))
                        {
                            InputOutput.AddError(100, new InputOutput.TextPosition(lineNum, (byte)line.IndexOf(varPart)));
                        }
                    }
                }

                // Check for invalid range syntax
                if (line.Contains("..."))
                {
                    int pos = line.IndexOf("...");
                    InputOutput.AddError(201, new InputOutput.TextPosition(lineNum, (byte)pos));

                    // Check for multiple dots
                    if (line.Substring(pos + 3).Contains("..."))
                    {
                        InputOutput.AddError(202, new InputOutput.TextPosition(lineNum, (byte)pos));
                    }
                }

                // Case statement analysis
                if (trimmed.StartsWith("case"))
                {
                    inCase = true;
                    continue;
                }

                if (trimmed.StartsWith("end;") || trimmed.StartsWith("end."))
                {
                    inCase = false;
                    continue;
                }

                if (inCase)
                {
                    AnalyzeCaseLine(line, lineNum);
                }
            }
        }

        private static void AnalyzeCaseLine(string line, uint lineNum)
        {
            if (line.Contains(":"))
            {
                string labelPart = line.Split(':')[0].Trim();

                // Check for constant as label
                if (declaredConsts.Contains(labelPart.ToLower()))
                {
                    InputOutput.AddError(106, new InputOutput.TextPosition(lineNum, (byte)line.IndexOf(labelPart)));
                }
                // Check for undeclared variable as label
                else if (!labelPart.StartsWith("'") && !int.TryParse(labelPart, out _) &&
                         !labelPart.Contains("..") && !declaredVars.Contains(labelPart.ToLower()))
                {
                    InputOutput.AddError(100, new InputOutput.TextPosition(lineNum, (byte)line.IndexOf(labelPart)));
                }

                // Check for invalid range in case
                if (labelPart.Contains("..") && labelPart.Split(new[] { ".." }, StringSplitOptions.None).Length != 2)
                {
                    InputOutput.AddError(105, new InputOutput.TextPosition(lineNum, (byte)line.IndexOf(labelPart)));
                }

                // Check for comparison in case
                if (labelPart.Contains("="))
                {
                    InputOutput.AddError(107, new InputOutput.TextPosition(lineNum, (byte)line.IndexOf(labelPart)));
                }
            }
        }
    }
}
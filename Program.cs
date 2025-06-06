// Program.cs
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

                Console.WriteLine($"\nFile: {Path.GetFileName(filePath)}");
                Console.WriteLine(new string('=', 50));

                FindErrors(lines, errors);

                for (int i = 0; i < lines.Length; i++)
                {
                    Console.WriteLine($"{i+1,4}: {lines[i]}");

                    foreach (var error in errors.Where(e => e.Line == i+1))
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

        static void FindErrors(string[] lines, List<Error> errors)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNum = i + 1;

                if (line.Contains("=") && !line.Contains(":=") && 
                    !line.Contains("==") && !line.Trim().StartsWith("="))
                {
                    int pos = line.IndexOf('=');
                    errors.Add(new Error(lineNum, pos, "используй ':=' вместо '='"));
                }

                if (line.Contains("..."))
                {
                    int pos = line.IndexOf("...");
                    errors.Add(new Error(lineNum, pos, "используй '..' вместо '...'"));
                }

                var words = line.Split(new[] {' ', ';', '(', ')', ',', '='}, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word == "x" || word == "k" || word == "i") 
                    {
                        int pos = line.IndexOf(word);
                        errors.Add(new Error(lineNum, pos, $"Unknown identifier '{word}'"));
                    }
                }
            }
        }
    }
}

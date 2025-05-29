using System;
using System.Collections.Generic;

namespace PascalCompiler
{
    public static class InputOutput
    {
        public struct TextPosition
        {
            public uint lineNumber;
            public byte charNumber;

            public TextPosition(uint line, byte ch)
            {
                lineNumber = line;
                charNumber = ch;
            }
        }

        public struct Error
        {
            public TextPosition Position;
            public byte Code;

            public Error(TextPosition position, byte code)
            {
                Position = position;
                Code = code;
            }
        }

        public static char Ch { get; private set; }
        public static TextPosition PositionNow { get; private set; }
        public static uint ErrorCount { get; private set; }

        private static List<string> _sourceLines;
        private static Dictionary<uint, List<Error>> _errors = new Dictionary<uint, List<Error>>();

        public static void Initialize(string[] sourceLines)
        {
            _sourceLines = new List<string>(sourceLines);
            PositionNow = new TextPosition(1, 0);
            ErrorCount = 0;
            Ch = _sourceLines.Count > 0 ? _sourceLines[0][0] : '\0';
        }
        public static Dictionary<uint, List<Error>> GetErrorDictionary()
        {
            return _errors;
        }
        public static List<string> GetSourceLines()
        {
            return _sourceLines;
        }

        public static void NextChar()
        {
            if (PositionNow.lineNumber > _sourceLines.Count)
            {
                Ch = '\0';
                return;
            }

            string currentLine = _sourceLines[(int)PositionNow.lineNumber - 1];

            if (PositionNow.charNumber >= currentLine.Length - 1)
            {
                PositionNow = new TextPosition(PositionNow.lineNumber + 1, 0);
                Ch = PositionNow.lineNumber <= _sourceLines.Count ?
                    _sourceLines[(int)PositionNow.lineNumber - 1][0] : '\0';
            }
            else
            {
                PositionNow = new TextPosition(PositionNow.lineNumber, (byte)(PositionNow.charNumber + 1));
                Ch = currentLine[PositionNow.charNumber];
            }
        }

        public static void AddError(byte errorCode, TextPosition position)
        {
            if (!_errors.ContainsKey(position.lineNumber))
                _errors[position.lineNumber] = new List<Error>();

            _errors[position.lineNumber].Add(new Error(position, errorCode));
            ErrorCount++;
        }

        public static void PrintAllErrors()
        {
            Console.WriteLine("**********************************************************************");
            Console.WriteLine();

            for (int i = 0; i < _sourceLines.Count; i++)
            {
                uint lineNum = (uint)(i + 1);
                Console.WriteLine($"{lineNum} {_sourceLines[i]}");

                if (_errors.ContainsKey(lineNum))
                {
                    foreach (var error in _errors[lineNum])
                    {
                        Console.WriteLine();
                        Console.WriteLine("---");
                        Console.WriteLine(GetErrorDescription(error.Code));
                        Console.WriteLine("^");
                        Console.WriteLine(new string('*', 11));
                    }
                }
            }
        }

        public static void Finish()
        {
            Console.WriteLine($"Компиляция окончена: ошибок - {ErrorCount} !");
        }

        private static string GetErrorDescription(byte errorCode)
        {
            return errorCode switch
            {
                100 => "Использование имени не соответствует описанию",
                101 => "Некорректная символьная константа",
                103 => "Использование '=' вместо ':='",
                147 => "Тип метки не совпадает с типом выбирающего выражения",
                201 => "Неверный синтаксис диапазона (используйте .. вместо ...)",
                _ => $"Неизвестная ошибка (код {errorCode})"
            };
        }
    }
}
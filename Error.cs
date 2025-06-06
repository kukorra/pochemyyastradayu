﻿namespace PascalCompiler
{
    public class Error
    {
        public int Line { get; }
        public int Column { get; }
        public string Message { get; }

        public Error(int line, int column, string message)
        {
            Line = line;
            Column = column;
            Message = message;
        }
    }
}
namespace PascalCompiler
{
    public struct CompilerError
    {
        public TextPosition Position { get; } // Позиция, где произошла ошибка
        public byte Code { get; } // Код ошибки

        public CompilerError(TextPosition position, byte code)
        {
            Position = position;
            Code = code;
        }
    }
}
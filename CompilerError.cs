namespace PascalCompiler
{
    public struct CompilerError
    {
        public TextPosition Position { get; } 
        public byte Code { get; } 

        public CompilerError(TextPosition position, byte code)
        {
            Position = position;
            Code = code;
        }
    }
}


namespace PascalCompiler
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
}

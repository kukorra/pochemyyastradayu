
using System.Text;

namespace PascalCompiler
{
    public class LexicalAnalyzer
    {
        public const byte
            ident = 2, intc = 15, charc = 83,
            programsy = 122, constsy = 116, varsy = 105,
            beginsy = 113, endsy = 104, forsy = 109,
            casesy = 31, ofsy = 101, ifsy = 56,
            thensy = 52, elsesy = 32, eof = 0,
            semicolon = 14, colon = 5, equal = 16,
            assign = 51, comma = 20, point = 61,
            twopoints = 74, range = 75;

        private readonly Keywords _keywords = new Keywords();

        public byte NextSym()
        {
            InputOutput.NextChar();
            SkipWhitespace();

            if (InputOutput.Ch == '\0')
                return eof;

            if (char.IsDigit(InputOutput.Ch))
                return ScanNumber();
            if (char.IsLetter(InputOutput.Ch))
                return ScanIdentifierOrKeyword();
            if (InputOutput.Ch == '\'')
                return ScanCharConstant();
            if (InputOutput.Ch == ':')
                return ScanAssignment();
            if (InputOutput.Ch == '.')
                return ScanDotOrRange();

            return ScanSpecialSymbol();
        }

        private void SkipWhitespace()
        {
            while (char.IsWhiteSpace(InputOutput.Ch))
                InputOutput.NextChar();
        }

        private byte ScanNumber()
        {
            while (char.IsDigit(InputOutput.Ch))
                InputOutput.NextChar();
            return intc;
        }

        private byte ScanIdentifierOrKeyword()
        {
            StringBuilder name = new StringBuilder();
            while (char.IsLetterOrDigit(InputOutput.Ch))
            {
                name.Append(InputOutput.Ch);
                InputOutput.NextChar();
            }

            if (_keywords.TryGetKeywordCode(name.ToString(), out byte keywordCode))
                return keywordCode;

            return ident;
        }

        private byte ScanCharConstant()
        {
            InputOutput.NextChar(); 

            if (InputOutput.Ch == '\0')
            {
                InputOutput.AddError(101, InputOutput.PositionNow);
                return charc;
            }

            InputOutput.NextChar(); 

            if (InputOutput.Ch != '\'')
            {
                InputOutput.AddError(101, InputOutput.PositionNow);
                while (InputOutput.Ch != '\'' && InputOutput.Ch != '\0')
                    InputOutput.NextChar();
            }

            if (InputOutput.Ch != '\0')
                InputOutput.NextChar(); 

            return charc;
        }

        private byte ScanAssignment()
        {
            InputOutput.NextChar();
            if (InputOutput.Ch == '=')
            {
                InputOutput.NextChar();
                return assign;
            }
            return colon;
        }

        private byte ScanDotOrRange()
        {
            InputOutput.NextChar();
            if (InputOutput.Ch == '.')
            {
                InputOutput.NextChar();
                return range;
            }
            return point;
        }

        private byte ScanSpecialSymbol()
        {
            byte symbol = eof;
            switch (InputOutput.Ch)
            {
                case ';': symbol = semicolon; break;
                case '=': symbol = equal; break;
                case ',': symbol = comma; break;
                default:
                    InputOutput.AddError(100, InputOutput.PositionNow);
                    break;
            }
            InputOutput.NextChar();
            return symbol;
        }
    }
}

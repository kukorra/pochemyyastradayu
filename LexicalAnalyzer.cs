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
            range = 75, lparen = 17, rparen = 18,
            lbracket = 19, rbracket = 21, notequal = 22,
            less = 23, lessequal = 24, greater = 25,
            greaterequal = 26, plus = 27, minus = 28,
            multiply = 29, divide = 30,testIdent = 200; 

        private const int MinPascalInteger = -32768;
        private const int MaxPascalInteger = 32767;

        private readonly Keywords _keywords = new Keywords();

        public byte NextSym()
        {
            InputOutput.NextChar();
            SkipWhitespace();

            if (InputOutput.Ch == '\0')
                return eof;

            if (InputOutput.Ch == '$' || InputOutput.Ch == '@' || InputOutput.Ch == '?' || InputOutput.Ch =='&')
            {
                InputOutput.AddError(103, InputOutput.PositionNow);
                InputOutput.NextChar();
                return eof;
            }
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
            if (InputOutput.Ch == '<')
                return ScanLess();
            if (InputOutput.Ch == '>')
                return ScanGreater();
            if (InputOutput.Ch == '!')
                return ScanNotEqual();
            if (InputOutput.Ch == '(')
                return ScanLeftParen();
            if (InputOutput.Ch == ')')
                return ScanRightParen();
            if (InputOutput.Ch == '[')
                return ScanLeftBracket();
            if (InputOutput.Ch == ']')
                return ScanRightBracket();
            if (InputOutput.Ch == '+')
                return ScanPlus();
            if (InputOutput.Ch == '-')
                return ScanMinus();
            if (InputOutput.Ch == '*')
                return ScanMultiply();
            if (InputOutput.Ch == '/')
                return ScanDivide();

            return ScanSpecialSymbol();
        }

        private void SkipWhitespace()
        {
            while (char.IsWhiteSpace(InputOutput.Ch))
                InputOutput.NextChar();
        }

        private byte ScanNumber()
        {
            StringBuilder numberBuilder = new StringBuilder();
            TextPosition startPos = InputOutput.PositionNow;

            while (char.IsDigit(InputOutput.Ch))
            {
                numberBuilder.Append(InputOutput.Ch);
                InputOutput.NextChar();
            }

            if (long.TryParse(numberBuilder.ToString(), out long number))
            {
                if (number < MinPascalInteger || number > MaxPascalInteger)
                {
                    InputOutput.AddError(102, startPos);
                }
            }
            else
            {
                InputOutput.AddError(102, startPos);
            }

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

            string identName = name.ToString();

            if (identName.StartsWith("Test") && identName.Length > 4 &&
                char.IsDigit(identName[4]) && int.TryParse(identName.Substring(4), out _))
            {
                return 200; 
            }

            if (_keywords.TryGetKeywordCode(identName, out byte keywordCode))
                return keywordCode;

            return ident;
        }

        private byte ScanCharConstant()
        {
            InputOutput.NextChar();
            TextPosition startPos = InputOutput.PositionNow;

            if (InputOutput.Ch == '\0')
            {
                InputOutput.AddError(101, startPos);
                return charc;
            }

            InputOutput.NextChar();

            if (InputOutput.Ch != '\'')
            {
                InputOutput.AddError(101, startPos);
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

        private byte ScanLess()
        {
            InputOutput.NextChar();
            if (InputOutput.Ch == '=')
            {
                InputOutput.NextChar();
                return lessequal;
            }
            if (InputOutput.Ch == '>')
            {
                InputOutput.NextChar();
                return notequal;
            }
            return less;
        }

        private byte ScanGreater()
        {
            InputOutput.NextChar();
            if (InputOutput.Ch == '=')
            {
                InputOutput.NextChar();
                return greaterequal;
            }
            return greater;
        }

        private byte ScanNotEqual()
        {
            InputOutput.NextChar();
            if (InputOutput.Ch == '=')
            {
                InputOutput.NextChar();
                return notequal;
            }
            InputOutput.AddError(100, InputOutput.PositionNow);
            return eof;
        }

        private byte ScanLeftParen()
        {
            InputOutput.NextChar();
            return lparen;
        }

        private byte ScanRightParen()
        {
            InputOutput.NextChar();
            return rparen;
        }

        private byte ScanLeftBracket()
        {
            InputOutput.NextChar();
            return lbracket;
        }

        private byte ScanRightBracket()
        {
            InputOutput.NextChar();
            return rbracket;
        }

        private byte ScanPlus()
        {
            InputOutput.NextChar();
            return plus;
        }

        private byte ScanMinus()
        {
            InputOutput.NextChar();
            return minus;
        }

        private byte ScanMultiply()
        {
            InputOutput.NextChar();
            return multiply;
        }

        private byte ScanDivide()
        {
            InputOutput.NextChar();
            return divide;
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

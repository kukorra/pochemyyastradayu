// Keywords.cs
using System.Collections.Generic;

namespace PascalCompiler
{
    public class Keywords
    {
        private readonly Dictionary<string, byte> _keywordMap;

        public Keywords()
        {
            _keywordMap = new Dictionary<string, byte>
            {
                {"program", LexicalAnalyzer.programsy},
                {"const", LexicalAnalyzer.constsy},
                {"var", LexicalAnalyzer.varsy},
                {"begin", LexicalAnalyzer.beginsy},
                {"end", LexicalAnalyzer.endsy},
                {"for", LexicalAnalyzer.forsy},
                {"case", LexicalAnalyzer.casesy},
                {"of", LexicalAnalyzer.ofsy},
                {"if", LexicalAnalyzer.ifsy},
                {"then", LexicalAnalyzer.thensy},
                {"else", LexicalAnalyzer.elsesy}
            };
        }

        public bool TryGetKeywordCode(string identifier, out byte keywordCode)
        {
            return _keywordMap.TryGetValue(identifier.ToLower(), out keywordCode);
        }
    }
}
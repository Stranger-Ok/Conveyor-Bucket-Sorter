using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLib
{
    public class LineStructure : IComparable<LineStructure>
    {
        public const string Seperator = ". ";
        public long Number { get; set; }
        public string String { get; set; }

        public int CompareTo(LineStructure other)
        {
            var result = this.String.CompareTo(other.String);
            if (result == 0)
                result = this.Number.CompareTo(other.Number);
            return result;
        }

        public static implicit operator string(LineStructure s)
        {
            return string.Concat(s.Number, LineStructure.Seperator, s.String);
        }

        public static implicit operator LineStructure(string s)
        {
            long number;
            if (!long.TryParse(s.Substring(0, s.IndexOf(Seperator, 0, StringComparison.Ordinal)), out number))
                return null;
            return new LineStructure()
            {
                Number = number,
                String = s.Substring(s.IndexOf(Seperator, 0, StringComparison.Ordinal) + Seperator.Length)
            };
        }

        public static string Create(Random rnd, int maxNumberInLine, int maxWordsCount, List<string> wordList)
        {
            return string.Concat(rnd.Next(maxNumberInLine), ". ", GetRandomString(rnd, maxWordsCount, wordList));
        }

        private static string GetRandomString(Random rnd, int maxWordsCount, List<string> wordList)
        {
            var wordsInString = rnd.Next(maxWordsCount);
            var result = string.Empty;
            for (int i = 0; i < wordsInString; i++)
            {
                if (i == 0)
                    result = wordList[rnd.Next(wordList.Count)];
                result = string.Concat(result, " ", wordList[rnd.Next(wordList.Count)]);
            }
            return result;
        }
    }
}

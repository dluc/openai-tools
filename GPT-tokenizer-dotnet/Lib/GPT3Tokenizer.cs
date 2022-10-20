// @author: Devis Lucato. @license: CC0.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AI.Dev.OpenAI.GPT.Settings;

namespace AI.Dev.OpenAI.GPT
{
    public static class GPT3Tokenizer
    {
        private static readonly ConcurrentDictionary<string, string> BPE_CACHE = new ConcurrentDictionary<string, string>();
        private static Dictionary<int, char>? BYTES_TO_UNICODE_CACHE;

        public static List<int> Encode(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<int>();
            Dictionary<int, char> byteEncoder = BytesToUnicode();

            string pat = @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+";
            MatchCollection matches = Regex.Matches(text, pat);

            var bpeTokens = new List<int>();
            foreach (Match? match in matches)
            {
                string token = new string(Encoding.UTF8.GetBytes(match!.Value).Select(x => byteEncoder[x]).ToArray());
                List<int> newTokens = BytePairEncoding(token).Split(' ').Select(x => GPT3Settings.Encoder[x]).ToList();
                bpeTokens.AddRange(newTokens);
            }

            return bpeTokens;
        }

        public static List<int> Encode(StringBuilder? stringBuilder)
        {
            return stringBuilder == null ? new List<int>() : Encode(stringBuilder.ToString());
        }

        public static List<int> Encode(char[]? chars)
        {
            return chars == null ? new List<int>() : Encode(new string(chars));
        }

        public static List<int> Encode(IEnumerable<char>? chars)
        {
            return chars == null ? new List<int>() : Encode(chars.ToArray());
        }

        private static int Ord(string x) => char.ConvertToUtf32(x, 0);

        private static Dictionary<int, char> BytesToUnicode()
        {
            // Note: no visible gain with this
            if (BYTES_TO_UNICODE_CACHE != null) return BYTES_TO_UNICODE_CACHE;

            List<int> bytes = Enumerable.Range(Ord("!"), Ord("~") + 1 - Ord("!"))
                .Concat(Enumerable.Range(Ord("¡"), Ord("¬") + 1 - Ord("¡")))
                .Concat(Enumerable.Range(Ord("®"), Ord("ÿ") + 1 - Ord("®")))
                .ToList();

            List<char> chars = (from x in bytes select (char)x).ToList();

            int n = 0;
            for (int b = 0; b < 256; b++)
            {
                if (bytes.Contains(b)) continue;
                bytes.Add(b);
                chars.Add((char)(256 + n++));
            }

            BYTES_TO_UNICODE_CACHE = bytes
                .Zip(chars, (k, v) => new { k, v })
                .ToDictionary(x => x.k, x => x.v);

            return BYTES_TO_UNICODE_CACHE;
        }

        private static string BytePairEncoding(string token)
        {
            if (BPE_CACHE.ContainsKey(token)) return BPE_CACHE[token];

            List<string> word = (from x in token.ToList() select x.ToString()).ToList();
            List<Tuple<string, string>> pairs = GetPairs(word);
            if (pairs.Count == 0)
            {
                BPE_CACHE.TryAdd(token, token);
                return token;
            }

            while (true)
            {
                var minPairs = new SortedDictionary<long, Tuple<string, string>>();
                foreach (Tuple<string, string> pair in pairs)
                {
                    if (GPT3Settings.BpeRanks.ContainsKey(pair))
                    {
                        int rank = GPT3Settings.BpeRanks[pair];
                        minPairs[rank] = pair;
                    }
                    else
                    {
                        minPairs[100000000000] = pair;
                    }
                }

                Tuple<string, string> biGram = minPairs[minPairs.Keys.Min()];
                if (!GPT3Settings.BpeRanks.ContainsKey(biGram)) break;

                string first = biGram.Item1;
                string second = biGram.Item2;

                var newWord = new List<string>();
                int i = 0;

                while (i < word.Count)
                {
                    int j = word.IndexOf(first, i);

                    if (j == -1)
                    {
                        var slice = new ArraySegment<string>((from x in word select x.ToString()).ToArray(), i, word.Count - i);
                        newWord.AddRange(slice);
                        break;
                    }

                    var slice2 = new ArraySegment<string>((from x in word select x.ToString()).ToArray(), i, j - i);
                    newWord.AddRange(slice2);
                    i = j;

                    if (word[i] == first && i < (word.Count - 1) && word[i + 1] == second)
                    {
                        newWord.Add($"{first}{second}");
                        i += 2;
                    }
                    else
                    {
                        newWord.Add(word[i]);
                        i += 1;
                    }
                }

                word = newWord;
                if (word.Count == 1) break;
                pairs = GetPairs(word);
            }

            string result = string.Join(" ", word);
            BPE_CACHE.TryAdd(token, result);
            return result;
        }

        /// <summary>
        /// Return set of symbol pairs in a word.
        /// </summary>
        private static List<Tuple<string, string>> GetPairs(List<string> word)
        {
            var result = new List<Tuple<string, string>>();

            string prevChar = word[0];
            for (int i = 1; i < word.Count; i++)
            {
                string currentChar = word[i];
                result.Add(new Tuple<string, string>(prevChar, currentChar));
                prevChar = currentChar;
            }

            return result;
        }
    }
}

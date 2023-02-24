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
        private static readonly ConcurrentDictionary<string, string> BPE_CACHE = new();
        private static readonly ConcurrentDictionary<int, char> BYTES_TO_UNICODE_CACHE = InitializeBytesToUnicodeCache();
        private static readonly Regex ENCODING_REGEX = new(@"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+", RegexOptions.Compiled);

        public static List<int> Encode(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<int>();

            var byteEncoder = BYTES_TO_UNICODE_CACHE;
            var matches = ENCODING_REGEX.Matches(text);

            var bpeTokens = new List<int>(matches.Count);

            foreach (Match match in matches.Cast<Match>())
            {
                var tokenBytes = Encoding.UTF8.GetBytes(match.Value);
                var token = new string(Array.ConvertAll(tokenBytes, x => byteEncoder[x]));
                var newTokens = BytePairEncoding(token).Split(' ').Select(x => GPT3Settings.Encoder[x]).ToList();
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

        private static ConcurrentDictionary<int, char> InitializeBytesToUnicodeCache()
        {
            var bytes = Enumerable.Range(Ord("!"), Ord("~") + 1 - Ord("!"))
                .Concat(Enumerable.Range(Ord("¡"), Ord("¬") + 1 - Ord("¡")))
                .Concat(Enumerable.Range(Ord("®"), Ord("ÿ") + 1 - Ord("®")))
                .ToList();

            var chars = (from x in bytes select (char)x).ToList();

            var n = 0;
            for (var b = 0; b < 256; b++)
            {
                if (bytes.Contains(b)) continue;
                bytes.Add(b);
                chars.Add((char)(256 + n++));
            }

            return new ConcurrentDictionary<int, char>(bytes
                .Zip(chars, (k, v) => new { k, v })
                .ToDictionary(x => x.k, x => x.v));
        }

        private static string BytePairEncoding(string token)
        {
            if (BPE_CACHE.TryGetValue(token, out var cachedResult)) return cachedResult;

            List<string> word = (from x in token.ToList() select x.ToString()).ToList();
            Dictionary<string, List<string>> pairs = GetPairs(word);
            if (pairs.Count == 0)
            {
                BPE_CACHE.TryAdd(token, token);
                return token;
            }

            while (true)
            {
                var minPairs = new SortedDictionary<long, Tuple<string, string>>();

                foreach (var pair in pairs.SelectMany(pair => pair.Value.Select(pairValue => new Tuple<string, string>(pair.Key, pairValue))))
                {
                    if (GPT3Settings.BpeRanks.TryGetValue(pair, out var rank))
                    {
                        minPairs[rank] = pair;
                    }
                    else
                    {
                        minPairs[100000000000] = pair;
                    }
                }

                var biGram = minPairs[minPairs.Keys.Min()];
                if (!GPT3Settings.BpeRanks.ContainsKey(biGram)) break;

                var first = biGram.Item1;
                var second = biGram.Item2;

                var newWord = new List<string>();
                var i = 0;

                while (i < word.Count)
                {
                    var j = word.IndexOf(first, i);

                    if (j == -1)
                    {
                        var slice = new ArraySegment<string>((from x in word select x.ToString()).ToArray(), i, word.Count - i);
                        newWord.AddRange(slice);
                        break;
                    }

                    var slice2 = new ArraySegment<string>((from x in word select x.ToString()).ToArray(), i, j - i);
                    newWord.AddRange(slice2);
                    i = j;

                    if (word[i] == first && i < word.Count - 1 && word[i + 1] == second)
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

            var result = string.Join(" ", word);
            BPE_CACHE.TryAdd(token, result);
            return result;
        }

        private static Dictionary<string, List<string>> GetPairs(IReadOnlyList<string> word)
        {
            var result = new Dictionary<string, List<string>>();

            var prevChar = word[0];
            for (var i = 1; i < word.Count; i++)
            {
                var currentChar = word[i];
                if (!result.ContainsKey(prevChar))
                {
                    result[prevChar] = new List<string>();
                }

                result[prevChar].Add(currentChar);
                prevChar = currentChar;
            }

            return result;
        }
    }
}

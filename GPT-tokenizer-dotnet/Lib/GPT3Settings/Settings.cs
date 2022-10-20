// @author: Devis Lucato. @license: CC0.

using System.Text.Json;
using System.Collections.Generic;
using System;
using System.Linq;

namespace AI.Dev.OpenAI.GPT.GPT3Settings
{
    internal static class Settings
    {
        internal static Dictionary<string, int> Encoder => ENCODER.Value;
        internal static Dictionary<Tuple<string, string>, int> BpeRanks => BPE_RANKS.Value;

        private static readonly Lazy<Dictionary<string, int>> ENCODER = new Lazy<Dictionary<string, int>>(BuildEncoder);
        private static readonly Lazy<Dictionary<Tuple<string, string>, int>> BPE_RANKS = new Lazy<Dictionary<Tuple<string, string>, int>>(BuildBpeRanks);
        private static readonly string? NAMESPACE = typeof(Settings).Namespace;

        private static Dictionary<Tuple<string, string>, int> BuildBpeRanks()
        {
            string[] lines = EmbeddedResource.Read("vocab.bpe").Split("\n");
            List<Tuple<string, string>> bpeMerges = new ArraySegment<string>(lines, 1, lines.Length - 1)
                .Where(x => x.Trim().Length > 0)
                .Select(x =>
                {
                    string[] y = x.Split(' ');
                    return new Tuple<string, string>(y[0], y[1]);
                }).ToList();
            return DictZip(bpeMerges, Range(0, bpeMerges.Count));
        }

        private static Dictionary<string, int> BuildEncoder()
        {
            string json = EmbeddedResource.Read("encoder.json");
            var encoder = JsonSerializer.Deserialize<Dictionary<string, int>>(json, new JsonSerializerOptions());
            if (encoder == null) throw new NullReferenceException($"[{NAMESPACE}] encoder.json deserialization returned NULL");
            return encoder;
        }

        private static Dictionary<Tuple<string, string>, int> DictZip(List<Tuple<string, string>> x, List<int> y)
        {
            var result = new Dictionary<Tuple<string, string>, int>();
            for (int i = 0; i < x.Count; i++) result.Add(x[i], y[i]);
            return result;
        }

        private static List<int> Range(int x, int y)
        {
            return Enumerable.Range(x, y - x).ToList();
        }
    }
}

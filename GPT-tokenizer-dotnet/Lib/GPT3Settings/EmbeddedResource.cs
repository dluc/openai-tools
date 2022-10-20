// Copyright (c) Devis Lucato. MIT License.

using System;
using System.IO;
using System.Reflection;

namespace AI.Dev.OpenAI.GPT.GPT3Settings
{
    internal static class EmbeddedResource
    {
        private static readonly string? NAMESPACE = typeof(EmbeddedResource).Namespace;

        internal static string Read(string name)
        {
            var assembly = typeof(EmbeddedResource).GetTypeInfo().Assembly;
            if (assembly == null) throw new NullReferenceException($"[{NAMESPACE}] {name} assembly not found");

            using Stream? resource = assembly.GetManifestResourceStream($"{NAMESPACE}." + name);
            if (resource == null) throw new NullReferenceException($"[{NAMESPACE}] {name} resource not found");

            using var reader = new StreamReader(resource);
            return reader.ReadToEnd();
        }
    }
}

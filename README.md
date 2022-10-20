# GPT Tokenizer

## .NET / C#

When using
[OpenAI GPT](https://openai.com/blog/gpt-3-apps/),
you may need to know how many
[tokens](https://help.openai.com/en/articles/4936856-what-are-tokens-and-how-to-count-them)
your code is using for various purposes, such as estimating costs and improving
results. 

The `GPT3Tokenizer` C# class can help you **count tokens** in your prompts and
in the responses received.

```csharp
using AI.Dev.OpenAI.GPT;

string text = "January 1st, 2000";

// 5 tokens => [21339, 352, 301, 11, 4751]
List<int> tokens = GPT3Tokenizer.Encode(text);
```

The tokenizer uses a byte-pair encoding (BPE) algorithm to split words into
subwords based on frequency and merges rules. It can handle out-of-vocabulary
words, punctuation, and special tokens.

The result of this library is compatible with OpenAI GPT tokenizer that you can
also test
[here](https://beta.openai.com/tokenizer).

### Installation

Install [AI.Dev.OpenAI.GPT](https://www.nuget.org/packages/AI.Dev.OpenAI.GPT) NuGet package from nuget.org, e.g.:

    dotnet add package AI.Dev.OpenAI.GPT --version 1.0.2

or

    NuGet\Install-Package AI.Dev.OpenAI.GPT -Version 1.0.2

## Python and Node.js

If you are looking for an equivalent solution in other languages:

* [Python GPT tokenizer](https://huggingface.co/docs/transformers/model_doc/gpt2#transformers.GPT2Tokenizer)
* [Node.js GPT encoder](https://www.npmjs.com/package/gpt-3-encoder)

# Licensing

This library is licensed CC0, in the public domain. You can use it for any
application, you can modify the code, and you can redistribute any part of it.

I am not affiliated with OpenAI and this library is not endorsed by them. I just
work with several AI solutions and I share this code hoping to make technology
more accessible and easier to work with.

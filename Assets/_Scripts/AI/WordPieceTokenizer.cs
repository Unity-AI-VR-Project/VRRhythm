using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEngine;

public class WordPieceTokenizer
{
    public Dictionary<string, int> Vocab { get; private set; }
    public Dictionary<int, string> IdsToTokens { get; private set; }

    public string ClsToken { get; private set; }
    public string SepToken { get; private set; }
    public string UnkToken { get; private set; }
    public string PadToken { get; private set; }
    public string MaskToken { get; private set; }

    public int ClsTokenId { get; private set; }
    public int SepTokenId { get; private set; }
    public int UnkTokenId { get; private set; }
    public int PadTokenId { get; private set; }
    public int MaskTokenId { get; private set; }

    private bool doLowerCase;
    private bool stripAccents;
    private bool cleanText;

    public WordPieceTokenizer(string vocabFileName, bool doLowerCase = false, bool stripAccents = false, bool cleanText = true)
    {
        Vocab = LoadVocab(vocabFileName);
        IdsToTokens = Vocab.ToDictionary(pair => pair.Value, pair => pair.Key);

        ClsToken = "[CLS]";
        SepToken = "[SEP]";
        UnkToken = "[UNK]";
        PadToken = "[PAD]";
        MaskToken = "[MASK]";

        ClsTokenId = Vocab.GetValueOrDefault(ClsToken, -1);
        SepTokenId = Vocab.GetValueOrDefault(SepToken, -1);
        UnkTokenId = Vocab.GetValueOrDefault(UnkToken, -1);
        PadTokenId = Vocab.GetValueOrDefault(PadToken, -1);
        MaskTokenId = Vocab.GetValueOrDefault(MaskToken, -1);

        this.doLowerCase = doLowerCase;
        this.stripAccents = stripAccents;
        this.cleanText = cleanText;

        if (UnkTokenId == -1)
        {
            UnityEngine.Debug.LogWarning($"'{UnkToken}' ÅäÅ«ÀÌ vocab¿¡ ¾ø½À´Ï´Ù. UNK ÅäÅ« Ã³¸®¿¡ ¹®Á¦°¡ ¹ß»ýÇÒ ¼ö ÀÖ½À´Ï´Ù.");
        }
        if (ClsTokenId == -1)
        {
            UnityEngine.Debug.LogWarning($"'{ClsToken}' ÅäÅ«ÀÌ vocab¿¡ ¾ø½À´Ï´Ù.");
        }
        if (SepTokenId == -1)
        {
            UnityEngine.Debug.LogWarning($"'{SepToken}' ÅäÅ«ÀÌ vocab¿¡ ¾ø½À´Ï´Ù.");
        }
        if (PadTokenId == -1)
        {
            UnityEngine.Debug.LogWarning($"'{PadToken}' ÅäÅ«ÀÌ vocab¿¡ ¾ø½À´Ï´Ù.");
        }
        if (MaskTokenId == -1)
        {
            UnityEngine.Debug.LogWarning($"'{MaskToken}' ÅäÅ«ÀÌ vocab¿¡ ¾ø½À´Ï´Ù.");
        }
    }

    private Dictionary<string, int> LoadVocab(string vocabFileName)
    {
        var vocab = new Dictionary<string, int>();
        string filePath = Path.Combine(Application.streamingAssetsPath, vocabFileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"¾îÈÖ ÆÄÀÏÀ» Ã£À» ¼ö ¾ø½À´Ï´Ù: {filePath}");
            return vocab;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            for (int idx = 0; idx < lines.Length; idx++)
            {
                string token = lines[idx].Trim();
                if (!string.IsNullOrEmpty(token))
                {
                    vocab[token] = idx;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"¾îÈÖ ÆÄÀÏÀ» ·ÎµåÇÏ´Â Áß ¿À·ù ¹ß»ý: {e.Message}");
            throw;
        }
        return vocab;
    }

    private string CleanText(string text)
    {
        text = text ?? "";
        text = Regex.Replace(text, @"http\S+|www\.\S+", "");
        text = Regex.Replace(text, @"\S*@\S*\s?", "");
        text = Regex.Replace(text, @"<[^>]*>", "");

        string target = @"[^°¡-ÆR0-9a-zA-Z.,!?'"" ]";
        text = Regex.Replace(text, target, " ");

        text = Regex.Replace(text, @"([.,!?""'])\1{1,}", "$1");
        text = Regex.Replace(text, @"([¤¡-¤¾¤¿-¤Ó])\1+", "$1");
        text = Regex.Replace(text, @"\d+", "");
        text = Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }

    private bool IsControl(char ch)
    {
        UnicodeCategory category = Char.GetUnicodeCategory(ch);
        return category == UnicodeCategory.Control || category == UnicodeCategory.Format;
    }

    private bool IsPunctuation(char ch)
    {
        int cp = ch;
        if ((cp >= 0x4E00 && cp <= 0x9FFF) ||
            (cp >= 0x3040 && cp <= 0x309F) ||
            (cp >= 0x30A0 && cp <= 0x30FF) ||
            (cp >= 0xAC00 && cp <= 0xD7A3))
        {
            return false;
        }

        if ((ch >= '!' && ch <= '/') ||
            (ch >= ':' && ch <= '@') ||
            (ch >= '[' && ch <= '`') ||
            (ch >= '{' && ch <= '~') ||
            (ch == '¡¢') || (ch == '¡£') || (ch == '¡¶') || (ch == '¡·') ||
            (ch == '¡¸') || (ch == '¡¹') || (ch == '¡º') || (ch == '¡»') ||
            (ch == '¡¼') || (ch == '¡½') || (ch == '¡²') || (ch == '¡³') ||
            (ch == '¡ª') || (ch == '¡¦') || (ch == '?') || (ch == '?'))
        {
            return true;
        }

        UnicodeCategory category = Char.GetUnicodeCategory(ch);
        return category.ToString().StartsWith("P");
    }

    private bool IsWhitespace(char ch)
    {
        return Char.IsWhiteSpace(ch) || ch == 0x3000;
    }

    private List<string> WordpieceSplit(string token)
    {
        List<string> outputTokens = new List<string>();
        if (UnkTokenId == -1)
        {
            outputTokens.Add(UnkToken);
            return outputTokens;
        }

        if (string.IsNullOrEmpty(token))
        {
            return outputTokens;
        }

        int start = 0;
        while (start < token.Length)
        {
            int end = token.Length;
            string currentSubstr = null;
            bool found = false;
            while (start < end)
            {
                string sub = token.Substring(start, end - start);
                if (start > 0)
                {
                    sub = "##" + sub;
                }

                if (Vocab.ContainsKey(sub))
                {
                    currentSubstr = sub;
                    found = true;
                    break;
                }
                end--;
            }

            if (!found)
            {
                outputTokens.Add(UnkToken);
                break;
            }

            outputTokens.Add(currentSubstr);
            start = end;
        }
        return outputTokens;
    }

    public int GetVocabSize()
    {
        return Vocab.Count;
    }

    public List<string> Tokenize(string text)
    {
        if (cleanText)
        {
            text = CleanText(text);
        }
        if (doLowerCase)
        {
            text = text.ToLower();
        }

        List<string> basicTokens = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        List<string> tokens = new List<string>();
        foreach (string basicToken in basicTokens)
        {
            string currentToken = basicToken;
            if (stripAccents)
            {
                currentToken = RemoveAccents(currentToken);
            }
            tokens.AddRange(WordpieceSplit(currentToken));
        }
        return tokens;
    }

    private string RemoveAccents(string text)
    {
        text = text.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();
        foreach (char ch in text)
        {
            UnicodeCategory uc = Char.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public Dictionary<string, List<int>> Encode(string text, int maxLength, bool addSpecialTokens = true, bool padding = true, bool truncation = true)
    {
        List<string> tokens = Tokenize(text);

        List<string> finalTokens = new List<string>();
        if (addSpecialTokens)
        {
            finalTokens.Add(ClsToken);
        }
        finalTokens.AddRange(tokens);
        if (addSpecialTokens)
        {
            finalTokens.Add(SepToken);
        }

        if (truncation && finalTokens.Count > maxLength)
        {
            if (addSpecialTokens)
            {
                finalTokens = finalTokens.Take(maxLength - 1).ToList();
                finalTokens.Add(SepToken);
            }
            else
            {
                finalTokens = finalTokens.Take(maxLength).ToList();
            }
        }

        List<int> inputIds = finalTokens.Select(token => Vocab.GetValueOrDefault(token, UnkTokenId)).ToList();
        List<int> attentionMask = Enumerable.Repeat(1, inputIds.Count).ToList();
        List<int> tokenTypeIds = Enumerable.Repeat(0, inputIds.Count).ToList();

        if (padding)
        {
            int padCount = maxLength - inputIds.Count;
            if (padCount > 0)
            {
                inputIds.AddRange(Enumerable.Repeat(PadTokenId, padCount));
                attentionMask.AddRange(Enumerable.Repeat(0, padCount));
                tokenTypeIds.AddRange(Enumerable.Repeat(0, padCount));
            }
        }

        if (inputIds.Count != maxLength)
        {
            UnityEngine.Debug.LogWarning($"°æ°í: ÃÖÁ¾ ÀÔ·Â ±æÀÌ({inputIds.Count})°¡ ¿¹»ó ÃÖ´ë ±æÀÌ({maxLength})¿Í ÀÏÄ¡ÇÏÁö ¾Ê½À´Ï´Ù. Àß¸²/ÆÐµù ·ÎÁ÷À» È®ÀÎÇÏ¼¼¿ä.");
        }

        return new Dictionary<string, List<int>>
        {
            { "input_ids", inputIds },
            { "attention_mask", attentionMask },
            { "token_type_ids", tokenTypeIds }
        };
    }

    public string Decode(List<int> inputIds)
    {
        List<string> tokens = new List<string>();
        foreach (int id in inputIds)
        {
            if (IdsToTokens.TryGetValue(id, out string token))
            {
                if (token == ClsToken)
                {
                    tokens.Add("[CLS]");
                }
                else if (token == SepToken)
                {
                    tokens.Add("[SEP]");
                }
                else if (token == PadToken)
                {
                    tokens.Add("[PAD]");
                }
                else if (token == UnkToken)
                {
                    tokens.Add("[UNK]");
                }
                else if (token == MaskToken)
                {
                    tokens.Add("[MASK]");
                }
                else if (token.StartsWith("##"))
                {
                    tokens.Add(token.Substring(2));
                }
                else
                {
                    tokens.Add(" " + token);
                }
            }
            else
            {
                tokens.Add("[UNK]");
            }
        }
        return string.Join("", tokens).Trim();
    }
}
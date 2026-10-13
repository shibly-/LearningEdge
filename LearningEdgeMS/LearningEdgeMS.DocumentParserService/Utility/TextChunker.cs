using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdgeMS.DocumentParserService.Utility;
public static class TextChunker
{
    public static IEnumerable<string> Split(string text, int maxLength)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var word in words)
        {
            if (sb.Length + word.Length + 1 > maxLength)
            {
                yield return sb.ToString();
                sb.Clear();
            }
            sb.Append(word).Append(' ');
        }
        if (sb.Length > 0)
            yield return sb.ToString();
    }
}

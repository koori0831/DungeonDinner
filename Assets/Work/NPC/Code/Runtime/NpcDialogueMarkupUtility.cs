using System;
using System.Collections.Generic;
using System.Text;

namespace Work.NPC.Code.Runtime
{
    public sealed class NpcDialogueMarkupResult
    {
        public NpcDialogueMarkupResult(string rawText, string richText, IReadOnlyList<string> boldSegments)
        {
            RawText = rawText ?? string.Empty;
            RichText = richText ?? string.Empty;
            BoldSegments = boldSegments ?? Array.Empty<string>();
        }

        public string RawText { get; }
        public string RichText { get; }
        public IReadOnlyList<string> BoldSegments { get; }
    }

    public static class NpcDialogueMarkupUtility
    {
        private const string BoldMarker = "**";

        public static NpcDialogueMarkupResult Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new NpcDialogueMarkupResult(string.Empty, string.Empty, Array.Empty<string>());

            StringBuilder richText = new StringBuilder(text.Length);
            List<string> boldSegments = new List<string>();
            int index = 0;

            while (index < text.Length)
            {
                int start = text.IndexOf(BoldMarker, index, StringComparison.Ordinal);
                if (start < 0)
                {
                    richText.Append(text, index, text.Length - index);
                    break;
                }

                int end = text.IndexOf(BoldMarker, start + BoldMarker.Length, StringComparison.Ordinal);
                if (end < 0)
                {
                    richText.Append(text, index, text.Length - index);
                    break;
                }

                richText.Append(text, index, start - index);

                string segment = text.Substring(start + BoldMarker.Length, end - start - BoldMarker.Length);
                if (string.IsNullOrWhiteSpace(segment))
                {
                    richText.Append(text, start, end + BoldMarker.Length - start);
                }
                else
                {
                    richText.Append("<b>");
                    richText.Append(segment);
                    richText.Append("</b>");
                    boldSegments.Add(segment.Trim());
                }

                index = end + BoldMarker.Length;
            }

            return new NpcDialogueMarkupResult(text, richText.ToString(), boldSegments);
        }
    }
}

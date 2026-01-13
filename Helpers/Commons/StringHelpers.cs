using System.Text.RegularExpressions;

namespace Elder.Helpers.Commons
{
    public static class StringHelpers
    {
        public static bool ContainsHtmlTag(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var tagPattern = new Regex(@"<[^>]+>");
            return tagPattern.IsMatch(input);
        }
        public static string ExtractHtmlTagContent(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // HTML 태그를 제거하고 태그 안에 있는 내용만 추출
            var matches = Regex.Matches(input, @"<.*?>(.*?)</.*?>");
            string extractedContent = string.Empty;

            foreach (Match match in matches)
            {
                // 태그 내의 내용만 가져오기
                extractedContent += match.Groups[1].Value + " ";
            }

            return extractedContent.Trim();
        }
        public static bool ContainsText(string input, string keyword)
        {
            return !string.IsNullOrEmpty(input)
                   && !string.IsNullOrEmpty(keyword)
                   && input.Contains(keyword);
        }
    }
}

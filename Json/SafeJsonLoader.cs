using System.Text.RegularExpressions;

namespace Vrm.Json
{
    public static class SafeJsonLoader
    {
        public static string FixSimpleJson(string input)
        {
            // 1. Replace string "true"/"false" with boolean values true/false without quotes
            // To avoid breaking string fields containing "true" inside, use regex with the key

            // Example: "hadReferenceIssues" : "true" -> "hadReferenceIssues" : true
            // Assume the value is in quotes and the word true/false (regex)

            // Regex for a key with a boolean value in a string
            var boolStringPattern = new Regex(
                // Capturing the key in quotes: "key"
                @"(""[^""]+""\s*:\s*)""(true|false)""",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            string fixedBoolJson = boolStringPattern.Replace(input, m =>
            {
                string keyPart = m.Groups[1].Value; // "hadReferenceIssues":
                string boolValue = m.Groups[2].Value.ToLower(); // true or false
                return $"{keyPart}{boolValue}";
            });

            // 2. Fixing unclosed brackets – adding missing } at the end
            // Let's count how many { and } are in the document
            int openBraces = 0, closeBraces = 0;
            foreach (char c in fixedBoolJson)
            {
                if (c == '{') openBraces++;
                else if (c == '}') closeBraces++;
            }

            int missingBraces = openBraces - closeBraces;
            if (missingBraces > 0)
            {
                fixedBoolJson += new string('}', missingBraces);
            }

            return fixedBoolJson;
        }
    }
}

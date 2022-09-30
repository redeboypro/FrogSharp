using System;

namespace FrogSharp.Tools
{
    public class StringTools
    {
        public static string GetBetween(string left, string right, string all)
        {
            var from = all.IndexOf(left, StringComparison.Ordinal) + left.Length;
            var to = all.LastIndexOf(right, StringComparison.Ordinal);

            return all.Substring(from, to - from);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sayama.MaterialCreatorWindow.Editor
{
	public static class RegexHelper
	{
		public static string[] GetMatches(this RegexSearchMode mode, string input, string pattern, RegexOptions regexOptions)
		{
			switch (mode)
			{
				case RegexSearchMode.Replace:
				{
					string separator = "__-_-_|_-_-__";
					var alt = Regex.Replace(input, pattern, $"$1{separator}$2{separator}$3");
					return alt.Split(separator);
				}
				case RegexSearchMode.Split:
				{
					return Regex.Split(input, pattern, regexOptions);
				}
				case RegexSearchMode.Match:
				{
					var matches = Regex.Matches(input, pattern, regexOptions);
					string[] results = new string[matches.Count];
					for (int i = 0; i < matches.Count; i++)
					{
						results[i] = matches[i].Value;
					}
					return results;
				}
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
				}
			}
		}
	}
	
	public enum RegexSearchMode
	{
		Replace,
		Split,
		Match
	}
}
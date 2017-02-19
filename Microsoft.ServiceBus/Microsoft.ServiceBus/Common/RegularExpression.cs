using System;
using System.Globalization;

namespace Microsoft.ServiceBus.Common
{
	internal static class RegularExpression
	{
		public static class Alteration
		{
			public static string Patterns(params string[] patterns)
			{
				return string.Concat("(", string.Join("|", patterns), ")");
			}
		}

		public static class Anchor
		{
			public const string StartOfLine = "^";

			public const string EndOfLine = "$";
		}

		public static class CharacterClass
		{
			public const string Any = ".";

			public const string WordCharacter = "\\w";

			public const string NonWordCharacter = "\\W";

			public const string WhiteSpaceCharacter = "\\s";

			public const string NonWhiteSpaceCharacter = "\\S";

			public const string DecimalDigitCharacter = "\\d";

			public const string NonDecimalDigitCharacter = "\\D";

			public static string NegativeCharacterGroup(string expression)
			{
				return string.Concat("[^", expression, "]");
			}

			public static string NegativeUnicodeCategoryOrBlock(string name)
			{
				return string.Concat("\\P{", name, "}");
			}

			public static string PositiveCharacterGroup(string expression)
			{
				return string.Concat("[", expression, "]");
			}

			public static string UnicodeCategoryOrBlock(string name)
			{
				return string.Concat("\\p{", name, "}");
			}
		}

		public static class Group
		{
			public static string MatchedSubexpression(string expression)
			{
				return string.Concat("(", expression, ")");
			}

			public static string NamedMatchedSubexpression(string name, string expression)
			{
				string[] strArrays = new string[] { "(?<", name, ">", expression, ")" };
				return string.Concat(strArrays);
			}

			public static string NonCapturingGroup(string expression, string quantifier)
			{
				return string.Concat("(?:", expression, ")", quantifier);
			}
		}

		public static class Quantifier
		{
			public const string ZeroOrMore = "*";

			public const string OneOrMore = "+";

			public const string ZeroOrOne = "?";

			public static string AtLeast(int count)
			{
				return string.Concat("{", count.ToString(NumberFormatInfo.InvariantInfo), ",}");
			}

			public static string Exactly(int count)
			{
				return string.Concat("{", count.ToString(NumberFormatInfo.InvariantInfo), "}");
			}

			public static string Range(int min, int max)
			{
				string[] str = new string[] { "{", min.ToString(NumberFormatInfo.InvariantInfo), ",", max.ToString(NumberFormatInfo.InvariantInfo), "}" };
				return string.Concat(str);
			}
		}
	}
}
using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class ExpressionEvaluator
	{
		public const string InternalBodyProperty = "READ_ONCE_BODY_PUSHNOTIFICATION";

		public const string InternalJsonNavigationProperty = "FIRST_LEVEL_JSON_NAVIGATION_PUSHNOTIFICATION";

		private const string BodyExpression = "$body";

		private const int MaxLengthOfPropertyName = 120;

		private readonly static Regex PropertyNameRegEx;

		static ExpressionEvaluator()
		{
			ExpressionEvaluator.PropertyNameRegEx = new Regex("^[A-Za-z0-9_]+$");
		}

		public static string Evaluate(string expression, BrokeredMessage message)
		{
			ExpressionEvaluator.ExpressionType expressionType;
			return ExpressionEvaluator.Evaluate(expression, message, out expressionType);
		}

		public static string Evaluate(string expression, BrokeredMessage message, out ExpressionEvaluator.ExpressionType expressionType)
		{
			expressionType = ExpressionEvaluator.ExpressionType.None;
			if (expression == null)
			{
				return expression;
			}
			List<ExpressionEvaluator.Token> tokens = ExpressionEvaluator.ValidateAndTokenize(expression, out expressionType);
			if ((int)expressionType == 0)
			{
				return expression;
			}
			return ExpressionEvaluator.Evaluate(ExpressionEvaluator.ExtractValuesFromMessage(tokens, message));
		}

		private static string Evaluate(List<string> values)
		{
			if (values.Count == 1)
			{
				return values[0];
			}
			int num = 0;
			values.ForEach((string s) => num = num + s.Length);
			StringBuilder stringBuilder = new StringBuilder(num);
			values.ForEach((string s) => stringBuilder.Append(s));
			return stringBuilder.ToString();
		}

		private static int ExtractLiteral(string fullExpression, string tokenBegin, ExpressionEvaluator.Token token)
		{
			int num = 0;
			int num1 = 1;
			char chr = (token.Type == ExpressionEvaluator.TokenType.SingleLiteral ? '\'' : '\"');
			bool flag = false;
			while (true)
			{
				num = tokenBegin.IndexOf(chr, num1);
				if (num == -1)
				{
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					object[] objArray = new object[] { fullExpression, tokenBegin };
					throw new InvalidDataContractException(string.Format(invariantCulture, "Expression is {0}. Literal is missing closing notation. Current invalid token is {1}", objArray));
				}
				if (num + 1 >= tokenBegin.Length || tokenBegin[num + 1] != chr)
				{
					break;
				}
				flag = true;
				num1 = num + 2;
			}
			string str = tokenBegin.Substring(1, num - 1);
			if (!flag)
			{
				token.Property = str;
			}
			else
			{
				token.Property = (token.Type == ExpressionEvaluator.TokenType.DoubleLiteral ? str.Replace("\"\"", "\"") : str.Replace("''", "'"));
			}
			return num;
		}

		private static int ExtractToken(string fullExpression, string tokenBegin, ExpressionEvaluator.Token token)
		{
			if (tokenBegin[1] != '(')
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { fullExpression, tokenBegin };
				throw new InvalidDataContractException(string.Format(invariantCulture, "Expression is {0}. The character following $ or . or % should be a ( The current invalid token is {1}", objArray));
			}
			int num = tokenBegin.IndexOf(',');
			int num1 = tokenBegin.IndexOf(')');
			if (num1 == -1)
			{
				CultureInfo cultureInfo = CultureInfo.InvariantCulture;
				object[] objArray1 = new object[] { fullExpression, tokenBegin };
				throw new InvalidDataContractException(string.Format(cultureInfo, "Expression is {0}. Missing closing parentheses. Current invalid token is {1}", objArray1));
			}
			if ((token.Type == ExpressionEvaluator.TokenType.Percentage || token.Type == ExpressionEvaluator.TokenType.Hash) && num != -1 && num < num1)
			{
				CultureInfo invariantCulture1 = CultureInfo.InvariantCulture;
				object[] objArray2 = new object[] { fullExpression, tokenBegin };
				throw new InvalidDataContractException(string.Format(invariantCulture1, "Expression is {0}. The expression %(prop , n) is not a valid. The current invalid token is {1}", objArray2));
			}
			if (token.Type == ExpressionEvaluator.TokenType.Dot && num == -1)
			{
				CultureInfo cultureInfo1 = CultureInfo.InvariantCulture;
				object[] objArray3 = new object[] { fullExpression, tokenBegin };
				throw new InvalidDataContractException(string.Format(cultureInfo1, "Expression is {0}. Error trying to parse for .(prop , n) format. The current invalid token is {1}", objArray3));
			}
			int num2 = 0;
			if (num == -1 || num >= num1)
			{
				token.Property = tokenBegin.Substring(2, num1 - 2);
			}
			else
			{
				string str = tokenBegin.Substring(num + 1, num1 - num - 1);
				if (!int.TryParse(str, out num2) || num2 < 0)
				{
					CultureInfo invariantCulture2 = CultureInfo.InvariantCulture;
					object[] objArray4 = new object[] { fullExpression, tokenBegin, str };
					throw new InvalidDataContractException(string.Format(invariantCulture2, "Expression is {0}. {2} is not a positive integer. The current invalid token is {1}", objArray4));
				}
				if (num2 == 0)
				{
					token.EmptyString = true;
				}
				token.Length = num2;
				token.Property = tokenBegin.Substring(2, num - 2);
			}
			token.Property = token.Property.Trim();
			return num1;
		}

		private static List<string> ExtractValuesFromMessage(List<ExpressionEvaluator.Token> tokens, BrokeredMessage message)
		{
			object obj;
			object obj1;
			List<string> strs = new List<string>();
			IDictionary<string, string> strs1 = ExpressionEvaluator.ReadFirstLevelJsonProperties(message);
			foreach (ExpressionEvaluator.Token token in tokens)
			{
				if (token.Type == ExpressionEvaluator.TokenType.Body)
				{
					strs.Add(ExpressionEvaluator.ReadBody(message));
				}
				else if (token.Type == ExpressionEvaluator.TokenType.SingleLiteral || token.Type == ExpressionEvaluator.TokenType.DoubleLiteral)
				{
					strs.Add(token.Property);
				}
				else if (token.EmptyString || token.Length != 0)
				{
					string empty = string.Empty;
					if (token.Property == "$body")
					{
						empty = ExpressionEvaluator.ReadBody(message);
					}
					else if (!message.Properties.TryGetValue(token.Property, out obj))
					{
						strs1.TryGetValue(token.Property, out empty);
					}
					else
					{
						empty = obj.ToString();
					}
					if (string.IsNullOrEmpty(empty))
					{
						continue;
					}
					if (token.Length >= empty.Length || token.Type != ExpressionEvaluator.TokenType.Dollar && empty.Length <= token.Length + 3)
					{
						strs.Add(empty);
					}
					else if (token.Type != ExpressionEvaluator.TokenType.Dollar)
					{
						strs.Add(string.Concat(empty.Substring(0, token.Length), "..."));
					}
					else
					{
						strs.Add(empty.Substring(0, token.Length));
					}
				}
				else
				{
					string str = string.Empty;
					if (string.Equals(token.Property, "$body", StringComparison.OrdinalIgnoreCase))
					{
						str = ExpressionEvaluator.ReadBody(message);
					}
					else if (!message.Properties.TryGetValue(token.Property, out obj1))
					{
						strs1.TryGetValue(token.Property, out str);
					}
					else
					{
						str = obj1.ToString();
					}
					if (string.IsNullOrEmpty(str))
					{
						continue;
					}
					if (token.Type != ExpressionEvaluator.TokenType.Percentage)
					{
						strs.Add(str);
					}
					else
					{
						strs.Add(Uri.EscapeDataString(str));
					}
				}
			}
			return strs;
		}

		private static ExpressionEvaluator.ExpressionType PeekExpressionType(string expression)
		{
			if (string.IsNullOrWhiteSpace(expression))
			{
				return ExpressionEvaluator.ExpressionType.Literal;
			}
			char chr = expression[0];
			switch (chr)
			{
				case '#':
				{
					return ExpressionEvaluator.ExpressionType.Numeric;
				}
				case '$':
				case '%':
				{
					return ExpressionEvaluator.ExpressionType.String;
				}
				default:
				{
					if (chr == '.')
					{
						return ExpressionEvaluator.ExpressionType.String;
					}
					if (chr == '{')
					{
						return ExpressionEvaluator.ExpressionType.Composite;
					}
					return ExpressionEvaluator.ExpressionType.Literal;
				}
			}
		}

		public static string ReadBody(BrokeredMessage message)
		{
			return message.Properties["READ_ONCE_BODY_PUSHNOTIFICATION"] as string;
		}

		internal static IDictionary<string, string> ReadFirstLevelJsonProperties(BrokeredMessage message)
		{
			IDictionary<string, string> strs;
			object obj;
			if (!message.Properties.TryGetValue("FIRST_LEVEL_JSON_NAVIGATION_PUSHNOTIFICATION", out obj))
			{
				strs = new Dictionary<string, string>();
			}
			else
			{
				strs = (IDictionary<string, string>)obj;
			}
			return strs;
		}

		public static ExpressionEvaluator.ExpressionType Validate(string expression, ApiVersion version)
		{
			ExpressionEvaluator.ExpressionType expressionType;
			List<ExpressionEvaluator.Token> tokens = ExpressionEvaluator.ValidateAndTokenize(expression, out expressionType);
			if (version > ApiVersion.Three)
			{
				if (tokens.Find((ExpressionEvaluator.Token t) => t.Type == ExpressionEvaluator.TokenType.Body) != null)
				{
					throw new InvalidDataContractException(SRClient.BodyIsNotSupportedExpression);
				}
			}
			return expressionType;
		}

		private static List<ExpressionEvaluator.Token> ValidateAndTokenize(string expression, out ExpressionEvaluator.ExpressionType expressionType)
		{
			int length;
			object[] objArray;
			CultureInfo invariantCulture;
			expressionType = ExpressionEvaluator.PeekExpressionType(expression);
			if ((int)expressionType == 0)
			{
				return new List<ExpressionEvaluator.Token>();
			}
			List<ExpressionEvaluator.Token> tokens = new List<ExpressionEvaluator.Token>();
			string str = expression;
			if ((int)expressionType == 3)
			{
				if (expression[expression.Length - 1] != '}')
				{
					CultureInfo cultureInfo = CultureInfo.InvariantCulture;
					object[] objArray1 = new object[] { expression };
					throw new InvalidDataContractException(string.Format(cultureInfo, "Expression is {0}. Error is closing parenthesis is missing", objArray1));
				}
				str = expression.Substring(1, expression.Length - 2).TrimEnd(new char[0]);
			}
			while (true)
			{
				ExpressionEvaluator.Token token1 = new ExpressionEvaluator.Token();
				str = str.TrimStart(new char[0]);
				if (str.Length < 3)
				{
					CultureInfo invariantCulture1 = CultureInfo.InvariantCulture;
					object[] objArray2 = new object[] { expression, str };
					throw new InvalidDataContractException(string.Format(invariantCulture1, "Expression is {0}. Error is it contains an invalid token : {1}", objArray2));
				}
				char chr = str[0];
				switch (chr)
				{
					case '\"':
					{
						token1.Type = ExpressionEvaluator.TokenType.DoubleLiteral;
						length = ExpressionEvaluator.ExtractLiteral(expression, str, token1);
						break;
					}
					case '#':
					{
						token1.Type = ExpressionEvaluator.TokenType.Hash;
						length = ExpressionEvaluator.ExtractToken(expression, str, token1);
						break;
					}
					case '$':
					{
						if (!str.ToLowerInvariant().StartsWith("$body", StringComparison.OrdinalIgnoreCase))
						{
							token1.Type = ExpressionEvaluator.TokenType.Dollar;
							length = ExpressionEvaluator.ExtractToken(expression, str, token1);
							break;
						}
						else
						{
							length = "$body".Length - 1;
							token1.Type = ExpressionEvaluator.TokenType.Body;
							break;
						}
					}
					case '%':
					{
						token1.Type = ExpressionEvaluator.TokenType.Percentage;
						length = ExpressionEvaluator.ExtractToken(expression, str, token1);
						break;
					}
					case '&':
					{
						invariantCulture = CultureInfo.InvariantCulture;
						objArray = new object[] { expression, str };
						throw new InvalidDataContractException(string.Format(invariantCulture, "Expression is {0}. Token has to begin with one of these characters . % ' \" $. Current invalid token is {1}", objArray));
					}
					case '\'':
					{
						token1.Type = ExpressionEvaluator.TokenType.SingleLiteral;
						length = ExpressionEvaluator.ExtractLiteral(expression, str, token1);
						break;
					}
					default:
					{
						if (chr != '.')
						{
							invariantCulture = CultureInfo.InvariantCulture;
							objArray = new object[] { expression, str };
							throw new InvalidDataContractException(string.Format(invariantCulture, "Expression is {0}. Token has to begin with one of these characters . % ' \" $. Current invalid token is {1}", objArray));
						}
						token1.Type = ExpressionEvaluator.TokenType.Dot;
						length = ExpressionEvaluator.ExtractToken(expression, str, token1);
						break;
					}
				}
				if (token1.Type != ExpressionEvaluator.TokenType.SingleLiteral && token1.Type != ExpressionEvaluator.TokenType.DoubleLiteral && token1.Type != ExpressionEvaluator.TokenType.Body && !string.Equals(token1.Property, "$body", StringComparison.OrdinalIgnoreCase))
				{
					if (!ExpressionEvaluator.PropertyNameRegEx.IsMatch(token1.Property))
					{
						CultureInfo cultureInfo1 = CultureInfo.InvariantCulture;
						object[] property = new object[] { token1.Property };
						throw new InvalidDataContractException(string.Format(cultureInfo1, "Property name is {0}. Only ASCII-7 alphanumeric characters and '_' are permitted in the property name", property));
					}
					if (token1.Property.Length > 120)
					{
						CultureInfo invariantCulture2 = CultureInfo.InvariantCulture;
						object[] length1 = new object[] { token1.Property.Length, 120 };
						throw new InvalidDataContractException(string.Format(invariantCulture2, "Property name is of length. {0}. Maximum allowed length is {1}", length1));
					}
				}
				tokens.Add(token1);
				if (str.Length == length + 1)
				{
					if (tokens.Count > 1)
					{
						if (tokens.Find((ExpressionEvaluator.Token token) => token.Type == ExpressionEvaluator.TokenType.Hash) != null)
						{
							throw new InvalidDataContractException(string.Format(CultureInfo.InvariantCulture, "Token type '#(prop)' is not allowed in composite expression.", new object[0]));
						}
					}
					return tokens;
				}
				str = str.Substring(length + 1).TrimStart(new char[0]);
				if (str[0] != '+')
				{
					CultureInfo cultureInfo2 = CultureInfo.InvariantCulture;
					object[] objArray3 = new object[] { expression, str };
					throw new InvalidDataContractException(string.Format(cultureInfo2, "Expression is {0}. Only valid composition operator is +. Current invalid token is : {1}", objArray3));
				}
				str = str.Substring(1);
			}
			invariantCulture = CultureInfo.InvariantCulture;
			objArray = new object[] { expression, str };
			throw new InvalidDataContractException(string.Format(invariantCulture, "Expression is {0}. Token has to begin with one of these characters . % ' \" $. Current invalid token is {1}", objArray));
		}

		public enum ExpressionType
		{
			Literal,
			Numeric,
			String,
			Composite,
			None
		}

		private class Token
		{
			public bool EmptyString
			{
				get;
				set;
			}

			public int Length
			{
				get;
				set;
			}

			public string Property
			{
				get;
				set;
			}

			public ExpressionEvaluator.TokenType Type
			{
				get;
				set;
			}

			public Token()
			{
			}
		}

		private enum TokenType
		{
			None,
			Dollar,
			Hash,
			Dot,
			Percentage,
			SingleLiteral,
			DoubleLiteral,
			Body
		}
	}
}
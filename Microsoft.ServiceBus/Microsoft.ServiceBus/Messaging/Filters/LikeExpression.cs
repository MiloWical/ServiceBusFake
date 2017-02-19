using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal sealed class LikeExpression : System.Linq.Expressions.Expression
	{
		private readonly static MethodInfo LikeMethodInfo;

		public override bool CanReduce
		{
			get
			{
				return true;
			}
		}

		public System.Linq.Expressions.Expression Escape
		{
			get;
			private set;
		}

		public System.Linq.Expressions.Expression Expression
		{
			get;
			private set;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return ExpressionType.Extension;
			}
		}

		public System.Linq.Expressions.Expression Pattern
		{
			get;
			private set;
		}

		public override System.Type Type
		{
			get
			{
				return typeof(bool?);
			}
		}

		static LikeExpression()
		{
			LikeExpression.LikeMethodInfo = typeof(LikeExpression).GetMethod("LikeMethod", BindingFlags.Static | BindingFlags.NonPublic);
		}

		public LikeExpression(System.Linq.Expressions.Expression expression, System.Linq.Expressions.Expression pattern, System.Linq.Expressions.Expression escape)
		{
			if (expression == null)
			{
				throw FxTrace.Exception.ArgumentNull("expression");
			}
			if (pattern == null)
			{
				throw FxTrace.Exception.ArgumentNull("pattern");
			}
			this.Expression = expression;
			this.Pattern = pattern;
			this.Escape = escape;
		}

		private static string CreateRegex(string escapeString, string likePattern)
		{
			char chr = (string.IsNullOrEmpty(escapeString) ? '\0' : escapeString[0]);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("^");
			bool flag = false;
			string str = likePattern;
			for (int i = 0; i < str.Length; i++)
			{
				char chr1 = str[i];
				if (!flag && chr1 == chr)
				{
					flag = true;
				}
				else if (!flag && chr1 == '%')
				{
					stringBuilder.Append(".*");
				}
				else if (flag || chr1 != '\u005F')
				{
					stringBuilder.Append(Regex.Escape(chr1.ToString()));
					flag = false;
				}
				else
				{
					stringBuilder.Append(".");
				}
			}
			stringBuilder.Append("$");
			return stringBuilder.ToString();
		}

		private static bool? LikeMethod(object expression, object pattern, object escape)
		{
			if (expression is DBNull || pattern is DBNull || escape is DBNull)
			{
				return null;
			}
			string str = Convert.ToString(expression, CultureInfo.InvariantCulture);
			string str1 = Convert.ToString(pattern, CultureInfo.InvariantCulture);
			string str2 = Convert.ToString(escape, CultureInfo.InvariantCulture);
			string str3 = LikeExpression.CreateRegex(str2, str1);
			return new bool?(Regex.IsMatch(str, str3));
		}

		public override System.Linq.Expressions.Expression Reduce()
		{
			return System.Linq.Expressions.Expression.Call(LikeExpression.LikeMethodInfo, this.Expression, this.Pattern, this.Escape);
		}

		public override string ToString()
		{
			if (this.Escape == null)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] str = new object[] { this.Expression.ToString(), this.Pattern };
				return string.Format(invariantCulture, "{0} LIKE {1}", str);
			}
			CultureInfo cultureInfo = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { this.Expression.ToString(), this.Pattern, this.Escape };
			return string.Format(cultureInfo, "{0} LIKE {1} ESCAPE {2}", objArray);
		}
	}
}
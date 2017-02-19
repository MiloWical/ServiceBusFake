using Microsoft.CSharp.RuntimeBinder;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal static class ExpressionBuilder
	{
		private readonly static MethodInfo AddCheckedMethodInfo;

		private readonly static MethodInfo SubtractCheckedMethodInfo;

		private readonly static MethodInfo MultiplyCheckedMethodInfo;

		private readonly static MethodInfo DivideMethodInfo;

		private readonly static MethodInfo ModuloMethodInfo;

		private readonly static MethodInfo EqualMethodInfo;

		private readonly static MethodInfo NotEqualMethodInfo;

		private readonly static MethodInfo GreaterThanMethodInfo;

		private readonly static MethodInfo GreaterThanOrEqualsMethodInfo;

		private readonly static MethodInfo LessThanMethodInfo;

		private readonly static MethodInfo LessThanOrEqualMethodInfo;

		private readonly static MethodInfo NegateCheckedMethodInfo;

		static ExpressionBuilder()
		{
			ExpressionBuilder.AddCheckedMethodInfo = typeof(ExpressionBuilder).GetMethod("AddCheckedMethod", BindingFlags.Static | BindingFlags.NonPublic);
			ExpressionBuilder.SubtractCheckedMethodInfo = typeof(ExpressionBuilder).GetMethod("SubtractCheckedMethod", BindingFlags.Static | BindingFlags.NonPublic);
			ExpressionBuilder.MultiplyCheckedMethodInfo = typeof(ExpressionBuilder).GetMethod("MultiplyCheckedMethod", BindingFlags.Static | BindingFlags.NonPublic);
			ExpressionBuilder.DivideMethodInfo = typeof(ExpressionBuilder).GetMethod("DivideMethod", BindingFlags.Static | BindingFlags.NonPublic);
			ExpressionBuilder.ModuloMethodInfo = typeof(ExpressionBuilder).GetMethod("ModuloMethod", BindingFlags.Static | BindingFlags.NonPublic);
			ExpressionBuilder.EqualMethodInfo = typeof(ExpressionBuilder).GetMethod("EqualMethod", BindingFlags.Static | BindingFlags.NonPublic);
			ExpressionBuilder.NotEqualMethodInfo = typeof(ExpressionBuilder).GetMethod("NotEqualMethod", BindingFlags.Static | BindingFlags.NonPublic);
			ExpressionBuilder.GreaterThanMethodInfo = typeof(ExpressionBuilder).GetMethod("GreaterThanMethod", BindingFlags.Static | BindingFlags.NonPublic);
			ExpressionBuilder.GreaterThanOrEqualsMethodInfo = typeof(ExpressionBuilder).GetMethod("GreaterThanOrEqualMethod", BindingFlags.Static | BindingFlags.NonPublic);
			ExpressionBuilder.LessThanMethodInfo = typeof(ExpressionBuilder).GetMethod("LessThanMethod", BindingFlags.Static | BindingFlags.NonPublic);
			ExpressionBuilder.LessThanOrEqualMethodInfo = typeof(ExpressionBuilder).GetMethod("LessThanOrEqualMethod", BindingFlags.Static | BindingFlags.NonPublic);
			ExpressionBuilder.NegateCheckedMethodInfo = typeof(ExpressionBuilder).GetMethod("NegateCheckedMethod", BindingFlags.Static | BindingFlags.NonPublic);
		}

		public static Expression AddChecked(Expression left, Expression right)
		{
			return Expression.AddChecked(ExpressionBuilder.ConvertToObject(left), ExpressionBuilder.ConvertToObject(right), ExpressionBuilder.AddCheckedMethodInfo);
		}

		private static object AddCheckedMethod(object operand1, object operand2)
		{
			if (Convert.IsDBNull(operand1) || Convert.IsDBNull(operand2))
			{
				return DBNull.Value;
			}
			ExpressionBuilder.EnsureMaxAllowedStringLength(operand1);
			ExpressionBuilder.EnsureMaxAllowedStringLength(operand2);
			object obj = operand1 + operand2;
			ExpressionBuilder.EnsureMaxAllowedStringLength(obj);
			return obj;
		}

		public static Expression AndAlso(Expression left, Expression right)
		{
			return Expression.AndAlso(left, right);
		}

		public static Expression ConvertToObject(Expression expression)
		{
			if (expression.Type == typeof(object))
			{
				return expression;
			}
			return Expression.Convert(expression, typeof(object));
		}

		public static Expression Divide(Expression left, Expression right)
		{
			return Expression.Divide(ExpressionBuilder.ConvertToObject(left), ExpressionBuilder.ConvertToObject(right), ExpressionBuilder.DivideMethodInfo);
		}

		private static object DivideMethod(object operand1, object operand2)
		{
			if (Convert.IsDBNull(operand1) || Convert.IsDBNull(operand2))
			{
				return DBNull.Value;
			}
			return operand1 / operand2;
		}

		private static void EnsureMaxAllowedStringLength(object stringObject)
		{
			string str = stringObject as string;
			if (str != null && str.Length > 32767)
			{
				string str1 = str.Substring(0, Math.Min(20, str.Length));
				throw new InvalidOperationException(SRClient.StringIsTooLong(str1, str.Length, (short)32767));
			}
		}

		public static Expression Equal(Expression left, Expression right)
		{
			return Expression.Equal(ExpressionBuilder.ConvertToObject(left), ExpressionBuilder.ConvertToObject(right), true, ExpressionBuilder.EqualMethodInfo);
		}

		private static bool? EqualMethod(object operand1, object operand2)
		{
			if (Convert.IsDBNull(operand1) || Convert.IsDBNull(operand2))
			{
				return null;
			}
			return (bool?)(operand1 == operand2);
		}

		public static Expression Exists(Expression expression)
		{
			return new ExistsExpression(ExpressionBuilder.ConvertToObject(expression));
		}

		public static Expression GetParameter(ParameterExpression parametersParameter, string name)
		{
			return new GetParameterExpression(parametersParameter, name);
		}

		public static Expression GreaterThan(Expression left, Expression right)
		{
			return Expression.GreaterThan(ExpressionBuilder.ConvertToObject(left), ExpressionBuilder.ConvertToObject(right), true, ExpressionBuilder.GreaterThanMethodInfo);
		}

		private static bool? GreaterThanMethod(object operand1, object operand2)
		{
			if (Convert.IsDBNull(operand1) || Convert.IsDBNull(operand2))
			{
				return null;
			}
			return (bool?)(operand1 > operand2);
		}

		public static Expression GreaterThanOrEqual(Expression left, Expression right)
		{
			return Expression.GreaterThanOrEqual(ExpressionBuilder.ConvertToObject(left), ExpressionBuilder.ConvertToObject(right), true, ExpressionBuilder.GreaterThanOrEqualsMethodInfo);
		}

		private static bool? GreaterThanOrEqualMethod(object operand1, object operand2)
		{
			if (Convert.IsDBNull(operand1) || Convert.IsDBNull(operand2))
			{
				return null;
			}
			return (bool?)(operand1 >= operand2);
		}

		public static Expression In(Expression left, IEnumerable<Expression> right)
		{
			return new InExpression(left, right);
		}

		public static Expression IsFalse(Expression expression)
		{
			return Expression.IsFalse(expression);
		}

		public static Expression IsNotNull(Expression operand)
		{
			return Expression.IsFalse(new IsNullExpression(operand));
		}

		public static Expression IsNull(Expression operand)
		{
			return new IsNullExpression(operand);
		}

		public static Expression LessThan(Expression left, Expression right)
		{
			return Expression.LessThan(ExpressionBuilder.ConvertToObject(left), ExpressionBuilder.ConvertToObject(right), true, ExpressionBuilder.LessThanMethodInfo);
		}

		private static bool? LessThanMethod(object operand1, object operand2)
		{
			if (Convert.IsDBNull(operand1) || Convert.IsDBNull(operand2))
			{
				return null;
			}
			return (bool?)(operand1 < operand2);
		}

		public static Expression LessThanOrEqual(Expression left, Expression right)
		{
			return Expression.LessThanOrEqual(ExpressionBuilder.ConvertToObject(left), ExpressionBuilder.ConvertToObject(right), true, ExpressionBuilder.LessThanOrEqualMethodInfo);
		}

		private static bool? LessThanOrEqualMethod(object operand1, object operand2)
		{
			if (Convert.IsDBNull(operand1) || Convert.IsDBNull(operand2))
			{
				return null;
			}
			return (bool?)(operand1 <= operand2);
		}

		public static Expression Like(Expression expression, Expression pattern, Expression escape)
		{
			return new LikeExpression(expression, pattern, escape);
		}

		public static Expression MakeFunction(ParameterExpression message, string functionName, List<Expression> arguments)
		{
			if (string.Equals(functionName, "NEWID", StringComparison.OrdinalIgnoreCase))
			{
				if (arguments.Count != 0)
				{
					throw new ArgumentException(SRClient.FilterFunctionIncorrectNumberOfArguments(functionName, 0, arguments.Count), "arguments");
				}
				return Expression.Call(Constants.NewGuid, new Expression[0]);
			}
			if (!string.Equals(functionName, "P", StringComparison.OrdinalIgnoreCase) && !string.Equals(functionName, "Property", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException(SRClient.FilterUnknownFunctionName(functionName));
			}
			if (arguments.Count != 1)
			{
				throw new ArgumentException(SRClient.FilterFunctionIncorrectNumberOfArguments(functionName, 1, arguments.Count), "arguments");
			}
			Expression expression = Expression.Constant(PropertyScope.User);
			return new GetPropertyExpression(message, expression, arguments[0]);
		}

		public static Expression Modulo(Expression left, Expression right)
		{
			return Expression.Modulo(ExpressionBuilder.ConvertToObject(left), ExpressionBuilder.ConvertToObject(right), ExpressionBuilder.ModuloMethodInfo);
		}

		private static object ModuloMethod(object operand1, object operand2)
		{
			if (Convert.IsDBNull(operand1) || Convert.IsDBNull(operand2))
			{
				return DBNull.Value;
			}
			return operand1 % operand2;
		}

		public static Expression MultiplyChecked(Expression left, Expression right)
		{
			return Expression.MultiplyChecked(ExpressionBuilder.ConvertToObject(left), ExpressionBuilder.ConvertToObject(right), ExpressionBuilder.MultiplyCheckedMethodInfo);
		}

		private static object MultiplyCheckedMethod(object operand1, object operand2)
		{
			if (Convert.IsDBNull(operand1) || Convert.IsDBNull(operand2))
			{
				return DBNull.Value;
			}
			return operand1 * operand2;
		}

		public static Expression NegateChecked(Expression expression)
		{
			return Expression.NegateChecked(ExpressionBuilder.ConvertToObject(expression), ExpressionBuilder.NegateCheckedMethodInfo);
		}

		private static object NegateCheckedMethod(object value)
		{
			if (Convert.IsDBNull(value))
			{
				return DBNull.Value;
			}
			return -value;
		}

		public static Expression NotEqual(Expression left, Expression right)
		{
			return Expression.NotEqual(ExpressionBuilder.ConvertToObject(left), ExpressionBuilder.ConvertToObject(right), true, ExpressionBuilder.NotEqualMethodInfo);
		}

		private static bool? NotEqualMethod(object operand1, object operand2)
		{
			if (Convert.IsDBNull(operand1) || Convert.IsDBNull(operand2))
			{
				return null;
			}
			return (bool?)(operand1 != operand2);
		}

		public static Expression NotIn(Expression left, IEnumerable<Expression> right)
		{
			return Expression.IsFalse(ExpressionBuilder.In(left, right));
		}

		public static Expression NotLike(Expression expression, Expression pattern, Expression escape)
		{
			return Expression.IsFalse(ExpressionBuilder.Like(expression, pattern, escape));
		}

		public static Expression OrElse(Expression left, Expression right)
		{
			return Expression.OrElse(left, right);
		}

		public static Expression SubtractChecked(Expression left, Expression right)
		{
			return Expression.SubtractChecked(ExpressionBuilder.ConvertToObject(left), ExpressionBuilder.ConvertToObject(right), ExpressionBuilder.SubtractCheckedMethodInfo);
		}

		private static object SubtractCheckedMethod(object operand1, object operand2)
		{
			if (Convert.IsDBNull(operand1) || Convert.IsDBNull(operand2))
			{
				return DBNull.Value;
			}
			return operand1 - operand2;
		}

		private static class FunctionNames
		{
			public const string NewId = "NEWID";

			public const string P = "P";

			public const string Property = "Property";
		}
	}
}
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal sealed class GetParameterExpression : Expression
	{
		private readonly static MethodInfo GetParameterMethodInfo;

		private readonly ParameterExpression parametersParameter;

		private readonly string parameterName;

		public override bool CanReduce
		{
			get
			{
				return true;
			}
		}

		public override ExpressionType NodeType
		{
			get
			{
				return ExpressionType.Extension;
			}
		}

		public string ParameterName
		{
			get
			{
				return this.parameterName;
			}
		}

		public override System.Type Type
		{
			get
			{
				return Constants.ObjectType;
			}
		}

		static GetParameterExpression()
		{
			GetParameterExpression.GetParameterMethodInfo = typeof(GetParameterExpression).GetMethod("GetParameterMethod", BindingFlags.Static | BindingFlags.NonPublic);
		}

		public GetParameterExpression(ParameterExpression parametersParameter, string parameterName)
		{
			this.parametersParameter = parametersParameter;
			this.parameterName = parameterName;
		}

		private static object GetParameterMethod(IDictionary<string, object> parameters, string name)
		{
			if (parameters == null)
			{
				throw new ArgumentNullException("parameters");
			}
			return parameters[name];
		}

		public override Expression Reduce()
		{
			return Expression.Call(GetParameterExpression.GetParameterMethodInfo, this.parametersParameter, Expression.Constant(this.ParameterName));
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] parameterName = new object[] { this.ParameterName };
			return string.Format(invariantCulture, "@{0}", parameterName);
		}
	}
}
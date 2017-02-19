using Microsoft.ServiceBus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="CorrelationFilterTemplate", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal class CorrelationFilterTemplate : FilterTemplate
	{
		private readonly static Lazy<Func<CorrelationFilterTemplate, IEnumerable<KeyValuePair<QualifiedPropertyName, object>>>> EnumerateSystemPropertiesFunctionCompiler;

		public readonly static Lazy<Action<CorrelationFilterTemplate, CorrelationFilter>> MapSystemPropertiesActionCompiler;

		[DataMember(Name="Properties", EmitDefaultValue=false, IsRequired=false, Order=65544)]
		private PropertyReferenceDictionary properties;

		[DataMember(Name="ContentType", EmitDefaultValue=false, IsRequired=false, Order=65543)]
		[SystemPropertyMapping(typeof(string))]
		public object ContentType
		{
			get;
			private set;
		}

		[DataMember(Name="CorrelationId", EmitDefaultValue=false, IsRequired=false, Order=65537)]
		[SystemPropertyMapping(typeof(string))]
		public object CorrelationId
		{
			get;
			private set;
		}

		[DataMember(Name="Label", EmitDefaultValue=false, IsRequired=false, Order=65540)]
		[SystemPropertyMapping(typeof(string))]
		public object Label
		{
			get;
			private set;
		}

		public IDictionary<string, object> Properties
		{
			get
			{
				PropertyReferenceDictionary propertyReferenceDictionaries = this.properties;
				if (propertyReferenceDictionaries == null)
				{
					PropertyReferenceDictionary propertyReferenceDictionaries1 = new PropertyReferenceDictionary();
					PropertyReferenceDictionary propertyReferenceDictionaries2 = propertyReferenceDictionaries1;
					this.properties = propertyReferenceDictionaries1;
					propertyReferenceDictionaries = propertyReferenceDictionaries2;
				}
				return propertyReferenceDictionaries;
			}
		}

		[DataMember(Name="ReplyTo", EmitDefaultValue=false, IsRequired=false, Order=65539)]
		[SystemPropertyMapping(typeof(string))]
		public object ReplyTo
		{
			get;
			private set;
		}

		[DataMember(Name="ReplyToSessionId", EmitDefaultValue=false, IsRequired=false, Order=65542)]
		[SystemPropertyMapping(typeof(string))]
		public object ReplyToSessionId
		{
			get;
			private set;
		}

		[DataMember(Name="SessionId", EmitDefaultValue=false, IsRequired=false, Order=65541)]
		[SystemPropertyMapping(typeof(string))]
		public object SessionId
		{
			get;
			private set;
		}

		[DataMember(Name="To", EmitDefaultValue=false, IsRequired=false, Order=65538)]
		[SystemPropertyMapping(typeof(string))]
		public object To
		{
			get;
			private set;
		}

		static CorrelationFilterTemplate()
		{
			CorrelationFilterTemplate.EnumerateSystemPropertiesFunctionCompiler = new Lazy<Func<CorrelationFilterTemplate, IEnumerable<KeyValuePair<QualifiedPropertyName, object>>>>(new Func<Func<CorrelationFilterTemplate, IEnumerable<KeyValuePair<QualifiedPropertyName, object>>>>(CorrelationFilterTemplate.BuildEnumerateSystemPropertiesFunction), LazyThreadSafetyMode.None);
			CorrelationFilterTemplate.MapSystemPropertiesActionCompiler = new Lazy<Action<CorrelationFilterTemplate, CorrelationFilter>>(new Func<Action<CorrelationFilterTemplate, CorrelationFilter>>(CorrelationFilterTemplate.BuildMapSystemPropertiesAction), LazyThreadSafetyMode.None);
		}

		public CorrelationFilterTemplate()
		{
			this.properties = new PropertyReferenceDictionary();
		}

		private static Func<CorrelationFilterTemplate, IEnumerable<KeyValuePair<QualifiedPropertyName, object>>> BuildEnumerateSystemPropertiesFunction()
		{
			ParameterExpression parameterExpression = Expression.Parameter(typeof(CorrelationFilterTemplate), "template");
			Type type = typeof(KeyValuePair<QualifiedPropertyName, object>);
			Type[] typeArray = new Type[] { typeof(QualifiedPropertyName), typeof(object) };
			ConstructorInfo constructor = type.GetConstructor(typeArray);
			Type type1 = typeof(QualifiedPropertyName);
			Type[] typeArray1 = new Type[] { typeof(PropertyScope), typeof(string) };
			ConstructorInfo constructorInfo = type1.GetConstructor(typeArray1);
			List<Expression> expressions = new List<Expression>();
			foreach (Tuple<PropertyInfo, CorrelationFilterTemplate.SystemPropertyMappingAttribute> tuple in CorrelationFilterTemplate.EnumerateSystemProperties())
			{
				PropertyInfo item1 = tuple.Item1;
				Expression[] expressionArray = new Expression[2];
				Expression[] expressionArray1 = new Expression[] { Expression.Constant(PropertyScope.System), Expression.Constant(item1.Name) };
				expressionArray[0] = Expression.New(constructorInfo, expressionArray1);
				expressionArray[1] = Expression.Property(parameterExpression, item1.Name);
				expressions.Add(Expression.New(constructor, expressionArray));
			}
			Expression expression = Expression.ListInit(Expression.New(typeof(List<KeyValuePair<QualifiedPropertyName, object>>)), expressions);
			ParameterExpression[] parameterExpressionArray = new ParameterExpression[] { parameterExpression };
			return Expression.Lambda<Func<CorrelationFilterTemplate, IEnumerable<KeyValuePair<QualifiedPropertyName, object>>>>(expression, parameterExpressionArray).Compile();
		}

		private static Action<CorrelationFilterTemplate, CorrelationFilter> BuildMapSystemPropertiesAction()
		{
			ParameterExpression parameterExpression = Expression.Parameter(typeof(CorrelationFilterTemplate), "template");
			ParameterExpression parameterExpression1 = Expression.Parameter(typeof(CorrelationFilter), "filter");
			List<Expression> expressions = new List<Expression>();
			foreach (Tuple<PropertyInfo, CorrelationFilterTemplate.SystemPropertyMappingAttribute> tuple in CorrelationFilterTemplate.EnumerateSystemProperties())
			{
				PropertyInfo item1 = tuple.Item1;
				CorrelationFilterTemplate.SystemPropertyMappingAttribute item2 = tuple.Item2;
				ParameterExpression parameterExpression2 = Expression.Variable(typeof(PropertyReference), "reference");
				ParameterExpression parameterExpression3 = Expression.Variable(typeof(object), "possibleValue");
				BinaryExpression binaryExpression = Expression.NotEqual(Expression.Property(parameterExpression, item1.Name), Expression.Constant(null));
				ParameterExpression[] parameterExpressionArray = new ParameterExpression[] { parameterExpression2, parameterExpression3 };
				Expression[] expressionArray = new Expression[] { Expression.Assign(parameterExpression2, Expression.TypeAs(Expression.Property(parameterExpression1, item1.Name), typeof(PropertyReference))), Expression.Assign(parameterExpression3, Expression.Condition(Expression.NotEqual(parameterExpression2, Expression.Constant(null)), Expression.Property(parameterExpression2, "Value"), Expression.Property(parameterExpression, item1.Name))), Expression.Assign(Expression.Property(parameterExpression1, item1.Name), Expression.Convert(parameterExpression3, item2.TargetType)) };
				Expression expression = Expression.IfThen(binaryExpression, Expression.Block(parameterExpressionArray, expressionArray));
				expressions.Add(expression);
			}
			BlockExpression blockExpression = Expression.Block(expressions);
			ParameterExpression[] parameterExpressionArray1 = new ParameterExpression[] { parameterExpression, parameterExpression1 };
			return Expression.Lambda<Action<CorrelationFilterTemplate, CorrelationFilter>>(blockExpression, parameterExpressionArray1).Compile();
		}

		public override Filter Create()
		{
			CorrelationFilter correlationFilter = new CorrelationFilter();
			CorrelationFilterTemplate.MapSystemPropertiesActionCompiler.Value(this, correlationFilter);
			foreach (KeyValuePair<string, object> property in this.Properties)
			{
				correlationFilter.Properties[property.Key] = PropertyReference.GetValue<object>(property.Value);
			}
			return correlationFilter;
		}

		private static IEnumerable<Tuple<PropertyInfo, CorrelationFilterTemplate.SystemPropertyMappingAttribute>> EnumerateSystemProperties()
		{
			PropertyInfo[] propertyInfoArray = typeof(CorrelationFilterTemplate).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty);
			PropertyInfo[] propertyInfoArray1 = propertyInfoArray;
			for (int i = 0; i < (int)propertyInfoArray1.Length; i++)
			{
				PropertyInfo propertyInfo = propertyInfoArray1[i];
				object[] customAttributes = propertyInfo.GetCustomAttributes(typeof(CorrelationFilterTemplate.SystemPropertyMappingAttribute), false);
				if ((int)customAttributes.Length == 1)
				{
					yield return Tuple.Create<PropertyInfo, CorrelationFilterTemplate.SystemPropertyMappingAttribute>(propertyInfo, (CorrelationFilterTemplate.SystemPropertyMappingAttribute)customAttributes[0]);
				}
			}
		}

		internal override void Validate(ICollection<PropertyReference> initializationList)
		{
			IEnumerable<object> value = 
				from pair in CorrelationFilterTemplate.EnumerateSystemPropertiesFunctionCompiler.Value(this)
				select pair.Value;
			IEnumerable<object> properties = 
				from pair in this.Properties
				select pair.Value;
			foreach (object obj in value.Concat<object>(properties))
			{
				PropertyReference propertyReference = obj as PropertyReference;
				if (propertyReference == null || initializationList.Contains(propertyReference))
				{
					continue;
				}
				throw new RuleActionException(SRClient.PropertyReferenceUsedWithoutInitializes(propertyReference));
			}
		}

		[AttributeUsage(AttributeTargets.Property, AllowMultiple=false, Inherited=false)]
		private sealed class SystemPropertyMappingAttribute : Attribute
		{
			public Type TargetType
			{
				get;
				private set;
			}

			public SystemPropertyMappingAttribute(Type targetType)
			{
				this.TargetType = targetType;
			}
		}
	}
}
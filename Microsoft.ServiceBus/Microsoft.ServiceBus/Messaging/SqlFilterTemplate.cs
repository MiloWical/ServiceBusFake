using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="SqlFilterTemplate", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(DateTimeOffset))]
	internal class SqlFilterTemplate : FilterTemplate
	{
		[DataMember(Name="Parameters", Order=65538, EmitDefaultValue=false, IsRequired=false)]
		private PropertyReferenceDictionary parameters;

		public IDictionary<string, object> Parameters
		{
			get
			{
				PropertyReferenceDictionary propertyReferenceDictionaries = this.parameters;
				if (propertyReferenceDictionaries == null)
				{
					PropertyReferenceDictionary propertyReferenceDictionaries1 = new PropertyReferenceDictionary();
					PropertyReferenceDictionary propertyReferenceDictionaries2 = propertyReferenceDictionaries1;
					this.parameters = propertyReferenceDictionaries1;
					propertyReferenceDictionaries = propertyReferenceDictionaries2;
				}
				return propertyReferenceDictionaries;
			}
		}

		[DataMember(Name="SqlExpression", Order=65537, EmitDefaultValue=false, IsRequired=true)]
		public string SqlExpression
		{
			get;
			set;
		}

		public SqlFilterTemplate(string sqlExpression)
		{
			this.SqlExpression = sqlExpression;
		}

		public override Filter Create()
		{
			SqlFilter sqlFilter = new SqlFilter(this.SqlExpression);
			foreach (KeyValuePair<string, object> parameter in this.Parameters)
			{
				sqlFilter.Parameters[parameter.Key] = PropertyReference.GetValue<object>(parameter.Value);
			}
			return sqlFilter;
		}

		internal override void Validate(ICollection<PropertyReference> initializationList)
		{
			foreach (KeyValuePair<string, object> parameter in this.Parameters)
			{
				PropertyReference value = parameter.Value as PropertyReference;
				if (value == null || initializationList.Contains(value))
				{
					continue;
				}
				throw new RuleActionException(SRClient.PropertyReferenceUsedWithoutInitializes(value));
			}
		}
	}
}
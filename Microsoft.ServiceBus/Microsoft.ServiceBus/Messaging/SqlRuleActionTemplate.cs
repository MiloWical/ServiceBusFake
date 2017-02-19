using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="SqlRuleActionTemplate", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(DateTimeOffset))]
	internal class SqlRuleActionTemplate : RuleActionTemplate
	{
		[DataMember(Name="Parameters", Order=65538, EmitDefaultValue=false, IsRequired=false)]
		public IDictionary<string, object> Parameters
		{
			get;
			private set;
		}

		[DataMember(Name="SqlExpression", Order=65537, EmitDefaultValue=false, IsRequired=true)]
		public string SqlExpression
		{
			get;
			set;
		}

		public SqlRuleActionTemplate(string expression)
		{
			this.SqlExpression = expression;
			this.Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}

		public override RuleAction Create()
		{
			SqlRuleAction sqlRuleAction = new SqlRuleAction(this.SqlExpression);
			foreach (KeyValuePair<string, object> parameter in this.Parameters)
			{
				sqlRuleAction.Parameters[parameter.Key] = PropertyReference.GetValue<object>(parameter.Value);
			}
			return sqlRuleAction;
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (this.Parameters == null)
			{
				this.Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
				return;
			}
			this.Parameters = new Dictionary<string, object>(this.Parameters, StringComparer.OrdinalIgnoreCase);
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
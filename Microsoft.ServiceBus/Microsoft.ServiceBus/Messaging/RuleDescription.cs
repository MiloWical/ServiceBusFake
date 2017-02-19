using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="RuleDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class RuleDescription : EntityDescription, IResourceDescription
	{
		public const string DefaultRuleName = "$Default";

		private Microsoft.ServiceBus.Messaging.Filter filter;

		private RuleAction action;

		private string name;

		[DataMember(Name="Action", IsRequired=false, Order=65538, EmitDefaultValue=false)]
		public RuleAction Action
		{
			get
			{
				return this.action;
			}
			set
			{
				base.ThrowIfReadOnly();
				this.action = value;
			}
		}

		public DateTime CreatedAt
		{
			get
			{
				DateTime? internalCreatedAt = this.InternalCreatedAt;
				if (!internalCreatedAt.HasValue)
				{
					return DateTime.MinValue;
				}
				return internalCreatedAt.GetValueOrDefault();
			}
		}

		[DataMember(Name="Filter", IsRequired=false, Order=65537, EmitDefaultValue=false)]
		public Microsoft.ServiceBus.Messaging.Filter Filter
		{
			get
			{
				return this.filter;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (value == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("value");
				}
				this.filter = value;
			}
		}

		[DataMember(Name="CreatedAt", IsRequired=false, Order=131074, EmitDefaultValue=false)]
		internal DateTime? InternalCreatedAt
		{
			get;
			set;
		}

		string Microsoft.ServiceBus.Messaging.IResourceDescription.CollectionName
		{
			get
			{
				return "Rules";
			}
		}

		[DataMember(Name="Name", IsRequired=false, Order=131077, EmitDefaultValue=false)]
		public string Name
		{
			get
			{
				return this.name;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (string.IsNullOrWhiteSpace(value))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("Name");
				}
				this.name = value;
			}
		}

		[DataMember(Name="Tag", IsRequired=false, Order=131073, EmitDefaultValue=false)]
		internal string Tag
		{
			get;
			set;
		}

		public RuleDescription() : this(TrueFilter.Default)
		{
		}

		public RuleDescription(string name) : this(name, TrueFilter.Default)
		{
		}

		public RuleDescription(Microsoft.ServiceBus.Messaging.Filter filter)
		{
			if (filter == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("Filter");
			}
			this.Filter = filter;
		}

		public RuleDescription(string name, Microsoft.ServiceBus.Messaging.Filter filter)
		{
			if (filter == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("Filter");
			}
			this.Filter = filter;
			this.Name = name;
		}

		internal override bool IsValidForVersion(ApiVersion version)
		{
			if (!base.IsValidForVersion(version))
			{
				return false;
			}
			if (version < ApiVersion.Two && (this.Tag != null || this.InternalCreatedAt.HasValue || this.action is CompositeAction || this.action is RuleCreationAction))
			{
				return false;
			}
			if (this.filter != null && !this.filter.IsValidForVersion(version))
			{
				return false;
			}
			if (this.action != null && !this.action.IsValidForVersion(version))
			{
				return false;
			}
			return true;
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (this.filter is TrueFilter)
			{
				this.filter = TrueFilter.Default;
			}
			else if (this.filter is FalseFilter)
			{
				this.filter = FalseFilter.Default;
			}
			if (this.action == null || this.action is EmptyRuleAction)
			{
				this.action = EmptyRuleAction.Default;
			}
		}

		internal override void UpdateForVersion(ApiVersion version, EntityDescription existingDescription = null)
		{
			RuleAction ruleAction;
			Microsoft.ServiceBus.Messaging.Filter filter;
			string tag;
			DateTime? internalCreatedAt;
			RuleAction ruleAction1;
			string str;
			RuleDescription ruleDescription = existingDescription as RuleDescription;
			base.UpdateForVersion(version, existingDescription);
			bool flag = false;
			if (version < ApiVersion.Two)
			{
				if (ruleDescription == null)
				{
					tag = null;
				}
				else
				{
					tag = ruleDescription.Tag;
				}
				this.Tag = tag;
				if (ruleDescription == null)
				{
					internalCreatedAt = null;
				}
				else
				{
					internalCreatedAt = ruleDescription.InternalCreatedAt;
				}
				this.InternalCreatedAt = internalCreatedAt;
				if (this.action is CompositeAction || this.action is RuleCreationAction)
				{
					if (ruleDescription == null)
					{
						ruleAction1 = null;
					}
					else
					{
						ruleAction1 = ruleDescription.action;
					}
					this.action = ruleAction1;
					flag = true;
				}
				if (ruleDescription == null)
				{
					str = null;
				}
				else
				{
					str = ruleDescription.name;
				}
				this.name = str;
			}
			if (this.filter != null)
			{
				Microsoft.ServiceBus.Messaging.Filter filter1 = this.filter;
				ApiVersion apiVersion = version;
				if (ruleDescription == null)
				{
					filter = null;
				}
				else
				{
					filter = ruleDescription.filter;
				}
				filter1.UpdateForVersion(apiVersion, filter);
			}
			if (this.action != null && !flag)
			{
				RuleAction ruleAction2 = this.action;
				ApiVersion apiVersion1 = version;
				if (ruleDescription == null)
				{
					ruleAction = null;
				}
				else
				{
					ruleAction = ruleDescription.action;
				}
				ruleAction2.UpdateForVersion(apiVersion1, ruleAction);
			}
		}

		internal void Validate()
		{
			this.Filter.Validate();
			if (this.Action != null)
			{
				this.Action.Validate();
			}
		}
	}
}
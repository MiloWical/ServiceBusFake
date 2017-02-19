using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="VolatileTopicDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal sealed class VolatileTopicDescription : EntityDescription, IResourceDescription
	{
		private string path;

		[DataMember(Name="EnablePartitioning", IsRequired=false, Order=3, EmitDefaultValue=false)]
		public bool EnablePartitioning
		{
			get;
			set;
		}

		internal bool EnableRuleAction
		{
			get
			{
				bool? internalEnableRuleAction = this.InternalEnableRuleAction;
				if (!internalEnableRuleAction.HasValue)
				{
					return false;
				}
				return internalEnableRuleAction.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalEnableRuleAction = new bool?(value);
			}
		}

		[DataMember(Name="EnableRuleAction", IsRequired=false, Order=2, EmitDefaultValue=false)]
		internal bool? InternalEnableRuleAction
		{
			get;
			set;
		}

		[DataMember(Name="IsAnonymousAccessible", IsRequired=false, Order=1, EmitDefaultValue=false)]
		internal bool? InternalIsAnonymousAccessible
		{
			get;
			set;
		}

		public bool IsAnonymousAccessible
		{
			get
			{
				bool? internalIsAnonymousAccessible = this.InternalIsAnonymousAccessible;
				if (!internalIsAnonymousAccessible.HasValue)
				{
					return false;
				}
				return internalIsAnonymousAccessible.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalIsAnonymousAccessible = new bool?(value);
			}
		}

		string Microsoft.ServiceBus.Messaging.IResourceDescription.CollectionName
		{
			get
			{
				return "VolatileTopics";
			}
		}

		public string Path
		{
			get
			{
				return this.path;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (string.IsNullOrWhiteSpace(value))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("Path");
				}
				this.path = value;
			}
		}

		public VolatileTopicDescription(string path)
		{
			this.Path = path;
		}

		internal VolatileTopicDescription()
		{
		}
	}
}
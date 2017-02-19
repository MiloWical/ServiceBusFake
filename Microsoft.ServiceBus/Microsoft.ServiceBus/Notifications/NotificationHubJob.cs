using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="NotificationHubJob", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class NotificationHubJob : EntityDescription, IResourceDescription
	{
		public string CollectionName
		{
			get
			{
				return "jobs";
			}
		}

		[DataMember(Name="CreatedAt", IsRequired=false, Order=1010, EmitDefaultValue=false)]
		public DateTime CreatedAt
		{
			get;
			internal set;
		}

		[DataMember(Name="Failure", IsRequired=false, Order=1008, EmitDefaultValue=false)]
		public string Failure
		{
			get;
			internal set;
		}

		public string FailuresFileName
		{
			get
			{
				string empty = string.Empty;
				Dictionary<string, string> outputProperties = this.OutputProperties;
				if (outputProperties != null)
				{
					outputProperties.TryGetValue("FailedFilePath", out empty);
				}
				return empty;
			}
		}

		[DataMember(Name="ImportFileUri", IsRequired=false, Order=1006, EmitDefaultValue=false)]
		public Uri ImportFileUri
		{
			get;
			set;
		}

		[DataMember(Name="InputProperties", IsRequired=false, Order=1007, EmitDefaultValue=false)]
		public Dictionary<string, string> InputProperties
		{
			get;
			set;
		}

		[DataMember(Name="JobId", IsRequired=false, Order=1001, EmitDefaultValue=false)]
		public string JobId
		{
			get;
			internal set;
		}

		[DataMember(Name="Type", IsRequired=true, Order=1003, EmitDefaultValue=true)]
		public NotificationHubJobType JobType
		{
			get;
			set;
		}

		[DataMember(Name="OutputContainerUri", IsRequired=true, Order=1005, EmitDefaultValue=false)]
		public Uri OutputContainerUri
		{
			get;
			set;
		}

		public string OutputFileName
		{
			get
			{
				string empty = string.Empty;
				Dictionary<string, string> outputProperties = this.OutputProperties;
				if (outputProperties != null)
				{
					outputProperties.TryGetValue("OutputFilePath", out empty);
				}
				return empty;
			}
		}

		[DataMember(Name="OutputProperties", IsRequired=false, Order=1009, EmitDefaultValue=false)]
		public Dictionary<string, string> OutputProperties
		{
			get;
			internal set;
		}

		[DataMember(Name="Progress", IsRequired=false, Order=1002, EmitDefaultValue=false)]
		public decimal Progress
		{
			get;
			internal set;
		}

		[DataMember(Name="Status", IsRequired=false, Order=1004, EmitDefaultValue=false)]
		public NotificationHubJobStatus Status
		{
			get;
			internal set;
		}

		[DataMember(Name="UpdatedAt", IsRequired=false, Order=1011, EmitDefaultValue=false)]
		public DateTime UpdatedAt
		{
			get;
			internal set;
		}

		public NotificationHubJob()
		{
		}
	}
}
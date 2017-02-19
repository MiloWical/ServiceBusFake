using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="NotificationOutcome", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class NotificationOutcome
	{
		internal bool DelayedRetry
		{
			get;
			set;
		}

		[DataMember(Name="Failure", IsRequired=true, Order=1002, EmitDefaultValue=true)]
		public long Failure
		{
			get;
			set;
		}

		internal string NotificationId
		{
			get;
			set;
		}

		[DataMember(Name="Results", IsRequired=true, Order=1003, EmitDefaultValue=true)]
		public List<RegistrationResult> Results
		{
			get;
			set;
		}

		public NotificationOutcomeState State
		{
			get;
			internal set;
		}

		[DataMember(Name="Success", IsRequired=true, Order=1001, EmitDefaultValue=true)]
		public long Success
		{
			get;
			set;
		}

		public string TrackingId
		{
			get;
			internal set;
		}

		public NotificationOutcome()
		{
		}

		internal static NotificationOutcome GetUnknownOutCome()
		{
			RegistrationResult registrationResult = new RegistrationResult()
			{
				ApplicationPlatform = "Unknown",
				PnsHandle = "Unknown",
				RegistrationId = "Unknown",
				Outcome = "UnknownError"
			};
			RegistrationResult registrationResult1 = registrationResult;
			NotificationOutcome notificationOutcome = new NotificationOutcome()
			{
				Failure = (long)1,
				Success = (long)0,
				Results = new List<RegistrationResult>()
				{
					registrationResult1
				}
			};
			return notificationOutcome;
		}
	}
}
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="NotificationHubDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class NotificationHubDescription : EntityDescription, IResourceDescription
	{
		public const string DefaultListenSasRuleName = "DefaultListenSharedAccessSignature";

		public const string DefaultFullSasRuleName = "DefaultFullSharedAccessSignature";

		private string path;

		private bool? internalStatus = null;

		[DataMember(Name="AdmCredential", IsRequired=false, EmitDefaultValue=false, Order=1014)]
		public Microsoft.ServiceBus.Notifications.AdmCredential AdmCredential
		{
			get;
			set;
		}

		[DataMember(Name="ApnsCredential", IsRequired=false, EmitDefaultValue=false, Order=1001)]
		public Microsoft.ServiceBus.Notifications.ApnsCredential ApnsCredential
		{
			get;
			set;
		}

		public AuthorizationRules Authorization
		{
			get
			{
				if (this.InternalAuthorization == null)
				{
					this.InternalAuthorization = new AuthorizationRules();
				}
				return this.InternalAuthorization;
			}
		}

		[DataMember(Name="BaiduCredential", IsRequired=false, EmitDefaultValue=false, Order=1016)]
		public Microsoft.ServiceBus.Notifications.BaiduCredential BaiduCredential
		{
			get;
			set;
		}

		[DataMember(Name="DailyApiCalls", IsRequired=false, EmitDefaultValue=false, Order=1013)]
		public long DailyApiCalls
		{
			get;
			internal set;
		}

		[DataMember(Name="DailyMaxActiveDevices", IsRequired=false, EmitDefaultValue=false, Order=1008)]
		public long DailyMaxActiveDevices
		{
			get;
			internal set;
		}

		[DataMember(Name="DailyMaxActiveRegistrations", IsRequired=false, EmitDefaultValue=false, Order=1009)]
		public long DailyMaxActiveRegistrations
		{
			get;
			internal set;
		}

		[DataMember(Name="DailyOperations", IsRequired=false, EmitDefaultValue=false, Order=1007)]
		public long DailyOperations
		{
			get;
			internal set;
		}

		[DataMember(Name="DailyPushes", IsRequired=false, EmitDefaultValue=false, Order=1012)]
		public long DailyPushes
		{
			get;
			internal set;
		}

		[DataMember(Name="GcmCredential", IsRequired=false, EmitDefaultValue=false, Order=1005)]
		public Microsoft.ServiceBus.Notifications.GcmCredential GcmCredential
		{
			get;
			set;
		}

		[DataMember(Name="AuthorizationRules", IsRequired=false, Order=1004, EmitDefaultValue=false)]
		internal AuthorizationRules InternalAuthorization
		{
			get;
			set;
		}

		[DataMember(Name="RegistrationTtl", IsRequired=false, EmitDefaultValue=false, Order=1002)]
		internal TimeSpan? InternalRegistrationTtl
		{
			get;
			set;
		}

		[DataMember(Name="Status", IsRequired=false, EmitDefaultValue=false, Order=1016)]
		private bool? InternalStatus
		{
			get
			{
				return this.internalStatus;
			}
			set
			{
				this.internalStatus = value;
			}
		}

		[DataMember(Name="UserMetadata", IsRequired=false, EmitDefaultValue=false, Order=1010)]
		internal string InternalUserMetadata
		{
			get;
			set;
		}

		public bool IsAnonymousAccessible
		{
			get
			{
				return false;
			}
		}

		[IgnoreDataMember]
		public bool IsDisabled
		{
			get
			{
				bool? nullable = this.internalStatus;
				if (!nullable.HasValue)
				{
					return false;
				}
				return nullable.GetValueOrDefault();
			}
			set
			{
				this.internalStatus = new bool?(value);
			}
		}

		string Microsoft.ServiceBus.Messaging.IResourceDescription.CollectionName
		{
			get
			{
				return "NotificationHubs";
			}
		}

		[DataMember(Name="MpnsCredential", IsRequired=false, EmitDefaultValue=false, Order=1006)]
		public Microsoft.ServiceBus.Notifications.MpnsCredential MpnsCredential
		{
			get;
			set;
		}

		[DataMember(Name="NokiaXCredential", IsRequired=false, EmitDefaultValue=false, Order=1015)]
		internal Microsoft.ServiceBus.Notifications.NokiaXCredential NokiaXCredential
		{
			get;
			set;
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

		public TimeSpan? RegistrationTtl
		{
			get
			{
				if (!this.InternalRegistrationTtl.HasValue)
				{
					this.InternalRegistrationTtl = new TimeSpan?(Constants.DefaultRegistrationTtl);
				}
				return this.InternalRegistrationTtl;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (value.Value < Constants.MinimumRegistrationTtl)
				{
					ExceptionTrace exception = Microsoft.ServiceBus.Messaging.FxTrace.Exception;
					object obj = value;
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					object[] minimumRegistrationTtl = new object[] { Constants.MinimumRegistrationTtl };
					throw exception.ArgumentOutOfRange("value", obj, string.Format(invariantCulture, "Registration Ttl must be at least {0}", minimumRegistrationTtl));
				}
				if (value.Value > Constants.MaximumRegistrationTtl)
				{
					ExceptionTrace exceptionTrace = Microsoft.ServiceBus.Messaging.FxTrace.Exception;
					object obj1 = value;
					CultureInfo cultureInfo = CultureInfo.InvariantCulture;
					object[] maximumRegistrationTtl = new object[] { Constants.MaximumRegistrationTtl };
					throw exceptionTrace.ArgumentOutOfRange("value", obj1, string.Format(cultureInfo, "Registration Ttl must be at most {0}", maximumRegistrationTtl));
				}
				this.InternalRegistrationTtl = value;
			}
		}

		internal override bool RequiresEncryption
		{
			get
			{
				return true;
			}
		}

		[DataMember(Name="SmtpCredential", IsRequired=false, EmitDefaultValue=false, Order=1011)]
		internal Microsoft.ServiceBus.Notifications.SmtpCredential SmtpCredential
		{
			get;
			set;
		}

		public string UserMetadata
		{
			get
			{
				return this.InternalUserMetadata;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (string.IsNullOrWhiteSpace(value))
				{
					this.InternalUserMetadata = null;
					return;
				}
				if (value.Length > 1024)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentOutOfRange("UserMetadata", value.Length, SRClient.ArgumentOutOfRange(0, 1024));
				}
				this.InternalUserMetadata = value;
			}
		}

		[DataMember(Name="WnsCredential", IsRequired=false, EmitDefaultValue=false, Order=1003)]
		public Microsoft.ServiceBus.Notifications.WnsCredential WnsCredential
		{
			get;
			set;
		}

		internal NotificationHubDescription()
		{
		}

		public NotificationHubDescription(string path)
		{
			this.Path = path;
		}

		internal override bool IsValidForVersion(ApiVersion version)
		{
			if (!base.IsValidForVersion(version))
			{
				return false;
			}
			if (version < ApiVersion.Four && this.GcmCredential != null)
			{
				return false;
			}
			if (version < MinimalApiVersionFor.BaiduSupport && this.BaiduCredential != null)
			{
				return false;
			}
			if (version < MinimalApiVersionFor.AdmSupport && this.AdmCredential != null)
			{
				return false;
			}
			if (version < MinimalApiVersionFor.DisableNotificationHub && this.InternalStatus.HasValue)
			{
				return false;
			}
			return true;
		}

		private void SetAccessPassword(string accessKeyName, string password, IEnumerable<AccessRights> rights)
		{
			SharedAccessAuthorizationRule sharedAccessAuthorizationRule;
			lock (this.Authorization)
			{
				if (!this.Authorization.TryGetSharedAccessAuthorizationRule(accessKeyName, out sharedAccessAuthorizationRule))
				{
					sharedAccessAuthorizationRule = new SharedAccessAuthorizationRule(accessKeyName, password, rights);
					this.Authorization.Add(sharedAccessAuthorizationRule);
				}
				else
				{
					sharedAccessAuthorizationRule.PrimaryKey = password;
					sharedAccessAuthorizationRule.Rights = rights;
				}
			}
		}

		public void SetAccessPasswords(string fullAccessRuleName, string fullAccessPassword, string listenAccessRuleName, string listenAccessPassword)
		{
			if (string.IsNullOrWhiteSpace(fullAccessRuleName))
			{
				throw new ArgumentNullException("fullAccessRuleName");
			}
			if (string.IsNullOrWhiteSpace(fullAccessPassword))
			{
				throw new ArgumentNullException("fullAccessPassword");
			}
			if (string.IsNullOrWhiteSpace(listenAccessRuleName))
			{
				throw new ArgumentNullException("listenAccessRuleName");
			}
			if (string.IsNullOrWhiteSpace(listenAccessPassword))
			{
				throw new ArgumentNullException("listenAccessPassword");
			}
			AccessRights[] accessRightsArray = new AccessRights[] { AccessRights.Listen, AccessRights.Send, AccessRights.Manage };
			this.SetAccessPassword(fullAccessRuleName, fullAccessPassword, accessRightsArray);
			this.SetAccessPassword(listenAccessRuleName, listenAccessPassword, new AccessRights[] { AccessRights.Listen });
		}

		public void SetDefaultAccessPasswords(string fullAccessPassword, string listenAccessPassword)
		{
			if (string.IsNullOrWhiteSpace(fullAccessPassword))
			{
				throw new ArgumentNullException("fullAccessPassword");
			}
			if (string.IsNullOrWhiteSpace(listenAccessPassword))
			{
				throw new ArgumentNullException("listenAccessPassword");
			}
			AccessRights[] accessRightsArray = new AccessRights[] { AccessRights.Listen, AccessRights.Send, AccessRights.Manage };
			this.SetAccessPassword("DefaultFullSharedAccessSignature", fullAccessPassword, accessRightsArray);
			this.SetAccessPassword("DefaultListenSharedAccessSignature", listenAccessPassword, new AccessRights[] { AccessRights.Listen });
		}

		internal override void UpdateForVersion(ApiVersion version, EntityDescription existingDescription = null)
		{
			bool? internalStatus;
			Microsoft.ServiceBus.Notifications.BaiduCredential baiduCredential;
			Microsoft.ServiceBus.Notifications.AdmCredential admCredential;
			Microsoft.ServiceBus.Notifications.GcmCredential gcmCredential;
			Microsoft.ServiceBus.Notifications.MpnsCredential mpnsCredential;
			NotificationHubDescription notificationHubDescription = existingDescription as NotificationHubDescription;
			base.UpdateForVersion(version, existingDescription);
			if (version < ApiVersion.Four)
			{
				if (notificationHubDescription == null)
				{
					gcmCredential = null;
				}
				else
				{
					gcmCredential = notificationHubDescription.GcmCredential;
				}
				this.GcmCredential = gcmCredential;
				if (notificationHubDescription == null)
				{
					mpnsCredential = null;
				}
				else
				{
					mpnsCredential = notificationHubDescription.MpnsCredential;
				}
				this.MpnsCredential = mpnsCredential;
			}
			if (version < ApiVersion.Five)
			{
				this.DailyMaxActiveDevices = (long)0;
				this.DailyMaxActiveRegistrations = (long)0;
				this.DailyOperations = (long)0;
			}
			if (version < ApiVersion.Seven)
			{
				this.DailyPushes = (long)0;
				this.DailyApiCalls = (long)0;
			}
			if (version < MinimalApiVersionFor.AdmSupport)
			{
				if (notificationHubDescription == null)
				{
					admCredential = null;
				}
				else
				{
					admCredential = notificationHubDescription.AdmCredential;
				}
				this.AdmCredential = admCredential;
			}
			if (version < MinimalApiVersionFor.BaiduSupport)
			{
				if (notificationHubDescription == null)
				{
					baiduCredential = null;
				}
				else
				{
					baiduCredential = notificationHubDescription.BaiduCredential;
				}
				this.BaiduCredential = baiduCredential;
			}
			if (version < MinimalApiVersionFor.DisableNotificationHub)
			{
				if (notificationHubDescription == null)
				{
					internalStatus = null;
				}
				else
				{
					internalStatus = notificationHubDescription.InternalStatus;
				}
				this.InternalStatus = internalStatus;
			}
		}
	}
}
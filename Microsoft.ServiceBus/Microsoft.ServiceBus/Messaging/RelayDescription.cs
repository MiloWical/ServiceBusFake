using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="RelayDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class RelayDescription : EntityDescription, IResourceDescription
	{
		internal static DataContractSerializer Serializer;

		internal string Author
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

		public string CollectionName
		{
			get
			{
				return "Relays";
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
			internal set
			{
				this.InternalCreatedAt = new DateTime?(value);
			}
		}

		internal long? Id
		{
			get;
			set;
		}

		[DataMember(Name="AuthorizationRules", IsRequired=false, Order=1055, EmitDefaultValue=false)]
		internal AuthorizationRules InternalAuthorization
		{
			get;
			set;
		}

		[DataMember(Name="CreatedAt", IsRequired=false, Order=30, EmitDefaultValue=false)]
		[NonSensitiveMember]
		internal DateTime? InternalCreatedAt
		{
			get;
			set;
		}

		[DataMember(Name="IsDynamic", IsRequired=false, Order=1056, EmitDefaultValue=false)]
		[NonSensitiveMember]
		internal bool? InternalIsDynamic
		{
			get;
			set;
		}

		[DataMember(Name="IsHybridConnection", IsRequired=false, Order=1061, EmitDefaultValue=false)]
		[NonSensitiveMember]
		internal bool? InternalIsHybridConnection
		{
			get;
			set;
		}

		[DataMember(Name="ListenerCount", IsRequired=false, EmitDefaultValue=false, Order=20)]
		[NonSensitiveMember]
		internal int? InternalListenerCount
		{
			get;
			set;
		}

		[DataMember(Name="ListenerType", IsRequired=false, EmitDefaultValue=false, Order=10)]
		[NonSensitiveMember]
		internal Microsoft.ServiceBus.ListenerType? InternalListenerType
		{
			get;
			set;
		}

		[DataMember(Name="Path", IsRequired=false, EmitDefaultValue=false, Order=1)]
		[NonSensitiveMember]
		internal string InternalPath
		{
			get;
			set;
		}

		[DataMember(Name="PublishToRegistry", IsRequired=false, Order=1054, EmitDefaultValue=false)]
		[NonSensitiveMember]
		internal bool? InternalPublishToRegistry
		{
			get;
			set;
		}

		[DataMember(Name="RelayType", IsRequired=false, Order=1051, EmitDefaultValue=false)]
		[NonSensitiveMember]
		internal Microsoft.ServiceBus.RelayType? InternalRelayType
		{
			get;
			set;
		}

		[DataMember(Name="RequiresClientAuthorization", IsRequired=false, Order=1052, EmitDefaultValue=false)]
		[NonSensitiveMember]
		internal bool? InternalRequiresClientAuthorization
		{
			get;
			set;
		}

		[DataMember(Name="RequiresTransportSecurity", IsRequired=false, Order=1053, EmitDefaultValue=false)]
		[NonSensitiveMember]
		internal bool? InternalRequiresTransportSecurity
		{
			get;
			set;
		}

		[DataMember(Name="UpdatedAt", IsRequired=false, Order=40, EmitDefaultValue=false)]
		[NonSensitiveMember]
		internal DateTime? InternalUpdatedAt
		{
			get;
			set;
		}

		[DataMember(Name="UserMetadata", IsRequired=false, Order=1060, EmitDefaultValue=false)]
		[NonSensitiveMember]
		internal string InternalUserMetadata
		{
			get;
			set;
		}

		public bool IsDynamic
		{
			get
			{
				bool? internalIsDynamic = this.InternalIsDynamic;
				if (!internalIsDynamic.HasValue)
				{
					return false;
				}
				return internalIsDynamic.GetValueOrDefault();
			}
			internal set
			{
				this.InternalIsDynamic = new bool?(value);
			}
		}

		internal bool IsHybridConnection
		{
			get
			{
				bool? internalIsHybridConnection = this.InternalIsHybridConnection;
				if (!internalIsHybridConnection.HasValue)
				{
					return false;
				}
				return internalIsHybridConnection.GetValueOrDefault();
			}
			set
			{
				this.InternalIsHybridConnection = new bool?(value);
			}
		}

		public int ListenerCount
		{
			get
			{
				int? internalListenerCount = this.InternalListenerCount;
				if (!internalListenerCount.HasValue)
				{
					return 0;
				}
				return internalListenerCount.GetValueOrDefault();
			}
			internal set
			{
				this.InternalListenerCount = new int?(value);
			}
		}

		internal Microsoft.ServiceBus.ListenerType ListenerType
		{
			get
			{
				Microsoft.ServiceBus.ListenerType? internalListenerType = this.InternalListenerType;
				if (internalListenerType.HasValue)
				{
					return internalListenerType.GetValueOrDefault();
				}
				return RelayDescription.MapRelayTypeToListenerType(this.InternalRelayType);
			}
			set
			{
				this.InternalListenerType = new Microsoft.ServiceBus.ListenerType?(value);
			}
		}

		public string Path
		{
			get
			{
				return this.InternalPath;
			}
			set
			{
				if (string.IsNullOrWhiteSpace(value))
				{
					throw new ArgumentNullException("Path");
				}
				this.InternalPath = value;
			}
		}

		internal bool PublishToRegistry
		{
			get
			{
				bool? internalPublishToRegistry = this.InternalPublishToRegistry;
				if (!internalPublishToRegistry.HasValue)
				{
					return false;
				}
				return internalPublishToRegistry.GetValueOrDefault();
			}
			set
			{
				this.InternalPublishToRegistry = new bool?(value);
			}
		}

		public Microsoft.ServiceBus.RelayType RelayType
		{
			get
			{
				Microsoft.ServiceBus.RelayType? internalRelayType = this.InternalRelayType;
				if (internalRelayType.HasValue)
				{
					return internalRelayType.GetValueOrDefault();
				}
				return RelayDescription.MapListenerTypeToRelayType(this.InternalListenerType);
			}
			set
			{
				this.InternalRelayType = new Microsoft.ServiceBus.RelayType?(value);
			}
		}

		public bool RequiresClientAuthorization
		{
			get
			{
				bool? internalRequiresClientAuthorization = this.InternalRequiresClientAuthorization;
				if (!internalRequiresClientAuthorization.HasValue)
				{
					return true;
				}
				return internalRequiresClientAuthorization.GetValueOrDefault();
			}
			set
			{
				this.InternalRequiresClientAuthorization = new bool?(value);
			}
		}

		internal override bool RequiresEncryption
		{
			get
			{
				return this.Authorization.RequiresEncryption;
			}
		}

		public bool RequiresTransportSecurity
		{
			get
			{
				bool? internalRequiresTransportSecurity = this.InternalRequiresTransportSecurity;
				if (!internalRequiresTransportSecurity.HasValue)
				{
					return true;
				}
				return internalRequiresTransportSecurity.GetValueOrDefault();
			}
			set
			{
				this.InternalRequiresTransportSecurity = new bool?(value);
			}
		}

		internal Uri RuntimeAddress
		{
			get;
			set;
		}

		internal EntityStatus Status
		{
			get;
			set;
		}

		public DateTime UpdatedAt
		{
			get
			{
				DateTime? internalUpdatedAt = this.InternalUpdatedAt;
				if (!internalUpdatedAt.HasValue)
				{
					return DateTime.MinValue;
				}
				return internalUpdatedAt.GetValueOrDefault();
			}
			internal set
			{
				this.InternalUpdatedAt = new DateTime?(value);
			}
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
					throw Fx.Exception.ArgumentOutOfRange("UserMetadata", value.Length, SRClient.ArgumentOutOfRange(0, 1024));
				}
				this.InternalUserMetadata = value;
			}
		}

		static RelayDescription()
		{
			RelayDescription.Serializer = new DataContractSerializer(typeof(RelayDescription));
		}

		internal RelayDescription()
		{
		}

		public RelayDescription(string relayPath, Microsoft.ServiceBus.RelayType type)
		{
			this.Path = relayPath;
			this.RelayType = type;
		}

		internal void ClearSensitiveMembers()
		{
			MemberInfo[] members = base.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			for (int i = 0; i < (int)members.Length; i++)
			{
				MemberInfo memberInfo = members[i];
				object[] customAttributes = memberInfo.GetCustomAttributes(typeof(DataMemberAttribute), true);
				if (customAttributes != null && (int)customAttributes.Length > 0)
				{
					object[] objArray = memberInfo.GetCustomAttributes(typeof(RelayDescription.NonSensitiveMemberAttribute), true);
					if (objArray == null || (int)objArray.Length == 0)
					{
						PropertyInfo propertyInfo = memberInfo as PropertyInfo;
						PropertyInfo propertyInfo1 = propertyInfo;
						if (propertyInfo == null)
						{
							((FieldInfo)memberInfo).SetValue(this, null);
						}
						else
						{
							propertyInfo1.GetSetMethod(true).Invoke(this, new object[1]);
						}
					}
				}
			}
		}

		internal override bool IsValidForVersion(ApiVersion version)
		{
			if (version < ApiVersion.Eight && this.InternalUserMetadata != null)
			{
				return false;
			}
			if (version < ApiVersion.Five && (this.InternalRelayType.HasValue || this.InternalRequiresClientAuthorization.HasValue || this.InternalRequiresTransportSecurity.HasValue || this.InternalPublishToRegistry.HasValue || this.InternalAuthorization != null || this.InternalIsDynamic.HasValue))
			{
				return false;
			}
			return base.IsValidForVersion(version);
		}

		internal static Microsoft.ServiceBus.RelayType MapListenerTypeToRelayType(Microsoft.ServiceBus.ListenerType? listenerType)
		{
			if (!listenerType.HasValue)
			{
				return Microsoft.ServiceBus.RelayType.None;
			}
			Microsoft.ServiceBus.ListenerType valueOrDefault = listenerType.GetValueOrDefault();
			if (listenerType.HasValue)
			{
				switch (valueOrDefault)
				{
					case Microsoft.ServiceBus.ListenerType.Unicast:
					{
						return Microsoft.ServiceBus.RelayType.NetOneway;
					}
					case Microsoft.ServiceBus.ListenerType.Multicast:
					{
						return Microsoft.ServiceBus.RelayType.NetEvent;
					}
					case Microsoft.ServiceBus.ListenerType.RelayedConnection:
					{
						return Microsoft.ServiceBus.RelayType.NetTcp;
					}
					case Microsoft.ServiceBus.ListenerType.RelayedHttp:
					case Microsoft.ServiceBus.ListenerType.RoutedHttp:
					{
						return Microsoft.ServiceBus.RelayType.Http;
					}
				}
			}
			return Microsoft.ServiceBus.RelayType.None;
		}

		internal static Microsoft.ServiceBus.ListenerType MapRelayTypeToListenerType(Microsoft.ServiceBus.RelayType? relayType)
		{
			if (!relayType.HasValue)
			{
				return Microsoft.ServiceBus.ListenerType.None;
			}
			Microsoft.ServiceBus.RelayType valueOrDefault = relayType.GetValueOrDefault();
			if (relayType.HasValue)
			{
				switch (valueOrDefault)
				{
					case Microsoft.ServiceBus.RelayType.NetTcp:
					{
						return Microsoft.ServiceBus.ListenerType.RelayedConnection;
					}
					case Microsoft.ServiceBus.RelayType.Http:
					{
						return Microsoft.ServiceBus.ListenerType.RelayedHttp;
					}
					case Microsoft.ServiceBus.RelayType.NetEvent:
					{
						return Microsoft.ServiceBus.ListenerType.Multicast;
					}
					case Microsoft.ServiceBus.RelayType.NetOneway:
					{
						return Microsoft.ServiceBus.ListenerType.Unicast;
					}
				}
			}
			return Microsoft.ServiceBus.ListenerType.None;
		}

		internal override void UpdateForVersion(ApiVersion version, EntityDescription existingDescription = null)
		{
			base.UpdateForVersion(version, existingDescription);
			if (!this.InternalListenerType.HasValue && this.InternalRelayType.HasValue)
			{
				this.InternalListenerType = new Microsoft.ServiceBus.ListenerType?(RelayDescription.MapRelayTypeToListenerType(this.InternalRelayType));
			}
			if (version < ApiVersion.Eight)
			{
				this.InternalUserMetadata = null;
			}
			if (version >= ApiVersion.Five)
			{
				if (!this.InternalRelayType.HasValue && this.InternalListenerType.HasValue)
				{
					this.InternalRelayType = new Microsoft.ServiceBus.RelayType?(RelayDescription.MapListenerTypeToRelayType(this.InternalListenerType));
				}
				return;
			}
			this.InternalRelayType = null;
			this.InternalRequiresClientAuthorization = null;
			this.InternalRequiresTransportSecurity = null;
			this.InternalPublishToRegistry = null;
			this.InternalAuthorization = null;
			this.InternalIsDynamic = null;
		}

		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=false)]
		private class NonSensitiveMemberAttribute : Attribute
		{
			public NonSensitiveMemberAttribute()
			{
			}
		}
	}
}
using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="ServiceSettings", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	internal class ServiceSettings : IExtensibleDataObject
	{
		[DataMember(Name="Discovery", IsRequired=false, EmitDefaultValue=false, Order=0)]
		private DiscoveryType? discovery;

		[DataMember(Name="IsDiscoverable", IsRequired=false, EmitDefaultValue=false, Order=1)]
		private bool? isDiscoverable;

		[DataMember(Name="ClientAuthenticationType", IsRequired=false, EmitDefaultValue=false, Order=2)]
		private Microsoft.ServiceBus.RelayClientAuthenticationType? clientAuthenticationType;

		[DataMember(Name="ListenerType", IsRequired=false, EmitDefaultValue=false, Order=3)]
		private Microsoft.ServiceBus.ListenerType? listenerType;

		[DataMember(Name="Address", IsRequired=false, EmitDefaultValue=false, Order=4)]
		private EndpointAddress10 address;

		[DataMember(Name="Uri", IsRequired=false, EmitDefaultValue=false, Order=5)]
		private Uri webUri;

		[DataMember(Name="TransportProtection", IsRequired=false, EmitDefaultValue=false, Order=6)]
		private RelayTransportProtectionMode? transportProtection;

		[DataMember(Name="PreserveRawHttp", IsRequired=false, EmitDefaultValue=false, Order=7)]
		private bool? preserveRawHttp;

		[DataMember(Name="IsDynamic", IsRequired=false, EmitDefaultValue=true, Order=8)]
		private bool? isDynamic;

		[DataMember(Name="ClientAgent", IsRequired=false, EmitDefaultValue=false, Order=9)]
		private string clientAgent;

		private ExtensionDataObject extensionData;

		[Obsolete]
		public EndpointAddress Address
		{
			get
			{
				if (this.address == null)
				{
					return null;
				}
				return this.address.ToEndpointAddress();
			}
			set
			{
				if (value != null)
				{
					this.address = EndpointAddress10.FromEndpointAddress(value);
					this.webUri = value.Uri;
					return;
				}
				if (this.address != null)
				{
					this.webUri = null;
				}
				this.address = null;
			}
		}

		public EndpointAddress10 Address10
		{
			get
			{
				return this.address;
			}
		}

		public string ClientAgent
		{
			get
			{
				return this.clientAgent;
			}
			set
			{
				this.clientAgent = value;
			}
		}

		public DiscoveryType Discovery
		{
			get
			{
				if (!this.discovery.HasValue)
				{
					return DiscoveryType.Private;
				}
				return this.discovery.Value;
			}
			set
			{
				this.discovery = new DiscoveryType?(value);
				this.isDiscoverable = ServiceSettings.ComputeIsDiscoverable(this.discovery);
			}
		}

		internal bool HasNoListener
		{
			get
			{
				Microsoft.ServiceBus.ListenerType? nullable = this.listenerType;
				if (nullable.GetValueOrDefault() != Microsoft.ServiceBus.ListenerType.None)
				{
					return false;
				}
				return nullable.HasValue;
			}
		}

		internal bool IsDiscoverable
		{
			get
			{
				if (!this.isDiscoverable.HasValue)
				{
					return false;
				}
				return this.isDiscoverable.Value;
			}
		}

		public bool IsDynamic
		{
			get
			{
				bool? nullable = this.isDynamic;
				if (!nullable.HasValue)
				{
					return true;
				}
				return nullable.GetValueOrDefault();
			}
			set
			{
				this.isDynamic = new bool?(value);
			}
		}

		internal bool IsJunction
		{
			get
			{
				Microsoft.ServiceBus.ListenerType? nullable = this.listenerType;
				if (nullable.GetValueOrDefault() != Microsoft.ServiceBus.ListenerType.Junction)
				{
					return false;
				}
				return nullable.HasValue;
			}
		}

		internal bool IsMulticastListener
		{
			get
			{
				Microsoft.ServiceBus.ListenerType? nullable = this.listenerType;
				if (nullable.GetValueOrDefault() != Microsoft.ServiceBus.ListenerType.Multicast)
				{
					return false;
				}
				return nullable.HasValue;
			}
		}

		internal bool IsUnicastListener
		{
			get
			{
				Microsoft.ServiceBus.ListenerType? nullable = this.listenerType;
				if (nullable.GetValueOrDefault() != Microsoft.ServiceBus.ListenerType.Unicast)
				{
					return false;
				}
				return nullable.HasValue;
			}
		}

		internal Microsoft.ServiceBus.ListenerType ListenerType
		{
			get
			{
				if (!this.listenerType.HasValue)
				{
					return Microsoft.ServiceBus.ListenerType.None;
				}
				return this.listenerType.Value;
			}
			set
			{
				this.listenerType = new Microsoft.ServiceBus.ListenerType?(value);
			}
		}

		public bool PreserveRawHttp
		{
			get
			{
				if (!this.preserveRawHttp.HasValue)
				{
					return false;
				}
				return this.preserveRawHttp.Value;
			}
			set
			{
				if (!value)
				{
					this.preserveRawHttp = null;
					return;
				}
				this.preserveRawHttp = new bool?(value);
			}
		}

		internal Microsoft.ServiceBus.RelayClientAuthenticationType RelayClientAuthenticationType
		{
			get
			{
				if (!this.clientAuthenticationType.HasValue)
				{
					return Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken;
				}
				return this.clientAuthenticationType.Value;
			}
			set
			{
				this.clientAuthenticationType = new Microsoft.ServiceBus.RelayClientAuthenticationType?(value);
			}
		}

		ExtensionDataObject System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
		{
			get
			{
				return this.extensionData;
			}
			set
			{
				this.extensionData = value;
			}
		}

		public RelayTransportProtectionMode TransportProtection
		{
			get
			{
				if (!this.transportProtection.HasValue)
				{
					return RelayTransportProtectionMode.None;
				}
				return this.transportProtection.Value;
			}
			set
			{
				this.transportProtection = new RelayTransportProtectionMode?(value);
			}
		}

		public Uri WebUri
		{
			get
			{
				return this.webUri;
			}
			set
			{
				this.webUri = value;
			}
		}

		public ServiceSettings()
		{
			this.clientAuthenticationType = new Microsoft.ServiceBus.RelayClientAuthenticationType?(Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken);
			this.discovery = new DiscoveryType?(DiscoveryType.Private);
			this.listenerType = new Microsoft.ServiceBus.ListenerType?(Microsoft.ServiceBus.ListenerType.None);
			this.transportProtection = new RelayTransportProtectionMode?(RelayTransportProtectionMode.None);
			this.isDynamic = new bool?(true);
		}

		public ServiceSettings(ServiceSettings serviceSettings)
		{
			this.discovery = serviceSettings.discovery;
			this.isDiscoverable = serviceSettings.isDiscoverable;
			this.clientAuthenticationType = serviceSettings.clientAuthenticationType;
			this.listenerType = serviceSettings.listenerType;
			this.address = serviceSettings.address;
			this.webUri = serviceSettings.webUri;
			this.transportProtection = serviceSettings.transportProtection;
			this.preserveRawHttp = serviceSettings.preserveRawHttp;
			this.isDynamic = serviceSettings.isDynamic;
			this.clientAgent = serviceSettings.clientAgent;
		}

		private static bool? ComputeIsDiscoverable(DiscoveryType? discovery)
		{
			if (!discovery.HasValue)
			{
				return new bool?(false);
			}
			bool flag = false;
			switch (discovery.Value)
			{
				case DiscoveryType.Public:
				{
					flag = true;
					break;
				}
				case DiscoveryType.Private:
				{
					flag = false;
					break;
				}
			}
			return new bool?(flag);
		}

		internal bool IsCompatible(ServiceSettings serviceSettings)
		{
			DiscoveryType? nullable = this.discovery;
			DiscoveryType? nullable1 = serviceSettings.discovery;
			if ((nullable.GetValueOrDefault() != nullable1.GetValueOrDefault() ? false : nullable.HasValue == nullable1.HasValue))
			{
				Microsoft.ServiceBus.RelayClientAuthenticationType? nullable2 = this.clientAuthenticationType;
				Microsoft.ServiceBus.RelayClientAuthenticationType? nullable3 = serviceSettings.clientAuthenticationType;
				if ((nullable2.GetValueOrDefault() != nullable3.GetValueOrDefault() ? false : nullable2.HasValue == nullable3.HasValue))
				{
					Microsoft.ServiceBus.ListenerType? nullable4 = this.listenerType;
					Microsoft.ServiceBus.ListenerType? nullable5 = serviceSettings.listenerType;
					if ((nullable4.GetValueOrDefault() != nullable5.GetValueOrDefault() ? false : nullable4.HasValue == nullable5.HasValue))
					{
						RelayTransportProtectionMode? nullable6 = this.transportProtection;
						RelayTransportProtectionMode? nullable7 = serviceSettings.transportProtection;
						if ((nullable6.GetValueOrDefault() != nullable7.GetValueOrDefault() ? false : nullable6.HasValue == nullable7.HasValue))
						{
							bool? nullable8 = this.isDynamic;
							bool? nullable9 = serviceSettings.isDynamic;
							if ((nullable8.GetValueOrDefault() != nullable9.GetValueOrDefault() ? false : nullable8.HasValue == nullable9.HasValue))
							{
								bool? nullable10 = this.preserveRawHttp;
								bool? nullable11 = serviceSettings.preserveRawHttp;
								if (nullable10.GetValueOrDefault() != nullable11.GetValueOrDefault())
								{
									return false;
								}
								return nullable10.HasValue == nullable11.HasValue;
							}
						}
					}
				}
			}
			return false;
		}

		public bool IsTcpListener()
		{
			Microsoft.ServiceBus.ListenerType? nullable = this.listenerType;
			if ((nullable.GetValueOrDefault() != Microsoft.ServiceBus.ListenerType.RelayedConnection ? true : !nullable.HasValue))
			{
				Microsoft.ServiceBus.ListenerType? nullable1 = this.listenerType;
				if ((nullable1.GetValueOrDefault() != Microsoft.ServiceBus.ListenerType.DirectConnection ? true : !nullable1.HasValue))
				{
					Microsoft.ServiceBus.ListenerType? nullable2 = this.listenerType;
					if (nullable2.GetValueOrDefault() != Microsoft.ServiceBus.ListenerType.HybridConnection)
					{
						return false;
					}
					return nullable2.HasValue;
				}
			}
			return true;
		}
	}
}
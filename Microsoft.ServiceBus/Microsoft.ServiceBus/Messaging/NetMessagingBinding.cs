using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Channels;
using Microsoft.ServiceBus.Messaging.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class NetMessagingBinding : Binding
	{
		private BinaryMessageEncodingBindingElement encoder;

		private NetMessagingTransportBindingElement transport;

		[DefaultValue(524288L)]
		public long MaxBufferPoolSize
		{
			get
			{
				return this.transport.MaxBufferPoolSize;
			}
			set
			{
				this.transport.MaxBufferPoolSize = value;
			}
		}

		[DefaultValue(-1)]
		public int PrefetchCount
		{
			get
			{
				return this.transport.PrefetchCount;
			}
			set
			{
				this.transport.PrefetchCount = value;
			}
		}

		public override string Scheme
		{
			get
			{
				return "sb";
			}
		}

		[DefaultValue(typeof(TimeSpan), "00:01:00")]
		public TimeSpan SessionIdleTimeout
		{
			get
			{
				return this.transport.SessionIdleTimeout;
			}
			set
			{
				this.transport.SessionIdleTimeout = value;
			}
		}

		public NetMessagingTransportSettings TransportSettings
		{
			get
			{
				return this.transport.TransportSettings;
			}
			set
			{
				if (value == null)
				{
					throw FxTrace.Exception.ArgumentNull("value");
				}
				this.transport.TransportSettings = value;
			}
		}

		public NetMessagingBinding()
		{
			this.encoder = Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.CreateDefaultEncoder();
			this.transport = new NetMessagingTransportBindingElement();
		}

		public NetMessagingBinding(string configurationName) : this()
		{
			this.ApplyConfiguration(configurationName);
		}

		private void ApplyConfiguration(string configurationName)
		{
			NetMessagingBindingExtensionElement item = NetMessagingBindingCollectionElement.GetBindingCollectionElement().Bindings[configurationName];
			if (item == null)
			{
				string str = SRClient.ConfigInvalidBindingConfigurationName(configurationName, "netMessagingBinding");
				throw FxTrace.Exception.AsError(new ConfigurationErrorsException(str), null);
			}
			item.ApplyConfiguration(this);
		}

		public override BindingElementCollection CreateBindingElements()
		{
			return (new BindingElementCollection()
			{
				this.encoder,
				this.transport
			}).Clone();
		}

		private void InitializeFrom(BinaryMessageEncodingBindingElement binaryEncoding, NetMessagingTransportBindingElement netMessagingTransport)
		{
			this.transport.MaxBufferPoolSize = netMessagingTransport.MaxBufferPoolSize;
			this.transport.MaxReceivedMessageSize = netMessagingTransport.MaxReceivedMessageSize;
			binaryEncoding.ReaderQuotas.CopyTo(this.encoder.ReaderQuotas);
		}

		private bool IsBindingElementsMatch(BinaryMessageEncodingBindingElement binaryEncoding, NetMessagingTransportBindingElement netMessagingTransport)
		{
			if (!NetMessagingBinding.IsTransportMatch(this.transport, netMessagingTransport))
			{
				return false;
			}
			if (!NetMessagingBinding.IsEncodingMatch(this.encoder, binaryEncoding))
			{
				return false;
			}
			return true;
		}

		private static bool IsEncodingMatch(BindingElement a, BindingElement b)
		{
			if (b == null)
			{
				return false;
			}
			if (!(b is MessageEncodingBindingElement))
			{
				return false;
			}
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = a as BinaryMessageEncodingBindingElement;
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement1 = b as BinaryMessageEncodingBindingElement;
			if (binaryMessageEncodingBindingElement1 == null)
			{
				return false;
			}
			if (binaryMessageEncodingBindingElement.MaxReadPoolSize != binaryMessageEncodingBindingElement1.MaxReadPoolSize)
			{
				return false;
			}
			if (binaryMessageEncodingBindingElement.MaxWritePoolSize != binaryMessageEncodingBindingElement1.MaxWritePoolSize)
			{
				return false;
			}
			if (binaryMessageEncodingBindingElement.ReaderQuotas.MaxStringContentLength != binaryMessageEncodingBindingElement1.ReaderQuotas.MaxStringContentLength)
			{
				return false;
			}
			if (binaryMessageEncodingBindingElement.ReaderQuotas.MaxArrayLength != binaryMessageEncodingBindingElement1.ReaderQuotas.MaxArrayLength)
			{
				return false;
			}
			if (binaryMessageEncodingBindingElement.ReaderQuotas.MaxBytesPerRead != binaryMessageEncodingBindingElement1.ReaderQuotas.MaxBytesPerRead)
			{
				return false;
			}
			if (binaryMessageEncodingBindingElement.ReaderQuotas.MaxDepth != binaryMessageEncodingBindingElement1.ReaderQuotas.MaxDepth)
			{
				return false;
			}
			if (binaryMessageEncodingBindingElement.ReaderQuotas.MaxNameTableCharCount != binaryMessageEncodingBindingElement1.ReaderQuotas.MaxNameTableCharCount)
			{
				return false;
			}
			if (!NetMessagingBinding.IsMessageVersionMatch(binaryMessageEncodingBindingElement.MessageVersion, binaryMessageEncodingBindingElement1.MessageVersion))
			{
				return false;
			}
			return true;
		}

		private static bool IsMessageVersionMatch(System.ServiceModel.Channels.MessageVersion a, System.ServiceModel.Channels.MessageVersion b)
		{
			if (b == null)
			{
				throw FxTrace.Exception.ArgumentNull("b");
			}
			if (a.Addressing == null)
			{
				throw FxTrace.Exception.AsError(new InvalidOperationException("MessageVersion.Addressing cannot be null"), null);
			}
			if (a.Envelope != b.Envelope)
			{
				return false;
			}
			return true;
		}

		private static bool IsTransportMatch(BindingElement a, BindingElement b)
		{
			if (b == null)
			{
				return false;
			}
			NetMessagingTransportBindingElement netMessagingTransportBindingElement = a as NetMessagingTransportBindingElement;
			NetMessagingTransportBindingElement netMessagingTransportBindingElement1 = b as NetMessagingTransportBindingElement;
			if (netMessagingTransportBindingElement1 == null)
			{
				return false;
			}
			if (netMessagingTransportBindingElement.MaxBufferPoolSize != netMessagingTransportBindingElement1.MaxBufferPoolSize)
			{
				return false;
			}
			if (netMessagingTransportBindingElement.MaxReceivedMessageSize != netMessagingTransportBindingElement1.MaxReceivedMessageSize)
			{
				return false;
			}
			return true;
		}

		internal static bool TryCreate(BindingElementCollection elements, out NetMessagingBinding binding)
		{
			bool flag;
			binding = null;
			if (elements.Count > 2)
			{
				return false;
			}
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = null;
			NetMessagingTransportBindingElement netMessagingTransportBindingElement = null;
			using (IEnumerator<BindingElement> enumerator = elements.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					BindingElement current = enumerator.Current;
					if (current is TransportBindingElement)
					{
						netMessagingTransportBindingElement = current as NetMessagingTransportBindingElement;
					}
					else if (!(current is BinaryMessageEncodingBindingElement))
					{
						flag = false;
						return flag;
					}
					else
					{
						binaryMessageEncodingBindingElement = current as BinaryMessageEncodingBindingElement;
					}
				}
				if (netMessagingTransportBindingElement == null)
				{
					return false;
				}
				if (binaryMessageEncodingBindingElement == null)
				{
					return false;
				}
				NetMessagingBinding netMessagingBinding = new NetMessagingBinding();
				netMessagingBinding.InitializeFrom(binaryMessageEncodingBindingElement, netMessagingTransportBindingElement);
				if (!netMessagingBinding.IsBindingElementsMatch(binaryMessageEncodingBindingElement, netMessagingTransportBindingElement))
				{
					return false;
				}
				binding = netMessagingBinding;
				return true;
			}
			return flag;
		}
	}
}
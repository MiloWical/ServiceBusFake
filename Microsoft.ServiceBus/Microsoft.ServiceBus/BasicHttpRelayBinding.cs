using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Configuration;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus
{
	public class BasicHttpRelayBinding : Binding, IBindingRuntimePreferences
	{
		private WSMessageEncoding messageEncoding;

		private HttpRelayTransportBindingElement httpTransport;

		private HttpsRelayTransportBindingElement httpsTransport;

		private TextMessageEncodingBindingElement textEncoding;

		private MtomMessageEncodingBindingElement mtomEncoding;

		private BasicHttpRelaySecurity security = new BasicHttpRelaySecurity();

		public bool AllowCookies
		{
			get
			{
				return this.httpTransport.AllowCookies;
			}
			set
			{
				this.httpTransport.AllowCookies = value;
				this.httpsTransport.AllowCookies = value;
			}
		}

		public System.ServiceModel.EnvelopeVersion EnvelopeVersion
		{
			get
			{
				return System.ServiceModel.EnvelopeVersion.Soap11;
			}
		}

		public System.ServiceModel.HostNameComparisonMode HostNameComparisonMode
		{
			get
			{
				return this.httpTransport.HostNameComparisonMode;
			}
			set
			{
				this.httpTransport.HostNameComparisonMode = value;
				this.httpsTransport.HostNameComparisonMode = value;
			}
		}

		public bool IsDynamic
		{
			get
			{
				return this.httpTransport.IsDynamic;
			}
			set
			{
				this.httpTransport.IsDynamic = value;
				this.httpsTransport.IsDynamic = value;
			}
		}

		public long MaxBufferPoolSize
		{
			get
			{
				return this.httpTransport.MaxBufferPoolSize;
			}
			set
			{
				this.httpTransport.MaxBufferPoolSize = value;
				this.httpsTransport.MaxBufferPoolSize = value;
			}
		}

		public int MaxBufferSize
		{
			get
			{
				return this.httpTransport.MaxBufferSize;
			}
			set
			{
				this.httpTransport.MaxBufferSize = value;
				this.httpsTransport.MaxBufferSize = value;
				this.mtomEncoding.MaxBufferSize = value;
			}
		}

		public long MaxReceivedMessageSize
		{
			get
			{
				return this.httpTransport.MaxReceivedMessageSize;
			}
			set
			{
				this.httpTransport.MaxReceivedMessageSize = value;
				this.httpsTransport.MaxReceivedMessageSize = value;
			}
		}

		public WSMessageEncoding MessageEncoding
		{
			get
			{
				return this.messageEncoding;
			}
			set
			{
				this.messageEncoding = value;
			}
		}

		public Uri ProxyAddress
		{
			get
			{
				return this.httpTransport.ProxyAddress;
			}
			set
			{
				this.httpTransport.ProxyAddress = value;
				this.httpsTransport.ProxyAddress = value;
			}
		}

		public XmlDictionaryReaderQuotas ReaderQuotas
		{
			get
			{
				return this.textEncoding.ReaderQuotas;
			}
			set
			{
				if (value == null)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
				}
				value.CopyTo(this.textEncoding.ReaderQuotas);
				value.CopyTo(this.mtomEncoding.ReaderQuotas);
			}
		}

		public override string Scheme
		{
			get
			{
				return this.GetTransport().Scheme;
			}
		}

		public BasicHttpRelaySecurity Security
		{
			get
			{
				return this.security;
			}
		}

		bool System.ServiceModel.Channels.IBindingRuntimePreferences.ReceiveSynchronously
		{
			get
			{
				return false;
			}
		}

		public Encoding TextEncoding
		{
			get
			{
				return this.textEncoding.WriteEncoding;
			}
			set
			{
				this.textEncoding.WriteEncoding = value;
				this.mtomEncoding.WriteEncoding = value;
			}
		}

		public System.ServiceModel.TransferMode TransferMode
		{
			get
			{
				return this.httpTransport.TransferMode;
			}
			set
			{
				this.httpTransport.TransferMode = value;
				this.httpsTransport.TransferMode = value;
			}
		}

		public bool UseDefaultWebProxy
		{
			get
			{
				return this.httpTransport.UseDefaultWebProxy;
			}
			set
			{
				this.httpTransport.UseDefaultWebProxy = value;
				this.httpsTransport.UseDefaultWebProxy = value;
			}
		}

		public BasicHttpRelayBinding() : this(EndToEndBasicHttpSecurityMode.Transport, RelayClientAuthenticationType.RelayAccessToken)
		{
		}

		public BasicHttpRelayBinding(string configurationName) : this()
		{
			this.ApplyConfiguration(configurationName);
		}

		public BasicHttpRelayBinding(EndToEndBasicHttpSecurityMode securityMode, RelayClientAuthenticationType relayClientAuthenticationType)
		{
			this.Initialize();
			this.security.RelayClientAuthenticationType = relayClientAuthenticationType;
			this.security.Mode = securityMode;
		}

		private BasicHttpRelayBinding(BasicHttpRelaySecurity security)
		{
			this.Initialize();
			this.security = security;
		}

		private void ApplyConfiguration(string configurationName)
		{
			BasicHttpRelayBindingElement item = BasicHttpRelayBindingCollectionElement.GetBindingCollectionElement().Bindings[configurationName];
			if (item == null)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string configInvalidBindingConfigurationName = Resources.ConfigInvalidBindingConfigurationName;
				object[] objArray = new object[] { configurationName, "basicHttpRelayBinding" };
				throw exceptionUtility.ThrowHelperError(new ConfigurationErrorsException(Microsoft.ServiceBus.SR.GetString(configInvalidBindingConfigurationName, objArray)));
			}
			item.ApplyConfiguration(this);
		}

		public override BindingElementCollection CreateBindingElements()
		{
			BindingElementCollection bindingElementCollection = new BindingElementCollection();
			SecurityBindingElement securityBindingElement = this.CreateMessageSecurity();
			if (securityBindingElement != null)
			{
				bindingElementCollection.Add(securityBindingElement);
			}
			Microsoft.ServiceBus.WSMessageEncodingHelper.SyncUpEncodingBindingElementProperties(this.textEncoding, this.mtomEncoding);
			if (this.MessageEncoding == WSMessageEncoding.Text)
			{
				bindingElementCollection.Add(this.textEncoding);
			}
			else if (this.MessageEncoding == WSMessageEncoding.Mtom)
			{
				bindingElementCollection.Add(this.mtomEncoding);
			}
			bindingElementCollection.Add(this.GetTransport());
			return bindingElementCollection.Clone();
		}

		private SecurityBindingElement CreateMessageSecurity()
		{
			return this.security.CreateMessageSecurity();
		}

		private static bool GetSecurityModeFromTransport(HttpRelayTransportBindingElement http, HttpRelayTransportSecurity transportSecurity, out Microsoft.ServiceBus.UnifiedSecurityMode mode)
		{
			mode = Microsoft.ServiceBus.UnifiedSecurityMode.None;
			if (http == null)
			{
				return false;
			}
			if (!(http is HttpsRelayTransportBindingElement))
			{
				if (!HttpRelayTransportSecurity.IsDisabledTransportAuthentication(http))
				{
					return false;
				}
				mode = Microsoft.ServiceBus.UnifiedSecurityMode.None | Microsoft.ServiceBus.UnifiedSecurityMode.Message;
			}
			else
			{
				mode = Microsoft.ServiceBus.UnifiedSecurityMode.Transport | Microsoft.ServiceBus.UnifiedSecurityMode.TransportWithMessageCredential;
				BasicHttpRelaySecurity.EnableTransportSecurity((HttpsRelayTransportBindingElement)http, transportSecurity);
			}
			return true;
		}

		private TransportBindingElement GetTransport()
		{
			if (this.security.Mode != EndToEndBasicHttpSecurityMode.Transport && this.security.Mode != EndToEndBasicHttpSecurityMode.TransportWithMessageCredential && (this.security.Mode != EndToEndBasicHttpSecurityMode.Message || this.security.RelayClientAuthenticationType != RelayClientAuthenticationType.RelayAccessToken))
			{
				this.security.DisableTransportAuthentication(this.httpTransport);
				this.httpTransport.RelayClientAuthenticationType = this.Security.RelayClientAuthenticationType;
				return this.httpTransport;
			}
			this.security.EnableTransportSecurity(this.httpsTransport);
			this.httpsTransport.RelayClientAuthenticationType = this.Security.RelayClientAuthenticationType;
			return this.httpsTransport;
		}

		private void Initialize()
		{
			this.httpTransport = new HttpRelayTransportBindingElement();
			this.httpsTransport = new HttpsRelayTransportBindingElement();
			this.messageEncoding = WSMessageEncoding.Text;
			this.textEncoding = new TextMessageEncodingBindingElement()
			{
				MessageVersion = System.ServiceModel.Channels.MessageVersion.Soap11
			};
			this.mtomEncoding = new MtomMessageEncodingBindingElement()
			{
				MessageVersion = System.ServiceModel.Channels.MessageVersion.Soap11
			};
		}

		private void InitializeFrom(HttpRelayTransportBindingElement transport, MessageEncodingBindingElement encoding)
		{
			this.MaxBufferPoolSize = transport.MaxBufferPoolSize;
			this.MaxBufferSize = transport.MaxBufferSize;
			this.MaxReceivedMessageSize = transport.MaxReceivedMessageSize;
			this.ProxyAddress = transport.ProxyAddress;
			this.TransferMode = transport.TransferMode;
			this.UseDefaultWebProxy = transport.UseDefaultWebProxy;
			if (encoding is TextMessageEncodingBindingElement)
			{
				this.MessageEncoding = WSMessageEncoding.Text;
				TextMessageEncodingBindingElement textMessageEncodingBindingElement = (TextMessageEncodingBindingElement)encoding;
				this.TextEncoding = textMessageEncodingBindingElement.WriteEncoding;
				this.ReaderQuotas = textMessageEncodingBindingElement.ReaderQuotas;
				return;
			}
			if (encoding is MtomMessageEncodingBindingElement)
			{
				this.messageEncoding = WSMessageEncoding.Mtom;
				MtomMessageEncodingBindingElement mtomMessageEncodingBindingElement = (MtomMessageEncodingBindingElement)encoding;
				this.TextEncoding = mtomMessageEncodingBindingElement.WriteEncoding;
				this.ReaderQuotas = mtomMessageEncodingBindingElement.ReaderQuotas;
			}
		}

		private bool IsBindingElementsMatch(HttpRelayTransportBindingElement transport, MessageEncodingBindingElement encoding)
		{
			if (this.MessageEncoding == WSMessageEncoding.Text)
			{
				Type type = typeof(BindingElement);
				TextMessageEncodingBindingElement textMessageEncodingBindingElement = this.textEncoding;
				object[] objArray = new object[] { encoding };
				if (!(bool)InvokeHelper.InvokeInstanceMethod(type, textMessageEncodingBindingElement, "IsMatch", objArray))
				{
					return false;
				}
			}
			else if (this.MessageEncoding == WSMessageEncoding.Mtom)
			{
				Type type1 = typeof(BindingElement);
				MtomMessageEncodingBindingElement mtomMessageEncodingBindingElement = this.mtomEncoding;
				object[] objArray1 = new object[] { encoding };
				if (!(bool)InvokeHelper.InvokeInstanceMethod(type1, mtomMessageEncodingBindingElement, "IsMatch", objArray1))
				{
					return false;
				}
			}
			Type type2 = typeof(BindingElement);
			TransportBindingElement transportBindingElement = this.GetTransport();
			object[] objArray2 = new object[] { transport };
			if (!(bool)InvokeHelper.InvokeInstanceMethod(type2, transportBindingElement, "IsMatch", objArray2))
			{
				return false;
			}
			return true;
		}

		internal static bool TryCreate(BindingElementCollection elements, out Binding binding)
		{
			Microsoft.ServiceBus.UnifiedSecurityMode unifiedSecurityMode;
			BasicHttpRelaySecurity basicHttpRelaySecurity;
			bool flag;
			binding = null;
			if (elements.Count > 3)
			{
				return false;
			}
			SecurityBindingElement securityBindingElement = null;
			MessageEncodingBindingElement messageEncodingBindingElement = null;
			HttpRelayTransportBindingElement httpRelayTransportBindingElement = null;
			using (IEnumerator<BindingElement> enumerator = elements.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					BindingElement current = enumerator.Current;
					if (current is SecurityBindingElement)
					{
						securityBindingElement = current as SecurityBindingElement;
					}
					else if (current is HttpsRelayTransportBindingElement)
					{
						httpRelayTransportBindingElement = current as HttpsRelayTransportBindingElement;
					}
					else if (current is HttpRelayTransportBindingElement)
					{
						httpRelayTransportBindingElement = current as HttpRelayTransportBindingElement;
					}
					else if (!(current is MessageEncodingBindingElement))
					{
						flag = false;
						return flag;
					}
					else
					{
						messageEncodingBindingElement = current as MessageEncodingBindingElement;
					}
				}
				HttpRelayTransportSecurity httpRelayTransportSecurity = new HttpRelayTransportSecurity();
				if (!BasicHttpRelayBinding.GetSecurityModeFromTransport(httpRelayTransportBindingElement, httpRelayTransportSecurity, out unifiedSecurityMode))
				{
					return false;
				}
				if (messageEncodingBindingElement == null)
				{
					return false;
				}
				if (messageEncodingBindingElement.MessageVersion.Envelope != System.ServiceModel.EnvelopeVersion.Soap11)
				{
					return false;
				}
				if (!BasicHttpRelayBinding.TryCreateSecurity(securityBindingElement, httpRelayTransportBindingElement.RelayClientAuthenticationType, unifiedSecurityMode, httpRelayTransportSecurity, out basicHttpRelaySecurity))
				{
					return false;
				}
				BasicHttpRelayBinding basicHttpRelayBinding = new BasicHttpRelayBinding(basicHttpRelaySecurity);
				basicHttpRelayBinding.InitializeFrom(httpRelayTransportBindingElement, messageEncodingBindingElement);
				if (!basicHttpRelayBinding.IsBindingElementsMatch(httpRelayTransportBindingElement, messageEncodingBindingElement))
				{
					return false;
				}
				binding = basicHttpRelayBinding;
				return true;
			}
			return flag;
		}

		private static bool TryCreateSecurity(SecurityBindingElement securityElement, RelayClientAuthenticationType relayClientAuthenticationType, Microsoft.ServiceBus.UnifiedSecurityMode mode, HttpRelayTransportSecurity transportSecurity, out BasicHttpRelaySecurity security)
		{
			return BasicHttpRelaySecurity.TryCreate(securityElement, relayClientAuthenticationType, mode, transportSecurity, out security);
		}
	}
}
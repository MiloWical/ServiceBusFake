using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus
{
	public abstract class WSHttpRelayBindingBase : Binding, IBindingRuntimePreferences
	{
		private WSMessageEncoding messageEncoding;

		private OptionalReliableSession reliableSession;

		private HttpRelayTransportBindingElement httpTransport;

		private HttpsRelayTransportBindingElement httpsTransport;

		private TextMessageEncodingBindingElement textEncoding;

		private MtomMessageEncodingBindingElement mtomEncoding;

		private System.ServiceModel.Channels.ReliableSessionBindingElement session;

		public System.ServiceModel.EnvelopeVersion EnvelopeVersion
		{
			get
			{
				return System.ServiceModel.EnvelopeVersion.Soap12;
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

		internal HttpsRelayTransportBindingElement HttpsTransport
		{
			get
			{
				return this.httpsTransport;
			}
		}

		internal HttpRelayTransportBindingElement HttpTransport
		{
			get
			{
				return this.httpTransport;
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

		public long MaxReceivedMessageSize
		{
			get
			{
				return this.httpTransport.MaxReceivedMessageSize;
			}
			set
			{
				if (value > (long)2147483647)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", Microsoft.ServiceBus.SR.GetString(Resources.MaxReceivedMessageSizeMustBeInIntegerRange, new object[0])));
				}
				this.httpTransport.MaxReceivedMessageSize = value;
				this.httpsTransport.MaxReceivedMessageSize = value;
				this.mtomEncoding.MaxBufferSize = (int)value;
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

		internal abstract Microsoft.ServiceBus.RelayClientAuthenticationType RelayClientAuthenticationType
		{
			get;
		}

		public OptionalReliableSession ReliableSession
		{
			get
			{
				return this.reliableSession;
			}
		}

		internal System.ServiceModel.Channels.ReliableSessionBindingElement ReliableSessionBindingElement
		{
			get
			{
				return this.session;
			}
		}

		public override string Scheme
		{
			get
			{
				return this.GetTransport().Scheme;
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

		protected WSHttpRelayBindingBase()
		{
			this.Initialize();
		}

		protected WSHttpRelayBindingBase(bool reliableSessionEnabled) : this()
		{
			this.ReliableSession.Enabled = reliableSessionEnabled;
		}

		public override BindingElementCollection CreateBindingElements()
		{
			BindingElementCollection bindingElementCollection = new BindingElementCollection();
			if (this.reliableSession.Enabled)
			{
				bindingElementCollection.Add(this.session);
			}
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

		protected abstract SecurityBindingElement CreateMessageSecurity();

		protected abstract TransportBindingElement GetTransport();

		private void Initialize()
		{
			WSHttpBinding wSHttpBinding = new WSHttpBinding();
			this.httpTransport = new HttpRelayTransportBindingElement();
			this.httpsTransport = new HttpsRelayTransportBindingElement();
			this.messageEncoding = wSHttpBinding.MessageEncoding;
			this.session = new System.ServiceModel.Channels.ReliableSessionBindingElement(true);
			this.textEncoding = new TextMessageEncodingBindingElement()
			{
				MessageVersion = System.ServiceModel.Channels.MessageVersion.Soap12WSAddressing10
			};
			this.mtomEncoding = new MtomMessageEncodingBindingElement()
			{
				MessageVersion = System.ServiceModel.Channels.MessageVersion.Soap12WSAddressing10
			};
			this.reliableSession = new OptionalReliableSession(this.session);
		}

		private void InitializeFrom(HttpRelayTransportBindingElement transport, MessageEncodingBindingElement encoding, System.ServiceModel.Channels.ReliableSessionBindingElement session)
		{
			this.MaxBufferPoolSize = transport.MaxBufferPoolSize;
			this.MaxReceivedMessageSize = transport.MaxReceivedMessageSize;
			this.ProxyAddress = transport.ProxyAddress;
			this.UseDefaultWebProxy = transport.UseDefaultWebProxy;
			if (encoding is TextMessageEncodingBindingElement)
			{
				this.MessageEncoding = WSMessageEncoding.Text;
				TextMessageEncodingBindingElement textMessageEncodingBindingElement = (TextMessageEncodingBindingElement)encoding;
				this.TextEncoding = textMessageEncodingBindingElement.WriteEncoding;
				this.ReaderQuotas = textMessageEncodingBindingElement.ReaderQuotas;
			}
			else if (encoding is MtomMessageEncodingBindingElement)
			{
				this.messageEncoding = WSMessageEncoding.Mtom;
				MtomMessageEncodingBindingElement mtomMessageEncodingBindingElement = (MtomMessageEncodingBindingElement)encoding;
				this.TextEncoding = mtomMessageEncodingBindingElement.WriteEncoding;
				this.ReaderQuotas = mtomMessageEncodingBindingElement.ReaderQuotas;
			}
			this.reliableSession.Enabled = session != null;
			if (session != null)
			{
				this.session.InactivityTimeout = session.InactivityTimeout;
				this.session.Ordered = session.Ordered;
			}
		}

		private static bool IsBindingElementMatch(BindingElement elementThis, BindingElement elementThat)
		{
			Type type = elementThis.GetType();
			object[] objArray = new object[] { elementThat };
			return (bool)InvokeHelper.InvokeInstanceMethod(type, elementThis, "IsMatch", objArray);
		}

		private bool IsBindingElementsMatch(HttpRelayTransportBindingElement transport, MessageEncodingBindingElement encoding, System.ServiceModel.Channels.ReliableSessionBindingElement session)
		{
			if (!WSHttpRelayBindingBase.IsBindingElementMatch(this.GetTransport(), transport))
			{
				return false;
			}
			if (this.MessageEncoding == WSMessageEncoding.Text)
			{
				if (!WSHttpRelayBindingBase.IsBindingElementMatch(this.textEncoding, encoding))
				{
					return false;
				}
			}
			else if (this.MessageEncoding == WSMessageEncoding.Mtom && !WSHttpRelayBindingBase.IsBindingElementMatch(this.mtomEncoding, encoding))
			{
				return false;
			}
			if (this.reliableSession.Enabled)
			{
				if (!WSHttpRelayBindingBase.IsBindingElementMatch(this.session, session))
				{
					return false;
				}
			}
			else if (session != null)
			{
				return false;
			}
			return true;
		}

		internal static bool TryCreate(BindingElementCollection elements, out Binding binding)
		{
			bool flag;
			binding = null;
			if (elements.Count > 5)
			{
				return false;
			}
			PrivacyNoticeBindingElement privacyNoticeBindingElement = null;
			System.ServiceModel.Channels.ReliableSessionBindingElement reliableSessionBindingElement = null;
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
					else if (current is MessageEncodingBindingElement)
					{
						messageEncodingBindingElement = current as MessageEncodingBindingElement;
					}
					else if (current is System.ServiceModel.Channels.ReliableSessionBindingElement)
					{
						reliableSessionBindingElement = current as System.ServiceModel.Channels.ReliableSessionBindingElement;
					}
					else if (!(current is PrivacyNoticeBindingElement))
					{
						flag = false;
						return flag;
					}
					else
					{
						privacyNoticeBindingElement = current as PrivacyNoticeBindingElement;
					}
				}
				if (httpRelayTransportBindingElement == null)
				{
					return false;
				}
				if (messageEncodingBindingElement == null)
				{
					return false;
				}
				if (privacyNoticeBindingElement != null || !WS2007HttpRelayBinding.TryCreate(securityBindingElement, httpRelayTransportBindingElement.RelayClientAuthenticationType, httpRelayTransportBindingElement, reliableSessionBindingElement, out binding))
				{
					return false;
				}
				WSHttpRelayBindingBase wSHttpRelayBindingBase = binding as WSHttpRelayBindingBase;
				wSHttpRelayBindingBase.InitializeFrom(httpRelayTransportBindingElement, messageEncodingBindingElement, reliableSessionBindingElement);
				if (!wSHttpRelayBindingBase.IsBindingElementsMatch(httpRelayTransportBindingElement, messageEncodingBindingElement, reliableSessionBindingElement))
				{
					return false;
				}
				return true;
			}
			return flag;
		}
	}
}
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Net.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	public class HttpsRelayTransportBindingElement : HttpRelayTransportBindingElement
	{
		private System.ServiceModel.MessageSecurityVersion messageSecurityVersion;

		internal System.ServiceModel.MessageSecurityVersion MessageSecurityVersion
		{
			get
			{
				return this.messageSecurityVersion;
			}
			set
			{
				if (value == null)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("value"));
				}
				this.messageSecurityVersion = value;
			}
		}

		public override string Scheme
		{
			get
			{
				return "https";
			}
		}

		public HttpsRelayTransportBindingElement()
		{
		}

		public HttpsRelayTransportBindingElement(Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType) : base(relayClientAuthenticationType)
		{
		}

		protected HttpsRelayTransportBindingElement(HttpsRelayTransportBindingElement elementToBeCloned) : base(elementToBeCloned)
		{
			this.messageSecurityVersion = elementToBeCloned.MessageSecurityVersion;
		}

		public override BindingElement Clone()
		{
			return new HttpsRelayTransportBindingElement(this);
		}

		protected override HttpTransportBindingElement CreateInnerChannelBindingElement()
		{
			return new HttpsTransportBindingElement();
		}

		public override T GetProperty<T>(BindingContext context)
		where T : class
		{
			if (typeof(T) == typeof(ISecurityCapabilities))
			{
				return (T)(new HttpsRelayTransportBindingElement.SecurityCapabilitiesHelper());
			}
			return base.GetProperty<T>(context);
		}

		protected override void InitializeInnerChannelBindingElement(HttpTransportBindingElement httpTransportElement)
		{
			base.InitializeInnerChannelBindingElement(httpTransportElement);
			((HttpsTransportBindingElement)httpTransportElement).RequireClientCertificate = false;
		}

		private class SecurityCapabilitiesHelper : ISecurityCapabilities
		{
			ProtectionLevel System.ServiceModel.Channels.ISecurityCapabilities.SupportedRequestProtectionLevel
			{
				get
				{
					return ProtectionLevel.EncryptAndSign;
				}
			}

			ProtectionLevel System.ServiceModel.Channels.ISecurityCapabilities.SupportedResponseProtectionLevel
			{
				get
				{
					return ProtectionLevel.EncryptAndSign;
				}
			}

			bool System.ServiceModel.Channels.ISecurityCapabilities.SupportsClientAuthentication
			{
				get
				{
					return false;
				}
			}

			bool System.ServiceModel.Channels.ISecurityCapabilities.SupportsClientWindowsIdentity
			{
				get
				{
					return false;
				}
			}

			bool System.ServiceModel.Channels.ISecurityCapabilities.SupportsServerAuthentication
			{
				get
				{
					return true;
				}
			}

			internal SecurityCapabilitiesHelper()
			{
			}
		}
	}
}
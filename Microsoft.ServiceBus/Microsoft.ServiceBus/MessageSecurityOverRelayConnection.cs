using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;

namespace Microsoft.ServiceBus
{
	public sealed class MessageSecurityOverRelayConnection
	{
		internal const MessageCredentialType DefaultClientCredentialType = MessageCredentialType.Windows;

		private MessageCredentialType clientCredentialType;

		private SecurityAlgorithmSuite algorithmSuite;

		private bool wasAlgorithmSuiteSet;

		public SecurityAlgorithmSuite AlgorithmSuite
		{
			get
			{
				return this.algorithmSuite;
			}
			set
			{
				if (value == null)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
				}
				this.algorithmSuite = value;
				this.wasAlgorithmSuiteSet = true;
			}
		}

		public MessageCredentialType ClientCredentialType
		{
			get
			{
				return this.clientCredentialType;
			}
			set
			{
				if (!Microsoft.ServiceBus.MessageCredentialTypeHelper.IsDefined(value))
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
				}
				this.clientCredentialType = value;
			}
		}

		internal bool WasAlgorithmSuiteSet
		{
			get
			{
				return this.wasAlgorithmSuiteSet;
			}
		}

		internal MessageSecurityOverRelayConnection()
		{
			this.clientCredentialType = MessageCredentialType.Windows;
			this.algorithmSuite = SecurityAlgorithmSuite.Default;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal SecurityBindingElement CreateSecurityBindingElement(bool isSecureTransportMode, bool isReliableSession)
		{
			SecurityBindingElement wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11;
			SecurityBindingElement securityBindingElement;
			if (!isSecureTransportMode)
			{
				switch (this.clientCredentialType)
				{
					case MessageCredentialType.None:
					{
						securityBindingElement = SecurityBindingElement.CreateSslNegotiationBindingElement(false, true);
						break;
					}
					case MessageCredentialType.Windows:
					{
						securityBindingElement = SecurityBindingElement.CreateSspiNegotiationBindingElement(true);
						break;
					}
					case MessageCredentialType.UserName:
					{
						securityBindingElement = SecurityBindingElement.CreateUserNameForSslBindingElement(true);
						break;
					}
					case MessageCredentialType.Certificate:
					{
						securityBindingElement = SecurityBindingElement.CreateSslNegotiationBindingElement(true, true);
						break;
					}
					case MessageCredentialType.IssuedToken:
					{
						object[] objArray = new object[] { SecurityUtil.CreateSecurityStandardsManager(new object[0]), this.algorithmSuite };
						securityBindingElement = SecurityBindingElement.CreateIssuedTokenForSslBindingElement(SecurityUtil.IssuedSecurityTokenParameters.CreateInfoCardParameters(objArray), true);
						break;
					}
					default:
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert("unknown ClientCredentialType");
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
					}
				}
				wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11 = SecurityBindingElement.CreateSecureConversationBindingElement(securityBindingElement, true);
			}
			else
			{
				switch (this.clientCredentialType)
				{
					case MessageCredentialType.None:
					{
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.ClientCredentialTypeMustBeSpecifiedForMixedMode, new object[0])));
					}
					case MessageCredentialType.Windows:
					{
						securityBindingElement = SecurityBindingElement.CreateSspiNegotiationOverTransportBindingElement(true);
						break;
					}
					case MessageCredentialType.UserName:
					{
						securityBindingElement = SecurityBindingElement.CreateUserNameOverTransportBindingElement();
						break;
					}
					case MessageCredentialType.Certificate:
					{
						securityBindingElement = SecurityBindingElement.CreateCertificateOverTransportBindingElement();
						break;
					}
					case MessageCredentialType.IssuedToken:
					{
						object[] objArray1 = new object[] { SecurityUtil.CreateSecurityStandardsManager(new object[0]), this.algorithmSuite };
						securityBindingElement = SecurityBindingElement.CreateIssuedTokenOverTransportBindingElement(SecurityUtil.IssuedSecurityTokenParameters.CreateInfoCardParameters(objArray1));
						break;
					}
					default:
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert("unknown ClientCredentialType");
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
					}
				}
				wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11 = SecurityBindingElement.CreateSecureConversationBindingElement(securityBindingElement);
			}
			SecurityAlgorithmSuite algorithmSuite = this.AlgorithmSuite;
			SecurityAlgorithmSuite securityAlgorithmSuite = algorithmSuite;
			securityBindingElement.DefaultAlgorithmSuite = algorithmSuite;
			wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11.DefaultAlgorithmSuite = securityAlgorithmSuite;
			wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11.IncludeTimestamp = true;
			if (isReliableSession)
			{
				wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11.LocalServiceSettings.ReconnectTransportOnFailure = true;
				wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11.LocalClientSettings.ReconnectTransportOnFailure = true;
			}
			else
			{
				wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11.LocalServiceSettings.ReconnectTransportOnFailure = false;
				wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11.LocalClientSettings.ReconnectTransportOnFailure = false;
			}
			securityBindingElement.LocalServiceSettings.IssuedCookieLifetime = TimeSpan.FromMinutes(15);
			wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11.MessageSecurityVersion = MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11;
			securityBindingElement.MessageSecurityVersion = MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11;
			return wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11;
		}

		internal static bool TryCreate(SecurityBindingElement sbe, bool isReliableSession, out MessageSecurityOverRelayConnection messageSecurity)
		{
			MessageCredentialType messageCredentialType;
			SecurityBindingElement securityBindingElement;
			IssuedSecurityTokenParameters issuedSecurityTokenParameter;
			messageSecurity = null;
			if (sbe == null)
			{
				return false;
			}
			if (!sbe.IncludeTimestamp)
			{
				return false;
			}
			if (sbe.MessageSecurityVersion != MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11 && sbe.MessageSecurityVersion != MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10)
			{
				return false;
			}
			if (sbe.SecurityHeaderLayout != SecurityHeaderLayout.Strict)
			{
				return false;
			}
			if (!SecurityUtil.SecurityBindingElement.IsSecureConversationBinding(sbe, true, out securityBindingElement))
			{
				return false;
			}
			if (securityBindingElement is TransportSecurityBindingElement)
			{
				if (SecurityUtil.SecurityBindingElement.IsUserNameOverTransportBinding(securityBindingElement))
				{
					messageCredentialType = MessageCredentialType.UserName;
				}
				else if (SecurityUtil.SecurityBindingElement.IsCertificateOverTransportBinding(securityBindingElement))
				{
					messageCredentialType = MessageCredentialType.Certificate;
				}
				else if (!SecurityUtil.SecurityBindingElement.IsSspiNegotiationOverTransportBinding(securityBindingElement, true))
				{
					if (!SecurityUtil.SecurityBindingElement.IsIssuedTokenOverTransportBinding(securityBindingElement, out issuedSecurityTokenParameter))
					{
						return false;
					}
					if (!SecurityUtil.IssuedSecurityTokenParameters.IsInfoCardParameters(issuedSecurityTokenParameter))
					{
						return false;
					}
					messageCredentialType = MessageCredentialType.IssuedToken;
				}
				else
				{
					messageCredentialType = MessageCredentialType.Windows;
				}
			}
			else if (SecurityUtil.SecurityBindingElement.IsUserNameForSslBinding(securityBindingElement, true))
			{
				messageCredentialType = MessageCredentialType.UserName;
			}
			else if (SecurityUtil.SecurityBindingElement.IsSslNegotiationBinding(securityBindingElement, true, true))
			{
				messageCredentialType = MessageCredentialType.Certificate;
			}
			else if (SecurityUtil.SecurityBindingElement.IsSspiNegotiationBinding(securityBindingElement, true))
			{
				messageCredentialType = MessageCredentialType.Windows;
			}
			else if (!SecurityUtil.SecurityBindingElement.IsIssuedTokenForSslBinding(securityBindingElement, true, out issuedSecurityTokenParameter))
			{
				if (!SecurityUtil.SecurityBindingElement.IsSslNegotiationBinding(securityBindingElement, false, true))
				{
					return false;
				}
				messageCredentialType = MessageCredentialType.None;
			}
			else
			{
				if (!SecurityUtil.IssuedSecurityTokenParameters.IsInfoCardParameters(issuedSecurityTokenParameter))
				{
					return false;
				}
				messageCredentialType = MessageCredentialType.IssuedToken;
			}
			messageSecurity = new MessageSecurityOverRelayConnection()
			{
				ClientCredentialType = messageCredentialType
			};
			if (messageCredentialType != MessageCredentialType.IssuedToken)
			{
				messageSecurity.algorithmSuite = securityBindingElement.DefaultAlgorithmSuite;
			}
			return true;
		}
	}
}
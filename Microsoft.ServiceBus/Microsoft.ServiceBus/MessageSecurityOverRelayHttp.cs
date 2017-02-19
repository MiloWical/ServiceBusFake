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
	public class MessageSecurityOverRelayHttp
	{
		internal const MessageCredentialType DefaultClientCredentialType = MessageCredentialType.Windows;

		internal const bool DefaultNegotiateServiceCredential = true;

		private MessageCredentialType clientCredentialType;

		private bool negotiateServiceCredential;

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

		public bool NegotiateServiceCredential
		{
			get
			{
				return this.negotiateServiceCredential;
			}
			set
			{
				this.negotiateServiceCredential = value;
			}
		}

		internal bool WasAlgorithmSuiteSet
		{
			get
			{
				return this.wasAlgorithmSuiteSet;
			}
		}

		internal MessageSecurityOverRelayHttp()
		{
			this.clientCredentialType = MessageCredentialType.Windows;
			this.negotiateServiceCredential = true;
			this.algorithmSuite = SecurityAlgorithmSuite.Default;
		}

		private IssuedSecurityTokenParameters CreateInfoCardParameters(bool emitBspAttributes)
		{
			object[] wSSecurityTokenSerializer = new object[] { new WSSecurityTokenSerializer(emitBspAttributes) };
			object obj = SecurityUtil.CreateSecurityStandardsManager(wSSecurityTokenSerializer);
			object[] objArray = new object[] { obj, this.algorithmSuite };
			return SecurityUtil.IssuedSecurityTokenParameters.CreateInfoCardParameters(objArray);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal SecurityBindingElement CreateSecurityBindingElement(bool isSecureTransportMode, bool isReliableSession, MessageSecurityVersion version)
		{
			SecurityBindingElement securityBindingElement;
			SecurityBindingElement securityBindingElement1;
			if (isReliableSession && !this.IsSecureConversationEnabled())
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.SecureConversationRequiredByReliableSession, new object[0])));
			}
			bool flag = false;
			bool flag1 = true;
			if (!isSecureTransportMode)
			{
				if (!this.negotiateServiceCredential)
				{
					switch (this.clientCredentialType)
					{
						case MessageCredentialType.None:
						{
							securityBindingElement1 = SecurityBindingElement.CreateAnonymousForCertificateBindingElement();
							goto Label0;
						}
						case MessageCredentialType.Windows:
						{
							securityBindingElement1 = SecurityBindingElement.CreateKerberosBindingElement();
							flag = true;
							goto Label0;
						}
						case MessageCredentialType.UserName:
						{
							securityBindingElement1 = SecurityBindingElement.CreateUserNameForCertificateBindingElement();
							goto Label0;
						}
						case MessageCredentialType.Certificate:
						{
							securityBindingElement1 = SecurityBindingElement.CreateMutualCertificateBindingElement();
							goto Label0;
						}
						case MessageCredentialType.IssuedToken:
						{
							securityBindingElement1 = SecurityBindingElement.CreateIssuedTokenForCertificateBindingElement(this.CreateInfoCardParameters(flag1));
							goto Label0;
						}
					}
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert("unknown ClientCredentialType");
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
				}
				else
				{
					switch (this.clientCredentialType)
					{
						case MessageCredentialType.None:
						{
							securityBindingElement1 = SecurityBindingElement.CreateSslNegotiationBindingElement(false, true);
							goto Label0;
						}
						case MessageCredentialType.Windows:
						{
							securityBindingElement1 = SecurityBindingElement.CreateSspiNegotiationBindingElement(true);
							goto Label0;
						}
						case MessageCredentialType.UserName:
						{
							securityBindingElement1 = SecurityBindingElement.CreateUserNameForSslBindingElement(true);
							goto Label0;
						}
						case MessageCredentialType.Certificate:
						{
							securityBindingElement1 = SecurityBindingElement.CreateSslNegotiationBindingElement(true, true);
							goto Label0;
						}
						case MessageCredentialType.IssuedToken:
						{
							securityBindingElement1 = SecurityBindingElement.CreateIssuedTokenForSslBindingElement(this.CreateInfoCardParameters(flag1), true);
							goto Label0;
						}
					}
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert("unknown ClientCredentialType");
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
				}
			Label0:
				securityBindingElement = (!this.IsSecureConversationEnabled() ? securityBindingElement1 : SecurityBindingElement.CreateSecureConversationBindingElement(securityBindingElement1, true));
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
						securityBindingElement1 = SecurityBindingElement.CreateSspiNegotiationOverTransportBindingElement(true);
						break;
					}
					case MessageCredentialType.UserName:
					{
						securityBindingElement1 = SecurityBindingElement.CreateUserNameOverTransportBindingElement();
						break;
					}
					case MessageCredentialType.Certificate:
					{
						securityBindingElement1 = SecurityBindingElement.CreateCertificateOverTransportBindingElement();
						break;
					}
					case MessageCredentialType.IssuedToken:
					{
						securityBindingElement1 = SecurityBindingElement.CreateIssuedTokenOverTransportBindingElement(this.CreateInfoCardParameters(flag1));
						break;
					}
					default:
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert("unknown ClientCredentialType");
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
					}
				}
				securityBindingElement = (!this.IsSecureConversationEnabled() ? securityBindingElement1 : SecurityBindingElement.CreateSecureConversationBindingElement(securityBindingElement1, true));
			}
			if (this.wasAlgorithmSuiteSet || !flag)
			{
				SecurityAlgorithmSuite algorithmSuite = this.AlgorithmSuite;
				SecurityAlgorithmSuite securityAlgorithmSuite = algorithmSuite;
				securityBindingElement1.DefaultAlgorithmSuite = algorithmSuite;
				securityBindingElement.DefaultAlgorithmSuite = securityAlgorithmSuite;
			}
			else if (flag)
			{
				SecurityAlgorithmSuite basic128 = SecurityAlgorithmSuite.Basic128;
				SecurityAlgorithmSuite securityAlgorithmSuite1 = basic128;
				securityBindingElement1.DefaultAlgorithmSuite = basic128;
				securityBindingElement.DefaultAlgorithmSuite = securityAlgorithmSuite1;
			}
			securityBindingElement.IncludeTimestamp = true;
			securityBindingElement1.MessageSecurityVersion = version;
			securityBindingElement.MessageSecurityVersion = version;
			if (isReliableSession)
			{
				securityBindingElement.LocalServiceSettings.ReconnectTransportOnFailure = true;
				securityBindingElement.LocalClientSettings.ReconnectTransportOnFailure = true;
			}
			else
			{
				securityBindingElement.LocalServiceSettings.ReconnectTransportOnFailure = false;
				securityBindingElement.LocalClientSettings.ReconnectTransportOnFailure = false;
			}
			if (this.IsSecureConversationEnabled())
			{
				securityBindingElement1.LocalServiceSettings.IssuedCookieLifetime = TimeSpan.FromMinutes(15);
			}
			return securityBindingElement;
		}

		protected virtual bool IsSecureConversationEnabled()
		{
			return true;
		}

		internal static bool TryCreate<TSecurity>(SecurityBindingElement sbe, bool isSecureTransportMode, bool isReliableSession, out TSecurity messageSecurity)
		where TSecurity : MessageSecurityOverRelayHttp
		{
			MessageCredentialType messageCredentialType;
			bool flag;
			SecurityBindingElement securityBindingElement;
			IssuedSecurityTokenParameters issuedSecurityTokenParameter;
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(null != sbe, string.Empty);
			messageSecurity = default(TSecurity);
			if (!sbe.IncludeTimestamp)
			{
				return false;
			}
			if (sbe.SecurityHeaderLayout != SecurityHeaderLayout.Strict)
			{
				return false;
			}
			bool flag1 = true;
			if (SecurityUtil.SecurityBindingElement.IsSecureConversationBinding(sbe, true, out securityBindingElement))
			{
				flag = true;
			}
			else
			{
				flag = false;
				securityBindingElement = sbe;
			}
			if (!flag && typeof(TSecurity).Equals(typeof(MessageSecurityOverRelayHttp)))
			{
				return false;
			}
			if (!flag && isReliableSession)
			{
				return false;
			}
			if (isSecureTransportMode && !(securityBindingElement is TransportSecurityBindingElement))
			{
				return false;
			}
			if (isSecureTransportMode)
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
			else if (SecurityUtil.SecurityBindingElement.IsSslNegotiationBinding(securityBindingElement, false, true))
			{
				flag1 = true;
				messageCredentialType = MessageCredentialType.None;
			}
			else if (SecurityUtil.SecurityBindingElement.IsUserNameForSslBinding(securityBindingElement, true))
			{
				flag1 = true;
				messageCredentialType = MessageCredentialType.UserName;
			}
			else if (SecurityUtil.SecurityBindingElement.IsSslNegotiationBinding(securityBindingElement, true, true))
			{
				flag1 = true;
				messageCredentialType = MessageCredentialType.Certificate;
			}
			else if (SecurityUtil.SecurityBindingElement.IsSspiNegotiationBinding(securityBindingElement, true))
			{
				flag1 = true;
				messageCredentialType = MessageCredentialType.Windows;
			}
			else if (SecurityUtil.SecurityBindingElement.IsIssuedTokenForSslBinding(securityBindingElement, true, out issuedSecurityTokenParameter))
			{
				if (!SecurityUtil.IssuedSecurityTokenParameters.IsInfoCardParameters(issuedSecurityTokenParameter))
				{
					return false;
				}
				flag1 = true;
				messageCredentialType = MessageCredentialType.IssuedToken;
			}
			else if (SecurityUtil.SecurityBindingElement.IsUserNameForCertificateBinding(securityBindingElement))
			{
				flag1 = false;
				messageCredentialType = MessageCredentialType.UserName;
			}
			else if (SecurityUtil.SecurityBindingElement.IsMutualCertificateBinding(securityBindingElement))
			{
				flag1 = false;
				messageCredentialType = MessageCredentialType.Certificate;
			}
			else if (SecurityUtil.SecurityBindingElement.IsKerberosBinding(securityBindingElement))
			{
				flag1 = false;
				messageCredentialType = MessageCredentialType.Windows;
			}
			else if (!SecurityUtil.SecurityBindingElement.IsIssuedTokenForCertificateBinding(securityBindingElement, out issuedSecurityTokenParameter))
			{
				if (!SecurityUtil.SecurityBindingElement.IsAnonymousForCertificateBinding(securityBindingElement))
				{
					return false;
				}
				flag1 = false;
				messageCredentialType = MessageCredentialType.None;
			}
			else
			{
				if (!SecurityUtil.IssuedSecurityTokenParameters.IsInfoCardParameters(issuedSecurityTokenParameter))
				{
					return false;
				}
				flag1 = false;
				messageCredentialType = MessageCredentialType.IssuedToken;
			}
			if (!typeof(NonDualMessageSecurityOverRelayHttp).Equals(typeof(TSecurity)))
			{
				messageSecurity = (TSecurity)(new MessageSecurityOverRelayHttp());
			}
			else
			{
				messageSecurity = (TSecurity)(new NonDualMessageSecurityOverRelayHttp());
				((NonDualMessageSecurityOverRelayHttp)(object)messageSecurity).EstablishSecurityContext = flag;
			}
			messageSecurity.ClientCredentialType = messageCredentialType;
			messageSecurity.NegotiateServiceCredential = flag1;
			messageSecurity.AlgorithmSuite = sbe.DefaultAlgorithmSuite;
			return true;
		}
	}
}
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;

namespace Microsoft.ServiceBus
{
	public sealed class MessageSecurityOverRelayOneway
	{
		internal const MessageCredentialType DefaultClientCredentialType = MessageCredentialType.Certificate;

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

		internal MessageSecurityOverRelayOneway()
		{
			this.clientCredentialType = MessageCredentialType.Certificate;
			this.algorithmSuite = SecurityAlgorithmSuite.Default;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal SecurityBindingElement CreateSecurityBindingElement()
		{
			SymmetricSecurityBindingElement wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11;
			switch (this.clientCredentialType)
			{
				case MessageCredentialType.None:
				{
					wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11 = SecurityBindingElement.CreateAnonymousForCertificateBindingElement();
					break;
				}
				case MessageCredentialType.Windows:
				{
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert("unsupported ClientCredentialType");
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
				}
				case MessageCredentialType.UserName:
				{
					wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11 = SecurityBindingElement.CreateUserNameForCertificateBindingElement();
					break;
				}
				case MessageCredentialType.Certificate:
				{
					wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11 = (SymmetricSecurityBindingElement)SecurityBindingElement.CreateMutualCertificateBindingElement();
					break;
				}
				case MessageCredentialType.IssuedToken:
				{
					object[] objArray = new object[] { SecurityUtil.CreateSecurityStandardsManager(new object[0]), this.algorithmSuite };
					wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11 = SecurityBindingElement.CreateIssuedTokenForCertificateBindingElement(SecurityUtil.IssuedSecurityTokenParameters.CreateInfoCardParameters(objArray));
					break;
				}
				default:
				{
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert("unsupported ClientCredentialType");
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
				}
			}
			wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11.MessageSecurityVersion = MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11;
			if (this.wasAlgorithmSuiteSet)
			{
				wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11.DefaultAlgorithmSuite = this.AlgorithmSuite;
			}
			wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11.IncludeTimestamp = false;
			wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11.LocalServiceSettings.DetectReplays = false;
			wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11.LocalClientSettings.DetectReplays = false;
			return wSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11;
		}

		internal static bool TryCreate(SecurityBindingElement sbe, out MessageSecurityOverRelayOneway messageSecurity)
		{
			MessageCredentialType messageCredentialType;
			IssuedSecurityTokenParameters issuedSecurityTokenParameter;
			messageSecurity = null;
			if (sbe == null)
			{
				return false;
			}
			SymmetricSecurityBindingElement symmetricSecurityBindingElement = sbe as SymmetricSecurityBindingElement;
			if (symmetricSecurityBindingElement == null)
			{
				return false;
			}
			if (sbe.MessageSecurityVersion != MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10 && sbe.MessageSecurityVersion != MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11)
			{
				return false;
			}
			if (symmetricSecurityBindingElement.IncludeTimestamp)
			{
				return false;
			}
			if (SecurityUtil.SecurityBindingElement.IsAnonymousForCertificateBinding(sbe))
			{
				messageCredentialType = MessageCredentialType.None;
			}
			else if (SecurityUtil.SecurityBindingElement.IsUserNameForCertificateBinding(sbe))
			{
				messageCredentialType = MessageCredentialType.UserName;
			}
			else if (!SecurityUtil.SecurityBindingElement.IsMutualCertificateBinding(sbe))
			{
				if (!SecurityUtil.SecurityBindingElement.IsIssuedTokenForCertificateBinding(sbe, out issuedSecurityTokenParameter))
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
				messageCredentialType = MessageCredentialType.Certificate;
			}
			messageSecurity = new MessageSecurityOverRelayOneway()
			{
				ClientCredentialType = messageCredentialType
			};
			if (messageCredentialType != MessageCredentialType.IssuedToken)
			{
				messageSecurity.AlgorithmSuite = symmetricSecurityBindingElement.DefaultAlgorithmSuite;
			}
			return true;
		}
	}
}
using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;

namespace Microsoft.ServiceBus
{
	public sealed class BasicHttpRelayMessageSecurity
	{
		internal const BasicHttpMessageCredentialType DefaultClientCredentialType = BasicHttpMessageCredentialType.UserName;

		private BasicHttpMessageCredentialType clientCredentialType;

		private SecurityAlgorithmSuite algorithmSuite;

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
			}
		}

		public BasicHttpMessageCredentialType ClientCredentialType
		{
			get
			{
				return this.clientCredentialType;
			}
			set
			{
				if (value != BasicHttpMessageCredentialType.Certificate && value != BasicHttpMessageCredentialType.UserName)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
				}
				this.clientCredentialType = value;
			}
		}

		internal BasicHttpRelayMessageSecurity()
		{
			this.clientCredentialType = BasicHttpMessageCredentialType.UserName;
			this.algorithmSuite = SecurityAlgorithmSuite.Default;
		}

		internal SecurityBindingElement CreateMessageSecurity(bool isSecureTransportMode)
		{
			SecurityBindingElement algorithmSuite;
			if (!isSecureTransportMode)
			{
				if (this.clientCredentialType != BasicHttpMessageCredentialType.Certificate)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.BasicHttpMessageSecurityRequiresCertificate, new object[0])));
				}
				algorithmSuite = SecurityBindingElement.CreateMutualCertificateBindingElement(MessageSecurityVersion.WSSecurity10WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10, true);
			}
			else
			{
				MessageSecurityVersion wSSecurity10WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10 = MessageSecurityVersion.WSSecurity10WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10;
				switch (this.clientCredentialType)
				{
					case BasicHttpMessageCredentialType.UserName:
					{
						algorithmSuite = SecurityBindingElement.CreateUserNameOverTransportBindingElement();
						algorithmSuite.MessageSecurityVersion = wSSecurity10WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10;
						algorithmSuite.DefaultAlgorithmSuite = this.AlgorithmSuite;
						algorithmSuite.SecurityHeaderLayout = SecurityHeaderLayout.Lax;
						algorithmSuite.SetKeyDerivation(false);
						InvokeHelper.InvokeInstanceSet(algorithmSuite.GetType(), algorithmSuite, "DoNotEmitTrust", true);
						return algorithmSuite;
					}
					case BasicHttpMessageCredentialType.Certificate:
					{
						algorithmSuite = SecurityBindingElement.CreateCertificateOverTransportBindingElement(wSSecurity10WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10);
						algorithmSuite.DefaultAlgorithmSuite = this.AlgorithmSuite;
						algorithmSuite.SecurityHeaderLayout = SecurityHeaderLayout.Lax;
						algorithmSuite.SetKeyDerivation(false);
						InvokeHelper.InvokeInstanceSet(algorithmSuite.GetType(), algorithmSuite, "DoNotEmitTrust", true);
						return algorithmSuite;
					}
				}
				Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert("Unsupported basic http message credential type");
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
			}
			algorithmSuite.DefaultAlgorithmSuite = this.AlgorithmSuite;
			algorithmSuite.SecurityHeaderLayout = SecurityHeaderLayout.Lax;
			algorithmSuite.SetKeyDerivation(false);
			InvokeHelper.InvokeInstanceSet(algorithmSuite.GetType(), algorithmSuite, "DoNotEmitTrust", true);
			return algorithmSuite;
		}

		internal static bool TryCreate(SecurityBindingElement sbe, out BasicHttpRelayMessageSecurity security, out bool isSecureTransportMode)
		{
			BasicHttpMessageCredentialType basicHttpMessageCredentialType;
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(null != sbe, string.Empty);
			security = null;
			isSecureTransportMode = false;
			if (!(bool)InvokeHelper.InvokeInstanceGet(sbe.GetType(), sbe, "DoNotEmitTrust"))
			{
				return false;
			}
			Type type = sbe.GetType();
			object[] objArray = new object[] { false };
			if (!(bool)InvokeHelper.InvokeInstanceMethod(type, sbe, "IsSetKeyDerivation", objArray))
			{
				return false;
			}
			if (sbe.SecurityHeaderLayout != SecurityHeaderLayout.Lax)
			{
				return false;
			}
			if (sbe.MessageSecurityVersion != MessageSecurityVersion.WSSecurity10WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10)
			{
				return false;
			}
			if (SecurityUtil.SecurityBindingElement.IsMutualCertificateBinding(sbe, true))
			{
				basicHttpMessageCredentialType = BasicHttpMessageCredentialType.Certificate;
			}
			else
			{
				isSecureTransportMode = true;
				if (!SecurityUtil.SecurityBindingElement.IsCertificateOverTransportBinding(sbe))
				{
					if (!SecurityUtil.SecurityBindingElement.IsUserNameOverTransportBinding(sbe))
					{
						return false;
					}
					basicHttpMessageCredentialType = BasicHttpMessageCredentialType.UserName;
				}
				else
				{
					basicHttpMessageCredentialType = BasicHttpMessageCredentialType.Certificate;
				}
			}
			security = new BasicHttpRelayMessageSecurity()
			{
				ClientCredentialType = basicHttpMessageCredentialType,
				AlgorithmSuite = sbe.DefaultAlgorithmSuite
			};
			return true;
		}
	}
}
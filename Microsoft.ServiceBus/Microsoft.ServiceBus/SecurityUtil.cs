using Microsoft.ServiceBus.Channels;
using System;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal class SecurityUtil
	{
		public SecurityUtil()
		{
		}

		internal static object CreateSecurityStandardsManager(params object[] args)
		{
			return Activator.CreateInstance(typeof(SecurityVersion).Assembly.GetType("System.ServiceModel.Security.SecurityStandardsManager"), args);
		}

		internal class IssuedSecurityTokenParameters
		{
			public IssuedSecurityTokenParameters()
			{
			}

			internal static void AddAlgorithmParameters(System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters that, params object[] args)
			{
				InvokeHelper.InvokeInstanceMethod(typeof(System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters), that, "AddAlgorithmParameters", args);
			}

			internal static System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters CreateInfoCardParameters(params object[] args)
			{
				return (System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters)InvokeHelper.InvokeStaticMethod(typeof(System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters), "CreateInfoCardParameters", args);
			}

			internal static bool DoAlgorithmsMatch(System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters issuedTokenParameters, SecurityAlgorithmSuite securityAlgorithmSuite, object versionSpecificStandardsManager, out Collection<XmlElement> nonAlgorithmRequestParameters)
			{
				object[] objArray = new object[] { securityAlgorithmSuite, versionSpecificStandardsManager, null };
				bool flag = (bool)InvokeHelper.InvokeStaticMethod(typeof(System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters), "DoAlgorithmsMatch", objArray);
				nonAlgorithmRequestParameters = objArray[2] as Collection<XmlElement>;
				return flag;
			}

			internal static object GetAlternativeIssuerEndpoints(System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters issuedTokenParameters)
			{
				return InvokeHelper.InvokeStaticGet(typeof(System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters), "AlternativeIssuerEndpoints");
			}

			internal static bool IsInfoCardParameters(System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters infocardParameters)
			{
				return (bool)InvokeHelper.InvokeStaticMethod(typeof(System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters), "IsInfocardParameters", null);
			}
		}

		internal class SecurityBindingElement
		{
			public SecurityBindingElement()
			{
			}

			internal static bool AreBindingsMatching(System.ServiceModel.Channels.SecurityBindingElement securityBindingElement, System.ServiceModel.Channels.SecurityBindingElement sbe)
			{
				Type type = typeof(System.ServiceModel.Channels.SecurityBindingElement);
				object[] objArray = new object[] { securityBindingElement, sbe };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "AreBindingsMatching", objArray);
			}

			internal static bool IsAnonymousForCertificateBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity)
			{
				Type type = typeof(System.ServiceModel.Channels.SecurityBindingElement);
				object[] objArray = new object[] { bootstrapSecurity };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "IsAnonymousForCertificateBinding", objArray);
			}

			internal static bool IsCertificateOverTransportBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity)
			{
				Type type = typeof(System.ServiceModel.Channels.SecurityBindingElement);
				object[] objArray = new object[] { bootstrapSecurity };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "IsCertificateOverTransportBinding", objArray);
			}

			internal static bool IsIssuedTokenForCertificateBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity, out System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters infocardParameters)
			{
				object[] objArray = new object[] { bootstrapSecurity, null };
				bool flag = (bool)InvokeHelper.InvokeStaticMethod(typeof(System.ServiceModel.Channels.SecurityBindingElement), "IsIssuedTokenOverTransportBinding", objArray);
				infocardParameters = objArray[1] as System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters;
				return flag;
			}

			internal static bool IsIssuedTokenForSslBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity, bool p, out System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters infocardParameters)
			{
				object[] objArray = new object[] { bootstrapSecurity, p, null };
				bool flag = (bool)InvokeHelper.InvokeStaticMethod(typeof(System.ServiceModel.Channels.SecurityBindingElement), "IsIssuedTokenForSslBinding", objArray);
				infocardParameters = objArray[2] as System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters;
				return flag;
			}

			internal static bool IsIssuedTokenForSslBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity, out System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters issuedTokenParameters)
			{
				object[] objArray = new object[] { bootstrapSecurity, null };
				bool flag = (bool)InvokeHelper.InvokeStaticMethod(typeof(System.ServiceModel.Channels.SecurityBindingElement), "IsIssuedTokenForSslBinding", objArray);
				issuedTokenParameters = objArray[1] as System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters;
				return flag;
			}

			internal static bool IsIssuedTokenOverTransportBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity, out System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters infocardParameters)
			{
				object[] objArray = new object[] { bootstrapSecurity, null };
				bool flag = (bool)InvokeHelper.InvokeStaticMethod(typeof(System.ServiceModel.Channels.SecurityBindingElement), "IsIssuedTokenOverTransportBinding", objArray);
				infocardParameters = objArray[1] as System.ServiceModel.Security.Tokens.IssuedSecurityTokenParameters;
				return flag;
			}

			internal static bool IsKerberosBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity)
			{
				Type type = typeof(System.ServiceModel.Channels.SecurityBindingElement);
				object[] objArray = new object[] { bootstrapSecurity };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "IsKerberosBinding", objArray);
			}

			internal static bool IsMutualCertificateBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity)
			{
				Type type = typeof(System.ServiceModel.Channels.SecurityBindingElement);
				object[] objArray = new object[] { bootstrapSecurity };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "IsMutualCertificateBinding", objArray);
			}

			internal static bool IsMutualCertificateBinding(System.ServiceModel.Channels.SecurityBindingElement sbe, bool p)
			{
				Type type = typeof(System.ServiceModel.Channels.SecurityBindingElement);
				object[] objArray = new object[] { sbe, p };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "IsMutualCertificateBinding", objArray);
			}

			internal static bool IsSecureConversationBinding(System.ServiceModel.Channels.SecurityBindingElement sbe, out System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity)
			{
				object[] objArray = new object[] { sbe, null };
				bool flag = (bool)InvokeHelper.InvokeStaticMethod(typeof(System.ServiceModel.Channels.SecurityBindingElement), "IsSecureConversationBinding", objArray);
				bootstrapSecurity = objArray[1] as System.ServiceModel.Channels.SecurityBindingElement;
				return flag;
			}

			internal static bool IsSecureConversationBinding(System.ServiceModel.Channels.SecurityBindingElement sbe, bool requireCancellation, out System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity)
			{
				object[] objArray = new object[] { sbe, requireCancellation, null };
				bool flag = (bool)InvokeHelper.InvokeStaticMethod(typeof(System.ServiceModel.Channels.SecurityBindingElement), "IsSecureConversationBinding", objArray);
				bootstrapSecurity = objArray[2] as System.ServiceModel.Channels.SecurityBindingElement;
				return flag;
			}

			internal static bool IsSslNegotiationBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity, bool p, bool p_3)
			{
				Type type = typeof(System.ServiceModel.Channels.SecurityBindingElement);
				object[] objArray = new object[] { bootstrapSecurity, p, p_3 };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "IsSslNegotiationBinding", objArray);
			}

			internal static bool IsSspiNegotiationBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity, bool p)
			{
				Type type = typeof(System.ServiceModel.Channels.SecurityBindingElement);
				object[] objArray = new object[] { bootstrapSecurity, p };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "IsSspiNegotiationBinding", objArray);
			}

			internal static bool IsSspiNegotiationOverTransportBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity, bool p)
			{
				Type type = typeof(System.ServiceModel.Channels.SecurityBindingElement);
				object[] objArray = new object[] { bootstrapSecurity, p };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "IsSspiNegotiationOverTransportBinding", objArray);
			}

			internal static bool IsUserNameForCertificateBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity)
			{
				Type type = typeof(System.ServiceModel.Channels.SecurityBindingElement);
				object[] objArray = new object[] { bootstrapSecurity };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "IsUserNameForCertificateBinding", objArray);
			}

			internal static bool IsUserNameForSslBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity, bool p)
			{
				Type type = typeof(System.ServiceModel.Channels.SecurityBindingElement);
				object[] objArray = new object[] { bootstrapSecurity, p };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "IsUserNameForSslBinding", objArray);
			}

			internal static bool IsUserNameOverTransportBinding(System.ServiceModel.Channels.SecurityBindingElement bootstrapSecurity)
			{
				Type type = typeof(System.ServiceModel.Channels.SecurityBindingElement);
				object[] objArray = new object[] { bootstrapSecurity };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "IsUserNameOverTransportBinding", objArray);
			}
		}

		internal class SecurityKeyTypeHelper
		{
			public SecurityKeyTypeHelper()
			{
			}

			internal static bool IsDefined(SecurityKeyType value)
			{
				Type type = typeof(GenericXmlSecurityToken).Assembly.GetType("System.IdentityModel.Tokens.SecurityKeyTypeHelper");
				object[] objArray = new object[] { value };
				return (bool)InvokeHelper.InvokeStaticMethod(type, "IsDefined", objArray);
			}
		}
	}
}
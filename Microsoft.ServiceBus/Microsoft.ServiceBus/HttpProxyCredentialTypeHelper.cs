using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Net;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	internal static class HttpProxyCredentialTypeHelper
	{
		internal static bool IsDefined(HttpProxyCredentialType value)
		{
			if (value == HttpProxyCredentialType.None || value == HttpProxyCredentialType.Basic || value == HttpProxyCredentialType.Digest || value == HttpProxyCredentialType.Ntlm)
			{
				return true;
			}
			return value == HttpProxyCredentialType.Windows;
		}

		internal static AuthenticationSchemes MapToAuthenticationScheme(HttpProxyCredentialType proxyCredentialType)
		{
			AuthenticationSchemes authenticationScheme;
			switch (proxyCredentialType)
			{
				case HttpProxyCredentialType.None:
				{
					authenticationScheme = AuthenticationSchemes.Anonymous;
					break;
				}
				case HttpProxyCredentialType.Basic:
				{
					authenticationScheme = AuthenticationSchemes.Basic;
					break;
				}
				case HttpProxyCredentialType.Digest:
				{
					authenticationScheme = AuthenticationSchemes.Digest;
					break;
				}
				case HttpProxyCredentialType.Ntlm:
				{
					authenticationScheme = AuthenticationSchemes.Ntlm;
					break;
				}
				case HttpProxyCredentialType.Windows:
				{
					authenticationScheme = AuthenticationSchemes.Negotiate;
					break;
				}
				default:
				{
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert("unsupported proxy credential type");
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
				}
			}
			return authenticationScheme;
		}

		internal static HttpProxyCredentialType MapToProxyCredentialType(AuthenticationSchemes authenticationSchemes)
		{
			HttpProxyCredentialType httpProxyCredentialType;
			AuthenticationSchemes authenticationScheme = authenticationSchemes;
			switch (authenticationScheme)
			{
				case AuthenticationSchemes.Digest:
				{
					httpProxyCredentialType = HttpProxyCredentialType.Digest;
					break;
				}
				case AuthenticationSchemes.Negotiate:
				{
					httpProxyCredentialType = HttpProxyCredentialType.Windows;
					break;
				}
				case AuthenticationSchemes.Digest | AuthenticationSchemes.Negotiate:
				{
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert("unsupported authentication Scheme");
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
				}
				case AuthenticationSchemes.Ntlm:
				{
					httpProxyCredentialType = HttpProxyCredentialType.Ntlm;
					break;
				}
				default:
				{
					if (authenticationScheme == AuthenticationSchemes.Basic)
					{
						httpProxyCredentialType = HttpProxyCredentialType.Basic;
						break;
					}
					else
					{
						if (authenticationScheme != AuthenticationSchemes.Anonymous)
						{
							Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert("unsupported authentication Scheme");
							throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
						}
						httpProxyCredentialType = HttpProxyCredentialType.None;
						break;
					}
				}
			}
			return httpProxyCredentialType;
		}
	}
}
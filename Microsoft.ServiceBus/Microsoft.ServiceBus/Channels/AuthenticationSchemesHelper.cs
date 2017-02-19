using System;
using System.Net;

namespace Microsoft.ServiceBus.Channels
{
	internal static class AuthenticationSchemesHelper
	{
		public static bool IsSingleton(AuthenticationSchemes v)
		{
			bool flag;
			AuthenticationSchemes authenticationScheme = v;
			switch (authenticationScheme)
			{
				case AuthenticationSchemes.Digest:
				case AuthenticationSchemes.Negotiate:
				case AuthenticationSchemes.Ntlm:
				{
					flag = true;
					break;
				}
				case AuthenticationSchemes.Digest | AuthenticationSchemes.Negotiate:
				{
					flag = false;
					break;
				}
				default:
				{
					if (authenticationScheme == AuthenticationSchemes.Basic || authenticationScheme == AuthenticationSchemes.Anonymous)
					{
						goto case AuthenticationSchemes.Ntlm;
					}
					else
					{
						goto case AuthenticationSchemes.Digest | AuthenticationSchemes.Negotiate;
					}
				}
			}
			return flag;
		}
	}
}
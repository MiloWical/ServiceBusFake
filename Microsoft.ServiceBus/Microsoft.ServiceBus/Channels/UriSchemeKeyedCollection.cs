using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class UriSchemeKeyedCollection
	{
		public UriSchemeKeyedCollection()
		{
		}

		internal static void ValidateBaseAddress(Uri uri, string argumentName)
		{
			if (uri == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(argumentName);
			}
			if (!uri.IsAbsoluteUri)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument(argumentName, Microsoft.ServiceBus.SR.GetString(Resources.BaseAddressMustBeAbsolute, new object[0]));
			}
			if (!string.IsNullOrEmpty(uri.UserInfo))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument(argumentName, Microsoft.ServiceBus.SR.GetString(Resources.BaseAddressCannotHaveUserInfo, new object[0]));
			}
			if (!string.IsNullOrEmpty(uri.Query))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument(argumentName, Microsoft.ServiceBus.SR.GetString(Resources.BaseAddressCannotHaveQuery, new object[0]));
			}
			if (!string.IsNullOrEmpty(uri.Fragment))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument(argumentName, Microsoft.ServiceBus.SR.GetString(Resources.BaseAddressCannotHaveFragment, new object[0]));
			}
		}
	}
}
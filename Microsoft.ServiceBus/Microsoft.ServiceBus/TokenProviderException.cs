using System;
using System.IdentityModel.Tokens;

namespace Microsoft.ServiceBus
{
	[Serializable]
	public class TokenProviderException : SecurityTokenException
	{
		public TokenProviderException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
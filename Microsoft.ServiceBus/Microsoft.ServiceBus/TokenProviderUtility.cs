using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal static class TokenProviderUtility
	{
		private readonly static TimeSpan MaxGetTokenSyncTimeout;

		static TokenProviderUtility()
		{
			TokenProviderUtility.MaxGetTokenSyncTimeout = TimeSpan.FromSeconds(15.15);
		}

		internal static IAsyncResult BeginGetMessagingToken(this TokenProvider tokenProvider, Uri namespaceAddress, string appliesTo, string action, bool bypassCache, TimeSpan timeout, AsyncCallback callback, object state)
		{
			Exception exception;
			IAsyncResult asyncResult;
			try
			{
				asyncResult = tokenProvider.BeginGetToken(namespaceAddress, appliesTo, action, bypassCache, timeout, callback, state);
			}
			catch (Exception exception1)
			{
				if (TokenProviderUtility.HandleTokenException(exception1, out exception))
				{
					throw exception;
				}
				throw;
			}
			return asyncResult;
		}

		internal static TokenProvider CreateTokenProvider(BindingContext context)
		{
			return TokenProviderUtility.CreateTokenProvider(context.BindingParameters.Find<TransportClientEndpointBehavior>());
		}

		internal static TokenProvider CreateTokenProvider(TransportClientEndpointBehavior behavior)
		{
			if (behavior == null)
			{
				return null;
			}
			return behavior.TokenProvider;
		}

		internal static SecurityToken EndGetMessagingToken(this TokenProvider tokenProvider, IAsyncResult result)
		{
			Exception exception;
			SecurityToken securityToken;
			try
			{
				securityToken = tokenProvider.EndGetToken(result);
			}
			catch (Exception exception1)
			{
				if (TokenProviderUtility.HandleTokenException(exception1, out exception))
				{
					throw exception;
				}
				throw;
			}
			return securityToken;
		}

		internal static SecurityToken GetMessagingToken(this TokenProvider tokenProvider, Uri namespaceAddress, string appliesTo, string action, bool bypassCache, TimeSpan timeout)
		{
			SecurityToken securityToken;
			try
			{
				TimeSpan timeSpan = TimeoutHelper.Min(timeout, TokenProviderUtility.MaxGetTokenSyncTimeout);
				securityToken = tokenProvider.EndGetToken(tokenProvider.BeginGetToken(namespaceAddress, appliesTo, action, bypassCache, timeSpan, null, null));
			}
			catch (TimeoutException timeoutException)
			{
				throw;
			}
			catch (TokenProviderException tokenProviderException1)
			{
				TokenProviderException tokenProviderException = tokenProviderException1;
				throw new MessagingException(tokenProviderException.Message, tokenProviderException);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw new UnauthorizedAccessException(exception.Message, exception);
				}
				throw;
			}
			return securityToken;
		}

		internal static string GetMessagingWebToken(this TokenProvider tokenProvider, Uri namespaceAddress, string appliesTo, string action, bool bypassCache, TimeSpan timeout)
		{
			string str;
			try
			{
				str = tokenProvider.EndGetWebToken(tokenProvider.BeginGetWebToken(namespaceAddress, appliesTo, action, bypassCache, timeout, null, null));
			}
			catch (TimeoutException timeoutException)
			{
				throw;
			}
			catch (TokenProviderException tokenProviderException1)
			{
				TokenProviderException tokenProviderException = tokenProviderException1;
				throw new MessagingException(tokenProviderException.Message, tokenProviderException);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw new UnauthorizedAccessException(exception.Message, exception);
				}
				throw;
			}
			return str;
		}

		internal static SecurityToken GetToken(this TokenProvider tokenProvider, string appliesTo, string action, bool bypassCache, TimeSpan timeout)
		{
			TimeSpan timeSpan = TimeoutHelper.Min(timeout, TokenProviderUtility.MaxGetTokenSyncTimeout);
			return tokenProvider.EndGetToken(tokenProvider.BeginGetToken(appliesTo, action, bypassCache, timeSpan, null, null));
		}

		internal static string GetWebToken(this TokenProvider tokenProvider, string appliesTo, string action, bool bypassCache, TimeSpan timeout)
		{
			return tokenProvider.EndGetWebToken(tokenProvider.BeginGetWebToken(appliesTo, action, bypassCache, timeout, null, null));
		}

		private static bool HandleTokenException(Exception exception, out Exception outException)
		{
			outException = null;
			if (exception is TimeoutException)
			{
				return false;
			}
			if (Fx.IsFatal(exception))
			{
				return false;
			}
			if (!(exception is TokenProviderException))
			{
				outException = new UnauthorizedAccessException(exception.Message, exception);
			}
			else
			{
				outException = new MessagingException(exception.Message, exception);
			}
			return true;
		}
	}
}
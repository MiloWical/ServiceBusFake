using Microsoft.ServiceBus.Common;
using System;
using System.IdentityModel.Tokens;

namespace Microsoft.ServiceBus
{
	public class SimpleWebTokenProvider : TokenProvider
	{
		private readonly string simpleWebToken;

		private readonly Uri stsUri;

		internal string SimpleWebToken
		{
			get
			{
				return this.simpleWebToken;
			}
		}

		internal SimpleWebTokenProvider(string simpleWebToken) : this(simpleWebToken, Microsoft.ServiceBus.TokenScope.Entity)
		{
		}

		internal SimpleWebTokenProvider(string simpleWebToken, Microsoft.ServiceBus.TokenScope tokenScope) : base(true, true, tokenScope)
		{
			if (string.IsNullOrEmpty(simpleWebToken))
			{
				throw new ArgumentException(SRClient.NullSimpleWebToken, "simpleWebToken");
			}
			this.simpleWebToken = simpleWebToken;
			this.stsUri = null;
		}

		internal SimpleWebTokenProvider(string simpleWebToken, Uri stsUri) : this(simpleWebToken, stsUri, Microsoft.ServiceBus.TokenScope.Entity)
		{
		}

		internal SimpleWebTokenProvider(string simpleWebToken, Uri stsUri, Microsoft.ServiceBus.TokenScope tokenScope) : base(true, true, tokenScope)
		{
			if (string.IsNullOrEmpty(simpleWebToken))
			{
				throw new ArgumentException(SRClient.NullSimpleWebToken, "simpleWebToken");
			}
			if (stsUri == null)
			{
				throw new ArgumentNullException("stsUri");
			}
			if (!stsUri.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
			{
				throw new ArgumentException(SRClient.STSURIFormat);
			}
			this.simpleWebToken = simpleWebToken;
			this.stsUri = stsUri;
		}

		protected override TokenProvider.Key BuildKey(string appliesTo, string action)
		{
			return new TokenProvider.Key(appliesTo, string.Empty);
		}

		protected override IAsyncResult OnBeginGetToken(string appliesTo, string action, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ValidateAction(action);
			Uri stsUri = TokenProviderHelper.GetStsUri(this.stsUri, appliesTo);
			TokenProviderHelper.TokenResult<SecurityToken> accessTokenByAssertion = TokenProviderHelper.GetAccessTokenByAssertion(stsUri, appliesTo, this.SimpleWebToken, "SWT", timeout);
			return new CompletedAsyncResult<TokenProviderHelper.TokenResult<SecurityToken>>(accessTokenByAssertion, callback, state);
		}

		protected override IAsyncResult OnBeginGetWebToken(string appliesTo, string action, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ValidateAction(action);
			Uri stsUri = TokenProviderHelper.GetStsUri(this.stsUri, appliesTo);
			TokenProviderHelper.TokenResult<string> httpAuthAccessTokenByAssertion = TokenProviderHelper.GetHttpAuthAccessTokenByAssertion(stsUri, appliesTo, this.SimpleWebToken, "SWT", timeout);
			return new CompletedAsyncResult<TokenProviderHelper.TokenResult<string>>(httpAuthAccessTokenByAssertion, callback, state);
		}

		protected override SecurityToken OnEndGetToken(IAsyncResult result, out DateTime cacheUntil)
		{
			TokenProviderHelper.TokenResult<SecurityToken> tokenResult = CompletedAsyncResult<TokenProviderHelper.TokenResult<SecurityToken>>.End(result);
			cacheUntil = tokenResult.CacheUntil;
			return tokenResult.Token;
		}

		protected override string OnEndGetWebToken(IAsyncResult result, out DateTime cacheUntil)
		{
			TokenProviderHelper.TokenResult<string> tokenResult = CompletedAsyncResult<TokenProviderHelper.TokenResult<string>>.End(result);
			cacheUntil = tokenResult.CacheUntil;
			return tokenResult.Token;
		}
	}
}
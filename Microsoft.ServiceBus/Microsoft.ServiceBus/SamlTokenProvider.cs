using Microsoft.ServiceBus.Common;
using System;
using System.IdentityModel.Tokens;

namespace Microsoft.ServiceBus
{
	public class SamlTokenProvider : TokenProvider
	{
		private readonly string samlToken;

		private readonly Uri stsUri;

		internal string SamlToken
		{
			get
			{
				return this.samlToken;
			}
		}

		internal SamlTokenProvider(string samlToken) : this(samlToken, Microsoft.ServiceBus.TokenScope.Entity)
		{
		}

		internal SamlTokenProvider(string samlToken, Microsoft.ServiceBus.TokenScope tokenScope) : base(true, true, tokenScope)
		{
			if (string.IsNullOrEmpty(samlToken))
			{
				throw new ArgumentException(SRClient.NullSAMLs, "samlToken");
			}
			this.samlToken = samlToken;
			this.stsUri = null;
		}

		internal SamlTokenProvider(string samlToken, Uri stsUri) : this(samlToken, stsUri, Microsoft.ServiceBus.TokenScope.Entity)
		{
		}

		internal SamlTokenProvider(string samlToken, Uri stsUri, Microsoft.ServiceBus.TokenScope tokenScope) : base(true, true, tokenScope)
		{
			if (string.IsNullOrEmpty(samlToken))
			{
				throw new ArgumentException(SRClient.NullSAMLs, "samlToken");
			}
			if (stsUri == null)
			{
				throw new ArgumentNullException("stsUri");
			}
			if (!stsUri.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
			{
				throw new ArgumentException(SRClient.STSURIFormat, "stsUri");
			}
			this.samlToken = samlToken;
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
			TokenProviderHelper.TokenResult<SecurityToken> accessTokenByAssertion = TokenProviderHelper.GetAccessTokenByAssertion(stsUri, appliesTo, this.SamlToken, "SAML", timeout);
			return new CompletedAsyncResult<TokenProviderHelper.TokenResult<SecurityToken>>(accessTokenByAssertion, callback, state);
		}

		protected override IAsyncResult OnBeginGetWebToken(string appliesTo, string action, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ValidateAction(action);
			Uri stsUri = TokenProviderHelper.GetStsUri(this.stsUri, appliesTo);
			TokenProviderHelper.TokenResult<string> httpAuthAccessTokenByAssertion = TokenProviderHelper.GetHttpAuthAccessTokenByAssertion(stsUri, appliesTo, this.SamlToken, "SAML", timeout);
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
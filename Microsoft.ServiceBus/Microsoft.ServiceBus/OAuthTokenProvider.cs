using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Web;

namespace Microsoft.ServiceBus
{
	public class OAuthTokenProvider : TokenProvider
	{
		private const int DefaultCacheSize = 100;

		private const string OAuthTokenServicePath = "$STS/OAuth/";

		private const string ClientPasswordFormat = "grant_type=authorization_code&client_id={0}&client_secret={1}&scope={2}";

		private readonly Func<Uri, Uri> onBuildUri = new Func<Uri, Uri>(OAuthTokenProvider.BuildStsUri);

		private readonly List<Uri> stsUris;

		private readonly NetworkCredential credential;

		internal OAuthTokenProvider(IEnumerable<Uri> stsUris, NetworkCredential credential) : base(true, true, 100, Microsoft.ServiceBus.TokenScope.Namespace)
		{
			if (credential == null)
			{
				throw FxTrace.Exception.ArgumentNull("credential");
			}
			if (stsUris == null)
			{
				throw FxTrace.Exception.ArgumentNull("stsUris");
			}
			this.stsUris = stsUris.ToList<Uri>();
			if (this.stsUris.Count == 0)
			{
				throw FxTrace.Exception.ArgumentNull("stsUris");
			}
			this.credential = credential;
		}

		protected override TokenProvider.Key BuildKey(string appliesTo, string action)
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] domain = new object[] { this.credential.Domain, this.credential.UserName };
			string str = string.Format(invariantCulture, "{0}\\{1}", domain);
			return new TokenProvider.Key(str, string.Empty);
		}

		private string BuildRequestToken(string scope)
		{
			string userName;
			if (string.IsNullOrWhiteSpace(this.credential.Domain))
			{
				userName = this.credential.UserName;
			}
			else
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { this.credential.UserName, this.credential.Domain };
				userName = string.Format(invariantCulture, "{0}@{1}", objArray);
			}
			string str = userName;
			CultureInfo cultureInfo = CultureInfo.InvariantCulture;
			object[] objArray1 = new object[] { HttpUtility.UrlEncode(str), HttpUtility.UrlEncode(this.credential.Password), HttpUtility.UrlEncode(scope) };
			return string.Format(cultureInfo, "grant_type=authorization_code&client_id={0}&client_secret={1}&scope={2}", objArray1);
		}

		private static Uri BuildStsUri(Uri baseAddress)
		{
			UriBuilder uriBuilder = MessagingUtilities.CreateUriBuilderWithHttpsSchemeAndPort(baseAddress);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] path = new object[] { uriBuilder.Path, "$STS/OAuth/" };
			uriBuilder.Path = string.Format(invariantCulture, "{0}{1}", path);
			return uriBuilder.Uri;
		}

		protected override string NormalizeAppliesTo(string appliesTo)
		{
			return ServiceBusUriHelper.NormalizeUri(appliesTo, "http", this.StripQueryParameters, false, false);
		}

		protected override IAsyncResult OnBeginGetToken(string appliesTo, string action, TimeSpan timeout, AsyncCallback callback, object state)
		{
			DateTime dateTime;
			string str;
			string oAuthAccessTokenCore = TokenProviderHelper.GetOAuthAccessTokenCore(this.stsUris.GetEnumerator(), this.onBuildUri, this.BuildRequestToken(appliesTo), timeout, out dateTime, out str);
			SimpleWebSecurityToken simpleWebSecurityToken = new SimpleWebSecurityToken(oAuthAccessTokenCore, dateTime, str);
			TokenProviderHelper.TokenResult<SecurityToken> tokenResult = new TokenProviderHelper.TokenResult<SecurityToken>()
			{
				CacheUntil = dateTime,
				Token = simpleWebSecurityToken
			};
			return new CompletedAsyncResult<TokenProviderHelper.TokenResult<SecurityToken>>(tokenResult, callback, state);
		}

		protected override IAsyncResult OnBeginGetWebToken(string appliesTo, string action, TimeSpan timeout, AsyncCallback callback, object state)
		{
			DateTime dateTime;
			string str;
			string oAuthAccessTokenCore = TokenProviderHelper.GetOAuthAccessTokenCore(this.stsUris.GetEnumerator(), this.onBuildUri, this.BuildRequestToken(appliesTo), timeout, out dateTime, out str);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { "WRAP", "access_token", oAuthAccessTokenCore };
			string str1 = string.Format(invariantCulture, "{0} {1}=\"{2}\"", objArray);
			TokenProviderHelper.TokenResult<string> tokenResult = new TokenProviderHelper.TokenResult<string>()
			{
				CacheUntil = dateTime,
				Token = str1
			};
			return new CompletedAsyncResult<TokenProviderHelper.TokenResult<string>>(tokenResult, callback, state);
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
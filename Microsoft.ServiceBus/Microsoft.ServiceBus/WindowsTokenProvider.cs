using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Web;

namespace Microsoft.ServiceBus
{
	public class WindowsTokenProvider : TokenProvider
	{
		private const int DefaultCacheSize = 100;

		private const string WindowsTokenServicePath = "$STS/Windows/";

		private const string ClientPasswordFormat = "grant_type=authorization_code&client_id={0}&client_secret={1}&scope={2}";

		private const string ScopeFormat = "scope={0}";

		private readonly Func<Uri, Uri> onBuildUri = new Func<Uri, Uri>(WindowsTokenProvider.BuildStsUri);

		internal readonly List<Uri> stsUris;

		internal readonly NetworkCredential credential;

		internal WindowsTokenProvider(IEnumerable<Uri> stsUris) : this(stsUris, null)
		{
		}

		internal WindowsTokenProvider(IEnumerable<Uri> stsUris, NetworkCredential credential) : base(true, true, 100, Microsoft.ServiceBus.TokenScope.Namespace)
		{
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
			string name;
			if (this.credential != null)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] domain = new object[] { this.credential.Domain, this.credential.UserName };
				name = string.Format(invariantCulture, "{0}\\{1}", domain);
			}
			else
			{
				name = WindowsIdentity.GetCurrent().Name;
			}
			return new TokenProvider.Key(name, string.Empty);
		}

		private string BuildRequestToken(string scope)
		{
			string userName;
			if (this.credential == null)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { HttpUtility.UrlEncode(scope) };
				return string.Format(invariantCulture, "scope={0}", objArray);
			}
			if (string.IsNullOrWhiteSpace(this.credential.Domain))
			{
				userName = this.credential.UserName;
			}
			else
			{
				CultureInfo cultureInfo = CultureInfo.InvariantCulture;
				object[] userName1 = new object[] { this.credential.UserName, this.credential.Domain };
				userName = string.Format(cultureInfo, "{0}@{1}", userName1);
			}
			string str = userName;
			CultureInfo invariantCulture1 = CultureInfo.InvariantCulture;
			object[] objArray1 = new object[] { HttpUtility.UrlEncode(str), HttpUtility.UrlEncode(this.credential.Password), HttpUtility.UrlEncode(scope) };
			return string.Format(invariantCulture1, "grant_type=authorization_code&client_id={0}&client_secret={1}&scope={2}", objArray1);
		}

		private static Uri BuildStsUri(Uri baseAddress)
		{
			UriBuilder uriBuilder = MessagingUtilities.CreateUriBuilderWithHttpsSchemeAndPort(baseAddress);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] path = new object[] { uriBuilder.Path, "$STS/Windows/" };
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
			string windowsAccessTokenCore = TokenProviderHelper.GetWindowsAccessTokenCore(this.stsUris.GetEnumerator(), this.onBuildUri, this.BuildRequestToken(appliesTo), timeout, out dateTime, out str);
			SimpleWebSecurityToken simpleWebSecurityToken = new SimpleWebSecurityToken(windowsAccessTokenCore, dateTime, str);
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
			string windowsAccessTokenCore = TokenProviderHelper.GetWindowsAccessTokenCore(this.stsUris.GetEnumerator(), this.onBuildUri, this.BuildRequestToken(appliesTo), timeout, out dateTime, out str);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { "WRAP", "access_token", windowsAccessTokenCore };
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
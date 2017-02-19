using Microsoft.ServiceBus.Common;
using System;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Microsoft.ServiceBus
{
	public class SharedSecretTokenProvider : TokenProvider
	{
		private readonly string issuerName;

		private readonly byte[] issuerSecret;

		private readonly Uri stsUri;

		internal string IssuerName
		{
			get
			{
				return this.issuerName;
			}
		}

		internal byte[] IssuerSecret
		{
			get
			{
				return this.issuerSecret;
			}
		}

		internal SharedSecretTokenProvider(string issuerName, string issuerSecret) : this(issuerName, SharedSecretTokenProvider.DecodeSecret(issuerSecret), Microsoft.ServiceBus.TokenScope.Entity)
		{
		}

		internal SharedSecretTokenProvider(string issuerName, string issuerSecret, Microsoft.ServiceBus.TokenScope tokenScope) : this(issuerName, SharedSecretTokenProvider.DecodeSecret(issuerSecret), tokenScope)
		{
		}

		internal SharedSecretTokenProvider(string issuerName, byte[] issuerSecret) : this(issuerName, issuerSecret, Microsoft.ServiceBus.TokenScope.Entity)
		{
		}

		internal SharedSecretTokenProvider(string issuerName, byte[] issuerSecret, Microsoft.ServiceBus.TokenScope tokenScope) : base(true, true, tokenScope)
		{
			if (string.IsNullOrEmpty(issuerName))
			{
				throw new ArgumentException(SRClient.NullIssuerName, "issuerName");
			}
			if (issuerSecret == null || (int)issuerSecret.Length == 0)
			{
				throw new ArgumentException(SRClient.NullIssuerSecret, "issuerSecret");
			}
			this.issuerName = issuerName;
			this.issuerSecret = issuerSecret;
			this.stsUri = null;
		}

		internal SharedSecretTokenProvider(string issuerName, string issuerSecret, Uri stsUri) : this(issuerName, issuerSecret, stsUri, Microsoft.ServiceBus.TokenScope.Entity)
		{
		}

		internal SharedSecretTokenProvider(string issuerName, string issuerSecret, Uri stsUri, Microsoft.ServiceBus.TokenScope tokenScope) : this(issuerName, SharedSecretTokenProvider.DecodeSecret(issuerSecret), stsUri, tokenScope)
		{
		}

		internal SharedSecretTokenProvider(string issuerName, byte[] issuerSecret, Uri stsUri) : this(issuerName, issuerSecret, stsUri, Microsoft.ServiceBus.TokenScope.Entity)
		{
		}

		internal SharedSecretTokenProvider(string issuerName, byte[] issuerSecret, Uri stsUri, Microsoft.ServiceBus.TokenScope tokenScope) : base(true, true, tokenScope)
		{
			if (string.IsNullOrEmpty(issuerName))
			{
				throw new ArgumentException(SRClient.NullIssuerName, "issuerName");
			}
			if (issuerSecret == null || (int)issuerSecret.Length == 0)
			{
				throw new ArgumentException(SRClient.NullIssuerSecret, "issuerSecret");
			}
			if (stsUri == null)
			{
				throw new ArgumentNullException("stsUri");
			}
			if (!stsUri.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
			{
				throw new ArgumentException(SRClient.STSURIFormat, "stsUri");
			}
			this.issuerName = issuerName;
			this.issuerSecret = issuerSecret;
			this.stsUri = stsUri;
		}

		protected override TokenProvider.Key BuildKey(string appliesTo, string action)
		{
			return new TokenProvider.Key(appliesTo, string.Empty);
		}

		public static string ComputeSimpleWebTokenString(string issuerName, string issuerSecret)
		{
			if (string.IsNullOrEmpty(issuerName))
			{
				throw new ArgumentException(SRClient.NullIssuerName, "issuerName");
			}
			if (string.IsNullOrEmpty(issuerSecret))
			{
				throw new ArgumentException(SRClient.NullIssuerSecret, "issuerSecret");
			}
			byte[] numArray = null;
			try
			{
				numArray = Convert.FromBase64String(issuerSecret);
			}
			catch (FormatException formatException)
			{
				throw new ArgumentException(SRClient.InvalidIssuerSecret, "issuerSecret");
			}
			return SharedSecretTokenProvider.ComputeSimpleWebTokenString(issuerName, numArray);
		}

		public static string ComputeSimpleWebTokenString(string issuerName, byte[] issuerSecret)
		{
			string base64String;
			if (string.IsNullOrEmpty(issuerName))
			{
				throw new ArgumentException(SRClient.NullIssuerName, "issuerName");
			}
			if (issuerSecret == null || (int)issuerSecret.Length < 1)
			{
				throw new ArgumentException(SRClient.NullIssuerSecret, "issuerSecret");
			}
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { "Issuer", HttpUtility.UrlEncode(issuerName) };
			string str = string.Format(invariantCulture, "{0}={1}", objArray);
			using (HMACSHA256 hMACSHA256 = new HMACSHA256(issuerSecret))
			{
				base64String = Convert.ToBase64String(hMACSHA256.ComputeHash(Encoding.UTF8.GetBytes(str)));
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(str);
			stringBuilder.Append('&');
			CultureInfo cultureInfo = CultureInfo.InvariantCulture;
			object[] objArray1 = new object[] { "HMACSHA256", HttpUtility.UrlEncode(base64String) };
			stringBuilder.Append(string.Format(cultureInfo, "{0}={1}", objArray1));
			return stringBuilder.ToString();
		}

		internal static byte[] DecodeSecret(string issuerSecret)
		{
			byte[] numArray;
			try
			{
				numArray = Convert.FromBase64String(issuerSecret);
			}
			catch (FormatException formatException)
			{
				throw new ArgumentException(SRClient.InvalidIssuerSecret, "issuerSecret");
			}
			return numArray;
		}

		protected override IAsyncResult OnBeginGetToken(string appliesTo, string action, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ValidateAction(action);
			Uri stsUri = TokenProviderHelper.GetStsUri(this.stsUri, appliesTo);
			return TokenProviderHelper.BeginGetAccessTokenByAssertion(stsUri, appliesTo, SharedSecretTokenProvider.ComputeSimpleWebTokenString(this.issuerName, this.issuerSecret), "SWT", timeout, callback, state);
		}

		protected override IAsyncResult OnBeginGetWebToken(string appliesTo, string action, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ValidateAction(action);
			Uri stsUri = TokenProviderHelper.GetStsUri(this.stsUri, appliesTo);
			TokenProviderHelper.TokenResult<string> httpAuthAccessTokenByAssertion = TokenProviderHelper.GetHttpAuthAccessTokenByAssertion(stsUri, appliesTo, SharedSecretTokenProvider.ComputeSimpleWebTokenString(this.issuerName, this.issuerSecret), "SWT", timeout);
			return new CompletedAsyncResult<TokenProviderHelper.TokenResult<string>>(httpAuthAccessTokenByAssertion, callback, state);
		}

		protected override SecurityToken OnEndGetToken(IAsyncResult result, out DateTime cacheUntil)
		{
			TokenProviderHelper.TokenResult<SecurityToken> tokenResult = TokenProviderHelper.EndGetAccessTokenByAssertion(result);
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
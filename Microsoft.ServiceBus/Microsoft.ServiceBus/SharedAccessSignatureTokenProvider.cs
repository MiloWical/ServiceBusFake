using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Microsoft.ServiceBus
{
	public class SharedAccessSignatureTokenProvider : TokenProvider
	{
		public readonly static DateTime EpochTime;

		internal readonly byte[] encodedSharedAccessKey;

		internal readonly string keyName;

		internal readonly TimeSpan tokenTimeToLive;

		private readonly string sharedAccessSignature;

		protected override bool StripQueryParameters
		{
			get
			{
				return false;
			}
		}

		static SharedAccessSignatureTokenProvider()
		{
			SharedAccessSignatureTokenProvider.EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		}

		internal SharedAccessSignatureTokenProvider(string sharedAccessSignature) : base(false, true, Microsoft.ServiceBus.TokenScope.Entity)
		{
			SharedAccessSignatureToken.Validate(sharedAccessSignature);
			this.sharedAccessSignature = sharedAccessSignature;
		}

		internal SharedAccessSignatureTokenProvider(string keyName, string sharedAccessKey, TimeSpan tokenTimeToLive) : this(keyName, sharedAccessKey, tokenTimeToLive, Microsoft.ServiceBus.TokenScope.Entity)
		{
		}

		internal SharedAccessSignatureTokenProvider(string keyName, string sharedAccessKey, TimeSpan tokenTimeToLive, Microsoft.ServiceBus.TokenScope tokenScope) : base(true, true, tokenScope)
		{
			if (string.IsNullOrEmpty(keyName))
			{
				throw new ArgumentNullException("keyName");
			}
			if (keyName.Length > 256)
			{
				throw new ArgumentOutOfRangeException("keyName", SRCore.ArgumentStringTooBig("keyName", 256));
			}
			if (string.IsNullOrEmpty(sharedAccessKey))
			{
				throw new ArgumentNullException("sharedAccessKey");
			}
			if (sharedAccessKey.Length > 256)
			{
				throw new ArgumentOutOfRangeException("sharedAccessKey", SRCore.ArgumentStringTooBig("sharedAccessKey", 256));
			}
			this.encodedSharedAccessKey = Encoding.UTF8.GetBytes(sharedAccessKey);
			this.keyName = keyName;
			this.tokenTimeToLive = tokenTimeToLive;
		}

		protected override TokenProvider.Key BuildKey(string appliesTo, string action)
		{
			return new TokenProvider.Key(appliesTo, string.Empty);
		}

		private string BuildSignature(string targetUri)
		{
			string str;
			str = (!string.IsNullOrWhiteSpace(this.sharedAccessSignature) ? this.sharedAccessSignature : SharedAccessSignatureTokenProvider.SharedAccessSignatureBuilder.BuildSignature(this.keyName, this.encodedSharedAccessKey, targetUri, this.tokenTimeToLive));
			return str;
		}

		public static string GetSharedAccessSignature(string keyName, string sharedAccessKey, string resource, TimeSpan tokenTimeToLive)
		{
			if (string.IsNullOrWhiteSpace(keyName))
			{
				throw new ArgumentException("keyName");
			}
			if (string.IsNullOrWhiteSpace(sharedAccessKey))
			{
				throw new ArgumentException("sharedAccessKey");
			}
			if (string.IsNullOrWhiteSpace(resource))
			{
				throw new ArgumentException("resource");
			}
			if (tokenTimeToLive < TimeSpan.Zero)
			{
				throw new ArgumentException("tokenTimeToLive");
			}
			byte[] bytes = Encoding.UTF8.GetBytes(sharedAccessKey);
			return SharedAccessSignatureTokenProvider.SharedAccessSignatureBuilder.BuildSignature(keyName, bytes, resource, tokenTimeToLive);
		}

		protected override IAsyncResult OnBeginGetToken(string appliesTo, string action, TimeSpan timeout, AsyncCallback callback, object state)
		{
			SharedAccessSignatureToken sharedAccessSignatureToken = new SharedAccessSignatureToken(this.BuildSignature(appliesTo));
			TokenProviderHelper.TokenResult<SecurityToken> tokenResult = new TokenProviderHelper.TokenResult<SecurityToken>()
			{
				CacheUntil = sharedAccessSignatureToken.ExpiresOn,
				Token = sharedAccessSignatureToken
			};
			return new CompletedAsyncResult<TokenProviderHelper.TokenResult<SecurityToken>>(tokenResult, callback, state);
		}

		protected override IAsyncResult OnBeginGetWebToken(string appliesTo, string action, TimeSpan timeout, AsyncCallback callback, object state)
		{
			string str = this.BuildSignature(appliesTo);
			DateTime utcNow = DateTime.UtcNow + this.tokenTimeToLive;
			TokenProviderHelper.TokenResult<string> tokenResult = new TokenProviderHelper.TokenResult<string>()
			{
				CacheUntil = utcNow,
				Token = str
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

		internal static class SharedAccessSignatureBuilder
		{
			private static string BuildExpiresOn(TimeSpan timeToLive)
			{
				DateTime dateTime = DateTime.UtcNow.Add(timeToLive);
				TimeSpan timeSpan = dateTime.Subtract(SharedAccessSignatureTokenProvider.EpochTime);
				long num = Convert.ToInt64(timeSpan.TotalSeconds, CultureInfo.InvariantCulture);
				return Convert.ToString(num, CultureInfo.InvariantCulture);
			}

			public static string BuildSignature(string keyName, byte[] encodedSharedAccessKey, string targetUri, TimeSpan timeToLive)
			{
				string str = SharedAccessSignatureTokenProvider.SharedAccessSignatureBuilder.BuildExpiresOn(timeToLive);
				string str1 = HttpUtility.UrlEncode(targetUri.ToLowerInvariant());
				List<string> strs = new List<string>()
				{
					str1,
					str
				};
				string str2 = SharedAccessSignatureTokenProvider.SharedAccessSignatureBuilder.Sign(string.Join("\n", strs), encodedSharedAccessKey);
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { "SharedAccessSignature", "sr", str1, "sig", HttpUtility.UrlEncode(str2), "se", HttpUtility.UrlEncode(str), "skn", HttpUtility.UrlEncode(keyName) };
				return string.Format(invariantCulture, "{0} {1}={2}&{3}={4}&{5}={6}&{7}={8}", objArray);
			}

			private static string Sign(string requestString, byte[] encodedSharedAccessKey)
			{
				string base64String;
				using (HMACSHA256 hMACSHA256 = new HMACSHA256(encodedSharedAccessKey))
				{
					base64String = Convert.ToBase64String(hMACSHA256.ComputeHash(Encoding.UTF8.GetBytes(requestString)));
				}
				return base64String;
			}
		}
	}
}
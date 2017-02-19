using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.PerformanceCounters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus
{
	public abstract class TokenProvider
	{
		private const int DefaultCacheSize = 1000;

		private const Microsoft.ServiceBus.TokenScope DefaultTokenScope = Microsoft.ServiceBus.TokenScope.Entity;

		internal readonly static TimeSpan DefaultTokenTimeout;

		private readonly static TimeSpan InitialRetrySleepTime;

		private readonly static TimeSpan MaxRetrySleepTime;

		private readonly bool isWebTokenSupported;

		private readonly object mutex = new object();

		private readonly TimeSpan retrySleepTime;

		private int cacheSize;

		private MruCache<TokenProvider.Key, TokenProvider.TokenInfo> tokenCache;

		public int CacheSize
		{
			get
			{
				int num;
				lock (this.mutex)
				{
					num = this.cacheSize;
				}
				return num;
			}
			set
			{
				lock (this.mutex)
				{
					if (value < 1)
					{
						throw new ArgumentOutOfRangeException("value", SRClient.ArgumentOutOfRangeLessThanOne);
					}
					this.cacheSize = value;
					this.Clear();
				}
			}
		}

		public bool CacheTokens
		{
			get
			{
				bool flag;
				lock (this.mutex)
				{
					flag = this.tokenCache != null;
				}
				return flag;
			}
			set
			{
				lock (this.mutex)
				{
					if (!value)
					{
						this.tokenCache = null;
					}
					else
					{
						this.tokenCache = new MruCache<TokenProvider.Key, TokenProvider.TokenInfo>(this.cacheSize);
					}
				}
			}
		}

		public bool IsWebTokenSupported
		{
			get
			{
				return this.isWebTokenSupported;
			}
		}

		protected virtual bool StripQueryParameters
		{
			get
			{
				return true;
			}
		}

		internal MruCache<TokenProvider.Key, TokenProvider.TokenInfo> TokenCache
		{
			get
			{
				return this.tokenCache;
			}
		}

		public Microsoft.ServiceBus.TokenScope TokenScope
		{
			get;
			private set;
		}

		static TokenProvider()
		{
			TokenProvider.DefaultTokenTimeout = TimeSpan.FromMinutes(20);
			TokenProvider.InitialRetrySleepTime = TimeSpan.FromMilliseconds(50);
			TokenProvider.MaxRetrySleepTime = TimeSpan.FromSeconds(3);
		}

		protected TokenProvider(bool cacheTokens, bool supportHttpAuthToken) : this(cacheTokens, supportHttpAuthToken, 1000, Microsoft.ServiceBus.TokenScope.Entity)
		{
		}

		protected TokenProvider(bool cacheTokens, bool supportHttpAuthToken, Microsoft.ServiceBus.TokenScope tokenScope) : this(cacheTokens, supportHttpAuthToken, 1000, tokenScope)
		{
		}

		protected TokenProvider(bool cacheTokens, bool supportHttpAuthToken, int cacheSize, Microsoft.ServiceBus.TokenScope tokenScope)
		{
			if (cacheSize < 1)
			{
				throw new ArgumentOutOfRangeException("cacheSize", SRClient.ArgumentOutOfRangeLessThanOne);
			}
			this.TokenScope = tokenScope;
			this.cacheSize = cacheSize;
			this.CacheTokens = cacheTokens;
			this.isWebTokenSupported = supportHttpAuthToken;
			this.retrySleepTime = TokenProvider.InitialRetrySleepTime;
		}

		public IAsyncResult BeginGetToken(string appliesTo, string action, bool bypassCache, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginGetToken(null, appliesTo, action, bypassCache, timeout, callback, state);
		}

		internal IAsyncResult BeginGetToken(Uri namespaceAddress, string appliesTo, string action, bool bypassCache, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (string.IsNullOrEmpty(appliesTo))
			{
				throw new ArgumentException(SRClient.NullAppliesTo);
			}
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}
			if (timeout > TimeoutHelper.MaxWait)
			{
				timeout = TimeoutHelper.MaxWait;
			}
			TokenProvider.ValidateTimeout(timeout);
			string str = this.NormalizeAppliesTo(namespaceAddress, appliesTo);
			return new TokenProvider.GetTokenAsyncResult(this, str, action, bypassCache, timeout, callback, state);
		}

		public IAsyncResult BeginGetWebToken(string appliesTo, string action, bool bypassCache, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginGetWebToken(null, appliesTo, action, bypassCache, timeout, callback, state);
		}

		internal IAsyncResult BeginGetWebToken(Uri namespaceAddress, string appliesTo, string action, bool bypassCache, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (string.IsNullOrEmpty(appliesTo))
			{
				throw new ArgumentException(SRClient.NullAppliesTo);
			}
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}
			if (timeout > TimeoutHelper.MaxWait)
			{
				timeout = TimeoutHelper.MaxWait;
			}
			TokenProvider.ValidateTimeout(timeout);
			if (!this.IsWebTokenSupported)
			{
				throw new InvalidOperationException(SRClient.BeginGetWebTokenNotSupported);
			}
			string str = this.NormalizeAppliesTo(namespaceAddress, appliesTo);
			return new TokenProvider.GetWebTokenAsyncResult(this, str, action, bypassCache, timeout, callback, state);
		}

		protected virtual TokenProvider.Key BuildKey(string appliesTo, string action)
		{
			return new TokenProvider.Key(appliesTo, action);
		}

		public void Clear()
		{
			lock (this.mutex)
			{
				if (this.tokenCache != null)
				{
					this.tokenCache = new MruCache<TokenProvider.Key, TokenProvider.TokenInfo>(this.cacheSize);
				}
			}
		}

		public static TokenProvider CreateOAuthTokenProvider(IEnumerable<Uri> stsUris, NetworkCredential credential)
		{
			return new OAuthTokenProvider(stsUris, credential);
		}

		public static TokenProvider CreateSamlTokenProvider(string samlToken)
		{
			return new SamlTokenProvider(samlToken);
		}

		public static TokenProvider CreateSamlTokenProvider(string samlToken, Uri stsUri)
		{
			return new SamlTokenProvider(samlToken, stsUri);
		}

		public static TokenProvider CreateSamlTokenProvider(string samlToken, Uri stsUri, int cacheSize)
		{
			return new SamlTokenProvider(samlToken, stsUri)
			{
				CacheSize = cacheSize
			};
		}

		public static TokenProvider CreateSamlTokenProvider(string samlToken, Microsoft.ServiceBus.TokenScope tokenScope)
		{
			return new SamlTokenProvider(samlToken, tokenScope);
		}

		public static TokenProvider CreateSamlTokenProvider(string samlToken, Uri stsUri, Microsoft.ServiceBus.TokenScope tokenScope)
		{
			return new SamlTokenProvider(samlToken, stsUri, tokenScope);
		}

		public static TokenProvider CreateSamlTokenProvider(string samlToken, Uri stsUri, int cacheSize, Microsoft.ServiceBus.TokenScope tokenScope)
		{
			return new SamlTokenProvider(samlToken, stsUri, tokenScope)
			{
				CacheSize = cacheSize
			};
		}

		public static TokenProvider CreateSharedAccessSignatureTokenProvider(string sharedAccessSignature)
		{
			return new SharedAccessSignatureTokenProvider(sharedAccessSignature);
		}

		public static TokenProvider CreateSharedAccessSignatureTokenProvider(string keyName, string sharedAccessKey)
		{
			return new SharedAccessSignatureTokenProvider(keyName, sharedAccessKey, TokenProvider.DefaultTokenTimeout);
		}

		public static TokenProvider CreateSharedAccessSignatureTokenProvider(string keyName, string sharedAccessKey, TimeSpan tokenTimeToLive)
		{
			return new SharedAccessSignatureTokenProvider(keyName, sharedAccessKey, tokenTimeToLive);
		}

		public static TokenProvider CreateSharedAccessSignatureTokenProvider(string keyName, string sharedAccessKey, Microsoft.ServiceBus.TokenScope tokenScope)
		{
			return new SharedAccessSignatureTokenProvider(keyName, sharedAccessKey, TokenProvider.DefaultTokenTimeout, tokenScope);
		}

		public static TokenProvider CreateSharedAccessSignatureTokenProvider(string keyName, string sharedAccessKey, TimeSpan tokenTimeToLive, Microsoft.ServiceBus.TokenScope tokenScope)
		{
			return new SharedAccessSignatureTokenProvider(keyName, sharedAccessKey, tokenTimeToLive, tokenScope);
		}

		public static TokenProvider CreateSharedSecretTokenProvider(string issuerName, string issuerSecret)
		{
			return new SharedSecretTokenProvider(issuerName, issuerSecret);
		}

		public static TokenProvider CreateSharedSecretTokenProvider(string issuerName, string issuerSecret, Uri stsUri)
		{
			return new SharedSecretTokenProvider(issuerName, issuerSecret, stsUri);
		}

		public static TokenProvider CreateSharedSecretTokenProvider(string issuerName, byte[] issuerSecret)
		{
			return new SharedSecretTokenProvider(issuerName, issuerSecret);
		}

		public static TokenProvider CreateSharedSecretTokenProvider(string issuerName, byte[] issuerSecret, Uri stsUri)
		{
			return new SharedSecretTokenProvider(issuerName, issuerSecret, stsUri);
		}

		public static TokenProvider CreateSharedSecretTokenProvider(string issuerName, string issuerSecret, Microsoft.ServiceBus.TokenScope tokenScope)
		{
			return new SharedSecretTokenProvider(issuerName, issuerSecret, tokenScope);
		}

		public static TokenProvider CreateSharedSecretTokenProvider(string issuerName, string issuerSecret, Uri stsUri, Microsoft.ServiceBus.TokenScope tokenScope)
		{
			return new SharedSecretTokenProvider(issuerName, issuerSecret, stsUri, tokenScope);
		}

		public static TokenProvider CreateSharedSecretTokenProvider(string issuerName, byte[] issuerSecret, Microsoft.ServiceBus.TokenScope tokenScope)
		{
			return new SharedSecretTokenProvider(issuerName, issuerSecret, tokenScope);
		}

		public static TokenProvider CreateSharedSecretTokenProvider(string issuerName, byte[] issuerSecret, Uri stsUri, Microsoft.ServiceBus.TokenScope tokenScope)
		{
			return new SharedSecretTokenProvider(issuerName, issuerSecret, stsUri, tokenScope);
		}

		public static TokenProvider CreateSimpleWebTokenProvider(string token)
		{
			return new SimpleWebTokenProvider(token);
		}

		public static TokenProvider CreateSimpleWebTokenProvider(string token, Uri stsUri)
		{
			return new SimpleWebTokenProvider(token, stsUri);
		}

		public static TokenProvider CreateSimpleWebTokenProvider(string token, Microsoft.ServiceBus.TokenScope tokenScope)
		{
			return new SimpleWebTokenProvider(token, tokenScope);
		}

		public static TokenProvider CreateSimpleWebTokenProvider(string token, Uri stsUri, Microsoft.ServiceBus.TokenScope tokenScope)
		{
			return new SimpleWebTokenProvider(token, stsUri, tokenScope);
		}

		public static TokenProvider CreateWindowsTokenProvider(IEnumerable<Uri> stsUris)
		{
			return new WindowsTokenProvider(stsUris, null);
		}

		public static TokenProvider CreateWindowsTokenProvider(IEnumerable<Uri> stsUris, NetworkCredential credential)
		{
			return new WindowsTokenProvider(stsUris, credential);
		}

		public SecurityToken EndGetToken(IAsyncResult result)
		{
			return AsyncResult<TokenProvider.GetTokenAsyncResult>.End(result).SecurityToken;
		}

		public string EndGetWebToken(IAsyncResult result)
		{
			return AsyncResult<TokenProvider.GetWebTokenAsyncResult>.End(result).WebToken;
		}

		public Task<SecurityToken> GetTokenAsync(string appliesTo, string action, bool bypassCache, TimeSpan timeout)
		{
			return TaskHelpers.CreateTask<SecurityToken>((AsyncCallback c, object s) => this.BeginGetToken(appliesTo, action, bypassCache, timeout, c, s), new Func<IAsyncResult, SecurityToken>(this.EndGetToken));
		}

		private TokenProvider.TokenInfo GetTokenInfoFromCache(TokenProvider.Key key)
		{
			TokenProvider.TokenInfo tokenInfo;
			TokenProvider.TokenInfo tokenInfo1;
			lock (this.mutex)
			{
				if (!this.CacheTokens)
				{
					tokenInfo1 = new TokenProvider.TokenInfo();
				}
				else if (this.tokenCache.TryGetValue(key, out tokenInfo))
				{
					DateTime utcNow = DateTime.UtcNow;
					if (tokenInfo.WebTokenCacheUntil < utcNow)
					{
						tokenInfo.ResetWebToken();
					}
					if (tokenInfo.SecurityTokenCacheUntil < utcNow)
					{
						tokenInfo.ResetSecurityToken();
					}
					tokenInfo1 = tokenInfo;
				}
				else
				{
					tokenInfo = new TokenProvider.TokenInfo();
					this.tokenCache.Add(key, tokenInfo);
					tokenInfo1 = tokenInfo;
				}
			}
			return tokenInfo1;
		}

		public Task<string> GetWebTokenAsync(string appliesTo, string action, bool bypassCache, TimeSpan timeout)
		{
			return TaskHelpers.CreateTask<string>((AsyncCallback c, object s) => this.BeginGetWebToken(appliesTo, action, bypassCache, timeout, c, s), new Func<IAsyncResult, string>(this.EndGetWebToken));
		}

		protected virtual string NormalizeAppliesTo(string appliesTo)
		{
			return ServiceBusUriHelper.NormalizeUri(appliesTo, "http", this.StripQueryParameters, this.TokenScope == Microsoft.ServiceBus.TokenScope.Namespace, true);
		}

		private string NormalizeAppliesTo(Uri namespaceAddress, string appliesTo)
		{
			if (namespaceAddress == null)
			{
				return this.NormalizeAppliesTo(appliesTo);
			}
			string str = (this.TokenScope != Microsoft.ServiceBus.TokenScope.Namespace || !(namespaceAddress != null) ? appliesTo : namespaceAddress.AbsoluteUri);
			return ServiceBusUriHelper.NormalizeUri(str, "http", this.StripQueryParameters, false, true);
		}

		protected abstract IAsyncResult OnBeginGetToken(string appliesTo, string action, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginGetWebToken(string appliesTo, string action, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract SecurityToken OnEndGetToken(IAsyncResult result, out DateTime cacheUntil);

		protected abstract string OnEndGetWebToken(IAsyncResult result, out DateTime cacheUntil);

		internal void ValidateAction(string action)
		{
			string str = action;
			string str1 = str;
			if (str == null || !(str1 == "Send") && !(str1 == "Listen") && !(str1 == "Manage"))
			{
				throw new ArgumentException(SRClient.UnsupportedAction(action), "action");
			}
		}

		private static void ValidateTimeout(TimeSpan timeout)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException("timeout");
			}
		}

		private sealed class GetTokenAsyncResult : TokenProvider.GetTokenAsyncResultBase<TokenProvider.GetTokenAsyncResult>
		{
			protected override IteratorAsyncResult<TokenProvider.GetTokenAsyncResult>.BeginCall GetTokenBeginCall
			{
				get
				{
					return (TokenProvider.GetTokenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.TokenProvider.OnBeginGetToken(thisPtr.AppliesTo, thisPtr.Action, t, c, s);
				}
			}

			public SecurityToken SecurityToken
			{
				get;
				private set;
			}

			internal GetTokenAsyncResult(TokenProvider tokenProvider, string appliesTo, string action, bool bypassCache, TimeSpan timeout, AsyncCallback callback, object state) : base(tokenProvider, appliesTo, action, bypassCache, timeout, callback, state)
			{
				base.Start();
			}

			protected override void OnEndTokenProviderCallback(IAsyncResult result, out DateTime cacheUntil)
			{
				this.SecurityToken = base.TokenProvider.OnEndGetToken(result, out cacheUntil);
			}

			protected override bool OnProcessCachedEntryFromTokenProvider(TokenProvider.TokenInfo tokenInfo)
			{
				this.SecurityToken = tokenInfo.SecurityToken;
				return this.SecurityToken != null;
			}

			protected override void OnUpdateTokenProviderCacheEntry(DateTime cacheUntil, ref TokenProvider.TokenInfo tokenInfo)
			{
				if (tokenInfo.SecurityToken == null || cacheUntil > tokenInfo.SecurityTokenCacheUntil)
				{
					tokenInfo.SecurityToken = this.SecurityToken;
					tokenInfo.SecurityTokenCacheUntil = cacheUntil;
				}
			}
		}

		private abstract class GetTokenAsyncResultBase<T> : IteratorAsyncResult<T>
		where T : TokenProvider.GetTokenAsyncResultBase<T>
		{
			private readonly TokenProvider.Key cacheKey;

			private readonly bool bypassCache;

			private readonly Uri appliesToUri;

			private TimeSpan retrySleepTime;

			protected string Action
			{
				get;
				private set;
			}

			protected string AppliesTo
			{
				get;
				private set;
			}

			protected abstract IteratorAsyncResult<T>.BeginCall GetTokenBeginCall
			{
				get;
			}

			protected TokenProvider TokenProvider
			{
				get;
				private set;
			}

			protected GetTokenAsyncResultBase(TokenProvider tokenProvider, string appliesTo, string action, bool bypassCache, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.TokenProvider = tokenProvider;
				this.cacheKey = this.TokenProvider.BuildKey(appliesTo, action);
				this.retrySleepTime = this.TokenProvider.retrySleepTime;
				this.bypassCache = bypassCache;
				this.AppliesTo = appliesTo;
				this.Action = action;
				if (!Uri.TryCreate(appliesTo, UriKind.RelativeOrAbsolute, out this.appliesToUri))
				{
					UriBuilder uriBuilder = new UriBuilder()
					{
						Scheme = "sb",
						Host = appliesTo
					};
					this.appliesToUri = uriBuilder.Uri;
				}
			}

			protected override IEnumerator<IteratorAsyncResult<T>.AsyncStep> GetAsyncSteps()
			{
				bool flag;
				TimeSpan timeSpan;
				while (true)
				{
					bool flag1 = false;
					if (!this.bypassCache)
					{
						lock (this.TokenProvider.mutex)
						{
							TokenProvider.TokenInfo tokenInfoFromCache = this.TokenProvider.GetTokenInfoFromCache(this.cacheKey);
							flag = this.OnProcessCachedEntryFromTokenProvider(tokenInfoFromCache);
						}
						if (flag)
						{
							break;
						}
					}
					Stopwatch stopwatch = Stopwatch.StartNew();
					try
					{
						TokenProvider.GetTokenAsyncResultBase<T> getTokenAsyncResultBase = this;
						IteratorAsyncResult<T>.BeginCall getTokenBeginCall = this.GetTokenBeginCall;
						yield return getTokenAsyncResultBase.CallAsync(getTokenBeginCall, (T thisPtr, IAsyncResult r) => thisPtr.OnCompletion(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException != null)
						{
							goto Label0;
						}
						stopwatch.Stop();
						MessagingPerformanceCounters.IncrementTokenAcquisitionLatency(this.appliesToUri, stopwatch.ElapsedTicks);
						MessagingPerformanceCounters.IncrementTokensAcquiredPerSec(this.appliesToUri, 1);
						break;
					}
					finally
					{
						stopwatch.Stop();
					}
				Label0:
					MessagingPerformanceCounters.IncrementTokenAcquisitionFailuresPerSec(this.appliesToUri, 1);
					SecurityTokenException lastAsyncStepException = base.LastAsyncStepException as SecurityTokenException;
					TokenProviderException tokenProviderException = base.LastAsyncStepException as TokenProviderException;
					TimeoutException timeoutException = base.LastAsyncStepException as TimeoutException;
					if (timeoutException != null && timeoutException.InnerException != null && timeoutException.InnerException is WebException)
					{
						flag1 = true;
					}
					else if (tokenProviderException != null && tokenProviderException.InnerException != null && tokenProviderException.InnerException is WebException)
					{
						flag1 = true;
					}
					else if (lastAsyncStepException != null)
					{
						TokenProviderHelper.InternalSecurityTokenException internalSecurityTokenException = lastAsyncStepException as TokenProviderHelper.InternalSecurityTokenException;
						flag1 = true;
						if (internalSecurityTokenException != null)
						{
							base.LastAsyncStepException = new SecurityTokenException(internalSecurityTokenException.Message, internalSecurityTokenException.InnerException);
							switch (internalSecurityTokenException.StatusCode)
							{
								case HttpStatusCode.BadRequest:
								case HttpStatusCode.Unauthorized:
								{
									flag1 = false;
									break;
								}
							}
						}
					}
					if (flag1)
					{
						TimeSpan timeSpan1 = base.RemainingTime();
						if (timeSpan1 <= TimeSpan.Zero)
						{
							flag1 = false;
						}
						else
						{
							yield return base.CallAsyncSleep(TimeoutHelper.Min(this.retrySleepTime, timeSpan1));
							TimeSpan timeSpan2 = this.retrySleepTime.Add(this.retrySleepTime);
							TokenProvider.GetTokenAsyncResultBase<T> getTokenAsyncResultBase1 = this;
							timeSpan = (timeSpan2 < TokenProvider.MaxRetrySleepTime ? timeSpan2 : TokenProvider.MaxRetrySleepTime);
							getTokenAsyncResultBase1.retrySleepTime = timeSpan;
						}
					}
					if (!flag1)
					{
						if (base.LastAsyncStepException == null)
						{
							break;
						}
						base.Complete(base.LastAsyncStepException);
						break;
					}
				}
			}

			private void OnCompletion(IAsyncResult result)
			{
				DateTime dateTime;
				this.OnEndTokenProviderCallback(result, out dateTime);
				if (dateTime >= DateTime.UtcNow)
				{
					lock (this.TokenProvider.mutex)
					{
						TokenProvider.TokenInfo tokenInfoFromCache = this.TokenProvider.GetTokenInfoFromCache(this.cacheKey);
						this.OnUpdateTokenProviderCacheEntry(dateTime, ref tokenInfoFromCache);
					}
				}
			}

			protected abstract void OnEndTokenProviderCallback(IAsyncResult result, out DateTime cacheUntil);

			protected abstract bool OnProcessCachedEntryFromTokenProvider(TokenProvider.TokenInfo tokenInfo);

			protected abstract void OnUpdateTokenProviderCacheEntry(DateTime cacheUntil, ref TokenProvider.TokenInfo tokenInfo);
		}

		private sealed class GetWebTokenAsyncResult : TokenProvider.GetTokenAsyncResultBase<TokenProvider.GetWebTokenAsyncResult>
		{
			protected override IteratorAsyncResult<TokenProvider.GetWebTokenAsyncResult>.BeginCall GetTokenBeginCall
			{
				get
				{
					return (TokenProvider.GetWebTokenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.TokenProvider.OnBeginGetWebToken(thisPtr.AppliesTo, thisPtr.Action, t, c, s);
				}
			}

			public string WebToken
			{
				get;
				private set;
			}

			internal GetWebTokenAsyncResult(TokenProvider tokenProvider, string appliesTo, string action, bool bypassCache, TimeSpan timeout, AsyncCallback callback, object state) : base(tokenProvider, appliesTo, action, bypassCache, timeout, callback, state)
			{
				base.Start();
			}

			protected override void OnEndTokenProviderCallback(IAsyncResult result, out DateTime cacheUntil)
			{
				this.WebToken = base.TokenProvider.OnEndGetWebToken(result, out cacheUntil);
			}

			protected override bool OnProcessCachedEntryFromTokenProvider(TokenProvider.TokenInfo tokenInfo)
			{
				this.WebToken = tokenInfo.WebToken;
				return this.WebToken != null;
			}

			protected override void OnUpdateTokenProviderCacheEntry(DateTime cacheUntil, ref TokenProvider.TokenInfo tokenInfo)
			{
				if (tokenInfo.WebToken == null || cacheUntil > tokenInfo.WebTokenCacheUntil)
				{
					tokenInfo.WebToken = this.WebToken;
					tokenInfo.WebTokenCacheUntil = cacheUntil;
				}
			}
		}

		protected internal class Key : IEquatable<TokenProvider.Key>
		{
			private readonly string appliesTo;

			private readonly string claim;

			public Key(string appliesTo, string claim)
			{
				if (appliesTo == null)
				{
					throw new ArgumentNullException("appliesTo");
				}
				if (claim == null)
				{
					throw new ArgumentNullException("claim");
				}
				this.appliesTo = appliesTo;
				this.claim = claim;
			}

			public override bool Equals(object obj)
			{
				return this.Equals(obj as TokenProvider.Key);
			}

			public bool Equals(TokenProvider.Key other)
			{
				if (object.ReferenceEquals(null, other))
				{
					return false;
				}
				if (object.ReferenceEquals(this, other))
				{
					return true;
				}
				if (!object.Equals(other.appliesTo, this.appliesTo))
				{
					return false;
				}
				return object.Equals(other.claim, this.claim);
			}

			public override int GetHashCode()
			{
				return this.appliesTo.GetHashCode() * 397 ^ this.claim.GetHashCode();
			}
		}

		internal class TokenInfo
		{
			internal SecurityToken SecurityToken
			{
				get;
				set;
			}

			internal DateTime SecurityTokenCacheUntil
			{
				get;
				set;
			}

			internal string WebToken
			{
				get;
				set;
			}

			internal DateTime WebTokenCacheUntil
			{
				get;
				set;
			}

			internal TokenInfo()
			{
				this.ResetSecurityToken();
				this.ResetWebToken();
			}

			internal void ResetSecurityToken()
			{
				this.SecurityToken = null;
				this.SecurityTokenCacheUntil = DateTime.MaxValue;
			}

			internal void ResetWebToken()
			{
				this.WebToken = null;
				this.WebTokenCacheUntil = DateTime.MaxValue;
			}
		}
	}
}
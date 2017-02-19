using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text.RegularExpressions;
using System.Web.Configuration;

namespace Microsoft.ServiceBus.Messaging.Configuration
{
	internal class KeyValueConfigurationManager
	{
		public const string ServiceBusConnectionKeyName = "Microsoft.ServiceBus.ConnectionString";

		public const string OperationTimeoutConfigName = "OperationTimeout";

		public const string EndpointConfigName = "Endpoint";

		public const string SharedSecretIssuerConfigName = "SharedSecretIssuer";

		public const string SharedSecretValueConfigName = "SharedSecretValue";

		public const string SharedAccessKeyName = "SharedAccessKeyName";

		public const string SharedAccessValueName = "SharedAccessKey";

		public const string RuntimePortConfigName = "RuntimePort";

		public const string ManagementPortConfigName = "ManagementPort";

		public const string StsEndpointConfigName = "StsEndpoint";

		public const string WindowsDomainConfigName = "WindowsDomain";

		public const string WindowsUsernameConfigName = "WindowsUsername";

		public const string WindowsPasswordConfigName = "WindowsPassword";

		public const string OAuthDomainConfigName = "OAuthDomain";

		public const string OAuthUsernameConfigName = "OAuthUsername";

		public const string OAuthPasswordConfigName = "OAuthPassword";

		public const string TransportTypeConfigName = "TransportType";

		internal const string ValueSeparator = ",";

		internal const string KeyValueSeparator = "=";

		internal const string KeyDelimiter = ";";

		private const string KeyAttributeEnumRegexString = "(OperationTimeout|Endpoint|RuntimePort|ManagementPort|StsEndpoint|WindowsDomain|WindowsUsername|WindowsPassword|OAuthDomain|OAuthUsername|OAuthPassword|SharedSecretIssuer|SharedSecretValue|SharedAccessKeyName|SharedAccessKey|TransportType)";

		private const string KeyDelimiterRegexString = ";(OperationTimeout|Endpoint|RuntimePort|ManagementPort|StsEndpoint|WindowsDomain|WindowsUsername|WindowsPassword|OAuthDomain|OAuthUsername|OAuthPassword|SharedSecretIssuer|SharedSecretValue|SharedAccessKeyName|SharedAccessKey|TransportType)=";

		private readonly static Regex KeyRegex;

		private readonly static Regex ValueRegex;

		internal readonly static Lazy<ConcurrentDictionary<string, MessagingFactory>> CachedFactories;

		internal NameValueCollection connectionProperties;

		internal string connectionString;

		public string this[string key]
		{
			get
			{
				return this.connectionProperties[key];
			}
		}

		static KeyValueConfigurationManager()
		{
			KeyValueConfigurationManager.KeyRegex = new Regex("(OperationTimeout|Endpoint|RuntimePort|ManagementPort|StsEndpoint|WindowsDomain|WindowsUsername|WindowsPassword|OAuthDomain|OAuthUsername|OAuthPassword|SharedSecretIssuer|SharedSecretValue|SharedAccessKeyName|SharedAccessKey|TransportType)", RegexOptions.IgnoreCase);
			KeyValueConfigurationManager.ValueRegex = new Regex("([^\\s]+)");
			KeyValueConfigurationManager.CachedFactories = new Lazy<ConcurrentDictionary<string, MessagingFactory>>();
		}

		public KeyValueConfigurationManager() : this(null)
		{
		}

		public KeyValueConfigurationManager(Microsoft.ServiceBus.Messaging.TransportType? transportType)
		{
			string item = null;
			try
			{
				if (WebConfigurationManager.AppSettings.Count > 0)
				{
					item = WebConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];
				}
			}
			catch (ConfigurationErrorsException configurationErrorsException)
			{
			}
			if (string.IsNullOrWhiteSpace(this.connectionString))
			{
				item = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];
			}
			this.Initialize(item, transportType);
		}

		public KeyValueConfigurationManager(string connectionString) : this(connectionString, null)
		{
		}

		public KeyValueConfigurationManager(string connectionString, Microsoft.ServiceBus.Messaging.TransportType? transportType)
		{
			this.Initialize(connectionString, transportType);
		}

		private MessagingFactory CreateFactory(IEnumerable<Uri> endpoints, IEnumerable<Uri> stsEndpoints, string operationTimeout, string issuerName, string issuerKey, string sasKeyName, string sasKey, string windowsDomain, string windowsUser, SecureString windowsPassword, string oauthDomain, string oauthUser, SecureString oauthPassword, string transportType)
		{
			MessagingFactory messagingFactory;
			try
			{
				MessagingFactorySettings messagingFactorySetting = new MessagingFactorySettings();
				if (!string.IsNullOrWhiteSpace(transportType))
				{
					messagingFactorySetting.TransportType = (Microsoft.ServiceBus.Messaging.TransportType)Enum.Parse(typeof(Microsoft.ServiceBus.Messaging.TransportType), transportType);
				}
				messagingFactorySetting.TokenProvider = KeyValueConfigurationManager.CreateTokenProvider(stsEndpoints, issuerName, issuerKey, sasKeyName, sasKey, windowsDomain, windowsUser, windowsPassword, oauthDomain, oauthUser, oauthPassword);
				if (!string.IsNullOrEmpty(operationTimeout))
				{
					messagingFactorySetting.OperationTimeout = TimeSpan.Parse(operationTimeout, CultureInfo.CurrentCulture);
				}
				messagingFactory = MessagingFactory.Create(endpoints, messagingFactorySetting);
			}
			catch (ArgumentException argumentException1)
			{
				ArgumentException argumentException = argumentException1;
				throw new ConfigurationErrorsException(SRClient.AppSettingsCreateFactoryWithInvalidConnectionString(argumentException.Message), argumentException);
			}
			catch (UriFormatException uriFormatException1)
			{
				UriFormatException uriFormatException = uriFormatException1;
				throw new ConfigurationErrorsException(SRClient.AppSettingsCreateFactoryWithInvalidConnectionString(uriFormatException.Message), uriFormatException);
			}
			return messagingFactory;
		}

		public MessagingFactory CreateMessagingFactory()
		{
			return this.CreateMessagingFactory(true);
		}

		public MessagingFactory CreateMessagingFactory(bool useCachedFactory)
		{
			this.Validate();
			string item = this.connectionProperties["OperationTimeout"];
			IEnumerable<Uri> endpointAddresses = this.GetEndpointAddresses();
			IEnumerable<Uri> uris = KeyValueConfigurationManager.GetEndpointAddresses(this.connectionProperties["StsEndpoint"], null);
			string str = this.connectionProperties["SharedSecretIssuer"];
			string item1 = this.connectionProperties["SharedSecretValue"];
			string str1 = this.connectionProperties["SharedAccessKeyName"];
			string item2 = this.connectionProperties["SharedAccessKey"];
			string str2 = this.connectionProperties["WindowsDomain"];
			string item3 = this.connectionProperties["WindowsUsername"];
			SecureString windowsPassword = this.GetWindowsPassword();
			string str3 = this.connectionProperties["OAuthDomain"];
			string item4 = this.connectionProperties["OAuthUsername"];
			SecureString oAuthPassword = this.GetOAuthPassword();
			string str4 = this.connectionProperties["TransportType"];
			if (!useCachedFactory)
			{
				return this.CreateFactory(endpointAddresses, uris, item, str, item1, str1, item2, str2, item3, windowsPassword, str3, item4, oAuthPassword, str4);
			}
			return this.GetOrUpdateFactory(endpointAddresses, uris, item, str, item1, str1, item2, str2, item3, windowsPassword, str3, item4, oAuthPassword, str4);
		}

		public NamespaceManager CreateNamespaceManager()
		{
			NamespaceManager namespaceManager;
			this.Validate();
			string item = this.connectionProperties["OperationTimeout"];
			IEnumerable<Uri> endpointAddresses = KeyValueConfigurationManager.GetEndpointAddresses(this.connectionProperties["Endpoint"], this.connectionProperties["ManagementPort"]);
			IEnumerable<Uri> uris = KeyValueConfigurationManager.GetEndpointAddresses(this.connectionProperties["StsEndpoint"], null);
			string str = this.connectionProperties["SharedSecretIssuer"];
			string item1 = this.connectionProperties["SharedSecretValue"];
			string str1 = this.connectionProperties["SharedAccessKeyName"];
			string item2 = this.connectionProperties["SharedAccessKey"];
			string str2 = this.connectionProperties["WindowsDomain"];
			string item3 = this.connectionProperties["WindowsUsername"];
			SecureString windowsPassword = this.GetWindowsPassword();
			string str3 = this.connectionProperties["OAuthDomain"];
			string item4 = this.connectionProperties["OAuthUsername"];
			SecureString oAuthPassword = this.GetOAuthPassword();
			try
			{
				TokenProvider tokenProvider = KeyValueConfigurationManager.CreateTokenProvider(uris, str, item1, str1, item2, str2, item3, windowsPassword, str3, item4, oAuthPassword);
				if (!string.IsNullOrEmpty(item))
				{
					NamespaceManagerSettings namespaceManagerSetting = new NamespaceManagerSettings()
					{
						OperationTimeout = TimeSpan.Parse(item, CultureInfo.CurrentCulture),
						TokenProvider = tokenProvider
					};
					namespaceManager = new NamespaceManager(endpointAddresses, namespaceManagerSetting);
				}
				else
				{
					namespaceManager = new NamespaceManager(endpointAddresses, tokenProvider);
				}
			}
			catch (ArgumentException argumentException1)
			{
				ArgumentException argumentException = argumentException1;
				throw new ConfigurationErrorsException(SRClient.AppSettingsCreateManagerWithInvalidConnectionString(argumentException.Message), argumentException);
			}
			catch (UriFormatException uriFormatException1)
			{
				UriFormatException uriFormatException = uriFormatException1;
				throw new ConfigurationErrorsException(SRClient.AppSettingsCreateManagerWithInvalidConnectionString(uriFormatException.Message), uriFormatException);
			}
			return namespaceManager;
		}

		private static NameValueCollection CreateNameValueCollectionFromConnectionString(string connectionString)
		{
			NameValueCollection nameValueCollection = new NameValueCollection();
			if (!string.IsNullOrWhiteSpace(connectionString))
			{
				string str = string.Concat(";", connectionString);
				string[] strArrays = Regex.Split(str, ";(OperationTimeout|Endpoint|RuntimePort|ManagementPort|StsEndpoint|WindowsDomain|WindowsUsername|WindowsPassword|OAuthDomain|OAuthUsername|OAuthPassword|SharedSecretIssuer|SharedSecretValue|SharedAccessKeyName|SharedAccessKey|TransportType)=", RegexOptions.IgnoreCase);
				if ((int)strArrays.Length > 0)
				{
					if (!string.IsNullOrWhiteSpace(strArrays[0]))
					{
						throw new ConfigurationErrorsException(SRClient.AppSettingsConfigSettingInvalidKey(connectionString));
					}
					if ((int)strArrays.Length % 2 != 1)
					{
						throw new ConfigurationErrorsException(SRClient.AppSettingsConfigSettingInvalidKey(connectionString));
					}
					for (int i = 1; i < (int)strArrays.Length; i++)
					{
						string str1 = strArrays[i];
						if (string.IsNullOrWhiteSpace(str1) || !KeyValueConfigurationManager.KeyRegex.IsMatch(str1))
						{
							throw new ConfigurationErrorsException(SRClient.AppSettingsConfigSettingInvalidKey(str1));
						}
						string str2 = strArrays[i + 1];
						if (string.IsNullOrWhiteSpace(str2) || !KeyValueConfigurationManager.ValueRegex.IsMatch(str2))
						{
							throw new ConfigurationErrorsException(SRClient.AppSettingsConfigSettingInvalidValue(str1, str2));
						}
						if (nameValueCollection[str1] != null)
						{
							throw new ConfigurationErrorsException(SRClient.AppSettingsConfigDuplicateSetting(str1));
						}
						nameValueCollection[str1] = str2;
						i++;
					}
				}
			}
			return nameValueCollection;
		}

		internal TokenProvider CreateTokenProvider()
		{
			IEnumerable<Uri> endpointAddresses = KeyValueConfigurationManager.GetEndpointAddresses(this.connectionProperties["StsEndpoint"], null);
			string item = this.connectionProperties["SharedSecretIssuer"];
			string str = this.connectionProperties["SharedSecretValue"];
			string item1 = this.connectionProperties["SharedAccessKeyName"];
			string str1 = this.connectionProperties["SharedAccessKey"];
			string item2 = this.connectionProperties["WindowsDomain"];
			string str2 = this.connectionProperties["WindowsUsername"];
			SecureString windowsPassword = this.GetWindowsPassword();
			string item3 = this.connectionProperties["OAuthDomain"];
			string str3 = this.connectionProperties["OAuthUsername"];
			SecureString oAuthPassword = this.GetOAuthPassword();
			return KeyValueConfigurationManager.CreateTokenProvider(endpointAddresses, item, str, item1, str1, item2, str2, windowsPassword, item3, str3, oAuthPassword);
		}

		private static TokenProvider CreateTokenProvider(IEnumerable<Uri> stsEndpoints, string issuerName, string issuerKey, string sharedAccessKeyName, string sharedAccessKey, string windowsDomain, string windowsUser, SecureString windowsPassword, string oauthDomain, string oauthUser, SecureString oauthPassword)
		{
			if (!string.IsNullOrWhiteSpace(sharedAccessKey))
			{
				return TokenProvider.CreateSharedAccessSignatureTokenProvider(sharedAccessKeyName, sharedAccessKey);
			}
			if (!string.IsNullOrWhiteSpace(issuerName))
			{
				if (stsEndpoints == null || !stsEndpoints.Any<Uri>())
				{
					return TokenProvider.CreateSharedSecretTokenProvider(issuerName, issuerKey);
				}
				return TokenProvider.CreateSharedSecretTokenProvider(issuerName, issuerKey, stsEndpoints.First<Uri>());
			}
			bool flag = (stsEndpoints == null ? false : stsEndpoints.Any<Uri>());
			bool flag1 = (string.IsNullOrWhiteSpace(windowsUser) || windowsPassword == null ? false : windowsPassword.Length > 0);
			bool flag2 = (string.IsNullOrWhiteSpace(oauthUser) || oauthPassword == null ? false : oauthPassword.Length > 0);
			if (!flag)
			{
				return null;
			}
			if (flag2)
			{
				return TokenProvider.CreateOAuthTokenProvider(stsEndpoints, (string.IsNullOrWhiteSpace(oauthDomain) ? new NetworkCredential(oauthUser, oauthPassword) : new NetworkCredential(oauthUser, oauthPassword, oauthDomain)));
			}
			if (!flag1)
			{
				return TokenProvider.CreateWindowsTokenProvider(stsEndpoints);
			}
			return TokenProvider.CreateWindowsTokenProvider(stsEndpoints, (string.IsNullOrWhiteSpace(windowsDomain) ? new NetworkCredential(windowsUser, windowsPassword) : new NetworkCredential(windowsUser, windowsPassword, windowsDomain)));
		}

		internal KeyValueConfigurationManager EnsureAmqpTransport()
		{
			this.Validate();
			if (string.IsNullOrWhiteSpace(this.connectionProperties["TransportType"]) || !Microsoft.ServiceBus.Messaging.TransportType.Amqp.ToString().Equals(this.connectionProperties["TransportType"], StringComparison.OrdinalIgnoreCase))
			{
				this.connectionProperties["TransportType"] = Microsoft.ServiceBus.Messaging.TransportType.Amqp.ToString();
			}
			return this;
		}

		internal KeyValueConfigurationManager EnsureSbmpTransport()
		{
			this.Validate();
			if (!string.IsNullOrWhiteSpace(this.connectionProperties["TransportType"]))
			{
				this.connectionProperties["TransportType"] = Microsoft.ServiceBus.Messaging.TransportType.NetMessaging.ToString();
			}
			return this;
		}

		private static bool FactoryEquals(MessagingFactory factory1, MessagingFactory factory2)
		{
			bool flag;
			bool flag1;
			bool flag2;
			bool flag3;
			bool flag4;
			MessagingFactorySettings settings = factory1.GetSettings();
			SharedSecretTokenProvider tokenProvider = settings.TokenProvider as SharedSecretTokenProvider;
			SharedAccessSignatureTokenProvider sharedAccessSignatureTokenProvider = settings.TokenProvider as SharedAccessSignatureTokenProvider;
			WindowsTokenProvider windowsTokenProvider = settings.TokenProvider as WindowsTokenProvider;
			OAuthTokenProvider oAuthTokenProvider = settings.TokenProvider as OAuthTokenProvider;
			MessagingFactorySettings messagingFactorySetting = factory2.GetSettings();
			SharedSecretTokenProvider sharedSecretTokenProvider = messagingFactorySetting.TokenProvider as SharedSecretTokenProvider;
			SharedAccessSignatureTokenProvider tokenProvider1 = messagingFactorySetting.TokenProvider as SharedAccessSignatureTokenProvider;
			WindowsTokenProvider windowsTokenProvider1 = messagingFactorySetting.TokenProvider as WindowsTokenProvider;
			OAuthTokenProvider oAuthTokenProvider1 = messagingFactorySetting.TokenProvider as OAuthTokenProvider;
			if (settings.OperationTimeout != messagingFactorySetting.OperationTimeout)
			{
				return false;
			}
			if (settings.TransportType != messagingFactorySetting.TransportType)
			{
				return false;
			}
			if (tokenProvider != null || sharedSecretTokenProvider != null)
			{
				flag = (tokenProvider == null ? false : sharedSecretTokenProvider != null);
			}
			else
			{
				flag = true;
			}
			bool flag5 = flag;
			if (windowsTokenProvider != null || windowsTokenProvider1 != null)
			{
				flag1 = (windowsTokenProvider == null ? false : windowsTokenProvider1 != null);
			}
			else
			{
				flag1 = true;
			}
			bool flag6 = flag1;
			if (oAuthTokenProvider != null || oAuthTokenProvider1 != null)
			{
				flag2 = (oAuthTokenProvider == null ? false : oAuthTokenProvider1 != null);
			}
			else
			{
				flag2 = true;
			}
			bool flag7 = flag2;
			if (sharedAccessSignatureTokenProvider != null || tokenProvider1 != null)
			{
				flag3 = (sharedAccessSignatureTokenProvider == null ? false : tokenProvider1 != null);
			}
			else
			{
				flag3 = true;
			}
			bool flag8 = flag3;
			if (!flag5 || !flag6 || !flag7 || !flag8)
			{
				return false;
			}
			if (tokenProvider != null && sharedSecretTokenProvider != null && (tokenProvider.IssuerName != sharedSecretTokenProvider.IssuerName || !tokenProvider.IssuerSecret.SequenceEqual<byte>(sharedSecretTokenProvider.IssuerSecret) || tokenProvider.IsWebTokenSupported != sharedSecretTokenProvider.IsWebTokenSupported))
			{
				return false;
			}
			if (sharedAccessSignatureTokenProvider != null && tokenProvider1 != null)
			{
				if (sharedAccessSignatureTokenProvider.encodedSharedAccessKey != null || tokenProvider1.encodedSharedAccessKey != null)
				{
					flag4 = (sharedAccessSignatureTokenProvider.encodedSharedAccessKey == null ? false : tokenProvider1.encodedSharedAccessKey != null);
				}
				else
				{
					flag4 = true;
				}
				bool flag9 = flag4;
				if (sharedAccessSignatureTokenProvider.keyName != tokenProvider1.keyName || sharedAccessSignatureTokenProvider.tokenTimeToLive != tokenProvider1.tokenTimeToLive || !flag9)
				{
					return false;
				}
				if (sharedAccessSignatureTokenProvider.encodedSharedAccessKey != null && tokenProvider1.encodedSharedAccessKey != null)
				{
					if ((int)sharedAccessSignatureTokenProvider.encodedSharedAccessKey.Length != (int)tokenProvider1.encodedSharedAccessKey.Length)
					{
						return false;
					}
					if (!sharedAccessSignatureTokenProvider.encodedSharedAccessKey.SequenceEqual<byte>(tokenProvider1.encodedSharedAccessKey))
					{
						return false;
					}
				}
			}
			if (oAuthTokenProvider != null && oAuthTokenProvider1 != null && oAuthTokenProvider.IsWebTokenSupported != oAuthTokenProvider1.IsWebTokenSupported)
			{
				return false;
			}
			if (windowsTokenProvider != null && windowsTokenProvider1 != null)
			{
				if (windowsTokenProvider.IsWebTokenSupported != windowsTokenProvider1.IsWebTokenSupported || windowsTokenProvider.stsUris.Count != windowsTokenProvider1.stsUris.Count)
				{
					return false;
				}
				if (windowsTokenProvider.stsUris.Where<Uri>((Uri t, int i) => t != windowsTokenProvider1.stsUris[i]).Any<Uri>())
				{
					return false;
				}
				if (windowsTokenProvider.credential == null && windowsTokenProvider1.credential != null || windowsTokenProvider.credential != null && windowsTokenProvider1.credential == null)
				{
					return false;
				}
				if (windowsTokenProvider.credential != null && windowsTokenProvider1.credential != null && (!windowsTokenProvider.credential.Domain.Equals(windowsTokenProvider1.credential.Domain, StringComparison.OrdinalIgnoreCase) || !windowsTokenProvider.credential.UserName.Equals(windowsTokenProvider1.credential.UserName, StringComparison.OrdinalIgnoreCase) || !windowsTokenProvider.credential.Password.Equals(windowsTokenProvider1.credential.Password)))
				{
					return false;
				}
			}
			return factory1.Address == factory2.Address;
		}

		internal IList<Uri> GetEndpointAddresses()
		{
			return KeyValueConfigurationManager.GetEndpointAddresses(this.connectionProperties["Endpoint"], this.connectionProperties["RuntimePort"]);
		}

		public static IList<Uri> GetEndpointAddresses(string uriEndpoints, string portString)
		{
			int num;
			List<Uri> uris = new List<Uri>();
			if (string.IsNullOrWhiteSpace(uriEndpoints))
			{
				return uris;
			}
			string[] strArrays = new string[] { "," };
			string[] strArrays1 = uriEndpoints.Split(strArrays, StringSplitOptions.RemoveEmptyEntries);
			if (strArrays1 == null || (int)strArrays1.Length == 0)
			{
				return uris;
			}
			if (!int.TryParse(portString, out num))
			{
				num = -1;
			}
			string[] strArrays2 = strArrays1;
			for (int i = 0; i < (int)strArrays2.Length; i++)
			{
				UriBuilder uriBuilder = new UriBuilder(strArrays2[i]);
				if (num > 0)
				{
					uriBuilder.Port = num;
				}
				uris.Add(uriBuilder.Uri);
			}
			return uris;
		}

		public SecureString GetOAuthPassword()
		{
			return this.GetSecurePassword("OAuthPassword");
		}

		private MessagingFactory GetOrUpdateFactory(IEnumerable<Uri> endpoints, IEnumerable<Uri> stsEndpoints, string operationTimeout, string issuerName, string issuerKey, string sasKeyName, string sasKey, string windowsDomain, string windowsUser, SecureString windowsPassword, string oauthDomain, string oauthUser, SecureString oauthPassword, string transportType)
		{
			MessagingFactory messagingFactory;
			lock (KeyValueConfigurationManager.CachedFactories.Value)
			{
				MessagingFactory messagingFactory1 = this.CreateFactory(endpoints, stsEndpoints, operationTimeout, issuerName, issuerKey, sasKeyName, sasKey, windowsDomain, windowsUser, windowsPassword, oauthDomain, oauthUser, oauthPassword, transportType);
				MessagingFactory orAdd = KeyValueConfigurationManager.CachedFactories.Value.GetOrAdd("Microsoft.ServiceBus.ConnectionString", messagingFactory1);
				if (!KeyValueConfigurationManager.FactoryEquals(orAdd, messagingFactory1) || orAdd.IsClosedOrClosing)
				{
					KeyValueConfigurationManager.CachedFactories.Value["Microsoft.ServiceBus.ConnectionString"] = messagingFactory1;
					messagingFactory = messagingFactory1;
				}
				else
				{
					messagingFactory = orAdd;
				}
			}
			return messagingFactory;
		}

		private SecureString GetSecurePassword(string configName)
		{
			unsafe
			{
				char* chrPointer;
				SecureString secureString = null;
				string item = this.connectionProperties[configName];
				if (!string.IsNullOrWhiteSpace(item))
				{
					char[] charArray = item.ToCharArray();
					char[] chrArray = charArray;
					char[] chrArray1 = chrArray;
					if (chrArray == null || (int)chrArray1.Length == 0)
					{
						chrPointer = null;
					}
					else
					{
						chrPointer = &chrArray1[0];
					}
					secureString = new SecureString(chrPointer, (int)charArray.Length);
					chrPointer = null;
				}
				return secureString;
			}
		}

		public SecureString GetWindowsPassword()
		{
			return this.GetSecurePassword("WindowsPassword");
		}

		private void Initialize(string connection, Microsoft.ServiceBus.Messaging.TransportType? transportType)
		{
			this.connectionString = connection;
			if (transportType.HasValue)
			{
				ServiceBusConnectionStringBuilder serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder(this.connectionString)
				{
					TransportType = transportType.Value
				};
				this.connectionString = serviceBusConnectionStringBuilder.ToString();
			}
			this.connectionProperties = KeyValueConfigurationManager.CreateNameValueCollectionFromConnectionString(this.connectionString);
		}

		public void Validate()
		{
			TimeSpan timeSpan;
			int num;
			int num1;
			if (string.IsNullOrWhiteSpace(this.connectionProperties["Endpoint"]))
			{
				throw new ConfigurationErrorsException(SRClient.AppSettingsConfigMissingSetting("Endpoint", "Microsoft.ServiceBus.ConnectionString"));
			}
			bool flag = !string.IsNullOrWhiteSpace(this.connectionProperties["SharedSecretIssuer"]);
			bool flag1 = !string.IsNullOrWhiteSpace(this.connectionProperties["SharedSecretValue"]);
			if (flag && !flag1)
			{
				throw new ConfigurationErrorsException(SRClient.AppSettingsConfigMissingSetting("SharedSecretValue", "Microsoft.ServiceBus.ConnectionString"));
			}
			if (!flag && flag1)
			{
				throw new ConfigurationErrorsException(SRClient.AppSettingsConfigMissingSetting("SharedSecretIssuer", "Microsoft.ServiceBus.ConnectionString"));
			}
			bool flag2 = !string.IsNullOrWhiteSpace(this.connectionProperties["WindowsUsername"]);
			bool flag3 = !string.IsNullOrWhiteSpace(this.connectionProperties["WindowsPassword"]);
			if ((!flag2 || !flag3) && (flag2 || flag3))
			{
				CultureInfo currentCulture = CultureInfo.CurrentCulture;
				object[] objArray = new object[] { "WindowsUsername", "WindowsPassword" };
				string str = string.Format(currentCulture, "{0},{1}", objArray);
				throw new ConfigurationErrorsException(SRClient.AppSettingsConfigIncompleteSettingCombination("Microsoft.ServiceBus.ConnectionString", str));
			}
			bool flag4 = !string.IsNullOrWhiteSpace(this.connectionProperties["OAuthUsername"]);
			bool flag5 = !string.IsNullOrWhiteSpace(this.connectionProperties["OAuthPassword"]);
			if ((!flag4 || !flag5) && (flag4 || flag5))
			{
				CultureInfo cultureInfo = CultureInfo.CurrentCulture;
				object[] objArray1 = new object[] { "OAuthUsername", "OAuthPassword" };
				string str1 = string.Format(cultureInfo, "{0},{1}", objArray1);
				throw new ConfigurationErrorsException(SRClient.AppSettingsConfigIncompleteSettingCombination("Microsoft.ServiceBus.ConnectionString", str1));
			}
			string item = this.connectionProperties["OperationTimeout"];
			if (!string.IsNullOrWhiteSpace(item) && !TimeSpan.TryParse(item, CultureInfo.CurrentCulture, out timeSpan))
			{
				throw new ConfigurationErrorsException(SRClient.AppSettingsConfigSettingInvalidValue("OperationTimeout", item));
			}
			string item1 = this.connectionProperties["RuntimePort"];
			if (!string.IsNullOrWhiteSpace(item1) && !int.TryParse(item1, out num))
			{
				throw new ConfigurationErrorsException(SRClient.AppSettingsConfigSettingInvalidValue("RuntimePort", item1));
			}
			string item2 = this.connectionProperties["ManagementPort"];
			if (!string.IsNullOrWhiteSpace(item2) && !int.TryParse(item2, out num1))
			{
				throw new ConfigurationErrorsException(SRClient.AppSettingsConfigSettingInvalidValue("ManagementPort", item2));
			}
		}
	}
}
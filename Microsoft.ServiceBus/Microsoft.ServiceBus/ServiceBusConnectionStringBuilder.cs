using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

namespace Microsoft.ServiceBus
{
	public class ServiceBusConnectionStringBuilder
	{
		private Microsoft.ServiceBus.Messaging.TransportType? transportType;

		public HashSet<Uri> Endpoints
		{
			get;
			private set;
		}

		public int ManagementPort
		{
			get;
			set;
		}

		public string OAuthDomain
		{
			get;
			set;
		}

		public SecureString OAuthPassword
		{
			get;
			set;
		}

		public string OAuthUsername
		{
			get;
			set;
		}

		public TimeSpan OperationTimeout
		{
			get;
			set;
		}

		public int RuntimePort
		{
			get;
			set;
		}

		public string SharedAccessKey
		{
			get;
			set;
		}

		public string SharedAccessKeyName
		{
			get;
			set;
		}

		public string SharedSecretIssuerName
		{
			get;
			set;
		}

		public string SharedSecretIssuerSecret
		{
			get;
			set;
		}

		public HashSet<Uri> StsEndpoints
		{
			get;
			private set;
		}

		public Microsoft.ServiceBus.Messaging.TransportType TransportType
		{
			get
			{
				if (!this.transportType.HasValue)
				{
					return Microsoft.ServiceBus.Messaging.TransportType.NetMessaging;
				}
				return this.transportType.Value;
			}
			set
			{
				this.transportType = new Microsoft.ServiceBus.Messaging.TransportType?(value);
			}
		}

		public string WindowsCredentialDomain
		{
			get;
			set;
		}

		public SecureString WindowsCredentialPassword
		{
			get;
			set;
		}

		public string WindowsCredentialUsername
		{
			get;
			set;
		}

		public ServiceBusConnectionStringBuilder()
		{
			this.WindowsCredentialPassword = null;
			this.RuntimePort = -1;
			this.ManagementPort = -1;
			this.Endpoints = new HashSet<Uri>();
			this.StsEndpoints = new HashSet<Uri>();
			this.OperationTimeout = Constants.DefaultOperationTimeout;
		}

		public ServiceBusConnectionStringBuilder(string connectionString) : this()
		{
			if (!string.IsNullOrWhiteSpace(connectionString))
			{
				this.InitializeFromString(connectionString);
			}
		}

		internal ServiceBusConnectionStringBuilder(KeyValueConfigurationManager keyValueManager) : this()
		{
			if (keyValueManager != null)
			{
				this.InitializeFromKeyValueManager(keyValueManager);
			}
		}

		public static string CreateUsingOAuthCredential(IEnumerable<Uri> endpoints, IEnumerable<Uri> stsEndpoints, int runtimePort, int managementPort, string domain, string user, SecureString password)
		{
			Uri[] uriArray = endpoints as Uri[] ?? endpoints.ToArray<Uri>();
			if (endpoints == null || !uriArray.Any<Uri>())
			{
				throw new ArgumentNullException("endpoints");
			}
			if (string.IsNullOrWhiteSpace(user))
			{
				throw new ArgumentNullException("user");
			}
			if (password == null || password.Length == 0)
			{
				throw new ArgumentNullException("password");
			}
			ServiceBusConnectionStringBuilder serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder();
			Uri[] uriArray1 = uriArray;
			for (int i = 0; i < (int)uriArray1.Length; i++)
			{
				Uri uri = uriArray1[i];
				serviceBusConnectionStringBuilder.Endpoints.Add(uri);
			}
			if (stsEndpoints != null)
			{
				foreach (Uri stsEndpoint in stsEndpoints)
				{
					serviceBusConnectionStringBuilder.StsEndpoints.Add(stsEndpoint);
				}
			}
			serviceBusConnectionStringBuilder.RuntimePort = runtimePort;
			serviceBusConnectionStringBuilder.ManagementPort = managementPort;
			serviceBusConnectionStringBuilder.OAuthUsername = user;
			serviceBusConnectionStringBuilder.OAuthPassword = password;
			if (!string.IsNullOrWhiteSpace(domain))
			{
				serviceBusConnectionStringBuilder.OAuthDomain = domain;
			}
			return serviceBusConnectionStringBuilder.ToString();
		}

		public static string CreateUsingSharedAccessKey(Uri endpoint, string keyName, string key)
		{
			if (endpoint == null)
			{
				throw new ArgumentNullException("endpoint");
			}
			if (string.IsNullOrWhiteSpace(keyName))
			{
				throw new ArgumentNullException("keyName");
			}
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException("key");
			}
			Uri[] uriArray = new Uri[] { endpoint };
			return ServiceBusConnectionStringBuilder.CreateUsingSharedAccessKey(uriArray, -1, -1, keyName, key);
		}

		public static string CreateUsingSharedAccessKey(IEnumerable<Uri> endpoints, int runtimePort, int managementPort, string keyName, string key)
		{
			Uri[] uriArray = endpoints as Uri[] ?? endpoints.ToArray<Uri>();
			if (endpoints == null || !uriArray.Any<Uri>())
			{
				throw new ArgumentNullException("endpoints");
			}
			if (string.IsNullOrWhiteSpace(keyName))
			{
				throw new ArgumentNullException("keyName");
			}
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException("key");
			}
			ServiceBusConnectionStringBuilder serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder();
			Uri[] uriArray1 = uriArray;
			for (int i = 0; i < (int)uriArray1.Length; i++)
			{
				Uri uri = uriArray1[i];
				serviceBusConnectionStringBuilder.Endpoints.Add(uri);
			}
			serviceBusConnectionStringBuilder.RuntimePort = runtimePort;
			serviceBusConnectionStringBuilder.ManagementPort = managementPort;
			serviceBusConnectionStringBuilder.SharedAccessKeyName = keyName;
			serviceBusConnectionStringBuilder.SharedAccessKey = key;
			return serviceBusConnectionStringBuilder.ToString();
		}

		public static string CreateUsingSharedSecret(Uri endpoint, string issuer, string issuerSecret)
		{
			if (endpoint == null)
			{
				throw new ArgumentNullException("endpoint");
			}
			if (string.IsNullOrWhiteSpace(issuer))
			{
				throw new ArgumentNullException("issuer");
			}
			if (string.IsNullOrWhiteSpace(issuerSecret))
			{
				throw new ArgumentNullException("issuerSecret");
			}
			Uri[] uriArray = new Uri[] { endpoint };
			return ServiceBusConnectionStringBuilder.CreateUsingSharedSecret(uriArray, null, -1, -1, issuer, issuerSecret);
		}

		public static string CreateUsingSharedSecret(IEnumerable<Uri> endpoints, IEnumerable<Uri> stsEndpoints, int runtimePort, int managementPort, string issuer, string issuerSecret)
		{
			Uri[] uriArray = endpoints as Uri[] ?? endpoints.ToArray<Uri>();
			if (endpoints == null || !uriArray.Any<Uri>())
			{
				throw new ArgumentNullException("endpoints");
			}
			if (string.IsNullOrWhiteSpace(issuer))
			{
				throw new ArgumentNullException("issuer");
			}
			if (string.IsNullOrWhiteSpace(issuerSecret))
			{
				throw new ArgumentNullException("issuerSecret");
			}
			ServiceBusConnectionStringBuilder serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder();
			Uri[] uriArray1 = uriArray;
			for (int i = 0; i < (int)uriArray1.Length; i++)
			{
				Uri uri = uriArray1[i];
				serviceBusConnectionStringBuilder.Endpoints.Add(uri);
			}
			if (stsEndpoints != null)
			{
				foreach (Uri stsEndpoint in stsEndpoints)
				{
					serviceBusConnectionStringBuilder.StsEndpoints.Add(stsEndpoint);
				}
			}
			serviceBusConnectionStringBuilder.RuntimePort = runtimePort;
			serviceBusConnectionStringBuilder.ManagementPort = managementPort;
			serviceBusConnectionStringBuilder.SharedSecretIssuerName = issuer;
			serviceBusConnectionStringBuilder.SharedSecretIssuerSecret = issuerSecret;
			return serviceBusConnectionStringBuilder.ToString();
		}

		public static string CreateUsingWindowsCredential(IEnumerable<Uri> endpoints, IEnumerable<Uri> stsEndpoints, int runtimePort, int managementPort, string domain, string user, SecureString password)
		{
			Uri[] uriArray = endpoints as Uri[] ?? endpoints.ToArray<Uri>();
			if (endpoints == null || !uriArray.Any<Uri>())
			{
				throw new ArgumentNullException("endpoints");
			}
			if (string.IsNullOrWhiteSpace(user))
			{
				throw new ArgumentNullException("user");
			}
			if (password == null || password.Length == 0)
			{
				throw new ArgumentNullException("password");
			}
			ServiceBusConnectionStringBuilder serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder();
			Uri[] uriArray1 = uriArray;
			for (int i = 0; i < (int)uriArray1.Length; i++)
			{
				Uri uri = uriArray1[i];
				serviceBusConnectionStringBuilder.Endpoints.Add(uri);
			}
			if (stsEndpoints != null)
			{
				foreach (Uri stsEndpoint in stsEndpoints)
				{
					serviceBusConnectionStringBuilder.StsEndpoints.Add(stsEndpoint);
				}
			}
			serviceBusConnectionStringBuilder.RuntimePort = runtimePort;
			serviceBusConnectionStringBuilder.ManagementPort = managementPort;
			serviceBusConnectionStringBuilder.WindowsCredentialUsername = user;
			serviceBusConnectionStringBuilder.WindowsCredentialPassword = password;
			if (!string.IsNullOrWhiteSpace(domain))
			{
				serviceBusConnectionStringBuilder.WindowsCredentialDomain = domain;
			}
			return serviceBusConnectionStringBuilder.ToString();
		}

		private IList<Uri> GetAbsoluteEndpoints(int port, string uriScheme = null)
		{
			IList<Uri> uris = new List<Uri>();
			foreach (Uri endpoint in this.Endpoints)
			{
				UriBuilder uriBuilder = new UriBuilder(endpoint);
				if (port >= 0)
				{
					uriBuilder.Port = port;
				}
				if (!string.IsNullOrWhiteSpace(uriScheme))
				{
					uriBuilder.Scheme = uriScheme;
				}
				uris.Add(uriBuilder.Uri);
			}
			return uris;
		}

		public IList<Uri> GetAbsoluteManagementEndpoints()
		{
			return this.GetAbsoluteEndpoints(this.ManagementPort, null);
		}

		public IList<Uri> GetAbsoluteRuntimeEndpoints()
		{
			return this.GetAbsoluteEndpoints(this.RuntimePort, "sb");
		}

		private void InitializeFromKeyValueManager(KeyValueConfigurationManager manager)
		{
			int num;
			int num1;
			TimeSpan timeSpan;
			try
			{
				manager.Validate();
				foreach (Uri endpointAddress in KeyValueConfigurationManager.GetEndpointAddresses(manager.connectionProperties["Endpoint"], string.Empty))
				{
					this.Endpoints.Add(endpointAddress);
				}
				foreach (Uri uri in KeyValueConfigurationManager.GetEndpointAddresses(manager.connectionProperties["StsEndpoint"], string.Empty))
				{
					this.StsEndpoints.Add(uri);
				}
				if (int.TryParse(manager.connectionProperties["RuntimePort"], out num))
				{
					this.RuntimePort = num;
				}
				if (int.TryParse(manager.connectionProperties["ManagementPort"], out num1))
				{
					this.ManagementPort = num1;
				}
				string item = manager.connectionProperties["OperationTimeout"];
				if (!string.IsNullOrWhiteSpace(item) && TimeSpan.TryParse(item, CultureInfo.CurrentCulture, out timeSpan) && !timeSpan.Equals(this.OperationTimeout))
				{
					this.OperationTimeout = timeSpan;
				}
				string str = manager.connectionProperties["SharedSecretIssuer"];
				if (!string.IsNullOrWhiteSpace(str))
				{
					this.SharedSecretIssuerName = str;
				}
				string item1 = manager.connectionProperties["SharedSecretValue"];
				if (!string.IsNullOrWhiteSpace(item1))
				{
					this.SharedSecretIssuerSecret = item1;
				}
				string str1 = manager.connectionProperties["SharedAccessKeyName"];
				if (!string.IsNullOrWhiteSpace(str1))
				{
					this.SharedAccessKeyName = str1;
				}
				string item2 = manager.connectionProperties["SharedAccessKey"];
				if (!string.IsNullOrWhiteSpace(item2))
				{
					this.SharedAccessKey = item2;
				}
				string str2 = manager.connectionProperties["WindowsDomain"];
				if (!string.IsNullOrWhiteSpace(str2))
				{
					this.WindowsCredentialDomain = str2;
				}
				string item3 = manager.connectionProperties["WindowsUsername"];
				if (!string.IsNullOrWhiteSpace(item3))
				{
					this.WindowsCredentialUsername = item3;
				}
				this.WindowsCredentialPassword = manager.GetWindowsPassword();
				string str3 = manager.connectionProperties["OAuthDomain"];
				if (!string.IsNullOrWhiteSpace(str3))
				{
					this.OAuthDomain = str3;
				}
				string item4 = manager.connectionProperties["OAuthUsername"];
				if (!string.IsNullOrWhiteSpace(item4))
				{
					this.OAuthUsername = item4;
				}
				this.OAuthPassword = manager.GetOAuthPassword();
				string str4 = manager.connectionProperties["TransportType"];
				if (!string.IsNullOrWhiteSpace(str4))
				{
					this.TransportType = (Microsoft.ServiceBus.Messaging.TransportType)Enum.Parse(typeof(Microsoft.ServiceBus.Messaging.TransportType), str4);
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				throw new ArgumentException(exception.Message, "connectionString", exception);
			}
		}

		private void InitializeFromString(string connection)
		{
			this.InitializeFromKeyValueManager(new KeyValueConfigurationManager(connection));
		}

		public override string ToString()
		{
			this.Validate();
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder1 = new StringBuilder();
			string empty = string.Empty;
			string str = string.Empty;
			foreach (Uri endpoint in this.Endpoints)
			{
				UriBuilder uriBuilder = new UriBuilder(endpoint)
				{
					Scheme = "sb",
					Port = -1
				};
				UriBuilder uriBuilder1 = uriBuilder;
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] absoluteUri = new object[] { empty, uriBuilder1.Uri.AbsoluteUri };
				stringBuilder1.Append(string.Format(invariantCulture, "{0}{1}", absoluteUri));
				empty = ",";
			}
			CultureInfo cultureInfo = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { str, "Endpoint", "=", stringBuilder1 };
			stringBuilder.Append(string.Format(cultureInfo, "{0}{1}{2}{3}", objArray));
			stringBuilder1.Clear();
			str = ";";
			empty = string.Empty;
			if (this.StsEndpoints.Count > 0)
			{
				foreach (Uri stsEndpoint in this.StsEndpoints)
				{
					CultureInfo invariantCulture1 = CultureInfo.InvariantCulture;
					object[] absoluteUri1 = new object[] { empty, stsEndpoint.AbsoluteUri };
					stringBuilder1.Append(string.Format(invariantCulture1, "{0}{1}", absoluteUri1));
					empty = ",";
				}
				CultureInfo cultureInfo1 = CultureInfo.InvariantCulture;
				object[] objArray1 = new object[] { str, "StsEndpoint", "=", stringBuilder1 };
				stringBuilder.Append(string.Format(cultureInfo1, "{0}{1}{2}{3}", objArray1));
				stringBuilder1.Clear();
			}
			if (this.RuntimePort > 0)
			{
				CultureInfo invariantCulture2 = CultureInfo.InvariantCulture;
				object[] runtimePort = new object[] { str, "RuntimePort", "=", this.RuntimePort };
				stringBuilder.Append(string.Format(invariantCulture2, "{0}{1}{2}{3}", runtimePort));
			}
			if (this.ManagementPort > 0)
			{
				CultureInfo cultureInfo2 = CultureInfo.InvariantCulture;
				object[] managementPort = new object[] { str, "ManagementPort", "=", this.ManagementPort };
				stringBuilder.Append(string.Format(cultureInfo2, "{0}{1}{2}{3}", managementPort));
			}
			if (this.OperationTimeout != Constants.DefaultOperationTimeout)
			{
				CultureInfo invariantCulture3 = CultureInfo.InvariantCulture;
				object[] operationTimeout = new object[] { str, "OperationTimeout", "=", this.OperationTimeout };
				stringBuilder.Append(string.Format(invariantCulture3, "{0}{1}{2}{3}", operationTimeout));
			}
			if (!string.IsNullOrWhiteSpace(this.SharedSecretIssuerName))
			{
				CultureInfo cultureInfo3 = CultureInfo.InvariantCulture;
				object[] sharedSecretIssuerName = new object[] { str, "SharedSecretIssuer", "=", this.SharedSecretIssuerName };
				stringBuilder.Append(string.Format(cultureInfo3, "{0}{1}{2}{3}", sharedSecretIssuerName));
			}
			if (!string.IsNullOrWhiteSpace(this.SharedSecretIssuerSecret))
			{
				CultureInfo invariantCulture4 = CultureInfo.InvariantCulture;
				object[] sharedSecretIssuerSecret = new object[] { str, "SharedSecretValue", "=", this.SharedSecretIssuerSecret };
				stringBuilder.Append(string.Format(invariantCulture4, "{0}{1}{2}{3}", sharedSecretIssuerSecret));
			}
			if (!string.IsNullOrWhiteSpace(this.SharedAccessKeyName))
			{
				CultureInfo cultureInfo4 = CultureInfo.InvariantCulture;
				object[] sharedAccessKeyName = new object[] { str, "SharedAccessKeyName", "=", this.SharedAccessKeyName };
				stringBuilder.Append(string.Format(cultureInfo4, "{0}{1}{2}{3}", sharedAccessKeyName));
			}
			if (!string.IsNullOrWhiteSpace(this.SharedAccessKey))
			{
				CultureInfo invariantCulture5 = CultureInfo.InvariantCulture;
				object[] sharedAccessKey = new object[] { str, "SharedAccessKey", "=", this.SharedAccessKey };
				stringBuilder.Append(string.Format(invariantCulture5, "{0}{1}{2}{3}", sharedAccessKey));
			}
			if (!string.IsNullOrWhiteSpace(this.WindowsCredentialUsername))
			{
				CultureInfo cultureInfo5 = CultureInfo.InvariantCulture;
				object[] windowsCredentialUsername = new object[] { str, "WindowsUsername", "=", this.WindowsCredentialUsername };
				stringBuilder.Append(string.Format(cultureInfo5, "{0}{1}{2}{3}", windowsCredentialUsername));
			}
			if (!string.IsNullOrWhiteSpace(this.WindowsCredentialDomain))
			{
				CultureInfo invariantCulture6 = CultureInfo.InvariantCulture;
				object[] windowsCredentialDomain = new object[] { str, "WindowsDomain", "=", this.WindowsCredentialDomain };
				stringBuilder.Append(string.Format(invariantCulture6, "{0}{1}{2}{3}", windowsCredentialDomain));
			}
			if (this.WindowsCredentialPassword != null && this.WindowsCredentialPassword.Length > 0)
			{
				CultureInfo cultureInfo6 = CultureInfo.InvariantCulture;
				object[] str1 = new object[] { str, "WindowsPassword", "=", this.WindowsCredentialPassword.ConvertToString() };
				stringBuilder.Append(string.Format(cultureInfo6, "{0}{1}{2}{3}", str1));
			}
			if (!string.IsNullOrWhiteSpace(this.OAuthUsername))
			{
				CultureInfo invariantCulture7 = CultureInfo.InvariantCulture;
				object[] oAuthUsername = new object[] { str, "OAuthUsername", "=", this.OAuthUsername };
				stringBuilder.Append(string.Format(invariantCulture7, "{0}{1}{2}{3}", oAuthUsername));
			}
			if (!string.IsNullOrWhiteSpace(this.OAuthDomain))
			{
				CultureInfo cultureInfo7 = CultureInfo.InvariantCulture;
				object[] oAuthDomain = new object[] { str, "OAuthDomain", "=", this.OAuthDomain };
				stringBuilder.Append(string.Format(cultureInfo7, "{0}{1}{2}{3}", oAuthDomain));
			}
			if (this.OAuthPassword != null && this.OAuthPassword.Length > 0)
			{
				CultureInfo invariantCulture8 = CultureInfo.InvariantCulture;
				object[] str2 = new object[] { str, "OAuthPassword", "=", this.OAuthPassword.ConvertToString() };
				stringBuilder.Append(string.Format(invariantCulture8, "{0}{1}{2}{3}", str2));
			}
			if (this.transportType.HasValue)
			{
				CultureInfo cultureInfo8 = CultureInfo.InvariantCulture;
				object[] objArray2 = new object[] { str, "TransportType", "=", this.transportType.Value.ToString() };
				stringBuilder.Append(string.Format(cultureInfo8, "{0}{1}{2}{3}", objArray2));
			}
			return stringBuilder.ToString();
		}

		private void Validate()
		{
			if (this.Endpoints.Count == 0)
			{
				throw Fx.Exception.ArgumentNullOrEmpty("Endpoints");
			}
			bool flag = !string.IsNullOrWhiteSpace(this.SharedSecretIssuerName);
			bool flag1 = !string.IsNullOrWhiteSpace(this.SharedSecretIssuerSecret);
			if ((!flag || !flag1) && (flag || flag1))
			{
				throw Fx.Exception.Argument("SharedSecretIssuerName, SharedSecretIssuerSecret", SRClient.ArgumentInvalidCombination("SharedSecretIssuerName, SharedSecretIssuerSecret"));
			}
			bool flag2 = !string.IsNullOrWhiteSpace(this.SharedAccessKeyName);
			bool flag3 = !string.IsNullOrWhiteSpace(this.SharedAccessKey);
			if ((!flag2 || !flag3) && (flag2 || flag3))
			{
				throw Fx.Exception.Argument("SharedAccessKeyName, SharedAccessSecret", SRClient.ArgumentInvalidCombination("SharedAccessKeyName, SharedAccessSecret"));
			}
			bool flag4 = !string.IsNullOrWhiteSpace(this.WindowsCredentialUsername);
			bool flag5 = (this.WindowsCredentialPassword == null ? false : this.WindowsCredentialPassword.Length > 0);
			if ((!flag4 || !flag5) && (flag4 || flag5))
			{
				throw Fx.Exception.Argument("WindowsCredentialUsername, WindowsCredentialPassword", SRClient.ArgumentInvalidCombination("WindowsCredentialUsername, WindowsCredentialPassword"));
			}
			bool flag6 = !string.IsNullOrWhiteSpace(this.OAuthUsername);
			bool flag7 = (this.OAuthPassword == null ? false : this.OAuthPassword.Length > 0);
			if ((!flag6 || !flag7) && (flag6 || flag7))
			{
				throw Fx.Exception.Argument("OAuthUsername, OAuthPassword", SRClient.ArgumentInvalidCombination("OAuthUsername, OAuthPassword"));
			}
		}
	}
}
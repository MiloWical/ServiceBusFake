using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	[DataContract]
	internal sealed class BaseUriWithWildcard
	{
		private const char segmentDelimiter = '/';

		private const string plus = "+";

		private const string star = "*";

		private const int HttpUriDefaultPort = 80;

		private const int HttpsUriDefaultPort = 443;

		[DataMember]
		private Uri baseAddress;

		[DataMember]
		private System.ServiceModel.HostNameComparisonMode hostNameComparisonMode;

		private BaseUriWithWildcard.Comparand comparand;

		private int hashCode;

		internal Uri BaseAddress
		{
			get
			{
				return this.baseAddress;
			}
		}

		internal System.ServiceModel.HostNameComparisonMode HostNameComparisonMode
		{
			get
			{
				return this.hostNameComparisonMode;
			}
		}

		public BaseUriWithWildcard(Uri baseAddress, System.ServiceModel.HostNameComparisonMode hostNameComparisonMode)
		{
			this.baseAddress = baseAddress;
			this.hostNameComparisonMode = hostNameComparisonMode;
			this.SetComparisonAddressAndHashCode();
		}

		private BaseUriWithWildcard(string protocol, int defaultPort, string binding, int segmentCount, string path, string sampleBinding)
		{
			string[] strArrays = BaseUriWithWildcard.SplitBinding(binding);
			if ((int)strArrays.Length != segmentCount)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string hostingMisformattedBinding = Resources.Hosting_MisformattedBinding;
				object[] objArray = new object[] { binding, protocol, sampleBinding };
				throw exceptionUtility.ThrowHelperError(new UriFormatException(Microsoft.ServiceBus.SR.GetString(hostingMisformattedBinding, objArray)));
			}
			int num = segmentCount - 1;
			string str = this.ParseHostAndHostNameComparisonMode(strArrays[num]);
			int num1 = -1;
			int num2 = num - 1;
			num = num2;
			if (num2 >= 0)
			{
				string str1 = strArrays[num].Trim();
				if (!string.IsNullOrEmpty(str1) && !int.TryParse(str1, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out num1))
				{
					ExceptionUtility exceptionUtility1 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string hostingMisformattedPort = Resources.Hosting_MisformattedPort;
					object[] objArray1 = new object[] { protocol, binding, str1 };
					throw exceptionUtility1.ThrowHelperError(new UriFormatException(Microsoft.ServiceBus.SR.GetString(hostingMisformattedPort, objArray1)));
				}
				if (num1 == defaultPort)
				{
					num1 = -1;
				}
			}
			try
			{
				this.baseAddress = (new UriBuilder(protocol, str, num1, path)).Uri;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceError)
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(exception, TraceEventType.Error);
					}
					ExceptionUtility exceptionUtility2 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string hostingMisformattedBindingData = Resources.Hosting_MisformattedBindingData;
					object[] objArray2 = new object[] { binding, protocol };
					throw exceptionUtility2.ThrowHelperError(new UriFormatException(Microsoft.ServiceBus.SR.GetString(hostingMisformattedBindingData, objArray2)));
				}
				throw;
			}
			this.SetComparisonAddressAndHashCode();
		}

		internal static BaseUriWithWildcard CreateHostedPipeUri(string binding, string path)
		{
			return new BaseUriWithWildcard(Uri.UriSchemeNetPipe, -1, binding, 1, path, "*");
		}

		internal static BaseUriWithWildcard CreateHostedUri(string protocol, string binding, string path)
		{
			if (binding == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("binding");
			}
			if (path == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("path");
			}
			if (protocol.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
			{
				return new BaseUriWithWildcard(Uri.UriSchemeHttp, 80, binding, 3, path, ":80:");
			}
			if (protocol.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
			{
				return new BaseUriWithWildcard(Uri.UriSchemeHttps, 443, binding, 3, path, ":443:");
			}
			if (!protocol.Equals(Uri.UriSchemeNetTcp, StringComparison.OrdinalIgnoreCase))
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string hostingNotSupportedProtocol = Resources.Hosting_NotSupportedProtocol;
				object[] objArray = new object[] { binding };
				throw exceptionUtility.ThrowHelperError(new UriFormatException(Microsoft.ServiceBus.SR.GetString(hostingNotSupportedProtocol, objArray)));
			}
			return new BaseUriWithWildcard(Uri.UriSchemeNetTcp, 9350, binding, 2, path, "808:*");
		}

		public override bool Equals(object o)
		{
			BaseUriWithWildcard baseUriWithWildcard = o as BaseUriWithWildcard;
			if (baseUriWithWildcard == null || baseUriWithWildcard.hashCode != this.hashCode || baseUriWithWildcard.hostNameComparisonMode != this.hostNameComparisonMode || baseUriWithWildcard.comparand.Port != this.comparand.Port)
			{
				return false;
			}
			if (!object.ReferenceEquals(baseUriWithWildcard.comparand.Scheme, this.comparand.Scheme))
			{
				return false;
			}
			return this.comparand.Address.Equals(baseUriWithWildcard.comparand.Address);
		}

		public override int GetHashCode()
		{
			return this.hashCode;
		}

		internal bool IsBaseOf(Uri fullAddress)
		{
			if ((object)this.baseAddress.Scheme != (object)fullAddress.Scheme)
			{
				return false;
			}
			if (this.baseAddress.Port != fullAddress.Port)
			{
				return false;
			}
			if (this.HostNameComparisonMode == System.ServiceModel.HostNameComparisonMode.Exact && string.Compare(this.baseAddress.Host, fullAddress.Host, StringComparison.OrdinalIgnoreCase) != 0)
			{
				return false;
			}
			string components = this.baseAddress.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.Unescaped);
			string str = fullAddress.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.Unescaped);
			if (components.Length > str.Length)
			{
				return false;
			}
			if (components.Length < str.Length && components[components.Length - 1] != '/' && str[components.Length] != '/')
			{
				return false;
			}
			return string.Compare(str, 0, components, 0, components.Length, StringComparison.OrdinalIgnoreCase) == 0;
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			Microsoft.ServiceBus.Channels.UriSchemeKeyedCollection.ValidateBaseAddress(this.baseAddress, "context");
			if (!Microsoft.ServiceBus.Channels.HostNameComparisonModeHelper.IsDefined(this.HostNameComparisonMode))
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("context", Microsoft.ServiceBus.SR.GetString(Resources.Hosting_BaseUriDeserializedNotValid, new object[0]));
			}
			this.SetComparisonAddressAndHashCode();
		}

		private string ParseHostAndHostNameComparisonMode(string host)
		{
			if (string.IsNullOrEmpty(host) || host.Equals("*"))
			{
				this.hostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.WeakWildcard;
				host = DnsCache.MachineName;
			}
			else if (!host.Equals("+"))
			{
				this.hostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.Exact;
			}
			else
			{
				this.hostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.StrongWildcard;
				host = DnsCache.MachineName;
			}
			return host;
		}

		private void SetComparisonAddressAndHashCode()
		{
			if (this.HostNameComparisonMode != System.ServiceModel.HostNameComparisonMode.Exact)
			{
				this.comparand.Address = this.baseAddress.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.UriEscaped);
			}
			else
			{
				this.comparand.Address = this.baseAddress.ToString();
			}
			this.comparand.Port = this.baseAddress.Port;
			this.comparand.Scheme = this.baseAddress.Scheme;
			if (this.comparand.Port == -1 && (object)this.comparand.Scheme == (object)Uri.UriSchemeNetTcp)
			{
				this.comparand.Port = 9350;
			}
			this.hashCode = this.comparand.Address.GetHashCode() ^ this.comparand.Port ^ (int)this.HostNameComparisonMode;
		}

		private static string[] SplitBinding(string binding)
		{
			bool flag = false;
			string[] empty = null;
			List<int> nums = null;
			for (int i = 0; i < binding.Length; i++)
			{
				if (flag && binding[i] == ']')
				{
					flag = false;
				}
				else if (binding[i] == '[')
				{
					flag = true;
				}
				else if (!flag && binding[i] == ':')
				{
					if (nums == null)
					{
						nums = new List<int>();
					}
					nums.Add(i);
				}
			}
			if (nums != null)
			{
				empty = new string[nums.Count + 1];
				int num = 0;
				for (int j = 0; j < (int)empty.Length; j++)
				{
					if (j < nums.Count)
					{
						int item = nums[j];
						empty[j] = binding.Substring(num, item - num);
						num = item + 1;
					}
					else if (num >= binding.Length)
					{
						empty[j] = string.Empty;
					}
					else
					{
						empty[j] = binding.Substring(num, binding.Length - num);
					}
				}
			}
			else
			{
				empty = new string[] { binding };
			}
			return empty;
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] hostNameComparisonMode = new object[] { this.HostNameComparisonMode, this.BaseAddress };
			return string.Format(invariantCulture, "{0}:{1}", hostNameComparisonMode);
		}

		private struct Comparand
		{
			public string Address;

			public int Port;

			public string Scheme;
		}
	}
}
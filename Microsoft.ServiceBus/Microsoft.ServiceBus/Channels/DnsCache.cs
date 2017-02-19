using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Interop;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal static class DnsCache
	{
		private const int MruWatermark = 64;

		private readonly static MruCache<string, DnsCache.DnsCacheEntry> ResolveCache;

		private static string machineName;

		public static TimeSpan CacheTimeout
		{
			get;
			set;
		}

		public static string MachineName
		{
			get
			{
				if (DnsCache.machineName == null)
				{
					lock (DnsCache.ThisLock)
					{
						if (DnsCache.machineName == null)
						{
							try
							{
								DnsCache.machineName = Dns.GetHostEntry(string.Empty).HostName;
							}
							catch (SocketException socketException1)
							{
								SocketException socketException = socketException1;
								if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
								{
									Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(socketException, TraceEventType.Information);
								}
								DnsCache.machineName = UnsafeNativeMethods.GetComputerName(ComputerNameFormat.PhysicalNetBIOS);
							}
						}
					}
				}
				return DnsCache.machineName;
			}
		}

		private static object ThisLock
		{
			get
			{
				return DnsCache.ResolveCache;
			}
		}

		static DnsCache()
		{
			DnsCache.ResolveCache = new MruCache<string, DnsCache.DnsCacheEntry>(64);
			DnsCache.CacheTimeout = TimeSpan.FromSeconds(2);
		}

		public static IPHostEntry Resolve(string hostName)
		{
			DnsCache.DnsCacheEntry dnsCacheEntry;
			IPHostEntry hostEntry = null;
			DateTime utcNow = DateTime.UtcNow;
			lock (DnsCache.ThisLock)
			{
				if (DnsCache.ResolveCache.TryGetValue(hostName, out dnsCacheEntry))
				{
					if (utcNow.Subtract(dnsCacheEntry.TimeStamp) <= DnsCache.CacheTimeout)
					{
						if (dnsCacheEntry.HostEntry == null)
						{
							ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
							string dnsResolveFailed = Resources.DnsResolveFailed;
							object[] objArray = new object[] { hostName };
							throw exceptionUtility.ThrowHelperError(new EndpointNotFoundException(Microsoft.ServiceBus.SR.GetString(dnsResolveFailed, objArray)));
						}
						hostEntry = dnsCacheEntry.HostEntry;
					}
					else
					{
						DnsCache.ResolveCache.Remove(hostName);
					}
				}
			}
			if (hostEntry == null)
			{
				SocketException socketException = null;
				try
				{
					hostEntry = Dns.GetHostEntry(hostName);
				}
				catch (SocketException socketException1)
				{
					socketException = socketException1;
				}
				lock (DnsCache.ThisLock)
				{
					DnsCache.ResolveCache.Remove(hostName);
					DnsCache.ResolveCache.Add(hostName, new DnsCache.DnsCacheEntry(hostEntry, utcNow));
				}
				if (socketException != null)
				{
					ExceptionUtility exceptionUtility1 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string str = Resources.DnsResolveFailed;
					object[] objArray1 = new object[] { hostName };
					throw exceptionUtility1.ThrowHelperError(new EndpointNotFoundException(Microsoft.ServiceBus.SR.GetString(str, objArray1), socketException));
				}
			}
			return hostEntry;
		}

		private class DnsCacheEntry
		{
			private readonly IPHostEntry hostEntry;

			private readonly DateTime timeStamp;

			public IPHostEntry HostEntry
			{
				get
				{
					return this.hostEntry;
				}
			}

			public DateTime TimeStamp
			{
				get
				{
					return this.timeStamp;
				}
			}

			public DnsCacheEntry(IPHostEntry hostEntry, DateTime timeStamp)
			{
				this.hostEntry = hostEntry;
				this.timeStamp = timeStamp;
			}
		}
	}
}
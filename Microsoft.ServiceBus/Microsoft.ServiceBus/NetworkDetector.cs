using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus
{
	internal class NetworkDetector
	{
		private static int httpConnectivityStatus;

		private static int tcpConnectivityStatus;

		private static int httpWebStreamConnectivityStatus;

		private static int httpsWebStreamConnectivityStatus;

		private static int httpsWebSocketConnectivityStatus;

		static NetworkDetector()
		{
			NetworkChange.NetworkAvailabilityChanged += new NetworkAvailabilityChangedEventHandler((object o, NetworkAvailabilityEventArgs ea) => NetworkDetector.Reset());
			NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler((object o, EventArgs ea) => NetworkDetector.Reset());
		}

		public NetworkDetector()
		{
		}

		private static NetworkDetector.ConnectivityStatus CheckHttpConnectivity(Uri baseAddress, out Exception exception)
		{
			NetworkDetector.ConnectivityStatus connectivityStatu = NetworkDetector.ConnectivityStatus.Unavailable;
			exception = null;
			try
			{
				Uri uri = ServiceBusUriHelper.CreateServiceUri("http", baseAddress.Authority, "/");
				WebRequest webRequest = WebRequest.Create(uri);
				webRequest.Method = "GET";
				StreamReader streamReader = new StreamReader(webRequest.GetResponse().GetResponseStream());
				using (streamReader)
				{
					streamReader.ReadToEnd();
				}
				connectivityStatu = NetworkDetector.ConnectivityStatus.Available;
			}
			catch (WebException webException)
			{
				exception = webException;
			}
			NetworkDetector.LogResult(baseAddress, "Http", connectivityStatu);
			return connectivityStatu;
		}

		private static NetworkDetector.ConnectivityStatus CheckHttpsWebSocketConnectivity(Uri baseAddress, out Exception exception)
		{
			NetworkDetector.ConnectivityStatus connectivityStatu = NetworkDetector.ConnectivityStatus.Unavailable;
			exception = null;
			if (ServiceBusClientWebSocket.IsSupportingScheme(ServiceBusUriHelper.CreateServiceUri("https", baseAddress.Authority, "/"), out exception))
			{
				connectivityStatu = NetworkDetector.ConnectivityStatus.Available;
			}
			NetworkDetector.LogResult(baseAddress, "https WebSocket", connectivityStatu);
			return connectivityStatu;
		}

		private static NetworkDetector.ConnectivityStatus CheckHttpsWebStreamConnectivity(Uri baseAddress, out Exception exception)
		{
			NetworkDetector.ConnectivityStatus connectivityStatu = NetworkDetector.ConnectivityStatus.Unavailable;
			exception = null;
			if (WebStream.IsSupportingScheme(ServiceBusUriHelper.CreateServiceUri("https", baseAddress.Authority, "/"), out exception))
			{
				connectivityStatu = NetworkDetector.ConnectivityStatus.Available;
			}
			NetworkDetector.LogResult(baseAddress, "https WebStream", connectivityStatu);
			return connectivityStatu;
		}

		private static NetworkDetector.ConnectivityStatus CheckHttpWebStreamConnectivity(Uri baseAddress, out Exception exception)
		{
			NetworkDetector.ConnectivityStatus connectivityStatu = NetworkDetector.ConnectivityStatus.Unavailable;
			exception = null;
			if (!RelayEnvironment.GetEnvironmentVariable("RELAYFORCEHTTPS", false) && WebStream.IsSupportingScheme(ServiceBusUriHelper.CreateServiceUri("http", baseAddress.Authority, "/"), out exception))
			{
				connectivityStatu = NetworkDetector.ConnectivityStatus.Available;
			}
			NetworkDetector.LogResult(baseAddress, "Http WebStream", connectivityStatu);
			return connectivityStatu;
		}

		private static NetworkDetector.ConnectivityStatus CheckTcpConnectivity(Uri baseAddress, out Exception exception)
		{
			NetworkDetector.ConnectivityStatus connectivityStatu = NetworkDetector.ConnectivityStatus.Unavailable;
			exception = null;
			if (!RelayEnvironment.GetEnvironmentVariable("RELAYFORCEHTTP", false) && !RelayEnvironment.GetEnvironmentVariable("RELAYFORCEHTTPS", false))
			{
				try
				{
					BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = new BinaryMessageEncodingBindingElement();
					TcpTransportBindingElement tcpTransportBindingElement = new TcpTransportBindingElement();
					tcpTransportBindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = 100;
					tcpTransportBindingElement.MaxReceivedMessageSize = (long)65536;
					CustomBinding customBinding = new CustomBinding();
					customBinding.Elements.Add(binaryMessageEncodingBindingElement);
					customBinding.Elements.Add(tcpTransportBindingElement);
					customBinding.OpenTimeout = TimeSpan.FromSeconds(10);
					customBinding.SendTimeout = TimeSpan.FromSeconds(10);
					customBinding.ReceiveTimeout = TimeSpan.MaxValue;
					int num = 9350;
					Uri uri = ServiceBusUriHelper.CreateServiceUri("net.tcp", string.Concat(baseAddress.DnsSafeHost, ":", num.ToString(CultureInfo.InvariantCulture)), "/");
					IChannelFactory<IDuplexSessionChannel> channelFactory = null;
					IDuplexSessionChannel duplexSessionChannel = null;
					try
					{
						channelFactory = customBinding.BuildChannelFactory<IDuplexSessionChannel>(new object[0]);
						channelFactory.Open();
						duplexSessionChannel = channelFactory.CreateChannel(new EndpointAddress(uri, new AddressHeader[0]));
						duplexSessionChannel.Open();
						Message message = Message.CreateMessage(MessageVersion.Default, "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/OnewayPing", new OnewayPingMessage());
						duplexSessionChannel.Send(message, customBinding.SendTimeout);
						duplexSessionChannel.Close();
						duplexSessionChannel = null;
						channelFactory.Close();
						channelFactory = null;
					}
					finally
					{
						if (duplexSessionChannel != null)
						{
							duplexSessionChannel.Abort();
						}
						if (channelFactory != null)
						{
							channelFactory.Abort();
						}
					}
					connectivityStatu = NetworkDetector.ConnectivityStatus.Available;
				}
				catch (CommunicationException communicationException)
				{
					exception = communicationException;
				}
				catch (TimeoutException timeoutException)
				{
					exception = timeoutException;
				}
			}
			NetworkDetector.LogResult(baseAddress, "Tcp", connectivityStatu);
			return connectivityStatu;
		}

		public static ConnectivityMode DetectConnectivityModeForAutoDetect(Uri uri)
		{
			if (NetworkDetector.tcpConnectivityStatus == 3 && NetworkDetector.httpConnectivityStatus == 3)
			{
				NetworkDetector.Reset();
			}
			Exception[] exceptionArray = new Exception[2];
			if (NetworkDetector.IsNetTcpConnectivityAvailable(uri, out exceptionArray[0]))
			{
				return ConnectivityMode.Tcp;
			}
			if (!NetworkDetector.IsHttpConnectivityAvailable(uri, out exceptionArray[1]))
			{
				Exception[] array = (
					from e in (IEnumerable<Exception>)exceptionArray
					where e != null
					select e).ToArray<Exception>();
				string reach = SRClient.UnableToReach(uri.Host, 9351, 9352);
				if ((int)array.Length <= 0)
				{
					throw Fx.Exception.AsError(new CommunicationException(reach), null);
				}
				throw Fx.Exception.AsError(new CommunicationException(reach, new AggregateException(array)), null);
			}
			return ConnectivityMode.Http;
		}

		public static InternalConnectivityMode DetectInternalConnectivityModeForAutoDetect(Uri uri)
		{
			if (NetworkDetector.tcpConnectivityStatus == 3 && NetworkDetector.httpWebStreamConnectivityStatus == 3 && NetworkDetector.httpsWebStreamConnectivityStatus == 3)
			{
				NetworkDetector.Reset();
			}
			Exception[] exceptionArray = new Exception[4];
			if (NetworkDetector.IsNetTcpConnectivityAvailable(uri, out exceptionArray[0]))
			{
				return InternalConnectivityMode.Tcp;
			}
			if (NetworkDetector.IsHttpsWebSocketConnectivityAvailable(uri, out exceptionArray[1]))
			{
				return InternalConnectivityMode.HttpsWebSocket;
			}
			if (NetworkDetector.IsHttpWebStreamConnectivityAvailable(uri, out exceptionArray[2]))
			{
				return InternalConnectivityMode.Http;
			}
			if (!NetworkDetector.IsHttpsWebStreamConnectivityAvailable(uri, out exceptionArray[3]))
			{
				Exception[] array = (
					from e in (IEnumerable<Exception>)exceptionArray
					where e != null
					select e).ToArray<Exception>();
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] host = new object[] { uri.Host, 9351, 9352 };
				string str = string.Format(invariantCulture, "Unable to reach {0} via TCP ({1}, {2}) or HTTP (80, 443)", host);
				if ((int)array.Length <= 0)
				{
					throw Fx.Exception.AsError(new CommunicationException(str), null);
				}
				throw Fx.Exception.AsError(new CommunicationException(str, new AggregateException(array)), null);
			}
			return InternalConnectivityMode.Https;
		}

		public static InternalConnectivityMode DetectInternalConnectivityModeForHttp(Uri uri)
		{
			if (NetworkDetector.httpWebStreamConnectivityStatus == 3 && NetworkDetector.httpsWebSocketConnectivityStatus == 3 && NetworkDetector.httpsWebStreamConnectivityStatus == 3)
			{
				NetworkDetector.Reset();
			}
			Exception[] exceptionArray = new Exception[3];
			if (NetworkDetector.IsHttpsWebSocketConnectivityAvailable(uri, out exceptionArray[0]))
			{
				return InternalConnectivityMode.HttpsWebSocket;
			}
			if (NetworkDetector.IsHttpWebStreamConnectivityAvailable(uri, out exceptionArray[1]))
			{
				return InternalConnectivityMode.Http;
			}
			if (!NetworkDetector.IsHttpsWebStreamConnectivityAvailable(uri, out exceptionArray[2]))
			{
				Exception[] array = (
					from e in (IEnumerable<Exception>)exceptionArray
					where e != null
					select e).ToArray<Exception>();
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] host = new object[] { uri.Host };
				string str = string.Format(invariantCulture, "Unable to reach {0} HTTP (80, 443)", host);
				if ((int)array.Length <= 0)
				{
					throw Fx.Exception.AsError(new CommunicationException(str), null);
				}
				throw Fx.Exception.AsError(new CommunicationException(str, new AggregateException(array)), null);
			}
			return InternalConnectivityMode.Https;
		}

		public static bool IsHttpConnectivityAvailable(Uri baseAddress, out Exception exception)
		{
			NetworkDetector.ConnectivityStatus connectivityStatu;
			exception = null;
			while (true)
			{
				NetworkDetector.ConnectivityStatus connectivityStatu1 = (NetworkDetector.ConnectivityStatus)Interlocked.CompareExchange(ref NetworkDetector.httpConnectivityStatus, 1, 0);
				switch (connectivityStatu1)
				{
					case NetworkDetector.ConnectivityStatus.Testing:
					{
						Thread.Sleep(5);
						continue;
					}
					case NetworkDetector.ConnectivityStatus.Available:
					case NetworkDetector.ConnectivityStatus.Unavailable:
					{
						return connectivityStatu1 == NetworkDetector.ConnectivityStatus.Available;
					}
					default:
					{
						connectivityStatu = NetworkDetector.CheckHttpConnectivity(baseAddress, out exception);
						NetworkDetector.ConnectivityStatus connectivityStatu2 = (NetworkDetector.ConnectivityStatus)Interlocked.CompareExchange(ref NetworkDetector.httpConnectivityStatus, (int)connectivityStatu, 1);
						if (connectivityStatu2 == NetworkDetector.ConnectivityStatus.Testing)
						{
							return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
						}
						if (connectivityStatu2 == NetworkDetector.ConnectivityStatus.Unknown)
						{
							continue;
						}
						if (connectivityStatu == connectivityStatu2)
						{
							return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
						}
						NetworkDetector.httpConnectivityStatus = 0;
						continue;
					}
				}
			}
			return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
		}

		private static bool IsHttpsWebSocketConnectivityAvailable(Uri baseAddress, out Exception exception)
		{
			NetworkDetector.ConnectivityStatus connectivityStatu;
			exception = null;
			while (true)
			{
				NetworkDetector.ConnectivityStatus connectivityStatu1 = (NetworkDetector.ConnectivityStatus)Interlocked.CompareExchange(ref NetworkDetector.httpsWebSocketConnectivityStatus, 1, 0);
				switch (connectivityStatu1)
				{
					case NetworkDetector.ConnectivityStatus.Unknown:
					{
						connectivityStatu = NetworkDetector.CheckHttpsWebSocketConnectivity(baseAddress, out exception);
						NetworkDetector.ConnectivityStatus connectivityStatu2 = (NetworkDetector.ConnectivityStatus)Interlocked.CompareExchange(ref NetworkDetector.httpsWebSocketConnectivityStatus, (int)connectivityStatu, 1);
						if (connectivityStatu2 == NetworkDetector.ConnectivityStatus.Testing)
						{
							return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
						}
						if (connectivityStatu2 == NetworkDetector.ConnectivityStatus.Unknown)
						{
							continue;
						}
						if (connectivityStatu == connectivityStatu2)
						{
							return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
						}
						NetworkDetector.httpsWebSocketConnectivityStatus = 0;
						continue;
					}
					case NetworkDetector.ConnectivityStatus.Testing:
					{
						Thread.Sleep(5);
						continue;
					}
					case NetworkDetector.ConnectivityStatus.Available:
					case NetworkDetector.ConnectivityStatus.Unavailable:
					{
						return connectivityStatu1 == NetworkDetector.ConnectivityStatus.Available;
					}
					default:
					{
						goto case NetworkDetector.ConnectivityStatus.Unknown;
					}
				}
			}
			return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
		}

		public static bool IsHttpsWebStreamConnectivityAvailable(Uri baseAddress, out Exception exception)
		{
			NetworkDetector.ConnectivityStatus connectivityStatu;
			exception = null;
			while (true)
			{
				NetworkDetector.ConnectivityStatus connectivityStatu1 = (NetworkDetector.ConnectivityStatus)Interlocked.CompareExchange(ref NetworkDetector.httpsWebStreamConnectivityStatus, 1, 0);
				switch (connectivityStatu1)
				{
					case NetworkDetector.ConnectivityStatus.Testing:
					{
						Thread.Sleep(5);
						continue;
					}
					case NetworkDetector.ConnectivityStatus.Available:
					case NetworkDetector.ConnectivityStatus.Unavailable:
					{
						return connectivityStatu1 == NetworkDetector.ConnectivityStatus.Available;
					}
					default:
					{
						connectivityStatu = NetworkDetector.CheckHttpsWebStreamConnectivity(baseAddress, out exception);
						NetworkDetector.ConnectivityStatus connectivityStatu2 = (NetworkDetector.ConnectivityStatus)Interlocked.CompareExchange(ref NetworkDetector.httpsWebStreamConnectivityStatus, (int)connectivityStatu, 1);
						if (connectivityStatu2 == NetworkDetector.ConnectivityStatus.Testing)
						{
							return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
						}
						if (connectivityStatu2 == NetworkDetector.ConnectivityStatus.Unknown)
						{
							continue;
						}
						if (connectivityStatu == connectivityStatu2)
						{
							return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
						}
						NetworkDetector.httpWebStreamConnectivityStatus = 0;
						continue;
					}
				}
			}
			return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
		}

		public static bool IsHttpWebStreamConnectivityAvailable(Uri baseAddress, out Exception exception)
		{
			NetworkDetector.ConnectivityStatus connectivityStatu;
			exception = null;
			while (true)
			{
				NetworkDetector.ConnectivityStatus connectivityStatu1 = (NetworkDetector.ConnectivityStatus)Interlocked.CompareExchange(ref NetworkDetector.httpWebStreamConnectivityStatus, 1, 0);
				switch (connectivityStatu1)
				{
					case NetworkDetector.ConnectivityStatus.Testing:
					{
						Thread.Sleep(5);
						continue;
					}
					case NetworkDetector.ConnectivityStatus.Available:
					case NetworkDetector.ConnectivityStatus.Unavailable:
					{
						return connectivityStatu1 == NetworkDetector.ConnectivityStatus.Available;
					}
					default:
					{
						connectivityStatu = NetworkDetector.CheckHttpWebStreamConnectivity(baseAddress, out exception);
						NetworkDetector.ConnectivityStatus connectivityStatu2 = (NetworkDetector.ConnectivityStatus)Interlocked.CompareExchange(ref NetworkDetector.httpWebStreamConnectivityStatus, (int)connectivityStatu, 1);
						if (connectivityStatu2 == NetworkDetector.ConnectivityStatus.Testing)
						{
							return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
						}
						if (connectivityStatu2 == NetworkDetector.ConnectivityStatus.Unknown)
						{
							continue;
						}
						if (connectivityStatu == connectivityStatu2)
						{
							return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
						}
						NetworkDetector.httpWebStreamConnectivityStatus = 0;
						continue;
					}
				}
			}
			return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
		}

		public static bool IsNetTcpConnectivityAvailable(Uri baseAddress, out Exception exception)
		{
			NetworkDetector.ConnectivityStatus connectivityStatu;
			exception = null;
			while (true)
			{
				NetworkDetector.ConnectivityStatus connectivityStatu1 = (NetworkDetector.ConnectivityStatus)Interlocked.CompareExchange(ref NetworkDetector.tcpConnectivityStatus, 1, 0);
				switch (connectivityStatu1)
				{
					case NetworkDetector.ConnectivityStatus.Testing:
					{
						Thread.Sleep(5);
						continue;
					}
					case NetworkDetector.ConnectivityStatus.Available:
					case NetworkDetector.ConnectivityStatus.Unavailable:
					{
						return connectivityStatu1 == NetworkDetector.ConnectivityStatus.Available;
					}
					default:
					{
						connectivityStatu = NetworkDetector.CheckTcpConnectivity(baseAddress, out exception);
						NetworkDetector.ConnectivityStatus connectivityStatu2 = (NetworkDetector.ConnectivityStatus)Interlocked.CompareExchange(ref NetworkDetector.tcpConnectivityStatus, (int)connectivityStatu, 1);
						if (connectivityStatu2 == NetworkDetector.ConnectivityStatus.Testing)
						{
							return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
						}
						if (connectivityStatu2 == NetworkDetector.ConnectivityStatus.Unknown)
						{
							continue;
						}
						if (connectivityStatu == connectivityStatu2)
						{
							return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
						}
						NetworkDetector.tcpConnectivityStatus = 0;
						continue;
					}
				}
			}
			return connectivityStatu == NetworkDetector.ConnectivityStatus.Available;
		}

		private static void LogResult(Uri checkUri, string checkMode, NetworkDetector.ConnectivityStatus result)
		{
			if (result == NetworkDetector.ConnectivityStatus.Available)
			{
				return;
			}
			MessagingClientEtwProvider.Provider.DetectConnectivityModeFailed(checkUri.AbsoluteUri, checkMode);
		}

		public static void Reset()
		{
			NetworkDetector.tcpConnectivityStatus = 0;
			NetworkDetector.httpWebStreamConnectivityStatus = 0;
			NetworkDetector.httpsWebStreamConnectivityStatus = 0;
			NetworkDetector.httpConnectivityStatus = 0;
		}

		public static void SetConnectivityMode(InternalConnectivityMode mode)
		{
			switch (mode)
			{
				case InternalConnectivityMode.Tcp:
				{
					NetworkDetector.tcpConnectivityStatus = 2;
					NetworkDetector.httpsWebSocketConnectivityStatus = 3;
					NetworkDetector.httpWebStreamConnectivityStatus = 3;
					NetworkDetector.httpsWebStreamConnectivityStatus = 3;
					NetworkDetector.httpConnectivityStatus = 3;
					return;
				}
				case InternalConnectivityMode.Http:
				{
					NetworkDetector.tcpConnectivityStatus = 3;
					NetworkDetector.httpsWebSocketConnectivityStatus = 3;
					NetworkDetector.httpWebStreamConnectivityStatus = 2;
					NetworkDetector.httpsWebStreamConnectivityStatus = 3;
					NetworkDetector.httpConnectivityStatus = 2;
					return;
				}
				case InternalConnectivityMode.Https:
				{
					NetworkDetector.tcpConnectivityStatus = 3;
					NetworkDetector.httpsWebStreamConnectivityStatus = 2;
					NetworkDetector.httpsWebSocketConnectivityStatus = 3;
					NetworkDetector.httpWebStreamConnectivityStatus = 3;
					NetworkDetector.httpConnectivityStatus = 3;
					return;
				}
				case InternalConnectivityMode.HttpsWebSocket:
				{
					NetworkDetector.tcpConnectivityStatus = 3;
					NetworkDetector.httpsWebSocketConnectivityStatus = 2;
					NetworkDetector.httpsWebStreamConnectivityStatus = 3;
					NetworkDetector.httpWebStreamConnectivityStatus = 3;
					NetworkDetector.httpConnectivityStatus = 3;
					return;
				}
				default:
				{
					return;
				}
			}
		}

		private enum ConnectivityStatus
		{
			Unknown,
			Testing,
			Available,
			Unavailable
		}
	}
}
using Microsoft.ServiceBus.Channels;
using System;

namespace Microsoft.ServiceBus
{
	internal class ConnectivityModeConnectionInitiator : IConnectionInitiator
	{
		private readonly TokenProvider tokenProvider;

		private readonly SocketSecurityRole securityMode;

		private readonly int bufferSize;

		private readonly ConnectivityModeCache cache;

		private readonly object mutex = new object();

		private volatile IConnectionInitiator httpConnectionInitiator;

		private volatile IConnectionInitiator httpsConnectionInitiator;

		private volatile IConnectionInitiator tcpConnectionInitiator;

		private bool useWebSocket;

		private InternalConnectivityMode mode;

		private IConnectionInitiator HttpConnectionInitiator
		{
			get
			{
				if (this.httpConnectionInitiator == null)
				{
					lock (this.mutex)
					{
						if (this.httpConnectionInitiator == null)
						{
							this.httpConnectionInitiator = new WebStreamConnectionInitiator(this.tokenProvider, this.securityMode, this.bufferSize, false);
						}
					}
				}
				return this.httpConnectionInitiator;
			}
		}

		private IConnectionInitiator HttpsConnectionInitiator
		{
			get
			{
				if (this.httpsConnectionInitiator == null)
				{
					lock (this.mutex)
					{
						if (this.httpsConnectionInitiator == null)
						{
							if (!this.useWebSocket)
							{
								this.httpsConnectionInitiator = new WebStreamConnectionInitiator(this.tokenProvider, this.securityMode, this.bufferSize, true);
							}
							else
							{
								this.httpsConnectionInitiator = new WebSocketConnectionInitiator(this.tokenProvider, this.bufferSize);
							}
						}
					}
				}
				return this.httpsConnectionInitiator;
			}
		}

		private IConnectionInitiator TcpConnectionInitiator
		{
			get
			{
				if (this.tcpConnectionInitiator == null)
				{
					lock (this.mutex)
					{
						if (this.tcpConnectionInitiator == null)
						{
							this.tcpConnectionInitiator = new RelayedSocketInitiator(this.bufferSize, this.tokenProvider, this.securityMode);
						}
					}
				}
				return this.tcpConnectionInitiator;
			}
		}

		public ConnectivityModeConnectionInitiator(TokenProvider tokenProvider, SocketSecurityRole securityMode, int bufferSize, ConnectivityModeCache cache)
		{
			this.tokenProvider = tokenProvider;
			this.securityMode = securityMode;
			this.bufferSize = bufferSize;
			this.cache = cache;
		}

		public IAsyncResult BeginConnect(Uri uri, TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.mode = this.cache.GetInternalConnectivityMode(uri);
			switch (this.mode)
			{
				case InternalConnectivityMode.Tcp:
				{
					return this.TcpConnectionInitiator.BeginConnect(uri, timeout, callback, state);
				}
				case InternalConnectivityMode.Http:
				{
					return this.HttpConnectionInitiator.BeginConnect(uri, timeout, callback, state);
				}
				case InternalConnectivityMode.Https:
				{
					this.useWebSocket = false;
					return this.HttpsConnectionInitiator.BeginConnect(uri, timeout, callback, state);
				}
				case InternalConnectivityMode.HttpsWebSocket:
				{
					this.useWebSocket = true;
					return this.HttpsConnectionInitiator.BeginConnect(uri, timeout, callback, state);
				}
			}
			throw new InvalidOperationException(SRClient.UnsupportedConnectivityMode(this.mode));
		}

		public IConnection Connect(Uri uri, TimeSpan timeout)
		{
			InternalConnectivityMode internalConnectivityMode = this.cache.GetInternalConnectivityMode(uri);
			switch (internalConnectivityMode)
			{
				case InternalConnectivityMode.Tcp:
				{
					return this.TcpConnectionInitiator.Connect(uri, timeout);
				}
				case InternalConnectivityMode.Http:
				{
					return this.HttpConnectionInitiator.Connect(uri, timeout);
				}
				case InternalConnectivityMode.Https:
				{
					this.useWebSocket = false;
					return this.HttpsConnectionInitiator.Connect(uri, timeout);
				}
				case InternalConnectivityMode.HttpsWebSocket:
				{
					this.useWebSocket = true;
					return this.HttpsConnectionInitiator.Connect(uri, timeout);
				}
			}
			throw new InvalidOperationException(SRClient.UnsupportedConnectivityMode(internalConnectivityMode));
		}

		public IConnection EndConnect(IAsyncResult result)
		{
			switch (this.mode)
			{
				case InternalConnectivityMode.Tcp:
				{
					return this.TcpConnectionInitiator.EndConnect(result);
				}
				case InternalConnectivityMode.Http:
				{
					return this.HttpConnectionInitiator.EndConnect(result);
				}
				case InternalConnectivityMode.Https:
				{
					return this.HttpsConnectionInitiator.EndConnect(result);
				}
				case InternalConnectivityMode.HttpsWebSocket:
				{
					return this.HttpsConnectionInitiator.EndConnect(result);
				}
			}
			throw new InvalidOperationException(SRClient.UnsupportedConnectivityMode(this.mode));
		}
	}
}
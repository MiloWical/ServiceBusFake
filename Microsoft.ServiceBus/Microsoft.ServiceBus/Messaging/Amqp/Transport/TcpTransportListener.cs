using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class TcpTransportListener : TransportListener
	{
		private readonly Action<object> acceptTransportLoop;

		private readonly TcpTransportSettings transportSettings;

		private Socket[] listenSockets;

		public TcpTransportListener(TcpTransportSettings transportSettings) : base("tcp-listener")
		{
			this.acceptTransportLoop = new Action<object>(this.AcceptTransportLoop);
			this.transportSettings = transportSettings;
		}

		protected override void AbortInternal()
		{
			this.CloseOrAbortListenSockets(true);
		}

		private void AcceptTransportLoop(object state)
		{
			SocketAsyncEventArgs socketAsyncEventArg = (SocketAsyncEventArgs)state;
			Socket userToken = (Socket)socketAsyncEventArg.UserToken;
			while (base.State != AmqpObjectState.End)
			{
				try
				{
					socketAsyncEventArg.AcceptSocket = null;
					if (userToken.AcceptAsync(socketAsyncEventArg))
					{
						break;
					}
					else if (!this.HandleAcceptComplete(socketAsyncEventArg, true))
					{
						break;
					}
				}
				catch (SocketException socketException1)
				{
					SocketException socketException = socketException1;
					if (!this.ShouldRetryAccept(socketException.SocketErrorCode))
					{
						socketAsyncEventArg.Dispose();
						base.SafeClose(socketException);
						break;
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					MessagingClientEtwProvider.TraceClient<TcpTransportListener, Exception>((TcpTransportListener source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpListenSocketAcceptError(source, false, ex.ToStringSlim()), this, exception);
					socketAsyncEventArg.Dispose();
					base.SafeClose(exception);
					break;
				}
			}
		}

		protected override bool CloseInternal()
		{
			this.CloseOrAbortListenSockets(false);
			return true;
		}

		private void CloseOrAbortListenSockets(bool abort)
		{
			if (this.listenSockets != null)
			{
				for (int i = 0; i < (int)this.listenSockets.Length; i++)
				{
					Socket socket = this.listenSockets[i];
					this.listenSockets[i] = null;
					if (socket != null)
					{
						if (!abort)
						{
							socket.Close();
						}
						else
						{
							socket.Close(0);
						}
					}
				}
			}
		}

		private bool HandleAcceptComplete(SocketAsyncEventArgs e, bool completedSynchronously)
		{
			if (e.SocketError == SocketError.Success)
			{
				TcpTransport tcpTransport = new TcpTransport(e.AcceptSocket, this.transportSettings);
				tcpTransport.Open();
				TransportAsyncCallbackArgs transportAsyncCallbackArg = new TransportAsyncCallbackArgs()
				{
					Transport = tcpTransport,
					CompletedSynchronously = completedSynchronously
				};
				base.OnTransportAccepted(transportAsyncCallbackArg);
				return true;
			}
			bool flag = this.ShouldRetryAccept(e.SocketError);
			if (!flag)
			{
				e.Dispose();
				base.SafeClose(new SocketException((int)e.SocketError));
			}
			return flag;
		}

		private void OnAcceptComplete(object sender, SocketAsyncEventArgs e)
		{
			if (this.HandleAcceptComplete(e, false))
			{
				this.AcceptTransportLoop(e);
			}
		}

		protected override void OnListen()
		{
			IPAddress pAddress;
			string host = this.transportSettings.Host;
			List<IPAddress> pAddresses = new List<IPAddress>();
			if (host.Equals(string.Empty))
			{
				pAddresses.AddRange(Dns.GetHostAddresses(host));
			}
			else if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) || host.Equals(Dns.GetHostEntry(string.Empty).HostName, StringComparison.OrdinalIgnoreCase))
			{
				if (Socket.OSSupportsIPv4)
				{
					pAddresses.Add(IPAddress.Any);
				}
				if (Socket.OSSupportsIPv6)
				{
					pAddresses.Add(IPAddress.IPv6Any);
				}
			}
			else if (!IPAddress.TryParse(host, out pAddress))
			{
				pAddresses.AddRange(Dns.GetHostAddresses(this.transportSettings.Host));
			}
			else
			{
				pAddresses.Add(pAddress);
			}
			if (pAddresses.Count == 0)
			{
				throw new InvalidOperationException(SRAmqp.AmqpNoValidAddressForHost(this.transportSettings.Host));
			}
			this.listenSockets = new Socket[pAddresses.Count];
			for (int i = 0; i < pAddresses.Count; i++)
			{
				Socket[] socketArray = this.listenSockets;
				Socket socket = new Socket(pAddresses[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp)
				{
					NoDelay = true
				};
				socketArray[i] = socket;
				this.listenSockets[i].Bind(new IPEndPoint(pAddresses[i], this.transportSettings.Port));
				this.listenSockets[i].Listen(this.transportSettings.TcpBacklog);
				for (int j = 0; j < this.transportSettings.ListenerAcceptorCount; j++)
				{
					SocketAsyncEventArgs socketAsyncEventArg = new SocketAsyncEventArgs();
					socketAsyncEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnAcceptComplete);
					socketAsyncEventArg.UserToken = this.listenSockets[i];
					ActionItem.Schedule(this.acceptTransportLoop, socketAsyncEventArg);
				}
			}
		}

		private bool ShouldRetryAccept(SocketError error)
		{
			if (error == SocketError.OperationAborted)
			{
				return false;
			}
			MessagingClientEtwProvider.TraceClient<TcpTransportListener, SocketError>((TcpTransportListener source, SocketError err) => MessagingClientEtwProvider.Provider.EventWriteAmqpListenSocketAcceptError(source, true, err.ToString()), this, error);
			return true;
		}
	}
}
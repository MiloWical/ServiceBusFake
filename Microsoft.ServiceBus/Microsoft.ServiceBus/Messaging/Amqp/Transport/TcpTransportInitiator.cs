using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class TcpTransportInitiator : TransportInitiator
	{
		private readonly TcpTransportSettings transportSettings;

		private TransportAsyncCallbackArgs callbackArgs;

		internal TcpTransportInitiator(TcpTransportSettings transportSettings)
		{
			this.transportSettings = transportSettings;
		}

		private void Complete(SocketAsyncEventArgs e, bool completeSynchronously)
		{
			TransportBase tcpTransport = null;
			Exception socketException = null;
			if (e.SocketError == SocketError.Success)
			{
				try
				{
					e.ConnectSocket.NoDelay = true;
					tcpTransport = new TcpTransport(e.ConnectSocket, this.transportSettings);
					tcpTransport.Open();
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					socketException = exception;
					if (tcpTransport != null)
					{
						tcpTransport.SafeClose();
					}
					tcpTransport = null;
				}
			}
			else
			{
				socketException = new SocketException((int)e.SocketError);
				if (e.AcceptSocket != null)
				{
					e.AcceptSocket.Close(0);
				}
			}
			e.Dispose();
			this.callbackArgs.CompletedSynchronously = completeSynchronously;
			this.callbackArgs.Exception = socketException;
			this.callbackArgs.Transport = tcpTransport;
			if (!completeSynchronously)
			{
				this.callbackArgs.CompletedCallback(this.callbackArgs);
			}
		}

		public override bool ConnectAsync(TimeSpan timeout, TransportAsyncCallbackArgs callbackArgs)
		{
			this.callbackArgs = callbackArgs;
			DnsEndPoint dnsEndPoint = new DnsEndPoint(this.transportSettings.Host, this.transportSettings.Port);
			SocketAsyncEventArgs socketAsyncEventArg = new SocketAsyncEventArgs();
			socketAsyncEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(TcpTransportInitiator.OnConnectComplete);
			socketAsyncEventArg.RemoteEndPoint = dnsEndPoint;
			socketAsyncEventArg.UserToken = this;
			if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, socketAsyncEventArg))
			{
				return true;
			}
			this.Complete(socketAsyncEventArg, true);
			return false;
		}

		private static void OnConnectComplete(object sender, SocketAsyncEventArgs e)
		{
			((TcpTransportInitiator)e.UserToken).Complete(e, false);
		}
	}
}
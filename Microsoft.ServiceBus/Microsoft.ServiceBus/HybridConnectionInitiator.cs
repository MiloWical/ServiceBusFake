using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class HybridConnectionInitiator : Microsoft.ServiceBus.Channels.IConnectionInitiator
	{
		private int bufferSize;

		private Guid connectionId;

		private DirectSocketInitiator directSocketInitiator;

		private Microsoft.ServiceBus.Channels.IConnectionInitiator relayedSocketInitiator;

		public HybridConnectionInitiator(BindingContext context, TcpRelayTransportBindingElement transportBindingElement, int bufferSize, TokenProvider tokenProvider, RelayTransportProtectionMode transportProtection)
		{
			this.bufferSize = bufferSize;
			NameSettings nameSetting = new NameSettings();
			nameSetting.ServiceSettings.TransportProtection = transportProtection;
			nameSetting.ServiceSettings.ListenerType = ListenerType.HybridConnection;
			IConnectionElement relayedSocketElement = new RelayedSocketElement(context, nameSetting, tokenProvider, SocketSecurityRole.None);
			this.relayedSocketInitiator = (new DemuxSocketElement(relayedSocketElement, "relayed")).CreateInitiator(bufferSize);
			IConnectionElement demuxSocketElement = new DemuxSocketElement(relayedSocketElement, "direct");
			Binding binding = HybridConnectionElement.CreateDirectControlBindingElement(context, transportBindingElement, demuxSocketElement);
			this.directSocketInitiator = new DirectSocketInitiator(bufferSize, binding);
			this.connectionId = Guid.NewGuid();
		}

		public IAsyncResult BeginConnect(Uri uri, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult<Microsoft.ServiceBus.Channels.IConnection>(this.Connect(uri, timeout), callback, state);
		}

		public Microsoft.ServiceBus.Channels.IConnection Connect(Uri uri, TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			Microsoft.ServiceBus.Channels.IConnection connection = this.relayedSocketInitiator.Connect(uri, timeoutHelper.RemainingTime());
			this.SendInitiateMessage(connection, timeoutHelper.RemainingTime());
			HybridConnectionInitiator.DirectSocketClient directSocketClient = new HybridConnectionInitiator.DirectSocketClient(this, this.directSocketInitiator, uri, timeoutHelper.RemainingTime());
			HybridConnection hybridConnection = new HybridConnection(HybridConnectionRole.Initiator, connection, directSocketClient, this.bufferSize);
			directSocketClient.HybridConnection = hybridConnection;
			IOThreadScheduler.ScheduleCallbackNoFlow(new Action<object>(this.StartDirectConnect), directSocketClient);
			return hybridConnection;
		}

		public Microsoft.ServiceBus.Channels.IConnection EndConnect(IAsyncResult result)
		{
			return CompletedAsyncResult<Microsoft.ServiceBus.Channels.IConnection>.End(result);
		}

		private void SendInitiateMessage(Microsoft.ServiceBus.Channels.IConnection connection, TimeSpan timeout)
		{
			try
			{
				TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
				byte[] byteArray = this.connectionId.ToByteArray();
				connection.Write(byteArray, 0, (int)byteArray.Length, true, timeoutHelper.RemainingTime());
				byte[] numArray = new byte[1];
				if (connection.Read(numArray, 0, 1, timeoutHelper.RemainingTime()) < 1 || numArray[0] != 1)
				{
					throw new CommunicationException(SRClient.InvalidReceivedContent);
				}
			}
			catch
			{
				connection.Close(TimeSpan.Zero);
				throw;
			}
		}

		private void StartDirectConnect(object state)
		{
			((HybridConnectionInitiator.DirectSocketClient)state).StartConnecting();
		}

		private class DirectSocketClient : IDisposable
		{
			private bool disposed;

			private DirectSocketInitiator directSocketInitiator;

			private HybridConnectionInitiator hybridConnectionInitiator;

			private object mutex;

			private TimeoutHelper timeout;

			private Uri uri;

			public HybridConnection HybridConnection
			{
				get;
				internal set;
			}

			private object ThisLock
			{
				get
				{
					return this.mutex;
				}
			}

			public DirectSocketClient(HybridConnectionInitiator hybridConnectionInitiator, DirectSocketInitiator directSocketInitiator, Uri uri, TimeSpan timeout)
			{
				this.hybridConnectionInitiator = hybridConnectionInitiator;
				this.directSocketInitiator = directSocketInitiator;
				this.uri = uri;
				this.timeout = new TimeoutHelper(timeout);
				this.mutex = new object();
			}

			private static void DirectConnectCallback(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				((HybridConnectionInitiator.DirectSocketClient)result.AsyncState).DirectConnectComplete(result, false);
			}

			private void DirectConnectComplete(IAsyncResult result, bool completedSynchronously)
			{
				try
				{
					Microsoft.ServiceBus.Channels.IConnection connection = this.directSocketInitiator.EndConnect(result);
					if (!this.disposed)
					{
						this.hybridConnectionInitiator.SendInitiateMessage(connection, this.timeout.RemainingTime());
						this.HybridConnection.EnqueueConnection(connection);
					}
					else
					{
						connection.Abort();
					}
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
				}
			}

			public void Dispose()
			{
				lock (this.ThisLock)
				{
					if (!this.disposed)
					{
						this.disposed = true;
					}
					else
					{
						return;
					}
				}
				GC.SuppressFinalize(this);
			}

			public void StartConnecting()
			{
				if (this.disposed)
				{
					return;
				}
				IAsyncResult asyncResult = this.directSocketInitiator.BeginConnect(this.uri, this.timeout.RemainingTime(), new AsyncCallback(HybridConnectionInitiator.DirectSocketClient.DirectConnectCallback), this);
				if (asyncResult.CompletedSynchronously)
				{
					this.DirectConnectComplete(asyncResult, true);
				}
			}
		}
	}
}
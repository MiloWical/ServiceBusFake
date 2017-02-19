using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class HybridConnectionListener : Microsoft.ServiceBus.Channels.IConnectionListener
	{
		private int bufferSize;

		private InputQueue<HybridConnection> connectionQueue;

		private Dictionary<Guid, HybridConnection> connectionTable;

		private HybridConnectionListener.ConnectionAcceptor directSocketAcceptor;

		private Microsoft.ServiceBus.Channels.IConnectionListener directSocketListener;

		private HybridConnectionListener.ConnectionAcceptor relayedSocketAcceptor;

		private Microsoft.ServiceBus.Channels.IConnectionListener relayedSocketListener;

		public HybridConnectionListener(BindingContext context, TcpRelayTransportBindingElement transportBindingElement, int bufferSize, Uri uri, NameSettings nameSettings, TokenProvider tokenProvider)
		{
			this.bufferSize = bufferSize;
			IConnectionElement relayedSocketElement = new RelayedSocketElement(context, nameSettings, tokenProvider, SocketSecurityRole.None);
			DemuxSocketManager demuxSocketManager = new DemuxSocketManager(relayedSocketElement, bufferSize, uri);
			IConnectionElement demuxSocketElement = new DemuxSocketElement(relayedSocketElement, "relayed", demuxSocketManager);
			this.relayedSocketListener = demuxSocketElement.CreateListener(bufferSize, uri);
			this.relayedSocketAcceptor = new HybridConnectionListener.ConnectionAcceptor(this, this.relayedSocketListener, HybridConnectionListener.HybridConnectionSocketType.Relayed);
			IConnectionElement connectionElement = new DemuxSocketElement(relayedSocketElement, "direct", demuxSocketManager);
			Binding binding = HybridConnectionElement.CreateDirectControlBindingElement(context, transportBindingElement, connectionElement);
			this.directSocketListener = (new DirectSocketElement(binding)).CreateListener(bufferSize, uri);
			this.directSocketAcceptor = new HybridConnectionListener.ConnectionAcceptor(this, this.directSocketListener, HybridConnectionListener.HybridConnectionSocketType.Direct);
			this.connectionQueue = new InputQueue<HybridConnection>();
			this.connectionTable = new Dictionary<Guid, HybridConnection>();
		}

		public void Abort()
		{
			this.relayedSocketListener.Abort();
			this.directSocketListener.Abort();
		}

		public IAsyncResult BeginAccept(AsyncCallback callback, object state)
		{
			return this.connectionQueue.BeginDequeue(TimeSpan.MaxValue, callback, state);
		}

		public void Close(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			this.relayedSocketListener.Close(timeoutHelper.RemainingTime());
			this.directSocketListener.Close(timeoutHelper.RemainingTime());
		}

		public Microsoft.ServiceBus.Channels.IConnection EndAccept(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.IConnection connection;
			try
			{
				connection = this.connectionQueue.EndDequeue(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && !(exception is CommunicationException))
				{
					throw new CommunicationException(exception.Message, exception);
				}
				throw;
			}
			return connection;
		}

		private void EnqueueConnection(Guid connectionId, Microsoft.ServiceBus.Channels.IConnection connection, HybridConnectionListener.HybridConnectionSocketType socketType, Action dequeuedCallback)
		{
			HybridConnection hybridConnection;
			bool flag = false;
			try
			{
				lock (this.connectionTable)
				{
					if (this.connectionTable.TryGetValue(connectionId, out hybridConnection))
					{
						hybridConnection.EnqueueConnection(connection);
					}
					else if (socketType != HybridConnectionListener.HybridConnectionSocketType.Direct)
					{
						hybridConnection = new HybridConnection(HybridConnectionRole.Listener, connection, null, this.bufferSize);
						hybridConnection.Closed += new EventHandler((object o, EventArgs e) => this.connectionTable.Remove(connectionId));
						flag = true;
						this.connectionTable.Add(connectionId, hybridConnection);
					}
					else
					{
						connection.Abort();
						return;
					}
				}
				if (flag)
				{
					this.connectionQueue.EnqueueAndDispatch(hybridConnection);
				}
			}
			finally
			{
				dequeuedCallback();
			}
		}

		public void Open(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			this.relayedSocketListener.Open(timeoutHelper.RemainingTime());
			this.relayedSocketAcceptor.StartAccepting();
			try
			{
				this.directSocketListener.Open(timeoutHelper.RemainingTime());
				this.directSocketAcceptor.StartAccepting();
			}
			catch (Exception exception)
			{
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.relayedSocketListener.Abort();
				throw;
			}
		}

		private class ConnectionAcceptor
		{
			private HybridConnectionListener connectionListener;

			private Microsoft.ServiceBus.Channels.IConnectionListener socketListener;

			private HybridConnectionListener.HybridConnectionSocketType socketType;

			public ConnectionAcceptor(HybridConnectionListener connectionListener, Microsoft.ServiceBus.Channels.IConnectionListener socketListener, HybridConnectionListener.HybridConnectionSocketType socketType)
			{
				this.connectionListener = connectionListener;
				this.socketListener = socketListener;
				this.socketType = socketType;
			}

			private static void AcceptCallback(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				((HybridConnectionListener.ConnectionAcceptor)result.AsyncState).AcceptComplete(result, false);
			}

			private void AcceptComplete(IAsyncResult result, bool completedSynchronously)
			{
				Microsoft.ServiceBus.Channels.IConnection connection;
				try
				{
					try
					{
						connection = this.socketListener.EndAccept(result);
						if (connection == null)
						{
							return;
						}
					}
					catch (Exception exception)
					{
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						IOThreadScheduler.ScheduleCallbackNoFlow(new Action<object>(HybridConnectionListener.ConnectionAcceptor.StartAcceptingStatic), this);
						return;
					}
					try
					{
						this.ProcessConnection(connection);
					}
					catch (Exception exception1)
					{
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
					}
					IOThreadScheduler.ScheduleCallbackNoFlow(new Action<object>(HybridConnectionListener.ConnectionAcceptor.StartAcceptingStatic), this);
				}
				catch (Exception exception2)
				{
					if (Fx.IsFatal(exception2))
					{
						throw;
					}
				}
			}

			private void OnSocketDequeued()
			{
			}

			private void ProcessConnection(Microsoft.ServiceBus.Channels.IConnection connection)
			{
				TimeoutHelper timeoutHelper = new TimeoutHelper(Microsoft.ServiceBus.ServiceDefaults.ReceiveTimeout);
				bool flag = false;
				try
				{
					byte[] numArray = new byte[16];
					if (connection.Read(numArray, 0, 16, timeoutHelper.RemainingTime()) != 16)
					{
						throw new CommunicationException(SRClient.InvalidLengthofReceivedContent);
					}
					Guid guid = new Guid(numArray);
					this.connectionListener.EnqueueConnection(guid, connection, this.socketType, new Action(this.OnSocketDequeued));
					byte[] numArray1 = new byte[] { 1 };
					connection.Write(numArray1, 0, 1, true, timeoutHelper.RemainingTime());
					flag = true;
				}
				finally
				{
					if (!flag)
					{
						connection.Abort();
					}
				}
			}

			public void StartAccepting()
			{
				try
				{
					IAsyncResult asyncResult = this.socketListener.BeginAccept(new AsyncCallback(HybridConnectionListener.ConnectionAcceptor.AcceptCallback), this);
					if (asyncResult.CompletedSynchronously)
					{
						this.AcceptComplete(asyncResult, true);
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

			private static void StartAcceptingStatic(object state)
			{
				((HybridConnectionListener.ConnectionAcceptor)state).StartAccepting();
			}
		}

		private enum HybridConnectionSocketType
		{
			Relayed,
			Direct
		}
	}
}
using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus
{
	internal class DirectSocketInitiator : Microsoft.ServiceBus.Channels.IConnectionInitiator
	{
		private readonly int bufferSize;

		private readonly Binding innerBinding;

		public DirectSocketInitiator(int bufferSize, Binding innerBinding)
		{
			this.bufferSize = bufferSize;
			this.innerBinding = innerBinding;
		}

		public IAsyncResult BeginConnect(Uri uri, TimeSpan timeout, AsyncCallback callback, object state)
		{
			DirectSocketInitiator.DirectConnectAsyncResult directConnectAsyncResult = new DirectSocketInitiator.DirectConnectAsyncResult(this.bufferSize, uri, timeout, this.innerBinding, callback, state);
			directConnectAsyncResult.StartConnecting();
			return directConnectAsyncResult;
		}

		public Microsoft.ServiceBus.Channels.IConnection Connect(Uri uri, TimeSpan timeout)
		{
			Microsoft.ServiceBus.Channels.IConnection connection;
			using (DirectSocketInitiator.DirectConnectWaiter directConnectWaiter = new DirectSocketInitiator.DirectConnectWaiter(this.bufferSize, uri, timeout, this.innerBinding))
			{
				connection = directConnectWaiter.Connect();
			}
			return connection;
		}

		public Microsoft.ServiceBus.Channels.IConnection EndConnect(IAsyncResult result)
		{
			return AsyncResult<DirectSocketInitiator.DirectConnectAsyncResult>.End(result).Connection;
		}

		private class DirectConnectAsyncResult : AsyncResult<DirectSocketInitiator.DirectConnectAsyncResult>, IDirectConnectionParent
		{
			private readonly int bufferSize;

			private readonly DuplexChannelFactory<IDirectConnectionControl> channelFactory;

			private readonly DirectConnectionSession session;

			private Socket socket;

			private bool complete;

			protected internal override EventTraceActivity Activity
			{
				get
				{
					return this.session.EventTraceActivity;
				}
			}

			public Microsoft.ServiceBus.Channels.IConnection Connection
			{
				get;
				private set;
			}

			public DirectConnectAsyncResult(int bufferSize, Uri uri, TimeSpan timeout, Binding innerBinding, AsyncCallback callback, object state) : base(callback, state)
			{
				this.bufferSize = bufferSize;
				ProbingClient probingClient = new ProbingClient(uri.Host, ConnectConstants.DefaultProbePorts);
				this.session = new DirectConnectionSession(Guid.NewGuid(), probingClient, this);
				this.channelFactory = new DuplexChannelFactory<IDirectConnectionControl>(this.session, innerBinding, new EndpointAddress(uri, new AddressHeader[0]));
			}

			private void Cleanup()
			{
				try
				{
					this.session.Close(Microsoft.ServiceBus.ServiceDefaults.CloseTimeout);
					try
					{
						this.channelFactory.Close(Microsoft.ServiceBus.ServiceDefaults.CloseTimeout);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						Fx.Exception.TraceHandled(exception, "DirectSocketInitiator.DirectConnectAsyncResult.Cleanup", this.Activity);
						this.channelFactory.Abort();
					}
				}
				catch (Exception exception3)
				{
					Exception exception2 = exception3;
					if (Fx.IsFatal(exception2))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception2, "DirectSocketInitiator.DirectConnectAsyncResult.Cleanup", this.Activity);
					this.session.Abort();
				}
			}

			public void Failure(object sender, Exception exception)
			{
				lock (base.ThisLock)
				{
					if (!this.complete)
					{
						this.complete = true;
						this.Cleanup();
					}
					else
					{
						return;
					}
				}
				base.Complete(false, exception);
			}

			public void StartConnecting()
			{
				try
				{
					IDirectConnectionControl directConnectionControl = this.channelFactory.CreateChannel();
					((IChannel)directConnectionControl).Open();
					this.session.Channel = directConnectionControl;
					this.session.Initiate();
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception, "DirectSocketInitiator.DirectConnectAsyncResult.StartConnecting", this.Activity);
					this.session.Abort();
					this.channelFactory.Abort();
				}
			}

			public void Success(object sender, Socket socket)
			{
				lock (base.ThisLock)
				{
					if (!this.complete)
					{
						this.complete = true;
						this.Cleanup();
						this.socket = socket;
					}
					else
					{
						socket.Close();
						return;
					}
				}
				this.Connection = new Microsoft.ServiceBus.Channels.SocketConnection(this.socket, this.bufferSize, this.Activity);
				base.Complete(false);
			}
		}

		private class DirectConnectWaiter : IDirectConnectionParent, IDisposable
		{
			private readonly int bufferSize;

			private readonly DuplexChannelFactory<IDirectConnectionControl> channelFactory;

			private readonly object mutex;

			private readonly DirectConnectionSession session;

			private readonly ManualResetEvent socketEvent;

			private bool complete;

			private Exception exception;

			private Socket socket;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			private EventTraceActivity Activity
			{
				get
				{
					return this.session.EventTraceActivity;
				}
			}

			private object ThisLock
			{
				get
				{
					return this.mutex;
				}
			}

			public DirectConnectWaiter(int bufferSize, Uri uri, TimeSpan timeout, Binding innerBinding)
			{
				this.bufferSize = bufferSize;
				ProbingClient probingClient = new ProbingClient(uri.Host, ConnectConstants.DefaultProbePorts);
				this.session = new DirectConnectionSession(Guid.NewGuid(), probingClient, this);
				this.channelFactory = new DuplexChannelFactory<IDirectConnectionControl>(this.session, innerBinding, new EndpointAddress(uri, new AddressHeader[0]));
				this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				this.socketEvent = new ManualResetEvent(false);
				this.mutex = new object();
			}

			public Microsoft.ServiceBus.Channels.IConnection Connect()
			{
				Microsoft.ServiceBus.Channels.IConnection connection;
				try
				{
					IDirectConnectionControl directConnectionControl = this.channelFactory.CreateChannel();
					((IChannel)directConnectionControl).Open();
					this.session.Channel = directConnectionControl;
					this.session.Initiate();
					Microsoft.ServiceBus.Channels.IConnection connection1 = this.Wait();
					this.session.Close(this.timeoutHelper.RemainingTime());
					try
					{
						this.channelFactory.Close(this.timeoutHelper.RemainingTime());
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						Fx.Exception.TraceHandled(exception, "DirectSocketInitiator.DirectConnectWaiter.Connect", this.Activity);
					}
					connection = connection1;
				}
				finally
				{
					this.channelFactory.Abort();
				}
				return connection;
			}

			public void Dispose()
			{
				this.channelFactory.Abort();
				this.channelFactory.Close();
				this.socketEvent.Dispose();
			}

			public void Failure(object sender, Exception exception)
			{
				lock (this.ThisLock)
				{
					if (!this.complete)
					{
						this.complete = true;
						this.exception = exception;
						this.socketEvent.Set();
					}
				}
			}

			public void Success(object sender, Socket socket)
			{
				lock (this.ThisLock)
				{
					if (!this.complete)
					{
						this.complete = true;
						this.socket = socket;
						this.socketEvent.Set();
					}
					else
					{
						socket.Close();
					}
				}
			}

			public Microsoft.ServiceBus.Channels.IConnection Wait()
			{
				if (!this.socketEvent.WaitOne(this.timeoutHelper.RemainingTime(), false))
				{
					throw Fx.Exception.AsError(new TimeoutException(), null);
				}
				if (this.exception != null)
				{
					throw Fx.Exception.AsError(this.exception, null);
				}
				if (this.socket == null)
				{
					throw Fx.Exception.AsError(new CommunicationException(SRClient.ConnectFailedCommunicationException), null);
				}
				return new Microsoft.ServiceBus.Channels.SocketConnection(this.socket, this.bufferSize, this.Activity);
			}
		}
	}
}
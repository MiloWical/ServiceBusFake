using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace Microsoft.ServiceBus
{
	[CallbackBehavior(ConcurrencyMode=ConcurrencyMode.Multiple)]
	internal class RelayedConnectionSession
	{
		private int bufferSize;

		private bool complete;

		private bool disposed;

		private Guid id;

		private SocketMessageHelper messageHelper;

		private object mutex;

		private RelayedSocketListener listener;

		private SocketSecurityRole socketSecurityMode;

		private TokenProvider tokenProvider;

		private Uri uri;

		public EventTraceActivity Activity
		{
			get;
			private set;
		}

		public Guid Id
		{
			get
			{
				return this.id;
			}
		}

		private object ThisLock
		{
			get
			{
				return this.mutex;
			}
		}

		public RelayedConnectionSession(int bufferSize, Uri uri, TokenProvider tokenProvider, SocketSecurityRole socketSecurityMode, Guid id, RelayedSocketListener listener, EventTraceActivity activity)
		{
			this.bufferSize = bufferSize;
			this.uri = uri;
			this.tokenProvider = tokenProvider;
			this.socketSecurityMode = socketSecurityMode;
			this.id = id;
			this.listener = listener;
			this.Activity = activity;
			if (this.tokenProvider != null)
			{
				this.socketSecurityMode = SocketSecurityRole.SslClient;
			}
			this.messageHelper = new SocketMessageHelper();
			this.mutex = new object();
		}

		public IAsyncResult BeginConnect(RelayedConnectMessage request, AsyncCallback callback, object state)
		{
			return (new RelayedConnectionSession.ConnectAsyncResult(request, this, callback, state)).Start();
		}

		private void BeginConnectCallback(object state)
		{
			Microsoft.ServiceBus.Channels.IConnection connection = (Microsoft.ServiceBus.Channels.IConnection)state;
			this.messageHelper.BeginReceiveMessage(connection, ConnectConstants.ConnectionInitiateTimeout, new AsyncCallback(this.EndConnectCallback), connection);
		}

		public void Close()
		{
			lock (this.ThisLock)
			{
				if (!this.disposed)
				{
					this.disposed = true;
					this.complete = true;
				}
			}
		}

		public static void End(IAsyncResult result)
		{
			((RelayedConnectionSession.ConnectAsyncResult)result).RelayedConnectionSession.EndConnect(result);
		}

		public void EndConnect(IAsyncResult result)
		{
			try
			{
				AsyncResult<RelayedConnectionSession.ConnectAsyncResult>.End(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					this.listener.Failure(this, exception);
					throw new FaultException(exception.ToString());
				}
				throw;
			}
		}

		private void EndConnectCallback(IAsyncResult ar)
		{
			Microsoft.ServiceBus.Channels.IConnection asyncState = (Microsoft.ServiceBus.Channels.IConnection)ar.AsyncState;
			try
			{
				Message message = this.messageHelper.EndReceiveMessage(ar);
				if (message.IsFault)
				{
					throw ErrorUtility.ConvertToError(MessageFault.CreateFault(message, 65536));
				}
				this.listener.Success(this, asyncState);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				asyncState.Abort();
				this.listener.Failure(this, exception);
			}
		}

		public void Failure(object sender, Exception exception)
		{
			lock (this.ThisLock)
			{
				if (!this.complete)
				{
					this.complete = true;
				}
				else
				{
					return;
				}
			}
			this.listener.Failure(this, exception);
		}

		public void Success(object sender, Microsoft.ServiceBus.Channels.IConnection connection)
		{
			lock (this.ThisLock)
			{
				if (!this.complete)
				{
					this.complete = true;
				}
				else
				{
					connection.Close(TimeSpan.FromSeconds(1));
					return;
				}
			}
			this.listener.Success(this, connection);
		}

		private class ConnectAsyncResult : IteratorAsyncResult<RelayedConnectionSession.ConnectAsyncResult>
		{
			private readonly RelayedConnectMessage request;

			private Socket socket;

			private Microsoft.ServiceBus.Channels.IConnection connection;

			private byte[] typeBytes;

			private byte[] typeLengthBytes;

			private Uri via;

			private byte[] viaBytes;

			private byte[] viaLengthBytes;

			private Message message;

			protected internal override EventTraceActivity Activity
			{
				get
				{
					return this.RelayedConnectionSession.Activity;
				}
			}

			public RelayedConnectionSession RelayedConnectionSession
			{
				get;
				private set;
			}

			protected override TraceEventType TraceEventType
			{
				get
				{
					return TraceEventType.Warning;
				}
			}

			public ConnectAsyncResult(RelayedConnectMessage request, RelayedConnectionSession relayedConnectionSession, AsyncCallback callback, object state) : base(ConnectConstants.ConnectionInitiateTimeout, callback, state)
			{
				this.request = request;
				this.RelayedConnectionSession = relayedConnectionSession;
			}

			protected override IEnumerator<IteratorAsyncResult<RelayedConnectionSession.ConnectAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				RelayedConnectionSession.ConnectAsyncResult connectAsyncResult = this;
				IteratorAsyncResult<RelayedConnectionSession.ConnectAsyncResult>.BeginCall beginCall = (RelayedConnectionSession.ConnectAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.socket.BeginConnect(thisRef.request.IpEndpoint, c, s);
				yield return connectAsyncResult.CallAsync(beginCall, (RelayedConnectionSession.ConnectAsyncResult thisRef, IAsyncResult r) => thisRef.socket.EndConnect(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				this.connection = new Microsoft.ServiceBus.Channels.SocketConnection(this.socket, this.RelayedConnectionSession.bufferSize, this.Activity);
				this.typeBytes = Encoding.UTF8.GetBytes(ConnectConstants.ConnectType);
				this.typeLengthBytes = BitConverter.GetBytes((int)this.typeBytes.Length);
				RelayedConnectionSession.ConnectAsyncResult connectAsyncResult1 = this;
				IteratorAsyncResult<RelayedConnectionSession.ConnectAsyncResult>.BeginCall beginCall1 = (RelayedConnectionSession.ConnectAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.connection.BeginWrite(thisRef.typeLengthBytes, 0, (int)thisRef.typeLengthBytes.Length, true, t, c, s);
				yield return connectAsyncResult1.CallAsync(beginCall1, (RelayedConnectionSession.ConnectAsyncResult thisRef, IAsyncResult r) => thisRef.connection.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				RelayedConnectionSession.ConnectAsyncResult connectAsyncResult2 = this;
				IteratorAsyncResult<RelayedConnectionSession.ConnectAsyncResult>.BeginCall beginCall2 = (RelayedConnectionSession.ConnectAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.connection.BeginWrite(thisRef.typeBytes, 0, (int)thisRef.typeBytes.Length, true, t, c, s);
				yield return connectAsyncResult2.CallAsync(beginCall2, (RelayedConnectionSession.ConnectAsyncResult thisRef, IAsyncResult r) => thisRef.connection.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				string str = string.Concat("sb://", this.request.IpEndpoint, "/");
				this.via = new Uri(str);
				this.viaBytes = Encoding.UTF8.GetBytes(this.via.ToString());
				this.viaLengthBytes = BitConverter.GetBytes((int)this.viaBytes.Length);
				RelayedConnectionSession.ConnectAsyncResult connectAsyncResult3 = this;
				IteratorAsyncResult<RelayedConnectionSession.ConnectAsyncResult>.BeginCall beginCall3 = (RelayedConnectionSession.ConnectAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.connection.BeginWrite(thisRef.viaLengthBytes, 0, (int)thisRef.viaLengthBytes.Length, true, t, c, s);
				yield return connectAsyncResult3.CallAsync(beginCall3, (RelayedConnectionSession.ConnectAsyncResult thisRef, IAsyncResult r) => thisRef.connection.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				RelayedConnectionSession.ConnectAsyncResult connectAsyncResult4 = this;
				IteratorAsyncResult<RelayedConnectionSession.ConnectAsyncResult>.BeginCall beginCall4 = (RelayedConnectionSession.ConnectAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.connection.BeginWrite(thisRef.viaBytes, 0, (int)thisRef.viaBytes.Length, true, t, c, s);
				yield return connectAsyncResult4.CallAsync(beginCall4, (RelayedConnectionSession.ConnectAsyncResult thisRef, IAsyncResult r) => thisRef.connection.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				RelayedConnectionSession.ConnectAsyncResult connectAsyncResult5 = this;
				IteratorAsyncResult<RelayedConnectionSession.ConnectAsyncResult>.BeginCall beginCall5 = (RelayedConnectionSession.ConnectAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => SecureSocketUtil.BeginInitiateSecureClientUpgradeIfNeeded(thisRef.connection, null, thisRef.RelayedConnectionSession.socketSecurityMode, thisRef.RelayedConnectionSession.uri.Host, t, c, s);
				yield return connectAsyncResult5.CallAsync(beginCall5, (RelayedConnectionSession.ConnectAsyncResult thisRef, IAsyncResult r) => thisRef.connection = SecureSocketUtil.EndInitiateSecureClientUpgradeIfNeeded(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				this.message = Message.CreateMessage(this.RelayedConnectionSession.messageHelper.MessageVersion, "RelayedAccept", new AcceptMessage(this.RelayedConnectionSession.id.ToString()));
				this.message.Headers.To = EndpointAddress.AnonymousUri;
				RelayedConnectionSession.ConnectAsyncResult connectAsyncResult6 = this;
				IteratorAsyncResult<RelayedConnectionSession.ConnectAsyncResult>.BeginCall beginCall6 = (RelayedConnectionSession.ConnectAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.RelayedConnectionSession.messageHelper.BeginSendMessage(thisRef.connection, thisRef.message, t, c, s);
				yield return connectAsyncResult6.CallAsync(beginCall6, (RelayedConnectionSession.ConnectAsyncResult thisRef, IAsyncResult r) => thisRef.RelayedConnectionSession.messageHelper.EndSendMessage(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				IOThreadScheduler.ScheduleCallbackNoFlow(new Action<object>(this.RelayedConnectionSession.BeginConnectCallback), this.connection);
			}
		}
	}
}
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal class AmqpConnection : AmqpConnectionBase, ISessionFactory
	{
		private readonly bool isInitiator;

		private readonly ProtocolHeader initialHeader;

		private readonly Microsoft.ServiceBus.Messaging.Amqp.AmqpSettings amqpSettings;

		private readonly HandleTable<AmqpSession> sessionsByLocalHandle;

		private readonly HandleTable<AmqpSession> sessionsByRemoteHandle;

		private IOThreadTimer heartBeatTimer;

		private int heartBeatInterval;

		private DateTime lastSendTime;

		private KeyedByTypeCollection<object> extensions;

		public Microsoft.ServiceBus.Messaging.Amqp.AmqpSettings AmqpSettings
		{
			get
			{
				return this.amqpSettings;
			}
		}

		public KeyedByTypeCollection<object> Extensions
		{
			get
			{
				return LazyInitializer.EnsureInitialized<KeyedByTypeCollection<object>>(ref this.extensions);
			}
		}

		public bool IsInitiator
		{
			get
			{
				return this.isInitiator;
			}
		}

		public ISessionFactory SessionFactory
		{
			get;
			set;
		}

		public object SessionLock
		{
			get
			{
				return base.ThisLock;
			}
		}

		internal AmqpConnection(TransportBase transport, Microsoft.ServiceBus.Messaging.Amqp.AmqpSettings amqpSettings, AmqpConnectionSettings connectionSettings) : this(transport, amqpSettings.GetDefaultHeader(), true, amqpSettings, connectionSettings)
		{
		}

		internal AmqpConnection(TransportBase transport, ProtocolHeader protocolHeader, Microsoft.ServiceBus.Messaging.Amqp.AmqpSettings amqpSettings, AmqpConnectionSettings connectionSettings) : this(transport, protocolHeader, true, amqpSettings, connectionSettings)
		{
		}

		internal AmqpConnection(TransportBase transport, ProtocolHeader protocolHeader, bool isInitiator, Microsoft.ServiceBus.Messaging.Amqp.AmqpSettings amqpSettings, AmqpConnectionSettings connectionSettings) : base("connection", transport, connectionSettings, isInitiator)
		{
			if (amqpSettings == null)
			{
				throw new ArgumentNullException("amqpSettings");
			}
			this.initialHeader = protocolHeader;
			this.isInitiator = isInitiator;
			this.amqpSettings = amqpSettings;
			this.sessionsByLocalHandle = new HandleTable<AmqpSession>(base.Settings.ChannelMax.Value);
			this.sessionsByRemoteHandle = new HandleTable<AmqpSession>(base.Settings.ChannelMax.Value);
			this.SessionFactory = this;
		}

		protected override void AbortInternal()
		{
			this.CancelHeartBeatTimer();
			this.CloseSessions(true);
			base.AsyncIO.Abort();
		}

		public void AddSession(AmqpSession session, ushort? channel)
		{
			int? nullable1;
			session.Closed += new EventHandler(this.OnSessionClosed);
			lock (base.ThisLock)
			{
				session.LocalChannel = (ushort)this.sessionsByLocalHandle.Add(session);
				ushort? nullable2 = channel;
				if (nullable2.HasValue)
				{
					nullable1 = new int?((int)nullable2.GetValueOrDefault());
				}
				else
				{
					nullable1 = null;
				}
				if (nullable1.HasValue)
				{
					this.sessionsByRemoteHandle.Add(channel.Value, session);
				}
			}
			MessagingClientEtwProvider.TraceClient<AmqpConnection, AmqpSession, ushort?>((AmqpConnection source, AmqpSession sess, ushort? ch) => {
				MessagingClientEventSource provider = MessagingClientEtwProvider.Provider;
				AmqpConnection amqpConnection = source;
				AmqpSession amqpSession = sess;
				ushort localChannel = sess.LocalChannel;
				ushort? nullable = ch;
				provider.EventWriteAmqpAddSession(amqpConnection, amqpSession, localChannel, (nullable.HasValue ? nullable.GetValueOrDefault() : 0));
			}, this, session, channel);
		}

		private void CancelHeartBeatTimer()
		{
			if (this.heartBeatTimer != null)
			{
				this.heartBeatTimer.Cancel();
			}
		}

		protected override bool CloseInternal()
		{
			this.CancelHeartBeatTimer();
			this.CloseSessions(!this.SessionFrameAllowed());
			if (base.State == AmqpObjectState.OpenReceived)
			{
				this.SendOpen();
			}
			try
			{
				this.SendClose();
			}
			catch (AmqpException amqpException)
			{
				base.State = AmqpObjectState.End;
			}
			bool state = base.State == AmqpObjectState.End;
			if (state)
			{
				base.AsyncIO.SafeClose();
			}
			return state;
		}

		private void CloseSessions(bool abort)
		{
			IEnumerable<AmqpSession> values = null;
			lock (base.ThisLock)
			{
				values = this.sessionsByLocalHandle.Values;
				if (abort)
				{
					this.sessionsByLocalHandle.Clear();
					this.sessionsByRemoteHandle.Clear();
				}
			}
			foreach (AmqpSession value in values)
			{
				if (!abort)
				{
					value.SafeClose();
				}
				else
				{
					value.Abort();
				}
			}
		}

		public AmqpSession CreateSession(AmqpSessionSettings sessionSettings)
		{
			if (base.IsClosing())
			{
				throw new InvalidOperationException(SRClient.CreateSessionOnClosingConnection);
			}
			AmqpSession amqpSession = this.SessionFactory.CreateSession(this, sessionSettings);
			this.AddSession(amqpSession, null);
			return amqpSession;
		}

		AmqpSession Microsoft.ServiceBus.Messaging.Amqp.ISessionFactory.CreateSession(AmqpConnection connection, AmqpSessionSettings sessionSettings)
		{
			return new AmqpSession(this, sessionSettings, this.amqpSettings.RuntimeProvider);
		}

		private void Negotiate(Open open)
		{
			base.Settings.RemoteContainerId = open.ContainerId;
			base.Settings.RemoteHostName = open.HostName;
			base.Settings.ChannelMax = new ushort?(Math.Min(base.Settings.ChannelMax(), open.ChannelMax()));
			base.FindMutualCapabilites(base.Settings.DesiredCapabilities, open.OfferedCapabilities);
			if (open.MaxFrameSize.HasValue)
			{
				AmqpConnectionSettings settings = base.Settings;
				uint value = base.Settings.MaxFrameSize.Value;
				uint? maxFrameSize = open.MaxFrameSize;
				settings.MaxFrameSize = new uint?(Math.Min(value, maxFrameSize.Value));
			}
		}

		protected override void OnFrameBuffer(ByteBuffer buffer)
		{
			if (base.State == AmqpObjectState.End)
			{
				buffer.Dispose();
				return;
			}
			using (Frame frame = new Frame())
			{
				frame.Decode(buffer);
				if (frame.Command != null)
				{
					this.ProcessFrame(frame);
				}
			}
		}

		private static void OnHeartBeatTimer(object state)
		{
			AmqpConnection amqpConnection = (AmqpConnection)state;
			if (amqpConnection.State != AmqpObjectState.Opened)
			{
				return;
			}
			bool flag = false;
			DateTime dateTime = amqpConnection.lastSendTime;
			DateTime utcNow = DateTime.UtcNow;
			DateTime dateTime1 = utcNow;
			if (utcNow.Subtract(dateTime) < TimeSpan.FromMilliseconds((double)amqpConnection.heartBeatInterval))
			{
				flag = true;
				dateTime1 = dateTime;
			}
			try
			{
				if (!flag)
				{
					amqpConnection.SendBuffer(Frame.EncodeCommand(FrameType.Amqp, 0, null, 0));
				}
				IOThreadTimer oThreadTimer = amqpConnection.heartBeatTimer;
				DateTime dateTime2 = dateTime1.AddMilliseconds((double)amqpConnection.heartBeatInterval);
				oThreadTimer.Set(dateTime2.Subtract(utcNow));
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient<AmqpConnection, Exception>((AmqpConnection source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "OnHeartBeatTimer", ex.Message), amqpConnection, exception);
			}
		}

		protected override void OnProtocolHeader(ProtocolHeader header)
		{
			base.TransitState("R:HDR", StateTransition.ReceiveHeader);
			Exception amqpException = null;
			if (!this.isInitiator)
			{
				ProtocolHeader supportedHeader = this.amqpSettings.GetSupportedHeader(header);
				this.SendProtocolHeader(supportedHeader);
				if (!supportedHeader.Equals(header))
				{
					amqpException = new AmqpException(AmqpError.NotImplemented, SRAmqp.AmqpProtocolVersionNotSupported(this.initialHeader.ToString(), header.ToString()));
				}
			}
			else if (!this.initialHeader.Equals(header))
			{
				amqpException = new AmqpException(AmqpError.NotImplemented, SRAmqp.AmqpProtocolVersionNotSupported(this.initialHeader.ToString(), header.ToString()));
			}
			if (amqpException != null)
			{
				base.CompleteOpen(false, amqpException);
			}
		}

		private void OnReceiveClose(Close close)
		{
			base.OnReceiveCloseCommand("R:CLOSE", close.Error);
			if (base.State == AmqpObjectState.End)
			{
				base.AsyncIO.SafeClose();
			}
		}

		private void OnReceiveOpen(Open open)
		{
			StateTransition stateTransition = base.TransitState("R:OPEN", StateTransition.ReceiveOpen);
			this.Negotiate(open);
			base.NotifyOpening(open);
			uint num = open.IdleTimeOut();
			if (num < 60000)
			{
				base.CompleteOpen(false, new AmqpException(AmqpError.NotAllowed, SRAmqp.AmqpIdleTimeoutNotSupported(num, (uint)60000)));
				return;
			}
			if (stateTransition.To == AmqpObjectState.OpenReceived)
			{
				this.SendOpen();
			}
			if (this.isInitiator)
			{
				Error error = null;
				if (open.Properties != null && open.Properties.TryGetValue<Error>("com.microsoft:open-error", out error))
				{
					base.CompleteOpen(stateTransition.From == AmqpObjectState.Start, new AmqpException(error));
					return;
				}
			}
			if (num != -1)
			{
				this.heartBeatInterval = (int)(num * 7 / 8);
				this.heartBeatTimer = new IOThreadTimer(new Action<object>(AmqpConnection.OnHeartBeatTimer), this, false);
				this.heartBeatTimer.Set(this.heartBeatInterval);
			}
			base.CompleteOpen(stateTransition.From == AmqpObjectState.Start, null);
		}

		private void OnReceiveSessionFrame(Frame frame)
		{
			AmqpSession nullable = null;
			Performative command = frame.Command;
			ushort channel = frame.Channel;
			if (command.DescriptorCode != Begin.Code)
			{
				if (!this.sessionsByRemoteHandle.TryGetObject(channel, out nullable))
				{
					if (command.DescriptorCode != End.Code && command.DescriptorCode != Detach.Code && !base.Settings.IgnoreMissingSessions)
					{
						throw new AmqpException(AmqpError.NotFound, SRAmqp.AmqpChannelNotFound(channel, this));
					}
					return;
				}
				if (command.DescriptorCode == End.Code)
				{
					this.sessionsByRemoteHandle.Remove(channel);
					nullable.RemoteChannel = null;
				}
			}
			else
			{
				Begin begin = (Begin)command;
				if (!begin.RemoteChannel.HasValue)
				{
					AmqpSessionSettings amqpSessionSetting = AmqpSessionSettings.Create(begin);
					amqpSessionSetting.RemoteChannel = new ushort?(channel);
					nullable = this.SessionFactory.CreateSession(this, amqpSessionSetting);
					this.AddSession(nullable, new ushort?(channel));
				}
				else
				{
					lock (base.ThisLock)
					{
						if (!this.sessionsByLocalHandle.TryGetObject(begin.RemoteChannel.Value, out nullable))
						{
							Error notFound = AmqpError.NotFound;
							ushort? remoteChannel = begin.RemoteChannel;
							throw new AmqpException(notFound, SRAmqp.AmqpChannelNotFound(remoteChannel.Value, this));
						}
						nullable.RemoteChannel = new ushort?(channel);
						this.sessionsByRemoteHandle.Add(channel, nullable);
					}
				}
			}
			nullable.ProcessFrame(frame);
		}

		protected override void OnSendBuffer(int totalCount)
		{
			this.lastSendTime = DateTime.UtcNow;
			base.OnSendBuffer(totalCount);
		}

		private void OnSessionClosed(object sender, EventArgs e)
		{
			AmqpSession amqpSession = (AmqpSession)sender;
			lock (base.ThisLock)
			{
				this.sessionsByLocalHandle.Remove(amqpSession.LocalChannel);
				if (amqpSession.RemoteChannel.HasValue)
				{
					this.sessionsByRemoteHandle.Remove(amqpSession.RemoteChannel.Value);
				}
			}
			MessagingClientEtwProvider.TraceClient<AmqpConnection, AmqpSession>((AmqpConnection source, AmqpSession sess) => MessagingClientEtwProvider.Provider.EventWriteAmqpRemoveSession(source, sess, sess.LocalChannel, sess.CachedRemoteChannel), this, amqpSession);
		}

		protected override bool OpenInternal()
		{
			if (this.isInitiator)
			{
				base.AsyncIO.Open();
				this.SendProtocolHeader(this.initialHeader);
				this.SendOpen();
			}
			else if (this.initialHeader == null)
			{
				base.AsyncIO.Open();
			}
			else
			{
				this.OnProtocolHeader(this.initialHeader);
				base.AsyncIO.Open();
			}
			return false;
		}

		private void ProcessFrame(Frame frame)
		{
			Performative command = frame.Command;
			if (command.DescriptorCode == Open.Code)
			{
				this.OnReceiveOpen((Open)frame.Command);
				return;
			}
			if (command.DescriptorCode != Close.Code)
			{
				this.OnReceiveSessionFrame(frame);
				return;
			}
			this.OnReceiveClose((Close)frame.Command);
		}

		private void SendClose()
		{
			base.TransitState("S:CLOSE", StateTransition.SendClose);
			Close close = new Close();
			if (base.TerminalException != null)
			{
				close.Error = AmqpError.FromException(base.TerminalException, true);
			}
			this.SendCommand(close, 0, null);
		}

		public void SendCommand(Performative command, ushort channel, ArraySegment<byte>[] payload)
		{
			if (payload == null)
			{
				base.SendBuffer(Frame.EncodeCommand(FrameType.Amqp, channel, command, 0));
				return;
			}
			ByteBuffer[] byteBuffer = new ByteBuffer[1 + (int)payload.Length];
			int count = 0;
			for (int i = 0; i < (int)payload.Length; i++)
			{
				ArraySegment<byte> nums = payload[i];
				count = count + nums.Count;
				byteBuffer[i + 1] = new ByteBuffer(nums);
			}
			byteBuffer[0] = Frame.EncodeCommand(FrameType.Amqp, channel, command, count);
			base.SendBuffers(byteBuffer);
		}

		private void SendOpen()
		{
			base.TransitState("S:OPEN", StateTransition.SendOpen);
			if (base.TerminalException != null)
			{
				base.Settings.AddProperty("com.microsoft:open-error", AmqpError.FromException(base.TerminalException, true));
			}
			this.SendCommand(base.Settings, 0, null);
		}

		private void SendProtocolHeader(ProtocolHeader header)
		{
			base.TransitState("S:HDR", StateTransition.SendHeader);
			base.SendDatablock(header);
		}

		private bool SessionFrameAllowed()
		{
			if (base.State == AmqpObjectState.OpenPipe || base.State == AmqpObjectState.OpenSent)
			{
				return true;
			}
			return base.State == AmqpObjectState.Opened;
		}
	}
}
using Microsoft.ServiceBus.Common;
using System;
using System.Net;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal class ServerSingletonConnectionReader : Microsoft.ServiceBus.Channels.SingletonConnectionReader
	{
		private Microsoft.ServiceBus.Channels.ConnectionDemuxer connectionDemuxer;

		private Microsoft.ServiceBus.Channels.ServerSingletonDecoder decoder;

		private Microsoft.ServiceBus.Channels.IConnection rawConnection;

		private string contentType;

		protected override string ContentType
		{
			get
			{
				return this.contentType;
			}
		}

		protected override long StreamPosition
		{
			get
			{
				return this.decoder.StreamPosition;
			}
		}

		public ServerSingletonConnectionReader(Microsoft.ServiceBus.Channels.ServerSingletonPreambleConnectionReader preambleReader, Microsoft.ServiceBus.Channels.IConnection upgradedConnection, Microsoft.ServiceBus.Channels.ConnectionDemuxer connectionDemuxer) : base(upgradedConnection, preambleReader.BufferOffset, preambleReader.BufferSize, preambleReader.Security, preambleReader.TransportSettings, preambleReader.Via)
		{
			this.decoder = preambleReader.Decoder;
			this.contentType = this.decoder.ContentType;
			this.connectionDemuxer = connectionDemuxer;
			this.rawConnection = preambleReader.RawConnection;
		}

		protected override bool DecodeBytes(byte[] buffer, ref int offset, ref int size, ref bool isAtEof)
		{
			while (size > 0)
			{
				int num = this.decoder.Decode(buffer, offset, size);
				if (num > 0)
				{
					offset = offset + num;
					size = size - num;
				}
				Microsoft.ServiceBus.Channels.ServerSingletonDecoder.State currentState = this.decoder.CurrentState;
				if (currentState == Microsoft.ServiceBus.Channels.ServerSingletonDecoder.State.EnvelopeStart)
				{
					return true;
				}
				if (currentState == Microsoft.ServiceBus.Channels.ServerSingletonDecoder.State.End)
				{
					isAtEof = true;
					return false;
				}
			}
			return false;
		}

		protected override void OnClose(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			base.Connection.Write(Microsoft.ServiceBus.Channels.SingletonEncoder.EndBytes, 0, (int)Microsoft.ServiceBus.Channels.SingletonEncoder.EndBytes.Length, true, timeoutHelper.RemainingTime());
			this.connectionDemuxer.ReuseConnection(this.rawConnection, timeoutHelper.RemainingTime());
		}

		protected override void PrepareMessage(Message message)
		{
			base.PrepareMessage(message);
			IPEndPoint remoteIPEndPoint = this.rawConnection.RemoteIPEndPoint;
			if (remoteIPEndPoint != null)
			{
				RemoteEndpointMessageProperty remoteEndpointMessageProperty = new RemoteEndpointMessageProperty(remoteIPEndPoint.Address.ToString(), remoteIPEndPoint.Port);
				message.Properties.Add(RemoteEndpointMessageProperty.Name, remoteEndpointMessageProperty);
			}
		}
	}
}
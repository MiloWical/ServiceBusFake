using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class TransportOutputChannel : Microsoft.ServiceBus.Channels.OutputChannel
	{
		private bool anyHeadersToAdd;

		private bool manualAddressing;

		private System.ServiceModel.Channels.MessageVersion messageVersion;

		private EndpointAddress to;

		private Uri via;

		private Uri toUri;

		protected bool ManualAddressing
		{
			get
			{
				return this.manualAddressing;
			}
		}

		public System.ServiceModel.Channels.MessageVersion MessageVersion
		{
			get
			{
				return this.messageVersion;
			}
		}

		public override EndpointAddress RemoteAddress
		{
			get
			{
				return this.to;
			}
		}

		public override Uri Via
		{
			get
			{
				return this.via;
			}
		}

		protected TransportOutputChannel(ChannelManagerBase channelManager, EndpointAddress to, Uri via, bool manualAddressing, System.ServiceModel.Channels.MessageVersion messageVersion) : base(channelManager)
		{
			this.manualAddressing = manualAddressing;
			this.messageVersion = messageVersion;
			this.to = to;
			this.via = via;
			if (!manualAddressing && to != null)
			{
				if (to.IsAnonymous)
				{
					this.toUri = (Uri)InvokeHelper.InvokeInstanceGet(typeof(AddressingVersion), this.messageVersion.Addressing, "AnonymousUri");
				}
				else if (!to.IsNone)
				{
					this.toUri = to.Uri;
				}
				else
				{
					this.toUri = (Uri)InvokeHelper.InvokeInstanceGet(typeof(AddressingVersion), this.messageVersion.Addressing, "NoneUri");
				}
				this.anyHeadersToAdd = to.Headers.Count > 0;
			}
		}

		protected override void AddHeadersTo(Message message)
		{
			base.AddHeadersTo(message);
			if (this.toUri != null)
			{
				message.Headers.To = this.toUri;
				if (this.anyHeadersToAdd)
				{
					this.to.Headers.AddHeadersTo(message);
				}
			}
		}
	}
}
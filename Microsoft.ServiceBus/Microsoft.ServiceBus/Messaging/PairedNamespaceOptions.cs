using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using System;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class PairedNamespaceOptions
	{
		private readonly NamespaceManager secondaryNamespaceManager;

		private readonly MessagingFactory secondaryMessagingFactory;

		private readonly TimeSpan failoverInterval;

		private MessagingFactory primaryMessagingFactory;

		private readonly object syncLock = new object();

		public TimeSpan FailoverInterval
		{
			get
			{
				return this.failoverInterval;
			}
		}

		internal MessagingFactory PrimaryMessagingFactory
		{
			get
			{
				return this.primaryMessagingFactory;
			}
			set
			{
				if (this.primaryMessagingFactory == null)
				{
					lock (this.syncLock)
					{
						if (this.primaryMessagingFactory != null)
						{
							throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SRClient.PairedNamespaceMessagingFactoyCannotBeChanged));
						}
						else
						{
							this.primaryMessagingFactory = value;
						}
					}
					return;
				}
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SRClient.PairedNamespaceMessagingFactoyCannotBeChanged));
			}
		}

		public MessagingFactory SecondaryMessagingFactory
		{
			get
			{
				return this.secondaryMessagingFactory;
			}
		}

		public NamespaceManager SecondaryNamespaceManager
		{
			get
			{
				return this.secondaryNamespaceManager;
			}
		}

		protected PairedNamespaceOptions(NamespaceManager secondaryNamespaceManager, MessagingFactory secondaryMessagingFactory) : this(secondaryNamespaceManager, secondaryMessagingFactory, Constants.DefaultPrimaryFailoverInterval)
		{
		}

		protected PairedNamespaceOptions(NamespaceManager secondaryNamespaceManager, MessagingFactory secondaryMessagingFactory, TimeSpan failoverInterval)
		{
			if (secondaryNamespaceManager == null)
			{
				Fx.Exception.ArgumentNull("secondaryNamespaceManager");
			}
			if (secondaryMessagingFactory == null)
			{
				Fx.Exception.ArgumentNull("secondaryMessagingFactory");
			}
			if (failoverInterval < Constants.MinPrimaryFailoverInterval || failoverInterval > Constants.MaxPrimaryFailoverInterval)
			{
				Fx.Exception.ArgumentOutOfRange("failoverInterval", failoverInterval, SRClient.ArgumentOutOfRange(Constants.MinPrimaryFailoverInterval, Constants.MaxPrimaryFailoverInterval));
			}
			this.secondaryNamespaceManager = secondaryNamespaceManager;
			this.secondaryMessagingFactory = secondaryMessagingFactory;
			this.failoverInterval = failoverInterval;
		}

		protected internal virtual void ClearPairing()
		{
			this.primaryMessagingFactory = null;
		}

		internal abstract IPairedNamespaceFactory CreatePairedNamespaceFactory();

		internal void NotifyPrimarySendResult(string path, bool success)
		{
			this.OnNotifyPrimarySendResult(path, success);
		}

		protected abstract void OnNotifyPrimarySendResult(string path, bool success);
	}
}
using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Principal;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal abstract class TransportBase : AmqpObject
	{
		public bool IsAuthenticated
		{
			get
			{
				if (this.Principal == null)
				{
					return false;
				}
				return this.Principal.Identity.IsAuthenticated;
			}
		}

		public virtual bool IsSecure
		{
			get
			{
				return false;
			}
		}

		public abstract EndPoint LocalEndPoint
		{
			get;
		}

		public IPrincipal Principal
		{
			get;
			protected set;
		}

		public abstract EndPoint RemoteEndPoint
		{
			get;
		}

		public virtual bool RequiresCompleteFrames
		{
			get
			{
				return false;
			}
		}

		protected TransportBase(string type) : base(type)
		{
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.CloseInternal();
			base.State = AmqpObjectState.End;
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			base.State = AmqpObjectState.Opened;
		}

		protected override bool OpenInternal()
		{
			return true;
		}

		public abstract bool ReadAsync(TransportAsyncCallbackArgs args);

		public abstract bool WriteAsync(TransportAsyncCallbackArgs args);
	}
}
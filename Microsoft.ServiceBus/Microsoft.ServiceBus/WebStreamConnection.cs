using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.IO;

namespace Microsoft.ServiceBus
{
	internal class WebStreamConnection : BaseStreamConnection
	{
		private readonly Uri factoryEndpointUri;

		private readonly Uri sbUri;

		public WebStreamConnection(Uri factoryEndpointUri, int asyncReadBufferSize, EventTraceActivity activity, WebStream webStream, Uri sbUri) : base(webStream, asyncReadBufferSize, activity)
		{
			this.factoryEndpointUri = factoryEndpointUri;
			this.sbUri = sbUri;
		}

		public override void Abort()
		{
			MessagingClientEtwProvider.Provider.WebStreamConnectionAbort(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri);
			base.Abort();
		}

		public override void Close(TimeSpan timeout)
		{
			MessagingClientEtwProvider.Provider.WebStreamConnectionClose(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri);
			base.Close(timeout);
		}

		public override void Shutdown(TimeSpan timeout)
		{
			MessagingClientEtwProvider.Provider.WebStreamConnectionShutdown(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri);
			lock (base.ThisLock)
			{
				if (!base.IsShutdown)
				{
					base.IsShutdown = true;
				}
				else
				{
					return;
				}
			}
			try
			{
				((WebStream)base.Stream).Shutdown();
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				throw Fx.Exception.AsWarning(base.ConvertObjectDisposedException(objectDisposedException), null);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsWarning(BaseStreamConnection.ConvertIOException(oException), null);
			}
		}
	}
}
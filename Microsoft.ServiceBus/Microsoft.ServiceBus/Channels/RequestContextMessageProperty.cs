using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal class RequestContextMessageProperty : IDisposable
	{
		private RequestContext context;

		public static string Name
		{
			get
			{
				return "requestContext";
			}
		}

		public RequestContextMessageProperty(RequestContext context)
		{
			this.context = context;
		}

		void System.IDisposable.Dispose()
		{
			bool flag = false;
			try
			{
				try
				{
					this.context.Close();
					flag = true;
				}
				catch (CommunicationException communicationException1)
				{
					CommunicationException communicationException = communicationException1;
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(communicationException, TraceEventType.Information);
					}
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(timeoutException, TraceEventType.Information);
					}
				}
			}
			finally
			{
				if (!flag)
				{
					this.context.Abort();
				}
				this.context = null;
			}
			GC.SuppressFinalize(this);
		}
	}
}
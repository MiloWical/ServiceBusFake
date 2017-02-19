using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal static class CommunicationObjectExtension
	{
		internal static Exception CreateFaultedException(this CommunicationObject communicationObject)
		{
			return new CommunicationObjectFaultedException(SRClient.CommunicationObjectFaulted(communicationObject.GetType().ToString()));
		}

		public static bool DoneReceivingInCurrentState(this ICommunicationObjectInternals communicationObject)
		{
			switch (communicationObject.State)
			{
				case CommunicationState.Created:
				case CommunicationState.Opening:
				{
					communicationObject.ThrowIfDisposedOrNotOpen();
					return false;
				}
				case CommunicationState.Opened:
				{
					return false;
				}
				case CommunicationState.Closing:
				case CommunicationState.Closed:
				case CommunicationState.Faulted:
				{
					return true;
				}
				default:
				{
					communicationObject.ThrowIfDisposedOrNotOpen();
					return false;
				}
			}
		}

		internal static void Fault(this ICommunicationObject communicationObject, Exception exception)
		{
			Type type = communicationObject.GetType();
			object[] objArray = new object[] { exception };
			InvokeHelper.InvokeInstanceMethod(type, communicationObject, "Fault", objArray);
		}

		internal static Exception GetPendingException(this ICommunicationObjectInternals communicationObject)
		{
			Exception exception;
			try
			{
				communicationObject.ThrowPending();
				exception = null;
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				exception = exception1;
			}
			return exception;
		}

		internal static void ThrowIfFaulted(this CommunicationObject communicationObject)
		{
			switch (communicationObject.State)
			{
				case CommunicationState.Created:
				case CommunicationState.Opening:
				case CommunicationState.Opened:
				case CommunicationState.Closing:
				case CommunicationState.Closed:
				{
					return;
				}
				default:
				{
					throw Fx.Exception.AsWarning(communicationObject.CreateFaultedException(), null);
				}
			}
		}

		internal static void ThrowIfNotOpened(this ICommunicationObjectInternals communicationObject)
		{
			CommunicationState state = communicationObject.State;
			if (state == CommunicationState.Created || state == CommunicationState.Opening)
			{
				communicationObject.ThrowIfDisposedOrNotOpen();
			}
		}

		internal static void ThrowPending(this ICommunicationObjectInternals communicationObject)
		{
			switch (communicationObject.State)
			{
				case CommunicationState.Created:
				case CommunicationState.Opening:
				case CommunicationState.Opened:
				{
					communicationObject.ThrowIfDisposed();
					return;
				}
				case CommunicationState.Closing:
				case CommunicationState.Closed:
				case CommunicationState.Faulted:
				{
					return;
				}
			}
			throw Fx.AssertAndThrow("ThrowIfDisposed: Unknown CommunicationObject.state");
		}
	}
}
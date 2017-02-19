using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Properties;
using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal static class ConnectionUtilities
	{
		internal static void CloseNoThrow(IConnection connection, TimeSpan timeout)
		{
			bool flag = false;
			try
			{
				try
				{
					connection.Close(timeout);
					flag = true;
				}
				catch (TimeoutException timeoutException)
				{
					Fx.Exception.TraceHandled(timeoutException, "ConnectionUtiltities.CloseNoThrow", connection.Activity);
				}
				catch (CommunicationException communicationException)
				{
					Fx.Exception.TraceHandled(communicationException, "ConnectionUtiltities.CloseNoThrow", connection.Activity);
				}
			}
			finally
			{
				if (!flag)
				{
					connection.Abort();
				}
			}
		}

		internal static void ValidateBufferBounds(byte[] buffer, int offset, int size)
		{
			if (buffer == null)
			{
				throw Fx.Exception.ArgumentNull("buffer");
			}
			ConnectionUtilities.ValidateBufferBounds((int)buffer.Length, offset, size);
		}

		internal static void ValidateBufferBounds(int bufferSize, int offset, int size)
		{
			if (offset < 0)
			{
				throw Fx.Exception.AsError(new ArgumentOutOfRangeException("offset", (object)offset, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBeNonNegative, new object[0])), null);
			}
			if (offset > bufferSize)
			{
				ExceptionTrace exception = Fx.Exception;
				object obj = offset;
				string offsetExceedsBufferSize = Resources.OffsetExceedsBufferSize;
				object[] objArray = new object[] { bufferSize };
				throw exception.AsError(new ArgumentOutOfRangeException("offset", obj, Microsoft.ServiceBus.SR.GetString(offsetExceedsBufferSize, objArray)), null);
			}
			if (size <= 0)
			{
				throw Fx.Exception.AsError(new ArgumentOutOfRangeException("size", (object)size, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])), null);
			}
			int num = bufferSize - offset;
			if (size > num)
			{
				ExceptionTrace exceptionTrace = Fx.Exception;
				object obj1 = size;
				string sizeExceedsRemainingBufferSpace = Resources.SizeExceedsRemainingBufferSpace;
				object[] objArray1 = new object[] { num };
				throw exceptionTrace.AsError(new ArgumentOutOfRangeException("size", obj1, Microsoft.ServiceBus.SR.GetString(sizeExceedsRemainingBufferSpace, objArray1)), null);
			}
		}
	}
}
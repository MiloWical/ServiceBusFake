using Microsoft.ServiceBus.Tracing;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Common
{
	internal sealed class ExceptionInfo
	{
		private readonly Exception originalException;

		private byte[] serializedException;

		public ExceptionInfo(Exception exception)
		{
			this.originalException = exception;
			if (exception != null)
			{
				try
				{
					IFormatter netDataContractSerializer = new NetDataContractSerializer();
					using (MemoryStream memoryStream = new MemoryStream())
					{
						netDataContractSerializer.Serialize(memoryStream, exception);
						this.serializedException = memoryStream.ToArray();
					}
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception1, "ExceptionInfo..ctor", null);
				}
				if (this.serializedException == null)
				{
					MessagingClientEtwProvider.Provider.EventWriteNonSerializableException(exception.ToStringSlim());
				}
			}
		}

		public Exception CreateException()
		{
			Exception exception;
			if (this.serializedException != null)
			{
				IFormatter netDataContractSerializer = new NetDataContractSerializer();
				using (MemoryStream memoryStream = new MemoryStream(this.serializedException))
				{
					try
					{
						exception = (Exception)netDataContractSerializer.Deserialize(memoryStream);
						return exception;
					}
					catch (SerializationException serializationException)
					{
						MessagingClientEtwProvider.Provider.EventWriteNonSerializableException(this.originalException.ToStringSlim());
					}
					return this.originalException;
				}
				return exception;
			}
			return this.originalException;
		}
	}
}
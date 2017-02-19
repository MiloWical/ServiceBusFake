using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	[Serializable]
	internal sealed class AmqpException : Exception
	{
		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Error Error
		{
			get;
			private set;
		}

		public AmqpException(Microsoft.ServiceBus.Messaging.Amqp.Framing.Error error) : this(error, error.Description, null)
		{
		}

		public AmqpException(Microsoft.ServiceBus.Messaging.Amqp.Framing.Error error, string message) : this(error, message, null)
		{
		}

		public AmqpException(Microsoft.ServiceBus.Messaging.Amqp.Framing.Error error, Exception innerException) : this(error, error.Description ?? innerException.Message, innerException)
		{
		}

		private AmqpException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			this.Error = (Microsoft.ServiceBus.Messaging.Amqp.Framing.Error)info.GetValue("Error", typeof(Microsoft.ServiceBus.Messaging.Amqp.Framing.Error));
		}

		private AmqpException(Microsoft.ServiceBus.Messaging.Amqp.Framing.Error error, string message, Exception innerException) : base(message ?? SRAmqp.AmqpErrorOccurred(error.Condition), innerException)
		{
			this.Error = error;
			if (!string.IsNullOrEmpty(message))
			{
				this.Error.Description = message;
			}
		}

		public static AmqpException FromError(Microsoft.ServiceBus.Messaging.Amqp.Framing.Error error)
		{
			if (error == null || error.Condition.Value == null)
			{
				return null;
			}
			if (error.Description != null)
			{
				return new AmqpException(error, error.Description);
			}
			return new AmqpException(AmqpError.GetError(error.Condition));
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Error", this.Error);
		}
	}
}
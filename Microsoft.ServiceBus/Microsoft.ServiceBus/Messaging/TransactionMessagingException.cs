using System;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	internal sealed class TransactionMessagingException : MessagingException
	{
		public TransactionMessagingException(string message, Exception innerException) : base(message, innerException)
		{
			this.Initialize();
		}

		public TransactionMessagingException(string message) : base(message)
		{
			this.Initialize();
		}

		private void Initialize()
		{
			base.IsTransient = false;
		}
	}
}
using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Common
{
	internal sealed class TransactionResult : IExtensibleObject<TransactionResult>
	{
		public Exception CompletionException
		{
			get;
			set;
		}

		public IExtensionCollection<TransactionResult> Extensions
		{
			get
			{
				return JustDecompileGenerated_get_Extensions();
			}
			set
			{
				JustDecompileGenerated_set_Extensions(value);
			}
		}

		private IExtensionCollection<TransactionResult> JustDecompileGenerated_Extensions_k__BackingField;

		public IExtensionCollection<TransactionResult> JustDecompileGenerated_get_Extensions()
		{
			return this.JustDecompileGenerated_Extensions_k__BackingField;
		}

		private void JustDecompileGenerated_set_Extensions(IExtensionCollection<TransactionResult> value)
		{
			this.JustDecompileGenerated_Extensions_k__BackingField = value;
		}

		public int ReferenceCount
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Tracing.TrackingContext TrackingContext
		{
			get;
			set;
		}

		public TransactionResult()
		{
			this.Extensions = new ExtensionCollection<TransactionResult>(this);
		}
	}
}
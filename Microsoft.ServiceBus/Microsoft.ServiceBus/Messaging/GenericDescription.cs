using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class GenericDescription : EntityDescription, IResourceDescription
	{
		private string name;

		string Microsoft.ServiceBus.Messaging.IResourceDescription.CollectionName
		{
			get
			{
				return this.name;
			}
		}

		internal GenericDescription(string name)
		{
			this.name = name;
		}
	}
}
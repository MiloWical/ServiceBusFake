using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class ApplicationProperties : DescribedMap
	{
		public readonly static string Name;

		public readonly static ulong Code;

		private PropertiesMap propMap;

		public PropertiesMap Map
		{
			get
			{
				if (this.propMap == null)
				{
					this.propMap = new PropertiesMap();
					this.propMap.SetMap(base.InnerMap);
				}
				return this.propMap;
			}
		}

		static ApplicationProperties()
		{
			ApplicationProperties.Name = "amqp:application-properties:map";
			ApplicationProperties.Code = (ulong)116;
		}

		public ApplicationProperties() : base(ApplicationProperties.Name, ApplicationProperties.Code)
		{
		}
	}
}
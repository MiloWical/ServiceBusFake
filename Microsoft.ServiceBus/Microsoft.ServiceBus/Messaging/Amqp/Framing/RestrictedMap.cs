using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class RestrictedMap : IEnumerable<KeyValuePair<MapKey, object>>, IEnumerable
	{
		private AmqpMap innerMap;

		protected AmqpMap InnerMap
		{
			get
			{
				if (this.innerMap == null)
				{
					this.innerMap = new AmqpMap();
				}
				return this.innerMap;
			}
		}

		protected RestrictedMap()
		{
		}

		public void SetMap(AmqpMap map)
		{
			this.innerMap = map;
		}

		IEnumerator<KeyValuePair<MapKey, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<Microsoft.ServiceBus.Messaging.Amqp.Encoding.MapKey,System.Object>>.GetEnumerator()
		{
			return this.InnerMap.GetEnumerator();
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.InnerMap.GetEnumerator();
		}

		public override string ToString()
		{
			return this.InnerMap.ToString();
		}
	}
}
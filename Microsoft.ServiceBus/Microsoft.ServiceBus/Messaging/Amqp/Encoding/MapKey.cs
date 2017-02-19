using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal struct MapKey : IEquatable<MapKey>
	{
		private object key;

		public object Key
		{
			get
			{
				return this.key;
			}
		}

		public MapKey(object key)
		{
			this.key = key;
		}

		public bool Equals(MapKey other)
		{
			if (this.key == null && other.key == null)
			{
				return true;
			}
			if (this.key == null || other.key == null)
			{
				return false;
			}
			return this.key.Equals(other.key);
		}

		public override int GetHashCode()
		{
			if (this.key == null)
			{
				return 0;
			}
			return this.key.GetHashCode();
		}

		public override string ToString()
		{
			if (this.key == null)
			{
				return "<null>";
			}
			return this.key.ToString();
		}
	}
}
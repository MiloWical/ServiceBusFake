using System;
using System.Globalization;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal struct AmqpVersion : IEquatable<AmqpVersion>
	{
		private byte major;

		private byte minor;

		private byte revision;

		public byte Major
		{
			get
			{
				return this.major;
			}
		}

		public byte Minor
		{
			get
			{
				return this.minor;
			}
		}

		public byte Revision
		{
			get
			{
				return this.revision;
			}
		}

		public AmqpVersion(byte major, byte minor, byte revision)
		{
			this.major = major;
			this.minor = minor;
			this.revision = revision;
		}

		public AmqpVersion(Version version) : this((byte)version.Major, (byte)version.Minor, (byte)version.Revision)
		{
		}

		public bool Equals(AmqpVersion other)
		{
			if (this.Major != other.Major)
			{
				return false;
			}
			return this.Minor == other.Minor;
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] major = new object[] { this.Major, this.Minor, this.Revision };
			return string.Format(invariantCulture, "{0}.{1}.{2}", major);
		}
	}
}
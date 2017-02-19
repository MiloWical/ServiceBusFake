using System;
using System.IO;

namespace Microsoft.ServiceBus.Web
{
	internal class StreamMessageProperty
	{
		public const string Name = "StreamMessageProperty";

		private System.IO.Stream stream;

		public System.IO.Stream Stream
		{
			get
			{
				return this.stream;
			}
		}

		public StreamMessageProperty(System.IO.Stream stream)
		{
			this.stream = stream;
		}
	}
}
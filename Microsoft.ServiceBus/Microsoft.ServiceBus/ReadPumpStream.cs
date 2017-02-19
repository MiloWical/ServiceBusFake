using Microsoft.ServiceBus.Common;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.ServiceBus
{
	internal class ReadPumpStream : PumpStream
	{
		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		public override int ReadTimeout
		{
			get
			{
				return this.outputStream.ReadTimeout;
			}
			set
			{
				this.outputStream.ReadTimeout = value;
			}
		}

		public ReadPumpStream(Stream input, Stream output, Pump pump) : base(input, output, pump)
		{
		}

		public override void Flush()
		{
			throw Fx.Exception.AsError(new NotSupportedException(string.Concat(base.GetType().Name, ".Flush")), null);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return this.outputStream.Read(buffer, offset, count);
		}

		public override void Shutdown()
		{
			this.pump.WaitForCompletion();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw Fx.Exception.AsError(new NotSupportedException(string.Concat(base.GetType().Name, ".Write")), null);
		}
	}
}
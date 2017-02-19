using Microsoft.ServiceBus.Common;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.ServiceBus
{
	internal sealed class WritePumpStream : PumpStream
	{
		public override bool CanRead
		{
			get
			{
				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public override int WriteTimeout
		{
			get
			{
				return this.inputStream.WriteTimeout;
			}
			set
			{
				this.inputStream.WriteTimeout = value;
			}
		}

		public WritePumpStream(Stream input, Stream output, Pump pump) : base(input, output, pump)
		{
		}

		public override void Flush()
		{
			this.outputStream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw Fx.Exception.AsError(new NotSupportedException(string.Concat(base.GetType().Name, ".Read")), null);
		}

		public override void Shutdown()
		{
			((PipeStream)this.inputStream).SetEndOfStream();
			base.WaitForPumpToEnd();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			this.inputStream.Write(buffer, offset, count);
		}
	}
}
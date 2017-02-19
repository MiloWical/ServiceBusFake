using System;
using System.CodeDom.Compiler;

namespace Microsoft.ServiceBus.Messaging.Filters.FilterLanguage
{
	[GeneratedCode("Lexer", "1.0.0.0")]
	internal abstract class ScanBuff
	{
		public const int EOF = -1;

		public abstract int Pos
		{
			get;
			set;
		}

		public abstract int ReadPos
		{
			get;
		}

		protected ScanBuff()
		{
		}

		public abstract string GetString(int b, int e);

		public abstract int Peek();

		public abstract int Read();
	}
}
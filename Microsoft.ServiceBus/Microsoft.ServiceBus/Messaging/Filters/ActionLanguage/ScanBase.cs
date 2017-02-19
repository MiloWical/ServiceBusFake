using Babel.ParserGenerator;
using System;

namespace Microsoft.ServiceBus.Messaging.Filters.ActionLanguage
{
	internal abstract class ScanBase : AScanner<LexValue, LexLocation>
	{
		protected abstract int CurrentSc
		{
			get;
			set;
		}

		public virtual int EolState
		{
			get
			{
				return this.CurrentSc;
			}
			set
			{
				this.CurrentSc = value;
			}
		}

		protected ScanBase()
		{
		}
	}
}
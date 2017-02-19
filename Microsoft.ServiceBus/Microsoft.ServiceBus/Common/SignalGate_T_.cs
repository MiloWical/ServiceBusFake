using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Common
{
	[Serializable]
	internal class SignalGate<T> : SignalGate
	{
		private T Result
		{
			get;
			set;
		}

		public SignalGate()
		{
		}

		public bool Signal(T result)
		{
			this.Result = result;
			return base.Signal();
		}

		public bool Unlock(out T result)
		{
			if (!base.Unlock())
			{
				result = default(T);
				return false;
			}
			result = this.Result;
			return true;
		}
	}
}
using System;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IRequestBase
	{
		void Abort(RequestChannel requestChannel);

		void Fault(RequestChannel requestChannel);
	}
}
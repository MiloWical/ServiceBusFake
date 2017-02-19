using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IRequestReplyCorrelator
	{
		void Add<T>(Message request, T state);

		T Find<T>(Message reply, bool remove);

		void Remove<T>(Message request);
	}
}
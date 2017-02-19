using System;

namespace Microsoft.ServiceBus.Channels
{
	internal interface ISessionPreambleHandler
	{
		void HandleServerSessionPreamble(ServerSessionPreambleConnectionReader serverSessionPreambleReader, ConnectionDemuxer connectionDemuxer);
	}
}
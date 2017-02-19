using System;

namespace Microsoft.ServiceBus.Channels
{
	internal delegate void ServerSessionPreambleDemuxCallback(ServerSessionPreambleConnectionReader serverSessionPreambleReader, ConnectionDemuxer connectionDemuxer);
}
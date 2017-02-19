using System;

namespace Microsoft.ServiceBus.Tracing
{
	public enum ControllerCommand
	{
		Disable = -3,
		Enable = -2,
		SendManifest = -1,
		Update = 0
	}
}
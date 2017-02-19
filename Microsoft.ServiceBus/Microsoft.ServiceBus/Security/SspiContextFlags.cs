using System;

namespace Microsoft.ServiceBus.Security
{
	[Flags]
	internal enum SspiContextFlags
	{
		Zero = 0,
		Delegate = 1,
		MutualAuth = 2,
		ReplayDetect = 4,
		SequenceDetect = 8,
		Confidentiality = 16,
		UseSessionKey = 32,
		AllocateMemory = 256,
		InitExtendedError = 16384,
		AcceptExtendedError = 32768,
		InitStream = 32768,
		AcceptStream = 65536,
		InitIdentify = 131072,
		InitAnonymous = 262144,
		AcceptIdentify = 524288,
		InitManualCredValidation = 524288,
		AcceptAnonymous = 1048576,
		ChannelBindingProxyBindings = 67108864,
		ChannelBindingAllowMissingBindings = 268435456
	}
}
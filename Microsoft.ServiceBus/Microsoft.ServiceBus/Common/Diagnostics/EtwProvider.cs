using System;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.ServiceBus.Common.Diagnostics
{
	internal sealed class EtwProvider : DiagnosticsEventProvider
	{
		private Action invokeControllerCallback;

		internal Action ControllerCallBack
		{
			get
			{
				return this.invokeControllerCallback;
			}
			set
			{
				this.invokeControllerCallback = value;
			}
		}

		[PermissionSet(SecurityAction.Assert, Unrestricted=true)]
		[SecurityCritical]
		internal EtwProvider(Guid id) : base(id)
		{
		}

		protected override void OnControllerCommand()
		{
			if (this.invokeControllerCallback != null)
			{
				this.invokeControllerCallback();
			}
		}
	}
}
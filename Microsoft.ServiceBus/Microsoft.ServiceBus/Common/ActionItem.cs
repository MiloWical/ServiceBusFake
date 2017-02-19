using System;
using System.Security;
using System.Threading;

namespace Microsoft.ServiceBus.Common
{
	internal abstract class ActionItem
	{
		[SecurityCritical]
		private SecurityContext context;

		private bool isScheduled;

		private bool lowPriority;

		public bool LowPriority
		{
			get
			{
				return this.lowPriority;
			}
			protected set
			{
				this.lowPriority = value;
			}
		}

		protected ActionItem()
		{
		}

		[SecurityCritical]
		private SecurityContext ExtractContext()
		{
			SecurityContext securityContext = this.context;
			this.context = null;
			return securityContext;
		}

		[SecurityCritical]
		protected abstract void Invoke();

		public static void Schedule(Action<object> callback, object state)
		{
			ActionItem.Schedule(callback, state, false);
		}

		public static void Schedule(Action<object> callback, object state, bool lowPriority)
		{
			if (!PartialTrustHelpers.ShouldFlowSecurityContext && !WaitCallbackActionItem.ShouldUseActivity)
			{
				ActionItem.ScheduleCallback(callback, state, lowPriority);
				return;
			}
			(new ActionItem.DefaultActionItem(callback, state, lowPriority)).Schedule();
		}

		[SecurityCritical]
		protected void Schedule()
		{
			if (this.isScheduled)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRCore.ActionItemIsAlreadyScheduled), null);
			}
			this.isScheduled = true;
			if (PartialTrustHelpers.ShouldFlowSecurityContext)
			{
				this.context = PartialTrustHelpers.CaptureSecurityContextNoIdentityFlow();
			}
			if (this.context != null)
			{
				this.ScheduleCallback(ActionItem.CallbackHelper.InvokeWithContextCallback);
				return;
			}
			this.ScheduleCallback(ActionItem.CallbackHelper.InvokeWithoutContextCallback);
		}

		[SecurityCritical]
		private static void ScheduleCallback(Action<object> callback, object state, bool lowPriority)
		{
			if (lowPriority)
			{
				IOThreadScheduler.ScheduleCallbackLowPriNoFlow(callback, state);
				return;
			}
			IOThreadScheduler.ScheduleCallbackNoFlow(callback, state);
		}

		[SecurityCritical]
		private void ScheduleCallback(Action<object> callback)
		{
			ActionItem.ScheduleCallback(callback, this, this.lowPriority);
		}

		[SecurityCritical]
		protected void ScheduleWithContext(SecurityContext contextToSchedule)
		{
			if (contextToSchedule == null)
			{
				throw Fx.Exception.ArgumentNull("context");
			}
			if (this.isScheduled)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRCore.ActionItemIsAlreadyScheduled), null);
			}
			this.isScheduled = true;
			this.context = contextToSchedule.CreateCopy();
			this.ScheduleCallback(ActionItem.CallbackHelper.InvokeWithContextCallback);
		}

		[SecurityCritical]
		protected void ScheduleWithoutContext()
		{
			if (this.isScheduled)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRCore.ActionItemIsAlreadyScheduled), null);
			}
			this.isScheduled = true;
			this.ScheduleCallback(ActionItem.CallbackHelper.InvokeWithoutContextCallback);
		}

		[SecurityCritical]
		private static class CallbackHelper
		{
			private static Action<object> invokeWithContextCallback;

			private static Action<object> invokeWithoutContextCallback;

			private static ContextCallback onContextAppliedCallback;

			public static Action<object> InvokeWithContextCallback
			{
				get
				{
					if (ActionItem.CallbackHelper.invokeWithContextCallback == null)
					{
						ActionItem.CallbackHelper.invokeWithContextCallback = new Action<object>(ActionItem.CallbackHelper.InvokeWithContext);
					}
					return ActionItem.CallbackHelper.invokeWithContextCallback;
				}
			}

			public static Action<object> InvokeWithoutContextCallback
			{
				get
				{
					if (ActionItem.CallbackHelper.invokeWithoutContextCallback == null)
					{
						ActionItem.CallbackHelper.invokeWithoutContextCallback = new Action<object>(ActionItem.CallbackHelper.InvokeWithoutContext);
					}
					return ActionItem.CallbackHelper.invokeWithoutContextCallback;
				}
			}

			public static ContextCallback OnContextAppliedCallback
			{
				get
				{
					if (ActionItem.CallbackHelper.onContextAppliedCallback == null)
					{
						ActionItem.CallbackHelper.onContextAppliedCallback = new ContextCallback(ActionItem.CallbackHelper.OnContextApplied);
					}
					return ActionItem.CallbackHelper.onContextAppliedCallback;
				}
			}

			private static void InvokeWithContext(object state)
			{
				SecurityContext securityContext = ((ActionItem)state).ExtractContext();
				SecurityContext.Run(securityContext, ActionItem.CallbackHelper.OnContextAppliedCallback, state);
			}

			private static void InvokeWithoutContext(object state)
			{
				ActionItem actionItem = (ActionItem)state;
				actionItem.Invoke();
				actionItem.isScheduled = false;
			}

			private static void OnContextApplied(object o)
			{
				ActionItem actionItem = (ActionItem)o;
				actionItem.Invoke();
				actionItem.isScheduled = false;
			}
		}

		private class DefaultActionItem : ActionItem
		{
			[SecurityCritical]
			private Action<object> callback;

			[SecurityCritical]
			private object state;

			public DefaultActionItem(Action<object> callback, object state, bool isLowPriority)
			{
				base.LowPriority = isLowPriority;
				this.callback = callback;
				this.state = state;
			}

			[SecurityCritical]
			protected override void Invoke()
			{
				this.callback(this.state);
			}
		}
	}
}
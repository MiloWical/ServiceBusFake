using System;
using System.Diagnostics;

namespace Microsoft.ServiceBus.Tracing
{
	[EventSource(Guid="A307C7A2-A4CD-4D22-8093-94DB72934152", LocalizationResources="Microsoft.ServiceBus.Tracing.EventDefinitionResources", Name="Microsoft-ServiceBus-Client")]
	internal sealed class MessagingClientEventSource : EventSource
	{
		public MessagingClientEventSource(bool disableTracing) : base(disableTracing)
		{
		}

		[Conditional("CLIENT")]
		[Event(30500, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void AmqpInputSessionChannelMessageReceived(EventTraceActivity eventTraceActivity, string uri)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30500, eventTraceActivity, new object[] { uri });
			}
		}

		[Event(31117, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void DetectConnectivityModeFailed(string endPoint, string triedMode)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(31117, endPoint, triedMode);
			}
		}

		[Conditional("CLIENT")]
		[Event(31118, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void DetectConnectivityModeSucceeded(string endPoint, string triedMode)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(31118, endPoint, triedMode);
			}
		}

		[Conditional("CLIENT")]
		[Event(30300, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAcceptSessionRequestBegin(EventTraceActivity activity, string trackingId, string subsystemId, string entityName, string sessionId)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, entityName, sessionId };
				base.WriteEvent(30300, activity, objArray);
			}
		}

		[Conditional("CLIENT")]
		[Event(30301, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAcceptSessionRequestEnd(EventTraceActivity activity, string trackingId, string subsystemId, string entityName, string sessionId)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, entityName, sessionId };
				base.WriteEvent(30301, activity, objArray);
			}
		}

		[Event(30302, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAcceptSessionRequestFailed(EventTraceActivity activity, string trackingId, string subsystemId, string entityName, string sessionId, string errorMessage)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, entityName, sessionId, errorMessage };
				base.WriteSBTraceEvent(30302, activity, objArray);
			}
		}

		[Conditional("CLIENT")]
		[Event(30303, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAcceptSessionRequestTransfer(EventTraceActivity activity, EventTraceActivity relatedActivity)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteTransferEvent(30303, activity, relatedActivity, new object[0]);
			}
		}

		[Event(30403, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpAddSession(object source, object session, ushort localChannel, ushort remoteChannel)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), session.ToString(), localChannel, remoteChannel };
				base.WriteEvent(30403, str);
			}
		}

		[Event(30408, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpAttachLink(object source, object link, string linkName, string role)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), link.ToString(), linkName, role };
				base.WriteEvent(30408, str);
			}
		}

		[Event(30406, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpDeliveryNotFound(object source, string deliveryTag)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30406, source.ToString(), deliveryTag);
			}
		}

		[Conditional("CLIENT")]
		[Event(30405, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpDispose(object source, uint deliveryId, bool settled, object state)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), deliveryId, settled, state.ToString() };
				base.WriteEvent(30405, str);
			}
		}

		[Event(30418, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpDynamicBufferSizeChange(object source, string type, int oldSize, int newSize)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { source, type, oldSize, newSize };
				base.WriteEvent(30418, objArray);
			}
		}

		[Event(30413, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpInsecureTransport(object source, object transport, bool isSecure, bool isAuthenticated)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), transport.ToString(), isSecure, isAuthenticated };
				base.WriteEvent(30413, str);
			}
		}

		[Event(30416, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpListenSocketAcceptError(object source, bool willRetry, string error)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), willRetry, error };
				base.WriteEvent(30416, str);
			}
		}

		[Event(30402, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpLogError(object source, string operation, string message)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30402, source.ToString(), operation, message);
			}
		}

		[Event(30400, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpLogOperation(object source, TraceOperation operation, object detail)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), operation, detail.ToString() };
				base.WriteEvent(30400, str);
			}
		}

		[Conditional("CLIENT")]
		[Event(30401, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpLogOperationVerbose(object source, TraceOperation operation, object detail)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), operation, detail.ToString() };
				base.WriteEvent(30401, str);
			}
		}

		[Conditional("CLIENT")]
		[Event(30417, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpManageLink(string action, object link, string info)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30417, action, link.ToString(), info);
			}
		}

		[Event(30414, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpOpenEntityFailed(object source, object obj, string name, string entityName, string error)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), obj.ToString(), name, entityName, error };
				base.WriteEvent(30414, str);
			}
		}

		[Event(30415, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpOpenEntitySucceeded(object source, object obj, string name, string entityName)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), obj.ToString(), name, entityName };
				base.WriteEvent(30415, str);
			}
		}

		[Conditional("CLIENT")]
		[Event(30411, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpReceiveMessage(object source, uint deliveryId, int transferCount)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), deliveryId, transferCount };
				base.WriteEvent(30411, str);
			}
		}

		[Event(30409, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpRemoveLink(object source, object link, uint handle)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), link.ToString(), handle };
				base.WriteEvent(30409, str);
			}
		}

		[Event(30404, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpRemoveSession(object source, object session, ushort localChannel, ushort remoteChannel)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), session.ToString(), localChannel, remoteChannel };
				base.WriteEvent(30404, str);
			}
		}

		[Conditional("CLIENT")]
		[Event(30410, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpSettle(object source, int settleCount, uint lwm, uint next)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), settleCount, lwm, next };
				base.WriteEvent(30410, str);
			}
		}

		[Conditional("CLIENT")]
		[Event(30407, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpStateTransition(object source, string operation, object fromState, object toState)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { source.ToString(), operation, fromState.ToString(), toState.ToString() };
				base.WriteEvent(30407, str);
			}
		}

		[Event(30412, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAmqpUpgradeTransport(object source, object from, object to)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30412, source.ToString(), from.ToString(), to.ToString());
			}
		}

		[Event(30201, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWriteAppDomainUnload(string DomainName, string ProcessName, int ProcessId)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] domainName = new object[] { DomainName, ProcessName, ProcessId };
				base.WriteEvent(30201, domainName);
			}
		}

		[Event(40001, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWriteBatchManagerException(EventTraceActivity activity, string trackingId, string subsystemId, string functionName, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, functionName, exception };
				base.WriteSBTraceEvent(40001, activity, objArray);
			}
		}

		[Conditional("CLIENT")]
		[Event(40000, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteBatchManagerExecutingBatchedObject(EventTraceActivity activity, EventTraceActivity relatedActivity, string trackingId, string subsystemId, string newTrackingId)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, newTrackingId };
				base.WriteTransferEvent(40000, activity, relatedActivity, objArray);
			}
		}

		[Event(40002, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWriteBatchManagerTransactionInDoubt(EventTraceActivity activity, string trackingId, string subsystemId, string transactionId, bool rollbackCalled)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, transactionId, rollbackCalled };
				base.WriteEvent(40002, activity, objArray);
			}
		}

		[Event(30004, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWriteChannelFaulted(string Exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30004, null, new object[] { Exception });
			}
		}

		[Conditional("CLIENT")]
		[Event(30005, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteChannelReceiveContextAbandon(System.Guid LockToken)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30005, new object[] { LockToken });
			}
		}

		[Conditional("CLIENT")]
		[Event(30006, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteChannelReceiveContextComplete(System.Guid LockToken)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30006, new object[] { LockToken });
			}
		}

		[Conditional("CLIENT")]
		[Event(30008, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteChannelReceivedMessage(string ChannelId, string ActionId, string MessageId)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30008, ChannelId, ActionId, MessageId);
			}
		}

		[Conditional("CLIENT")]
		[Event(30007, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteChannelSendingMessage(string ChannelId, string ActionId, string MessageId)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30007, ChannelId, ActionId, MessageId);
			}
		}

		[Event(60002, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteExceptionAsInformation(string Exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(60002, null, new object[] { Exception });
			}
		}

		[Event(60001, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWriteExceptionAsWarning(string Exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(60001, null, new object[] { Exception });
			}
		}

		[Conditional("CLIENT")]
		[Event(30619, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteFailedToCancelNotification(string scheduledNotificationId, string exception)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30619, scheduledNotificationId, exception);
			}
		}

		[Event(39999, Level=EventLevel.Critical, Channel=17)]
		public void EventWriteFailFastOccurred(string errorMessage)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(39999, errorMessage);
			}
		}

		[Conditional("CLIENT")]
		[Event(30021, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteGetRuntimeEntityDescriptionCompleted(EventTraceActivity activity, string TrackingId, string SubsystemId, string entityName, string options)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] trackingId = new object[] { TrackingId, SubsystemId, entityName, options };
				base.WriteEvent(30021, activity, trackingId);
			}
		}

		[Event(30022, Level=EventLevel.Error, Keywords=140737488355328L, Channel=17)]
		public void EventWriteGetRuntimeEntityDescriptionFailed(EventTraceActivity activity, string TrackingId, string SubsystemId, string entityName, string exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, 17))
			{
				object[] trackingId = new object[] { TrackingId, SubsystemId, entityName, exception };
				base.WriteSBTraceEvent(30022, activity, trackingId);
			}
		}

		[Conditional("CLIENT")]
		[Event(30020, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteGetRuntimeEntityDescriptionStarted(EventTraceActivity activity, string TrackingId, string SubsystemId, string entityName)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] trackingId = new object[] { TrackingId, SubsystemId, entityName };
				base.WriteEvent(30020, activity, trackingId);
			}
		}

		[Event(30211, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWriteHandledExceptionWarning(string Exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30211, null, new object[] { Exception });
			}
		}

		[Event(60003, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWriteHandledExceptionWithEntityName(string EntityName, string ExceptionMessage, string StackTrace)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] entityName = new object[] { EntityName, ExceptionMessage, StackTrace };
				base.WriteSBTraceEvent(60003, null, entityName);
			}
		}

		[Event(60004, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWriteLogAsWarning(string Value)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(60004, null, new object[] { Value });
			}
		}

		[Conditional("CLIENT")]
		[Event(30031, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteLogOperation(string Value1, string Value2)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30031, Value1, Value2);
			}
		}

		[Event(30032, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWriteLogOperationWarning(string exception, string Value2)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] value2 = new object[] { Value2, exception };
				base.WriteSBTraceEvent(30032, null, value2);
			}
		}

		[Conditional("CLIENT")]
		[Event(30000, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteMessageAbandon(EventTraceActivity activity, string TrackingId, string SubsystemId, string TransportType, string LockTokens)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] trackingId = new object[] { TrackingId, SubsystemId, TransportType, LockTokens };
				base.WriteEvent(30000, activity, trackingId);
			}
		}

		[Conditional("CLIENT")]
		[Event(30617, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteMessageCanceling(EventTraceActivity activity, string TrackingId, string SubsystemId, string TransportType, string MessageIds)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] trackingId = new object[] { TrackingId, SubsystemId, TransportType, MessageIds };
				base.WriteEvent(30617, activity, trackingId);
			}
		}

		[Conditional("CLIENT")]
		[Event(30001, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteMessageComplete(EventTraceActivity activity, string TrackingId, string SubsystemId, string TransportType, string LockTokens)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] trackingId = new object[] { TrackingId, SubsystemId, TransportType, LockTokens };
				base.WriteEvent(30001, activity, trackingId);
			}
		}

		[Conditional("CLIENT")]
		[Event(30011, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteMessageDefer(EventTraceActivity activity, string TrackingId, string SubsystemId, string TransportType, string LockTokens)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] trackingId = new object[] { TrackingId, SubsystemId, TransportType, LockTokens };
				base.WriteEvent(30011, activity, trackingId);
			}
		}

		[Conditional("CLIENT")]
		[Event(30002, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteMessageReceived(EventTraceActivity activity, string TrackingId, string SubsystemId, string TransportType, string MessageIds)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] trackingId = new object[] { TrackingId, SubsystemId, TransportType, MessageIds };
				base.WriteEvent(30002, activity, trackingId);
			}
		}

		[Conditional("CLIENT")]
		[Event(30010, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteMessageRenew(EventTraceActivity activity, string TrackingId, string SubsystemId, string TransportType, string LockTokens)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] trackingId = new object[] { TrackingId, SubsystemId, TransportType, LockTokens };
				base.WriteEvent(30010, activity, trackingId);
			}
		}

		[Conditional("CLIENT")]
		[Event(30003, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteMessageSending(EventTraceActivity activity, string TrackingId, string SubsystemId, string TransportType, string MessageIds)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] trackingId = new object[] { TrackingId, SubsystemId, TransportType, MessageIds };
				base.WriteEvent(30003, activity, trackingId);
			}
		}

		[Conditional("CLIENT")]
		[Event(30009, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteMessageSuspend(EventTraceActivity activity, string TrackingId, string SubsystemId, string TransportType, string LockTokens)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] trackingId = new object[] { TrackingId, SubsystemId, TransportType, LockTokens };
				base.WriteEvent(30009, activity, trackingId);
			}
		}

		[Event(60007, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWriteNonSerializableException(string Exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(60007, null, new object[] { Exception });
			}
		}

		[Event(15001, Level=EventLevel.Error, Keywords=140737488355328L, Channel=17)]
		public void EventWriteNullReferenceErrorOccurred(string errorMessage)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, 17))
			{
				base.WriteSBTraceEvent(15001, null, new object[] { errorMessage });
			}
		}

		[Event(30604, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceCouldNotCreateMessageSender(string sbNamespace, string queueName, Exception exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { queueName, sbNamespace, exception.ToString() };
				base.WriteSBTraceEvent(30604, null, objArray);
			}
		}

		[Event(30605, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceDeadletterException(string sbNamespace, string queueName, Exception exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { queueName, sbNamespace, exception.ToString() };
				base.WriteSBTraceEvent(30605, null, objArray);
			}
		}

		[Event(30606, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceDestinationSendException(string sbNamespace, string queueName, Exception exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { queueName, sbNamespace, exception.ToString() };
				base.WriteSBTraceEvent(30606, null, objArray);
			}
		}

		[Event(30603, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceMessageNoPathInBacklog(string sbNamespace, string queueName, string messageId)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30603, queueName, sbNamespace, messageId);
			}
		}

		[Event(30610, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceMessagePumpProcessCloseSenderFailed(Exception exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { exception.ToString() };
				base.WriteSBTraceEvent(30610, null, str);
			}
		}

		[Event(30609, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceMessagePumpProcessQueueFailed(Exception exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { exception.ToString() };
				base.WriteSBTraceEvent(30609, null, str);
			}
		}

		[Event(30602, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceMessagePumpReceiveFailed(string sbNamespace, string queueName, Exception error)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { queueName, sbNamespace, error.ToString() };
				base.WriteSBTraceEvent(30602, null, objArray);
			}
		}

		[Event(30608, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespacePingException(Exception exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] str = new object[] { exception.ToString() };
				base.WriteSBTraceEvent(30608, null, str);
			}
		}

		[Event(30614, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceReceiveMessageFromSecondary(long sequenceNumber, string secondaryQueue)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { sequenceNumber, secondaryQueue };
				base.WriteEvent(30614, objArray);
			}
		}

		[Event(30611, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceSendingMessage(long sequenceNumber, string destinationPath)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { sequenceNumber, destinationPath };
				base.WriteEvent(30611, objArray);
			}
		}

		[Event(30613, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceSendMessageFailure(long sequenceNumber, string destinationPath, Exception exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { sequenceNumber, destinationPath, exception };
				base.WriteSBTraceEvent(30613, null, objArray);
			}
		}

		[Event(30612, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceSendMessageSuccess(long sequenceNumber, string destinationPath)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { sequenceNumber, destinationPath };
				base.WriteEvent(30612, objArray);
			}
		}

		[Event(30607, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceSendToBacklogFailed(string sbNamespace, string queueName, Exception exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { queueName, sbNamespace, exception.ToString() };
				base.WriteSBTraceEvent(30607, null, objArray);
			}
		}

		[Event(30615, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceStartSyphon(EventTraceActivity activity, int syphonId, string primaryNamespace, string secondaryNamespace, int backlogQueueCount)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { syphonId, primaryNamespace, secondaryNamespace, backlogQueueCount };
				base.WriteSBTraceEvent(30615, activity, objArray);
			}
		}

		[Event(30616, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceStopSyphon(EventTraceActivity activity, int syphonId, string primaryNamespace, string secondaryNamespace)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { syphonId, primaryNamespace, secondaryNamespace };
				base.WriteSBTraceEvent(30616, activity, objArray);
			}
		}

		[Event(30600, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceTransferQueueCreateError(string queueName, string sbNamespace, string error)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30600, queueName, sbNamespace, error);
			}
		}

		[Event(30601, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWritePairedNamespaceTransferQueueCreateFailure(string queueName, string sbNamespace, string error)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30601, queueName, sbNamespace, error);
			}
		}

		[Event(30037, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWritePerformanceCounterCreationFailed(string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30037, null, new object[] { exception });
			}
		}

		[Conditional("CLIENT")]
		[Event(30035, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWritePerformanceCounterInstanceCreated(string value)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30035, value);
			}
		}

		[Conditional("CLIENT")]
		[Event(30036, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWritePerformanceCounterInstanceRemoved(string value)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30036, value);
			}
		}

		[Event(30033, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteRetryOperation(string operation, int retryCount, string reason)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { operation, retryCount, reason };
				base.WriteSBTraceEvent(30033, null, objArray);
			}
		}

		[Event(30304, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteRetryPolicyIteration(EventTraceActivity activity, string trackingId, string policyType, string operation, int iteration, string iterationSleep, string lastExceptionType, string exceptionMessage)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, policyType, operation, iteration, iterationSleep, lastExceptionType, exceptionMessage };
				base.WriteSBTraceEvent(30304, activity, objArray);
			}
		}

		[Event(30306, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteRetryPolicyStreamNotClonable(EventTraceActivity activity, string trackingId, string policyType, string operation, string exceptionType, string exceptionMessage)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, policyType, operation, exceptionType, exceptionMessage };
				base.WriteSBTraceEvent(30306, activity, objArray);
			}
		}

		[Event(30305, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteRetryPolicyStreamNotSeekable(EventTraceActivity activity, string trackingId, string policyType, string operation)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, policyType, operation };
				base.WriteSBTraceEvent(30305, activity, objArray);
			}
		}

		[Event(30042, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteRuntimeChannelAborting(string channelType, string localAddress, string remoteAddress, string via, string sessionId)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { channelType, localAddress, remoteAddress, via, sessionId };
				base.WriteEvent(30042, objArray);
			}
		}

		[Event(30041, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteRuntimeChannelCreated(string channelType, string localAddress, string remoteAddress, string via, string sessionId)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { channelType, localAddress, remoteAddress, via, sessionId };
				base.WriteEvent(30041, objArray);
			}
		}

		[Event(30043, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteRuntimeChannelFaulting(string channelType, string localAddress, string remoteAddress, string via, string sessionId, string reason)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { channelType, localAddress, remoteAddress, via, sessionId, reason };
				base.WriteSBTraceEvent(30043, null, objArray);
			}
		}

		[Event(30044, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteRuntimeChannelPingFailed(string channelType, string localAddress, string remoteAddress, string via, string sessionId, string reason)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { channelType, localAddress, remoteAddress, via, sessionId, reason };
				base.WriteSBTraceEvent(30044, null, objArray);
			}
		}

		[Event(30045, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteRuntimeChannelPingIncorrectState(string channelType, string localAddress, string remoteAddress, string via, string sessionId, string state)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { channelType, localAddress, remoteAddress, via, sessionId, state };
				base.WriteSBTraceEvent(30045, null, objArray);
			}
		}

		[Event(30046, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteRuntimeChannelStopPingWithIncorrectState(string channelType, string localAddress, string remoteAddress, string via, string sessionId, string state, int pendingRequestsCount)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { channelType, localAddress, remoteAddress, via, sessionId, state, pendingRequestsCount };
				base.WriteSBTraceEvent(30046, null, objArray);
			}
		}

		[Event(30202, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWriteShipAssertExceptionMessage(string Exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30202, null, new object[] { Exception });
			}
		}

		[Event(31111, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteSingletonManagerLoadSucceeded(string KeyName)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(31111, KeyName);
			}
		}

		[Event(30034, Level=EventLevel.Error, Keywords=140737488355328L, Channel=17)]
		public void EventWriteThreadNeutralSemaphoreEnterFailed(string name, int occurrence, double accumulativeTimeInMilliseconds)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, 17))
			{
				object[] objArray = new object[] { name, occurrence, accumulativeTimeInMilliseconds };
				base.WriteEvent(30034, objArray);
			}
		}

		[Event(60005, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWriteThrowingExceptionWithEntityName(string EntityName, string ExceptionString)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] entityName = new object[] { EntityName, ExceptionString };
				base.WriteSBTraceEvent(60005, null, entityName);
			}
		}

		[Event(30206, Level=EventLevel.Critical, Keywords=140737488355328L, Channel=19)]
		public void EventWriteTraceCodeEventLogCritical(string Value)
		{
			if (base.IsEnabled(EventLevel.Critical, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30206, null, new object[] { Value });
			}
		}

		[Event(30207, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void EventWriteTraceCodeEventLogError(string Value)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30207, null, new object[] { Value });
			}
		}

		[Event(30208, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void EventWriteTraceCodeEventLogInformational(string Value)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30208, null, new object[] { Value });
			}
		}

		[Conditional("CLIENT")]
		[Event(30209, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteTraceCodeEventLogVerbose(string Value)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30209, null, new object[] { Value });
			}
		}

		[Event(30210, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void EventWriteTraceCodeEventLogWarning(string Value)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30210, null, new object[] { Value });
			}
		}

		[Event(60008, Level=EventLevel.Error, Keywords=140737488355328L, Channel=17)]
		public void EventWriteUnexpectedExceptionTelemetry(string exceptionType)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, 17))
			{
				base.WriteSBTraceEvent(60008, null, new object[] { exceptionType });
			}
		}

		[Conditional("CLIENT")]
		[Event(30618, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void EventWriteUnexpectedScheduledNotificationIdFormat(string scheduledNotificationId)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(30618, scheduledNotificationId);
			}
		}

		[Event(30204, Level=EventLevel.Error, Keywords=140737488355328L, Channel=17)]
		public void EventWriteUnhandledException(string Exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, 17))
			{
				base.WriteSBTraceEvent(30204, null, new object[] { Exception });
			}
		}

		[Event(40217, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void FramingOuputPumpPingException(EventTraceActivity activity, string endpoint, string exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, exception };
				base.WriteSBTraceEvent(40217, activity, objArray);
			}
		}

		[Event(40218, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void FramingOuputPumpRunException(EventTraceActivity activity, string endpoint, string exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, exception };
				base.WriteSBTraceEvent(40218, activity, objArray);
			}
		}

		[Event(31114, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void GetStateTransfer(EventTraceActivity activity, EventTraceActivity relatedActivity, string sessionId)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { sessionId };
				base.WriteTransferEvent(31114, activity, relatedActivity, objArray);
			}
		}

		[Event(60006, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void HandledExceptionWithFunctionName(EventTraceActivity activity, string FunctionName, string ExceptionMessage, string ExceptionToString)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] functionName = new object[] { FunctionName, ExceptionMessage, ExceptionToString };
				base.WriteSBTraceEvent(60006, activity, functionName);
			}
		}

		[Event(40306, Level=EventLevel.Error, Keywords=140737488355328L, Channel=17)]
		public void HybridConnectionFailedToReadResourceDescriptionMetaData(EventTraceActivity activity, string uri, string exception, string stackTrace)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, 17))
			{
				object[] objArray = new object[] { uri, exception, stackTrace };
				base.WriteEvent(40306, activity, objArray);
			}
		}

		[Event(40304, Level=EventLevel.Error, Keywords=140737488355328L, Channel=17)]
		public void HybridConnectionFailedToStart(EventTraceActivity activity, string uri, string message, string stackTrace)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, 17))
			{
				object[] objArray = new object[] { uri, message, stackTrace };
				base.WriteEvent(40304, activity, objArray);
			}
		}

		[Event(40305, Level=EventLevel.Error, Keywords=140737488355328L, Channel=17)]
		public void HybridConnectionFailedToStop(EventTraceActivity activity, string uri, string message, string stackTrace)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, 17))
			{
				object[] objArray = new object[] { uri, message, stackTrace };
				base.WriteEvent(40305, activity, objArray);
			}
		}

		[Event(40309, Level=EventLevel.Error, Keywords=140737488355328L, Channel=17)]
		public void HybridConnectionInvalidConnectionString(EventTraceActivity activity, string uri)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, 17))
			{
				base.WriteEvent(40309, activity, new object[] { uri });
			}
		}

		[Event(40308, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=16)]
		public void HybridConnectionManagerConfigSettingsChanged(EventTraceActivity activity, string message)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, 16))
			{
				base.WriteEvent(40308, activity, new object[] { message });
			}
		}

		[Event(40310, Level=EventLevel.Error, Keywords=140737488355328L, Channel=17)]
		public void HybridConnectionManagerConfigurationFileError(EventTraceActivity activity, string message)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, 17))
			{
				base.WriteEvent(40310, activity, new object[] { message });
			}
		}

		[Event(40311, Level=EventLevel.Error, Keywords=140737488355328L, Channel=17)]
		public void HybridConnectionManagerManagementServerError(EventTraceActivity activity, string error)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, 17))
			{
				base.WriteEvent(40311, activity, new object[] { error });
			}
		}

		[Event(40312, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=16)]
		public void HybridConnectionManagerManagementServiceStarting(EventTraceActivity activity, string port)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, 16))
			{
				base.WriteEvent(40312, activity, new object[] { port });
			}
		}

		[Event(40313, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=16)]
		public void HybridConnectionManagerManagementServiceStopping(EventTraceActivity activity, string port)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, 16))
			{
				base.WriteEvent(40313, activity, new object[] { port });
			}
		}

		[Event(40300, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=17)]
		public void HybridConnectionManagerStarting()
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, 17))
			{
				base.WriteEvent(40300);
			}
		}

		[Event(40301, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=17)]
		public void HybridConnectionManagerStopping()
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, 17))
			{
				base.WriteEvent(40301);
			}
		}

		[Event(40307, Level=EventLevel.Error, Keywords=140737488355328L, Channel=17)]
		public void HybridConnectionSecurityException(EventTraceActivity activity, string uri, string exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, 17))
			{
				object[] objArray = new object[] { uri, exception };
				base.WriteEvent(40307, activity, objArray);
			}
		}

		[Event(40302, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=17)]
		public void HybridConnectionStarted(EventTraceActivity activity, string uri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, 17))
			{
				base.WriteEvent(40302, activity, new object[] { uri });
			}
		}

		[Event(40303, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=17)]
		public void HybridConnectionStopped(EventTraceActivity activity, string uri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, 17))
			{
				base.WriteEvent(40303, activity, new object[] { uri });
			}
		}

		[Event(31119, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void MessagePeekTransfer(EventTraceActivity activity, EventTraceActivity relatedActivity)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteTransferEvent(31119, activity, relatedActivity, new object[0]);
			}
		}

		[Event(40009, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void MessageReceivePumpBackoff(EventTraceActivity activity, string trackingId, string subsystemId, int sleepAmountInMilliseconds, string exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sleepAmountInMilliseconds, exception };
				base.WriteSBTraceEvent(40009, activity, objArray);
			}
		}

		[Event(40005, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageReceivePumpFailedToAbandon(EventTraceActivity activity, string trackingId, string subsystemId, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, exception };
				base.WriteSBTraceEvent(40005, activity, objArray);
			}
		}

		[Event(40004, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageReceivePumpFailedToComplete(EventTraceActivity activity, string trackingId, string subsystemId, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, exception };
				base.WriteSBTraceEvent(40004, activity, objArray);
			}
		}

		[Event(40007, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageReceivePumpReceiveException(EventTraceActivity activity, string trackingId, string subsystemId, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, exception };
				base.WriteSBTraceEvent(40007, activity, objArray);
			}
		}

		[Event(40010, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageReceivePumpRenewLockFailed(EventTraceActivity activity, string trackingId, string subsystemId, string messageId, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, messageId, exception };
				base.WriteSBTraceEvent(40010, activity, objArray);
			}
		}

		[Event(40012, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void MessageReceivePumpRenewLockInvalidOperation(EventTraceActivity activity, string trackingId, string subsystemId, string messageId, string exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, messageId, exception };
				base.WriteSBTraceEvent(40012, activity, objArray);
			}
		}

		[Event(40011, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void MessageReceivePumpRenewLockNotSupported(EventTraceActivity activity, string trackingId, string subsystemId, string messageId, string exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, messageId, exception };
				base.WriteSBTraceEvent(40011, activity, objArray);
			}
		}

		[Event(40008, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void MessageReceivePumpStopped(EventTraceActivity activity, string trackingId, string subsystemId)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId };
				base.WriteEvent(40008, activity, objArray);
			}
		}

		[Event(40006, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void MessageReceivePumpUnexpectedException(EventTraceActivity activity, string trackingId, string subsystemId, string exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, exception };
				base.WriteSBTraceEvent(40006, activity, objArray);
			}
		}

		[Event(40003, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageReceivePumpUserCallbackException(EventTraceActivity activity, string trackingId, string subsystemId, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, exception };
				base.WriteSBTraceEvent(40003, activity, objArray);
			}
		}

		[Event(40013, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageReceivePumpUserCallTimedOut(EventTraceActivity activity, string trackingId, string subsystemId, string messageId)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, messageId };
				base.WriteSBTraceEvent(40013, activity, objArray);
			}
		}

		[Event(31115, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void MessageReceiveTransfer(EventTraceActivity activity, EventTraceActivity relatedActivity)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteTransferEvent(31115, activity, relatedActivity, new object[0]);
			}
		}

		[Event(31112, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void MessageSendingTransfer(EventTraceActivity activity, EventTraceActivity relatedActivity)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteTransferEvent(31112, activity, relatedActivity, new object[0]);
			}
		}

		[Event(31214, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpAcceptSessionFailed(EventTraceActivity activity, string trackingId, string subsystemId, string exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, exception };
				base.WriteSBTraceEvent(31214, activity, objArray);
			}
		}

		[Event(31209, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpActionFailed(EventTraceActivity activity, string trackingId, string subsystemId, string sessionId, string action, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sessionId, action, exception };
				base.WriteSBTraceEvent(31209, activity, objArray);
			}
		}

		[Event(31207, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpFirstReceiveFailed(EventTraceActivity activity, string trackingId, string subsystemId, string sessionId, string exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sessionId, exception };
				base.WriteSBTraceEvent(31207, activity, objArray);
			}
		}

		[Event(31208, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpFirstReceiveReturnedNoMessage(EventTraceActivity activity, string trackingId, string subsystemId, string sessionId)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sessionId };
				base.WriteSBTraceEvent(31208, activity, objArray);
			}
		}

		[Event(31203, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpRenewBeginFailed(EventTraceActivity activity, string trackingId, string subsystemId, string sessionId, string exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sessionId, exception };
				base.WriteSBTraceEvent(31203, activity, objArray);
			}
		}

		[Event(31201, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpRenewDetectedSessionLost(EventTraceActivity activity, string trackingId, string subsystemId, string sessionId, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sessionId, exception };
				base.WriteSBTraceEvent(31201, activity, objArray);
			}
		}

		[Event(31204, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpRenewEndFailed(EventTraceActivity activity, string trackingId, string subsystemId, string sessionId, string exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sessionId, exception };
				base.WriteSBTraceEvent(31204, activity, objArray);
			}
		}

		[Event(31202, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpRenewFailed(EventTraceActivity activity, string trackingId, string subsystemId, string sessionId, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sessionId, exception };
				base.WriteSBTraceEvent(31202, activity, objArray);
			}
		}

		[Event(31200, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpRenewNotSupported(EventTraceActivity activity, string trackingId, string subsystemId, string sessionId, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sessionId, exception };
				base.WriteSBTraceEvent(31200, activity, objArray);
			}
		}

		[Event(31210, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpSessionCloseFailed(EventTraceActivity activity, string trackingId, string subsystemId, string sessionId, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sessionId, exception };
				base.WriteSBTraceEvent(31210, activity, objArray);
			}
		}

		[Event(31213, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpUnexpectedException(EventTraceActivity activity, string trackingId, string subsystemId, string sessionId, string exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sessionId, exception };
				base.WriteSBTraceEvent(31213, activity, objArray);
			}
		}

		[Event(31205, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpUserCallTimedOut(EventTraceActivity activity, string trackingId, string subsystemId, string sessionId, string timeout)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sessionId, timeout };
				base.WriteSBTraceEvent(31205, activity, objArray);
			}
		}

		[Event(31206, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void MessageSessionPumpUserException(EventTraceActivity activity, string trackingId, string subsystemId, string sessionId, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { trackingId, subsystemId, sessionId, exception };
				base.WriteSBTraceEvent(31206, activity, objArray);
			}
		}

		[Event(40215, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void RelayChannelAborting(EventTraceActivity activity, string uri)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(40215, activity, new object[] { uri });
			}
		}

		[Event(40216, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayChannelClosing(EventTraceActivity activity, string uri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(40216, activity, new object[] { uri });
			}
		}

		[Event(40212, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayChannelConnectionTransfer(EventTraceActivity channelActivity, EventTraceActivity connectionActivity)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteTransferEvent(40212, channelActivity, connectionActivity, new object[0]);
			}
		}

		[Event(40214, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void RelayChannelFaulting(EventTraceActivity activity, string uri)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(40214, activity, new object[] { uri });
			}
		}

		[Event(40213, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayChannelOpening(EventTraceActivity activity, string channelType, string endpoint)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { channelType, endpoint };
				base.WriteEvent(40213, activity, objArray);
			}
		}

		[Event(40202, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayClientConnected(EventTraceActivity activity, string endpoint, bool isListener)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, isListener };
				base.WriteEvent(40202, activity, objArray);
			}
		}

		[Event(40226, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void RelayClientConnectFailed(EventTraceActivity activity, string endpoint, string message)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, message };
				base.WriteEvent(40226, activity, objArray);
			}
		}

		[Event(40201, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayClientConnecting(EventTraceActivity activity, string endpoint, bool isListener)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, isListener };
				base.WriteEvent(40201, activity, objArray);
			}
		}

		[Event(40203, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayClientConnectivityModeDetected(EventTraceActivity activity, string endpoint, bool isListener, string mode)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, isListener, mode };
				base.WriteEvent(40203, activity, objArray);
			}
		}

		[Event(40227, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayClientConnectRedirected(EventTraceActivity activity, string originalUri, string redirectedUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, redirectedUri };
				base.WriteEvent(40227, activity, objArray);
			}
		}

		[Event(40200, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayClientDisconnected(EventTraceActivity activity, string endpoint, bool isListener, string exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, isListener, exception };
				base.WriteSBTraceEvent(40200, activity, objArray);
			}
		}

		[Event(40209, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void RelayClientFailedToAcquireToken(EventTraceActivity activity, string endpoint, bool isListener, string action, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, isListener, action, exception };
				base.WriteSBTraceEvent(40209, activity, objArray);
			}
		}

		[Event(40191, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayClientGoingOnline(EventTraceActivity activity, string endpoint)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(40191, activity, new object[] { endpoint });
			}
		}

		[Event(40208, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void RelayClientPingFailed(EventTraceActivity activity, string endpoint, bool isListener, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, isListener, exception };
				base.WriteSBTraceEvent(40208, activity, objArray);
			}
		}

		[Event(40199, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayClientReconnecting(EventTraceActivity activity, string endpoint, string listenerType)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, listenerType };
				base.WriteEvent(40199, activity, objArray);
			}
		}

		[Event(40193, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayClientStopConnecting(EventTraceActivity activity, string endpoint, string listenerType)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, listenerType };
				base.WriteEvent(40193, activity, objArray);
			}
		}

		[Event(40205, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayListenerClientAccepted(EventTraceActivity activity, string endpoint, string clientId)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, clientId };
				base.WriteEvent(40205, activity, objArray);
			}
		}

		[Event(40206, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void RelayListenerClientAcceptFailed(EventTraceActivity activity, string endpoint, string clientId, string exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, clientId, exception };
				base.WriteSBTraceEvent(40206, activity, objArray);
			}
		}

		[Event(40210, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void RelayListenerFailedToDispatchMessage(EventTraceActivity activity, string endpoint, string incomingVia)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, incomingVia };
				base.WriteEvent(40210, activity, objArray);
			}
		}

		[Event(40204, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RelayListenerRelayedConnectReceived(EventTraceActivity activity, string endpoint, string clientId)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, clientId };
				base.WriteEvent(40204, activity, objArray);
			}
		}

		[Conditional("CLIENT")]
		[Event(40211, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void RelayLogVerbose(EventTraceActivity activity, string label, string detail)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { label, detail };
				base.WriteSBTraceEvent(40211, activity, objArray);
			}
		}

		[Event(31116, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void RenewSessionLockTransfer(EventTraceActivity activity, EventTraceActivity relatedActivity)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteTransferEvent(31116, activity, relatedActivity, new object[0]);
			}
		}

		[Event(31113, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void SetStateTransfer(EventTraceActivity activity, EventTraceActivity relatedActivity, string sessionId)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { sessionId };
				base.WriteTransferEvent(31113, activity, relatedActivity, objArray);
			}
		}

		[Event(30214, Level=EventLevel.Error, Keywords=140737488355328L, Channel=19)]
		public void ThrowingExceptionError(EventTraceActivity activity, string Exception)
		{
			if (base.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30214, activity, new object[] { Exception });
			}
		}

		[Event(30212, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void ThrowingExceptionInformational(EventTraceActivity activity, string Exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30212, activity, new object[] { Exception });
			}
		}

		[Conditional("CLIENT")]
		[Event(30203, Level=EventLevel.Verbose, Keywords=140737488355328L, Channel=19)]
		public void ThrowingExceptionVerbose(EventTraceActivity activity, string Exception)
		{
			if (base.IsEnabled(EventLevel.Verbose, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30203, activity, new object[] { Exception });
			}
		}

		[Event(30213, Level=EventLevel.Warning, Keywords=140737488355328L, Channel=19)]
		public void ThrowingExceptionWarning(EventTraceActivity activity, string Exception)
		{
			if (base.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteSBTraceEvent(30213, activity, new object[] { Exception });
			}
		}

		[Event(40244, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebSocketConnectionAborted(EventTraceActivity activity, string uri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(40244, activity, new object[] { uri });
			}
		}

		[Event(40243, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebSocketConnectionClosed(EventTraceActivity activity, string uri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(40243, activity, new object[] { uri });
			}
		}

		[Event(40241, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebSocketConnectionEstablished(EventTraceActivity activity, string sbUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(40241, activity, new object[] { sbUri });
			}
		}

		[Event(40242, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebSocketConnectionShutdown(EventTraceActivity activity, string uri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(40242, activity, new object[] { uri });
			}
		}

		[Event(40248, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebSocketTransportAborted(EventTraceActivity activity, string uri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(40248, activity, new object[] { uri });
			}
		}

		[Event(40247, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebSocketTransportClosed(EventTraceActivity activity, string uri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(40247, activity, new object[] { uri });
			}
		}

		[Event(40245, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebSocketTransportEstablished(EventTraceActivity activity, string sbUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(40245, activity, new object[] { sbUri });
			}
		}

		[Event(40246, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebSocketTransportShutdown(EventTraceActivity activity, string uri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				base.WriteEvent(40246, activity, new object[] { uri });
			}
		}

		[Event(40221, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamClose(EventTraceActivity activity, string endpoint, string sbUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, sbUri };
				base.WriteEvent(40221, activity, objArray);
			}
		}

		[Event(40229, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamConnectCompleted(EventTraceActivity activity, string originalUri, string sbUri, int retries)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, sbUri, retries };
				base.WriteEvent(40229, activity, objArray);
			}
		}

		[Event(40230, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamConnectFailed(EventTraceActivity activity, string originalUri, string sbUri, int retries, string exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, sbUri, retries, exception };
				base.WriteSBTraceEvent(40230, activity, objArray);
			}
		}

		[Event(40228, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamConnecting(EventTraceActivity activity, string originalUri, string sbUri, int retries)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, sbUri, retries };
				base.WriteEvent(40228, activity, objArray);
			}
		}

		[Event(40223, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamConnectionAbort(EventTraceActivity activity, string endpoint, string sbUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, sbUri };
				base.WriteEvent(40223, activity, objArray);
			}
		}

		[Event(40224, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamConnectionClose(EventTraceActivity activity, string endpoint, string sbUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, sbUri };
				base.WriteEvent(40224, activity, objArray);
			}
		}

		[Event(40225, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamConnectionShutdown(EventTraceActivity activity, string endpoint, string sbUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, sbUri };
				base.WriteEvent(40225, activity, objArray);
			}
		}

		[Event(40219, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamDispose(EventTraceActivity activity, string endpoint, string sbUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, sbUri };
				base.WriteEvent(40219, activity, objArray);
			}
		}

		[Event(40232, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamFramingInputPumpSlowRead(EventTraceActivity activity, string originalUri, int bytesRead, string elapsed)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, bytesRead, elapsed };
				base.WriteEvent(40232, activity, objArray);
			}
		}

		[Event(40231, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamFramingInputPumpSlowReadWithException(EventTraceActivity activity, string originalUri, string elapsed, string exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, elapsed, exception };
				base.WriteSBTraceEvent(40231, activity, objArray);
			}
		}

		[Event(40236, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamFramingOuputPumpPingSlow(EventTraceActivity activity, string originalUri, string elapsed)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, elapsed };
				base.WriteEvent(40236, activity, objArray);
			}
		}

		[Event(40233, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamFramingOuputPumpPingSlowException(EventTraceActivity activity, string originalUri, string elapsed, string exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, elapsed, exception };
				base.WriteSBTraceEvent(40233, activity, objArray);
			}
		}

		[Event(40239, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamFramingOuputPumpSlow(EventTraceActivity activity, string originalUri, string elapsed)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, elapsed };
				base.WriteEvent(40239, activity, objArray);
			}
		}

		[Event(40238, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamFramingOuputPumpSlowException(EventTraceActivity activity, string originalUri, string elapsed, string exception)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, elapsed, exception };
				base.WriteSBTraceEvent(40238, activity, objArray);
			}
		}

		[Event(40234, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamReadStreamCompleted(EventTraceActivity activity, string originalUri, string sbUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, sbUri };
				base.WriteEvent(40234, activity, objArray);
			}
		}

		[Event(40220, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamReset(EventTraceActivity activity, string endpoint, string sbUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, sbUri };
				base.WriteEvent(40220, activity, objArray);
			}
		}

		[Event(40237, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamReturningZero(EventTraceActivity activity, string originalUri, string sbUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, sbUri };
				base.WriteEvent(40237, activity, objArray);
			}
		}

		[Event(40222, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamShutdown(EventTraceActivity activity, string endpoint, string sbUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { endpoint, sbUri };
				base.WriteEvent(40222, activity, objArray);
			}
		}

		[Event(40235, Level=EventLevel.Informational, Keywords=140737488355328L, Channel=19)]
		public void WebStreamWriteStreamCompleted(EventTraceActivity activity, string originalUri, string sbUri)
		{
			if (base.IsEnabled(EventLevel.Informational, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
			{
				object[] objArray = new object[] { originalUri, sbUri };
				base.WriteEvent(40235, activity, objArray);
			}
		}

		public class Channels
		{
			[Channel(Type="Admin", Enabled=true)]
			public const EventChannel AdminChannel = 16;

			[Channel(Type="Operational", Enabled=true)]
			public const EventChannel OperationalChannel = 17;

			[Channel(Type="Analytic", Enabled=false)]
			public const EventChannel AnalyticChannel = 18;

			[Channel(Type="Debug", Enabled=false)]
			public const EventChannel DebugChannel = EventChannel.Application | EventChannel.Security | EventChannel.Setup;

			public Channels()
			{
			}
		}

		public class Keywords
		{
			public const EventKeywords Client = 140737488355328L;

			public Keywords()
			{
			}
		}
	}
}
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus.Channels
{
	internal class RequestReplyCorrelator : Microsoft.ServiceBus.Channels.IRequestReplyCorrelator
	{
		private Hashtable states;

		internal RequestReplyCorrelator()
		{
			this.states = new Hashtable();
		}

		internal static bool AddressReply(Message reply, Message request)
		{
			return Microsoft.ServiceBus.Channels.RequestReplyCorrelator.AddressReply(reply, Microsoft.ServiceBus.Channels.RequestReplyCorrelator.ExtractReplyToInfo(request));
		}

		internal static bool AddressReply(Message reply, Microsoft.ServiceBus.Channels.RequestReplyCorrelator.ReplyToInfo info)
		{
			EndpointAddress faultTo = null;
			if (info.HasFaultTo && reply.IsFault)
			{
				faultTo = info.FaultTo;
			}
			else if (info.HasReplyTo)
			{
				faultTo = info.ReplyTo;
			}
			else if (reply.Version.Addressing == AddressingVersion.WSAddressingAugust2004)
			{
				faultTo = (!info.HasFrom ? EndpointAddress2.AnonymousAddress : info.From);
			}
			if (faultTo == null)
			{
				return true;
			}
			faultTo.ApplyTo(reply);
			return !faultTo.IsNone;
		}

		internal static Microsoft.ServiceBus.Channels.RequestReplyCorrelator.ReplyToInfo ExtractReplyToInfo(Message message)
		{
			return new Microsoft.ServiceBus.Channels.RequestReplyCorrelator.ReplyToInfo(message);
		}

		private static UniqueId GetRelatesTo(Message reply)
		{
			UniqueId relatesTo = reply.Headers.RelatesTo;
			if (relatesTo == null)
			{
				throw TraceUtility.ThrowHelperError(new ArgumentException(Microsoft.ServiceBus.SR.GetString(Resources.SuppliedMessageIsNotAReplyItHasNoRelatesTo0, new object[0])), reply);
			}
			return relatesTo;
		}

		void Microsoft.ServiceBus.Channels.IRequestReplyCorrelator.Add<T>(Message request, T state)
		{
			Microsoft.ServiceBus.Channels.RequestReplyCorrelator.Key key = new Microsoft.ServiceBus.Channels.RequestReplyCorrelator.Key(request.Headers.MessageId, typeof(T));
			lock (this.states)
			{
				this.states.Add(key, state);
			}
		}

		T Microsoft.ServiceBus.Channels.IRequestReplyCorrelator.Find<T>(Message reply, bool remove)
		{
			T item;
			Microsoft.ServiceBus.Channels.RequestReplyCorrelator.Key key = new Microsoft.ServiceBus.Channels.RequestReplyCorrelator.Key(Microsoft.ServiceBus.Channels.RequestReplyCorrelator.GetRelatesTo(reply), typeof(T));
			lock (this.states)
			{
				item = (T)this.states[key];
				if (remove)
				{
					this.states.Remove(key);
				}
			}
			return item;
		}

		void Microsoft.ServiceBus.Channels.IRequestReplyCorrelator.Remove<T>(Message request)
		{
			Microsoft.ServiceBus.Channels.RequestReplyCorrelator.Key key = new Microsoft.ServiceBus.Channels.RequestReplyCorrelator.Key(request.Headers.MessageId, typeof(T));
			lock (this.states)
			{
				this.states.Remove(key);
			}
		}

		internal static void PrepareReply(Message reply, UniqueId messageId)
		{
			if (object.ReferenceEquals(messageId, null))
			{
				throw TraceUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.MissingMessageID, new object[0])), reply);
			}
			MessageHeaders headers = reply.Headers;
			if (object.ReferenceEquals(headers.RelatesTo, null))
			{
				headers.RelatesTo = messageId;
			}
			if (TraceUtility.PropagateUserActivity || TraceUtility.ShouldPropagateActivity)
			{
				TraceUtility.AddAmbientActivityToMessage(reply);
			}
		}

		internal static void PrepareReply(Message reply, Message request)
		{
			UniqueId messageId = request.Headers.MessageId;
			if (messageId != null)
			{
				MessageHeaders headers = reply.Headers;
				if (object.ReferenceEquals(headers.RelatesTo, null))
				{
					headers.RelatesTo = messageId;
				}
			}
			if (TraceUtility.PropagateUserActivity || TraceUtility.ShouldPropagateActivity)
			{
				TraceUtility.AddAmbientActivityToMessage(reply);
			}
		}

		internal static void PrepareRequest(Message request)
		{
			MessageHeaders headers = request.Headers;
			if (headers.MessageId == null)
			{
				headers.MessageId = new UniqueId();
			}
			request.Properties.AllowOutputBatching = false;
			if (TraceUtility.PropagateUserActivity || TraceUtility.ShouldPropagateActivity)
			{
				TraceUtility.AddAmbientActivityToMessage(request);
			}
		}

		private class Key
		{
			internal UniqueId MessageId;

			internal Type StateType;

			internal Key(UniqueId messageId, Type stateType)
			{
				this.MessageId = messageId;
				this.StateType = stateType;
			}

			public override bool Equals(object obj)
			{
				Microsoft.ServiceBus.Channels.RequestReplyCorrelator.Key key = obj as Microsoft.ServiceBus.Channels.RequestReplyCorrelator.Key;
				if (key == null)
				{
					return false;
				}
				if (key.MessageId != this.MessageId)
				{
					return false;
				}
				return key.StateType == this.StateType;
			}

			public override int GetHashCode()
			{
				return this.MessageId.GetHashCode() ^ this.StateType.GetHashCode();
			}

			public override string ToString()
			{
				object[] str = new object[] { typeof(Microsoft.ServiceBus.Channels.RequestReplyCorrelator.Key).ToString(), ": {", this.MessageId, ", ", this.StateType.ToString(), "}" };
				return string.Concat(str);
			}
		}

		internal struct ReplyToInfo
		{
			private readonly EndpointAddress faultTo;

			private readonly EndpointAddress @from;

			private readonly EndpointAddress replyTo;

			internal EndpointAddress FaultTo
			{
				get
				{
					return this.faultTo;
				}
			}

			internal EndpointAddress From
			{
				get
				{
					return this.@from;
				}
			}

			internal bool HasFaultTo
			{
				get
				{
					return !Microsoft.ServiceBus.Channels.RequestReplyCorrelator.ReplyToInfo.IsTrivial(this.FaultTo);
				}
			}

			internal bool HasFrom
			{
				get
				{
					return !Microsoft.ServiceBus.Channels.RequestReplyCorrelator.ReplyToInfo.IsTrivial(this.From);
				}
			}

			internal bool HasReplyTo
			{
				get
				{
					return !Microsoft.ServiceBus.Channels.RequestReplyCorrelator.ReplyToInfo.IsTrivial(this.ReplyTo);
				}
			}

			internal EndpointAddress ReplyTo
			{
				get
				{
					return this.replyTo;
				}
			}

			internal ReplyToInfo(Message message)
			{
				this.faultTo = message.Headers.FaultTo;
				this.replyTo = message.Headers.ReplyTo;
				if (message.Version.Addressing != AddressingVersion.WSAddressingAugust2004)
				{
					this.@from = null;
					return;
				}
				this.@from = message.Headers.From;
			}

			private static bool IsTrivial(EndpointAddress address)
			{
				if (address == null)
				{
					return true;
				}
				return address == EndpointAddress2.AnonymousAddress;
			}
		}
	}
}
using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Common
{
	internal static class MessageExtensionMethods
	{
		public static void AddHeader<T>(this Message thisMessage, string name, string ns, T value)
		{
			MessageHeader messageHeader = MessageHeader.CreateHeader(name, ns, value);
			thisMessage.Headers.Add(messageHeader);
		}

		public static void AddHeaderIfNotNull<T>(this Message thisMessage, string name, string ns, T value)
		where T : class
		{
			if (value != null)
			{
				MessageHeader messageHeader = MessageHeader.CreateHeader(name, ns, value);
				thisMessage.Headers.Add(messageHeader);
			}
		}

		public static T GetHeaderOrDefault<T>(this Message thisMessage, string name, string ns, T defaultValue)
		{
			T t;
			int num = thisMessage.Headers.FindHeader(name, ns);
			t = (num == -1 ? defaultValue : thisMessage.Headers.GetHeader<T>(num));
			return t;
		}

		public static bool HasHeader(this Message thisMessage, string name, string ns)
		{
			return thisMessage.Headers.FindHeader(name, ns) != -1;
		}

		public static EventData ToMessage(this BrokeredMessage brokeredMessage)
		{
			EventData eventDatum = null;
			if (brokeredMessage != null)
			{
				eventDatum = new EventData(brokeredMessage);
			}
			return eventDatum;
		}

		public static bool TryGetHeader<T>(this Message thisMessage, string name, string ns, out T value)
		{
			int num = thisMessage.Headers.FindHeader(name, ns);
			if (num == -1)
			{
				value = default(T);
				return false;
			}
			value = thisMessage.Headers.GetHeader<T>(num);
			return true;
		}

		public static void UpdateHeader(this Message thisMessage, string name, string ns, object value)
		{
			int num = thisMessage.Headers.FindHeader(name, ns);
			if (num != -1)
			{
				thisMessage.Headers.RemoveAt(num);
			}
			MessageHeader messageHeader = MessageHeader.CreateHeader(name, ns, value);
			thisMessage.Headers.Add(messageHeader);
		}
	}
}
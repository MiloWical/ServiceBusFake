using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	[KnownType(typeof(DelayFragmentInfo))]
	[KnownType(typeof(FaultWrapperInfo))]
	[KnownType(typeof(SleepInfo))]
	[KnownType(typeof(ThrowExceptionInfo))]
	internal class FaultInjectionInfo
	{
		public const string HeaderName = "FaultInjectionInfo";

		private readonly static DataContractSerializer serializer;

		[DataMember]
		public bool FireOnce
		{
			get;
			set;
		}

		[DataMember]
		public int RetryCount
		{
			get;
			set;
		}

		[DataMember]
		public FaultInjectionTarget Target
		{
			get;
			set;
		}

		[DataMember]
		public DateTime ValidAfter
		{
			get;
			set;
		}

		[DataMember]
		public DateTime ValidBefore
		{
			get;
			set;
		}

		static FaultInjectionInfo()
		{
			FaultInjectionInfo.serializer = new DataContractSerializer(typeof(FaultInjectionInfo));
		}

		protected FaultInjectionInfo()
		{
			this.RetryCount = 1;
			this.ValidBefore = DateTime.MaxValue;
			this.FireOnce = true;
		}

		public void AddToHeader(Message message)
		{
			MessageHeader messageHeader = MessageHeader.CreateHeader("FaultInjectionInfo", "http://schemas.microsoft.com/netservices/2011/06/servicebus", this, FaultInjectionInfo.serializer);
			message.Headers.Add(messageHeader);
		}

		public void AddToHeader(HttpWebRequest request)
		{
			using (Stream memoryStream = null)
			{
				memoryStream = new MemoryStream();
				FaultInjectionInfo.serializer.WriteObject(memoryStream, this);
				memoryStream.Seek((long)0, SeekOrigin.Begin);
				using (StreamReader streamReader = new StreamReader(memoryStream))
				{
					memoryStream = null;
					string end = streamReader.ReadToEnd();
					request.Headers.Add("FaultInjectionInfo", end);
				}
			}
		}

		public bool IsFaultDue(DateTime utcNow)
		{
			if (this.ValidAfter > utcNow)
			{
				return false;
			}
			return utcNow <= this.ValidBefore;
		}

		public static void RemoveHeader(WebHeaderCollection headers)
		{
			if (!string.IsNullOrWhiteSpace(headers["FaultInjectionInfo"]))
			{
				headers.Remove("FaultInjectionInfo");
			}
		}

		public static void RemoveHeader(MessageHeaders headers)
		{
			int num = headers.FindHeader("FaultInjectionInfo", "http://schemas.microsoft.com/netservices/2011/06/servicebus");
			if (num != -1)
			{
				headers.RemoveAt(num);
			}
		}

		public static bool TryGetHeader(WebHeaderCollection headers, out FaultInjectionInfo value)
		{
			if (headers == null)
			{
				value = null;
				return false;
			}
			string item = headers["FaultInjectionInfo"];
			if (string.IsNullOrWhiteSpace(item))
			{
				value = null;
				return false;
			}
			using (XmlReader xmlTextReader = new XmlTextReader(new StringReader(item)))
			{
				value = (FaultInjectionInfo)FaultInjectionInfo.serializer.ReadObject(xmlTextReader);
			}
			return true;
		}

		public static bool TryGetHeader(MessageHeaders headers, out FaultInjectionInfo value)
		{
			int num = headers.FindHeader("FaultInjectionInfo", "http://schemas.microsoft.com/netservices/2011/06/servicebus");
			if (num == -1)
			{
				value = null;
				return false;
			}
			value = headers.GetHeader<FaultInjectionInfo>(num, FaultInjectionInfo.serializer);
			return true;
		}

		public void UpdateHeader(MessageHeaders headers)
		{
			FaultInjectionInfo.RemoveHeader(headers);
			MessageHeader messageHeader = MessageHeader.CreateHeader("FaultInjectionInfo", "http://schemas.microsoft.com/netservices/2011/06/servicebus", this, FaultInjectionInfo.serializer);
			headers.Add(messageHeader);
		}
	}
}
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="Error", Namespace="")]
	public class ServiceBusErrorData : IExtensibleDataObject
	{
		public const string RootTag = "Error";

		public const string HttpStatusCodeTag = "Code";

		public const string DetailTag = "Detail";

		private ExtensionDataObject extensionDataObject;

		[DataMember(Name="Code", Order=101, IsRequired=false, EmitDefaultValue=false)]
		public int Code
		{
			get;
			set;
		}

		[DataMember(Name="Detail", Order=102, IsRequired=false, EmitDefaultValue=false)]
		public string Detail
		{
			get;
			set;
		}

		public ExtensionDataObject ExtensionData
		{
			get
			{
				return this.extensionDataObject;
			}
			set
			{
				this.extensionDataObject = value;
			}
		}

		public ServiceBusErrorData()
		{
		}

		public static ServiceBusErrorData GetServiceBusErrorData(HttpWebResponse webResponse)
		{
			Stream responseStream = null;
			ServiceBusErrorData serviceBusErrorDatum = new ServiceBusErrorData();
			try
			{
				responseStream = webResponse.GetResponseStream();
			}
			catch (ProtocolViolationException protocolViolationException)
			{
			}
			if (responseStream != null)
			{
				if (responseStream.CanSeek)
				{
					responseStream.Position = (long)0;
				}
				XmlReader xmlReader = XmlReader.Create(responseStream);
				try
				{
					try
					{
						xmlReader.Read();
						xmlReader.ReadStartElement("Error");
						xmlReader.ReadStartElement("Code");
						serviceBusErrorDatum.Code = Convert.ToInt32(xmlReader.ReadString(), CultureInfo.InvariantCulture);
						xmlReader.ReadEndElement();
						xmlReader.ReadStartElement("Detail");
						serviceBusErrorDatum.Detail = xmlReader.ReadString();
					}
					catch (XmlException xmlException)
					{
						serviceBusErrorDatum.Code = Convert.ToInt16(webResponse.StatusCode, CultureInfo.InvariantCulture);
						serviceBusErrorDatum.Detail = webResponse.StatusDescription;
					}
				}
				finally
				{
					xmlReader.Close();
				}
			}
			return serviceBusErrorDatum;
		}
	}
}
using Microsoft.ServiceBus.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging
{
	internal class MessagingDescriptionSerializer<TMessagingDescription>
	{
		private const int MaxItemsInObjectGraph = 256;

		private const int MessagingDescriptionStringCommonLength = 512;

		private const int MessagingDescriptionStringMaxLength = 24576;

		public static MessagingDescriptionSerializer<TMessagingDescription> Serializer;

		private readonly DataContractSerializer serializer;

		private readonly Dictionary<string, DataContractSerializer> registrationSerializers;

		static MessagingDescriptionSerializer()
		{
			MessagingDescriptionSerializer<TMessagingDescription>.Serializer = new MessagingDescriptionSerializer<TMessagingDescription>();
		}

		public MessagingDescriptionSerializer()
		{
			this.serializer = this.CreateSerializer<TMessagingDescription>();
			this.registrationSerializers = new Dictionary<string, DataContractSerializer>()
			{
				{ typeof(WindowsRegistrationDescription).Name, this.CreateSerializer<WindowsRegistrationDescription>() },
				{ typeof(WindowsTemplateRegistrationDescription).Name, this.CreateSerializer<WindowsTemplateRegistrationDescription>() },
				{ typeof(AppleRegistrationDescription).Name, this.CreateSerializer<AppleRegistrationDescription>() },
				{ typeof(AppleTemplateRegistrationDescription).Name, this.CreateSerializer<AppleTemplateRegistrationDescription>() },
				{ typeof(GcmRegistrationDescription).Name, this.CreateSerializer<GcmRegistrationDescription>() },
				{ typeof(GcmTemplateRegistrationDescription).Name, this.CreateSerializer<GcmTemplateRegistrationDescription>() },
				{ typeof(MpnsRegistrationDescription).Name, this.CreateSerializer<MpnsRegistrationDescription>() },
				{ typeof(MpnsTemplateRegistrationDescription).Name, this.CreateSerializer<MpnsTemplateRegistrationDescription>() },
				{ typeof(AdmRegistrationDescription).Name, this.CreateSerializer<AdmRegistrationDescription>() },
				{ typeof(AdmTemplateRegistrationDescription).Name, this.CreateSerializer<AdmTemplateRegistrationDescription>() },
				{ typeof(NokiaXRegistrationDescription).Name, this.CreateSerializer<NokiaXRegistrationDescription>() },
				{ typeof(NokiaXTemplateRegistrationDescription).Name, this.CreateSerializer<NokiaXTemplateRegistrationDescription>() },
				{ typeof(BaiduRegistrationDescription).Name, this.CreateSerializer<BaiduRegistrationDescription>() },
				{ typeof(BaiduTemplateRegistrationDescription).Name, this.CreateSerializer<BaiduTemplateRegistrationDescription>() }
			};
		}

		private DataContractSerializer CreateSerializer<T>()
		{
			return new DataContractSerializer(typeof(T), null, 256, false, false, null);
		}

		public TMessagingDescription Deserialize(string description)
		{
			TMessagingDescription tMessagingDescription;
			TMessagingDescription tMessagingDescription1;
			if (description == null)
			{
				throw new ArgumentNullException("description");
			}
			XmlReaderSettings xmlReaderSetting = new XmlReaderSettings()
			{
				ValidationType = ValidationType.None
			};
			if (typeof(TMessagingDescription) != typeof(RegistrationDescription))
			{
				using (XmlReader xmlReader = XmlReader.Create(new StringReader(description), xmlReaderSetting))
				{
					tMessagingDescription = (TMessagingDescription)this.serializer.ReadObject(xmlReader);
				}
				return tMessagingDescription;
			}
			Dictionary<string, DataContractSerializer>.Enumerator enumerator = this.registrationSerializers.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<string, DataContractSerializer> current = enumerator.Current;
					using (XmlReader xmlReader1 = XmlReader.Create(new StringReader(description), xmlReaderSetting))
					{
						if (xmlReader1.ReadToDescendant(current.Key))
						{
							tMessagingDescription1 = (TMessagingDescription)current.Value.ReadObject(xmlReader1);
							return tMessagingDescription1;
						}
					}
				}
				throw new SerializationException();
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return tMessagingDescription1;
		}

		public TMessagingDescription DeserializeFromAtomFeed(Stream stream)
		{
			TMessagingDescription tMessagingDescription;
			TMessagingDescription tMessagingDescription1;
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			XmlReaderSettings xmlReaderSetting = new XmlReaderSettings()
			{
				ValidationType = ValidationType.None
			};
			if (typeof(TMessagingDescription) != typeof(RegistrationDescription))
			{
				using (XmlReader xmlReader = XmlReader.Create(stream, xmlReaderSetting))
				{
					xmlReader.ReadToDescendant(typeof(TMessagingDescription).Name);
					tMessagingDescription = (TMessagingDescription)this.serializer.ReadObject(xmlReader);
				}
				return tMessagingDescription;
			}
			MemoryStream memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			Dictionary<string, DataContractSerializer>.Enumerator enumerator = this.registrationSerializers.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<string, DataContractSerializer> current = enumerator.Current;
					memoryStream.Seek((long)0, SeekOrigin.Begin);
					using (XmlReader xmlReader1 = XmlReader.Create(memoryStream, xmlReaderSetting))
					{
						if (xmlReader1.ReadToDescendant(current.Key))
						{
							tMessagingDescription1 = (TMessagingDescription)current.Value.ReadObject(xmlReader1);
							return tMessagingDescription1;
						}
					}
				}
				throw new SerializationException();
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return tMessagingDescription1;
		}

		public string Serialize(TMessagingDescription description)
		{
			StringBuilder stringBuilder = new StringBuilder(512, 24576);
			XmlWriterSettings xmlWriterSetting = new XmlWriterSettings()
			{
				ConformanceLevel = ConformanceLevel.Fragment,
				NamespaceHandling = NamespaceHandling.OmitDuplicates
			};
			using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, xmlWriterSetting))
			{
				this.serializer.WriteObject(xmlWriter, description);
			}
			return stringBuilder.ToString();
		}
	}
}
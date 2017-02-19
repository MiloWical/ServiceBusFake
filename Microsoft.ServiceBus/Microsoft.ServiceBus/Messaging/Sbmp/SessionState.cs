using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[XmlRoot("SessionState", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class SessionState : IXmlSerializable, IDisposable
	{
		private bool disposed;

		public System.IO.Stream Stream
		{
			get;
			set;
		}

		internal SessionState(System.IO.Stream sessionState)
		{
			this.Stream = sessionState;
		}

		private SessionState() : this(null)
		{
		}

		private static void DeserializeGroupState(SessionState messageGroupState, XmlReader reader)
		{
			int num;
			byte[] numArray = SessionState.ReadBytes(reader, 9);
			long num1 = BitConverter.ToInt64(numArray, 1);
			if (num1 == (long)0)
			{
				return;
			}
			InternalBufferManager bufferManager = ThrottledBufferManager.GetBufferManager();
			using (BufferedOutputStream bufferedOutputStream = new BufferedOutputStream(1024, 2147483647, bufferManager))
			{
				byte[] numArray1 = bufferManager.TakeBuffer(1024);
				long num2 = (long)0;
				try
				{
					while (true)
					{
						int num3 = reader.ReadContentAsBase64(numArray1, 0, (int)numArray1.Length);
						if (num3 == 0)
						{
							break;
						}
						num2 = num2 + (long)num3;
						bufferedOutputStream.Write(numArray1, 0, num3);
					}
				}
				finally
				{
					bufferManager.ReturnBuffer(numArray1);
				}
				byte[] array = bufferedOutputStream.ToArray(out num);
				messageGroupState.Stream = new BufferedInputStream(array, num, bufferManager);
				if (num1 > (long)0 && num2 != num1)
				{
					throw Fx.Exception.AsError(new InvalidOperationException(SRClient.FailedToDeSerializeEntireSessionStateStream), null);
				}
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing && this.Stream != null)
				{
					this.Stream.Dispose();
					this.Stream = null;
				}
				this.disposed = true;
			}
		}

		private static byte[] ReadBytes(XmlReader reader, int bytesToRead)
		{
			int num;
			byte[] numArray = new byte[bytesToRead];
			int num1 = 0;
			do
			{
				if (num1 >= bytesToRead)
				{
					break;
				}
				num = reader.ReadContentAsBase64(numArray, num1, (int)numArray.Length - num1);
				num1 = num1 + num;
			}
			while (num != 0);
			if (num1 < bytesToRead)
			{
				throw Fx.Exception.AsError(new InvalidOperationException("Insufficient data in the byte-stream"), null);
			}
			return numArray;
		}

		private static void SerializeSessionState(SessionState sessionState, XmlWriter writer)
		{
			writer.WriteBase64(new byte[] { 1 }, 0, 1);
			long num = (long)0;
			if (sessionState.Stream != null)
			{
				num = (sessionState.Stream.CanSeek ? sessionState.Stream.Length : (long)-1);
			}
			writer.WriteBase64(BitConverter.GetBytes(num), 0, 8);
			if (sessionState.Stream != null)
			{
				if (sessionState.Stream.CanSeek && sessionState.Stream.Position != (long)0)
				{
					throw Fx.Exception.AsError(new InvalidOperationException(SRClient.CannotSerializeSessionStateWithPartiallyConsumedStream), null);
				}
				byte[] numArray = new byte[1024];
				long num1 = (long)0;
				while (true)
				{
					int num2 = sessionState.Stream.Read(numArray, 0, (int)numArray.Length);
					if (num2 == 0)
					{
						break;
					}
					num1 = num1 + (long)num2;
					writer.WriteBase64(numArray, 0, num2);
				}
				if (num > (long)0 && num1 != num)
				{
					throw Fx.Exception.AsError(new InvalidOperationException(SRClient.FailedToSerializeEntireSessionStateStream), null);
				}
			}
		}

		XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema()
		{
			return null;
		}

		void System.Xml.Serialization.IXmlSerializable.ReadXml(XmlReader reader)
		{
			reader.Read();
			reader.ReadStartElement();
			SessionState.DeserializeGroupState(this, reader);
			reader.ReadEndElement();
		}

		void System.Xml.Serialization.IXmlSerializable.WriteXml(XmlWriter writer)
		{
			writer.WriteStartElement("SessionState");
			SessionState.SerializeSessionState(this, writer);
			writer.WriteEndElement();
		}

		private enum FieldId : byte
		{
			SessionState = 1
		}
	}
}
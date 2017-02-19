using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;

namespace Microsoft.ServiceBus
{
	internal class ProbingClient
	{
		private IPEndPoint probeAddress;

		private int[] probePorts;

		public ProbingClient(string hostName, int[] probePorts)
		{
			this.probeAddress = ProbingClient.GetAddress(hostName, probePorts[0]);
			this.probePorts = probePorts;
		}

		private static IPEndPoint GetAddress(string hostName, int port)
		{
			IPAddress[] hostAddresses = Dns.GetHostAddresses(hostName);
			for (int i = 0; i < (int)hostAddresses.Length; i++)
			{
				if (hostAddresses[i].AddressFamily == AddressFamily.InterNetwork)
				{
					return new IPEndPoint(hostAddresses[i], port);
				}
			}
			throw new InvalidOperationException(SRClient.NoValidHostAddress);
		}

		public bool PredictNextExternalPort(DerivedEndpoint derivedEndpoint)
		{
			return ProbingClient.PredictNextExternalPort(this.probeAddress, this.probePorts, derivedEndpoint);
		}

		public static bool PredictNextExternalPort(IPEndPoint probeAddress, int[] probePorts, DerivedEndpoint derivedEndpoint)
		{
			bool flag;
			try
			{
				IPEndPoint pEndPoint = new IPEndPoint(IPAddress.Any, 0);
				IPEndPoint pEndPoint1 = new IPEndPoint(IPAddress.Any, 0);
				ProbingClient.SendProbeMessage(probeAddress, ProbeMessageType.GetServerAddress, ref pEndPoint, ref pEndPoint1);
				IPEndPoint pEndPoint2 = new IPEndPoint(pEndPoint1.Address, probePorts[0]);
				IPEndPoint pEndPoint3 = new IPEndPoint(pEndPoint1.Address, probePorts[1]);
				IPEndPoint pEndPoint4 = new IPEndPoint(IPAddress.Any, 0);
				IPEndPoint pEndPoint5 = new IPEndPoint(IPAddress.Any, 0);
				pEndPoint = new IPEndPoint(IPAddress.Any, 0);
				ProbingClient.SendProbeMessage(pEndPoint2, ProbeMessageType.GetClientAddress, ref pEndPoint, ref pEndPoint4);
				if (pEndPoint.Address.GetRawIPv4Address() == pEndPoint4.Address.GetRawIPv4Address())
				{
					pEndPoint.Port = 0;
				}
				ProbingClient.SendProbeMessage(pEndPoint3, ProbeMessageType.GetClientAddress, ref pEndPoint, ref pEndPoint5);
				if (pEndPoint4.Address.GetRawIPv4Address() != pEndPoint5.Address.GetRawIPv4Address())
				{
					flag = false;
				}
				else if (pEndPoint.Address.GetRawIPv4Address() != pEndPoint4.Address.GetRawIPv4Address())
				{
					derivedEndpoint.LocalEndpoint = pEndPoint;
					derivedEndpoint.ExternalEndpoint = pEndPoint4;
					if (pEndPoint4.Port != pEndPoint5.Port)
					{
						int port = pEndPoint5.Port - pEndPoint4.Port;
						if (pEndPoint5.Port + port <= 65535)
						{
							derivedEndpoint.ExternalEndpoint.Port = pEndPoint5.Port + port;
						}
						else
						{
							flag = false;
							return flag;
						}
					}
					flag = true;
				}
				else
				{
					pEndPoint.Port = 0;
					derivedEndpoint.LocalEndpoint = pEndPoint;
					derivedEndpoint.ExternalEndpoint = pEndPoint;
					flag = true;
				}
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		private static void SendProbeMessage(IPEndPoint probeServer, ProbeMessageType messageType, ref IPEndPoint localEndpoint, ref IPEndPoint returnedEndpoint)
		{
			int num;
			Socket socket = new Socket(probeServer.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			try
			{
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
				socket.Bind(localEndpoint);
				socket.Connect(probeServer);
				if (localEndpoint.Port == 0)
				{
					localEndpoint = (IPEndPoint)socket.LocalEndPoint;
				}
			}
			catch
			{
				socket.Close();
				throw;
			}
			byte[] bytes = Encoding.UTF8.GetBytes(ConnectConstants.ProbeType);
			byte[] numArray = BitConverter.GetBytes((int)bytes.Length);
			socket.Send(numArray);
			socket.Send(bytes);
			socket.Send(BitConverter.GetBytes((int)messageType));
			byte[] numArray1 = new byte[256];
			try
			{
				num = socket.Receive(numArray1);
			}
			finally
			{
				socket.Close();
			}
			if (num != 12)
			{
				throw new CommunicationException(SRClient.InvalidLengthofReceivedContent);
			}
			BinaryReader binaryReader = new BinaryReader(new MemoryStream(numArray1, 0, num));
			returnedEndpoint.Address = new IPAddress(binaryReader.ReadInt64());
			returnedEndpoint.Port = binaryReader.ReadInt32();
		}
	}
}
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading;

namespace Microsoft.ServiceBus
{
	[CallbackBehavior(ConcurrencyMode=ConcurrencyMode.Multiple)]
	internal class DirectConnectionSession : IDirectConnectionControl, IDirectConnectionParent
	{
		public const int MaxAttempts = 4;

		private DirectConnectionSession.Activity activity;

		private int attempt;

		private IDirectConnectionControl channel;

		private bool complete;

		private bool isClosed;

		private Guid id;

		private object mutex;

		private IDirectConnectionParent parent;

		private Microsoft.ServiceBus.ProbingClient probingClient;

		public int Attempts
		{
			get
			{
				return this.attempt;
			}
			set
			{
				this.attempt = value;
			}
		}

		public IDirectConnectionControl Channel
		{
			get
			{
				return this.channel;
			}
			set
			{
				this.channel = value;
			}
		}

		internal Microsoft.ServiceBus.Tracing.EventTraceActivity EventTraceActivity
		{
			get;
			private set;
		}

		public Guid Id
		{
			get
			{
				return this.id;
			}
		}

		public Microsoft.ServiceBus.ProbingClient ProbingClient
		{
			get
			{
				return this.probingClient;
			}
		}

		private object ThisLock
		{
			get
			{
				return this.mutex;
			}
		}

		public DirectConnectionSession(Guid id, Microsoft.ServiceBus.ProbingClient probingClient, IDirectConnectionParent parent)
		{
			this.id = id;
			this.EventTraceActivity = new Microsoft.ServiceBus.Tracing.EventTraceActivity(this.id);
			this.probingClient = probingClient;
			this.parent = parent;
			this.mutex = new object();
		}

		public void Abort()
		{
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.isClosed = true;
					this.complete = true;
					if (this.activity != null)
					{
						this.activity.Dispose();
					}
				}
			}
		}

		public void Close(TimeSpan timeout)
		{
			this.Abort();
		}

		public void Failure(object sender, Exception exception)
		{
			lock (this.ThisLock)
			{
				if (!this.complete)
				{
					this.complete = true;
				}
				else
				{
					return;
				}
			}
			this.parent.Failure(this, exception);
		}

		public void Initiate()
		{
			this.StartActivity(new DirectConnectionSession.ConnectActivity(this));
		}

		public void Listen()
		{
			this.StartActivity(new DirectConnectionSession.ListenActivity(this));
		}

		void Microsoft.ServiceBus.IDirectConnectionControl.Abort(AbortMessage request)
		{
			this.activity.Abort(request);
		}

		void Microsoft.ServiceBus.IDirectConnectionControl.Connect(DirectConnectMessage request)
		{
			this.activity.Connect(request);
		}

		void Microsoft.ServiceBus.IDirectConnectionControl.ConnectResponse(ConnectResponseMessage request)
		{
			this.activity.ConnectResponse(request);
		}

		void Microsoft.ServiceBus.IDirectConnectionControl.ConnectRetry(ConnectRetryMessage request)
		{
			this.activity.ConnectRetry(request);
		}

		void Microsoft.ServiceBus.IDirectConnectionControl.SwitchRoles(SwitchRolesMessage request)
		{
			this.activity.SwitchRoles(request);
		}

		private void StartActivity(DirectConnectionSession.Activity activity)
		{
			lock (this.ThisLock)
			{
				if (this.activity != null)
				{
					this.activity.Dispose();
				}
				this.activity = activity;
			}
			try
			{
				this.activity.Start();
			}
			catch (Exception exception)
			{
				this.Failure(this, exception);
			}
		}

		public void Success(object sender, Socket socket)
		{
			lock (this.ThisLock)
			{
				if (!this.complete)
				{
					this.complete = true;
				}
				else
				{
					socket.Close();
					return;
				}
			}
			this.parent.Success(this, socket);
		}

		private abstract class Activity : IDirectConnectionControl, IDirectConnectionParent, IDisposable
		{
			private DirectConnectionCandidateManager candidates;

			private bool complete;

			private bool disposed;

			private IDirectConnectionParent parent;

			private DirectConnectionSession session;

			protected DirectConnectionCandidateManager Candidates
			{
				get
				{
					return this.candidates;
				}
			}

			protected DirectConnectionSession Session
			{
				get
				{
					return this.session;
				}
			}

			protected object ThisLock
			{
				get
				{
					return this.session.ThisLock;
				}
			}

			protected Activity(DirectConnectionSession session)
			{
				this.session = session;
				this.parent = session;
				this.candidates = new DirectConnectionCandidateManager(this);
			}

			public virtual void Abort(AbortMessage request)
			{
				this.Failure(new CommunicationException("Connect attempt aborted"));
			}

			public virtual void Connect(DirectConnectMessage request)
			{
				throw new FaultException(SRClient.InvalidCallFaultException);
			}

			public virtual void ConnectResponse(ConnectResponseMessage request)
			{
				throw new FaultException(SRClient.InvalidCallFaultException);
			}

			public virtual void ConnectRetry(ConnectRetryMessage request)
			{
				throw new FaultException(SRClient.InvalidCallFaultException);
			}

			public void Dispose()
			{
				lock (this.ThisLock)
				{
					if (!this.disposed)
					{
						this.disposed = true;
						this.complete = true;
					}
					else
					{
						return;
					}
				}
				try
				{
					this.OnDispose();
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
				}
				GC.SuppressFinalize(this);
			}

			public void Failure(Exception exception)
			{
				lock (this.ThisLock)
				{
					if (!this.complete)
					{
						this.complete = true;
					}
					else
					{
						return;
					}
				}
				this.parent.Failure(this, exception);
			}

			void Microsoft.ServiceBus.IDirectConnectionParent.Failure(object sender, Exception exception)
			{
				lock (this.ThisLock)
				{
					if (this.complete)
					{
						return;
					}
				}
				this.OnFailure(exception);
			}

			void Microsoft.ServiceBus.IDirectConnectionParent.Success(object sender, Socket socket)
			{
				lock (this.ThisLock)
				{
					if (this.complete)
					{
						socket.Close();
						return;
					}
				}
				if (this.ValidateSocket(socket))
				{
					this.OnSuccess(socket);
					return;
				}
				this.OnFailure(new CommunicationException("Socket validation failed"));
			}

			protected virtual void OnDispose()
			{
				this.candidates.Dispose();
			}

			protected virtual void OnFailure(Exception exception)
			{
				this.Failure(exception);
			}

			protected virtual void OnSuccess(Socket socket)
			{
				this.Success(socket);
			}

			public abstract void Start();

			public void Success(Socket socket)
			{
				lock (this.ThisLock)
				{
					if (!this.complete)
					{
						this.complete = true;
					}
					else
					{
						socket.Close();
						return;
					}
				}
				this.parent.Success(this, socket);
			}

			public virtual void SwitchRoles(SwitchRolesMessage request)
			{
				throw new FaultException(SRClient.InvalidCallFaultException);
			}

			protected abstract bool ValidateSocket(Socket socket);
		}

		private class ConnectActivity : DirectConnectionSession.Activity
		{
			private const int ConnectTimeout = 15;

			private const int WSAEADDRINUSE = 10048;

			private ManualResetEvent completeEvent;

			private DerivedEndpoint localDerivedEndpoint;

			private List<IPEndPoint> localEndpoints;

			private IDisposable localListener;

			private Socket socket;

			public ConnectActivity(DirectConnectionSession session) : base(session)
			{
				this.completeEvent = new ManualResetEvent(false);
				this.localDerivedEndpoint = new DerivedEndpoint();
				this.localEndpoints = new List<IPEndPoint>();
			}

			private void ClientSendConnectRequest(IPEndPoint externalEndpoint, List<IPEndPoint> localEndpoints)
			{
				AddressCandidates addressCandidate = new AddressCandidates();
				addressCandidate.AddEndpoints(AddressType.External, new IPEndPoint[] { externalEndpoint });
				addressCandidate.AddEndpoints(AddressType.Local, localEndpoints.ToArray());
				try
				{
					DirectConnectionSession session = base.Session;
					int attempts = session.Attempts;
					int num = attempts;
					session.Attempts = attempts + 1;
					if (num != 0)
					{
						IDirectConnectionControl channel = base.Session.Channel;
						Guid id = base.Session.Id;
						channel.ConnectRetry(new ConnectRetryMessage(id.ToString(), addressCandidate));
					}
					else
					{
						IDirectConnectionControl directConnectionControl = base.Session.Channel;
						Guid guid = base.Session.Id;
						directConnectionControl.Connect(new DirectConnectMessage(guid.ToString(), addressCandidate));
					}
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
				}
			}

			private bool ClientStartConnecting(ref IPEndPoint localEndpoint, IPEndPoint remoteExternalEndpoint, out Exception exception)
			{
				if (!base.Candidates.StartConnecting(ref localEndpoint, remoteExternalEndpoint, "Connector.ServerExternalEndpoint", out exception))
				{
					return false;
				}
				return true;
			}

			private void ClientStartListening(DerivedEndpoint localDerivedEndpoint, List<IPEndPoint> localEndpoints)
			{
				bool flag = false;
				if (base.Session.ProbingClient.PredictNextExternalPort(localDerivedEndpoint))
				{
					if (!base.Candidates.StartListening(ref localDerivedEndpoint.LocalEndpoint, "Listener.ClientLocalEndpoint", out this.localListener))
					{
						return;
					}
					if (localDerivedEndpoint.ExternalEndpoint.Port == 0)
					{
						localDerivedEndpoint.ExternalEndpoint.Port = localDerivedEndpoint.LocalEndpoint.Port;
					}
					localEndpoints.Add(localDerivedEndpoint.LocalEndpoint);
					flag = true;
				}
				List<IPAddress> localAddresses = DirectConnectionSession.ConnectActivity.GetLocalAddresses();
				for (int i = 0; i < localAddresses.Count; i++)
				{
					if (!flag || localAddresses[i].GetRawIPv4Address() != localDerivedEndpoint.LocalEndpoint.Address.GetRawIPv4Address())
					{
						IPEndPoint pEndPoint = new IPEndPoint(localAddresses[i], 0);
						if (!base.Candidates.StartListening(ref pEndPoint, "Listener.ClientRealAddress"))
						{
							return;
						}
						localEndpoints.Add(pEndPoint);
					}
				}
				Thread.Sleep(200);
			}

			public override void ConnectResponse(ConnectResponseMessage request)
			{
				Exception exception;
				IPEndPoint endpoint = request.Addresses.GetEndpoint(AddressType.External);
				if (!this.ClientStartConnecting(ref this.localDerivedEndpoint.LocalEndpoint, endpoint, out exception))
				{
					SocketException socketException = exception as SocketException;
					if (socketException != null && socketException.ErrorCode == 10048)
					{
						this.localListener.Dispose();
						this.ClientStartConnecting(ref this.localDerivedEndpoint.LocalEndpoint, endpoint, out exception);
					}
				}
				IOThreadScheduler.ScheduleCallbackNoFlow(new Action<object>(this.WaitForTimeout), null);
			}

			public static List<IPAddress> GetLocalAddresses()
			{
				List<IPAddress> pAddresses = new List<IPAddress>();
				NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
				for (int i = 0; i < (int)allNetworkInterfaces.Length; i++)
				{
					NetworkInterface networkInterface = allNetworkInterfaces[i];
					if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
					{
						foreach (UnicastIPAddressInformation unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
						{
							if (unicastAddress.Address.AddressFamily != AddressFamily.InterNetwork)
							{
								continue;
							}
							pAddresses.Add(unicastAddress.Address);
						}
					}
				}
				return pAddresses;
			}

			protected override void OnFailure(Exception exception)
			{
				this.completeEvent.Set();
			}

			protected override void OnSuccess(Socket socket)
			{
				this.socket = socket;
				this.completeEvent.Set();
			}

			public override void Start()
			{
				if (base.Session.Attempts == 4)
				{
					try
					{
						IDirectConnectionControl channel = base.Session.Channel;
						Guid id = base.Session.Id;
						channel.Abort(new AbortMessage(id.ToString()));
					}
					catch (Exception exception)
					{
						if (Fx.IsFatal(exception))
						{
							throw;
						}
					}
					throw new CommunicationException(SRClient.MaximumAttemptsExceeded);
				}
				this.ClientStartListening(this.localDerivedEndpoint, this.localEndpoints);
				this.ClientSendConnectRequest(this.localDerivedEndpoint.ExternalEndpoint, this.localEndpoints);
			}

			protected override bool ValidateSocket(Socket socket)
			{
				bool flag;
				try
				{
					Guid id = base.Session.Id;
					socket.Send(id.ToByteArray());
					byte[] numArray = new byte[1];
					if (socket.Receive(numArray, 0, 1, SocketFlags.None) < 1 || numArray[0] != 1)
					{
						throw new CommunicationException(SRClient.InvalidReceivedContent);
					}
					return true;
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					socket.Close();
					flag = false;
				}
				return flag;
			}

			private void WaitForTimeout(object state)
			{
				if (this.completeEvent.WaitOne(TimeSpan.FromSeconds(15), false) && this.socket != null)
				{
					base.Success(this.socket);
					return;
				}
				base.Session.StartActivity(new DirectConnectionSession.ListenActivity(base.Session));
				try
				{
					IDirectConnectionControl channel = base.Session.Channel;
					Guid id = base.Session.Id;
					channel.SwitchRoles(new SwitchRolesMessage(id.ToString()));
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
				}
			}
		}

		private class ListenActivity : DirectConnectionSession.Activity
		{
			private bool connecting;

			private DerivedEndpoint localDerivedEndpoint;

			private List<IPEndPoint> localEndpoints;

			public ListenActivity(DirectConnectionSession session) : base(session)
			{
				this.localDerivedEndpoint = new DerivedEndpoint();
				this.localEndpoints = new List<IPEndPoint>();
			}

			public override void Connect(DirectConnectMessage request)
			{
				this.Connect(request.Addresses);
			}

			public void Connect(AddressCandidates addresses)
			{
				lock (base.ThisLock)
				{
					if (this.connecting)
					{
						throw new FaultException(SRClient.InvalidCallFaultException);
					}
					this.connecting = true;
				}
				try
				{
					this.localEndpoints = addresses.GetEndpoints(AddressType.Local);
					IPEndPoint endpoint = addresses.GetEndpoint(AddressType.External);
					this.ServerStartConnectingLocal(this.localEndpoints);
					this.ServerStartConnectingExternal(this.localDerivedEndpoint, endpoint);
					IOThreadScheduler.ScheduleCallbackNoFlow(new Action<object>(this.SendResponse), null);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					base.Failure(exception);
					throw new FaultException(exception.ToString());
				}
			}

			public override void ConnectRetry(ConnectRetryMessage request)
			{
				this.Connect(request.Addresses);
			}

			private void SendResponse(object state)
			{
				try
				{
					AddressCandidates addressCandidate = new AddressCandidates();
					IPEndPoint[] externalEndpoint = new IPEndPoint[] { this.localDerivedEndpoint.ExternalEndpoint };
					addressCandidate.AddEndpoints(AddressType.External, externalEndpoint);
					IDirectConnectionControl channel = base.Session.Channel;
					Guid id = base.Session.Id;
					channel.ConnectResponse(new ConnectResponseMessage(id.ToString(), addressCandidate));
				}
				catch (Exception exception)
				{
					base.Failure(exception);
				}
			}

			private void ServerStartConnectingExternal(DerivedEndpoint localDerivedEndpoint, IPEndPoint remoteExternalEndpoint)
			{
				if (base.Session.ProbingClient.PredictNextExternalPort(localDerivedEndpoint))
				{
					if (!base.Candidates.StartConnecting(ref localDerivedEndpoint.LocalEndpoint, remoteExternalEndpoint, "Connector.ClientExternalEndpoint"))
					{
						return;
					}
					base.Candidates.StartListening(ref localDerivedEndpoint.LocalEndpoint, "Listener.ServerLocalEndpoint");
					if (localDerivedEndpoint.ExternalEndpoint.Port == 0)
					{
						localDerivedEndpoint.ExternalEndpoint.Port = localDerivedEndpoint.LocalEndpoint.Port;
					}
				}
			}

			private void ServerStartConnectingLocal(List<IPEndPoint> localEndpoints)
			{
				NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
				for (int i = 0; i < (int)allNetworkInterfaces.Length; i++)
				{
					NetworkInterface networkInterface = allNetworkInterfaces[i];
					if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
					{
						foreach (UnicastIPAddressInformation unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
						{
							if (unicastAddress.Address.AddressFamily != AddressFamily.InterNetwork || unicastAddress.IPv4Mask == null)
							{
								continue;
							}
							IPEndPoint pEndPoint = new IPEndPoint(unicastAddress.Address, 0);
							for (int j = 0; j < localEndpoints.Count; j++)
							{
								IPEndPoint item = localEndpoints[j];
								if ((unicastAddress.IPv4Mask.GetRawIPv4Address() & pEndPoint.Address.GetRawIPv4Address()) == (unicastAddress.IPv4Mask.GetRawIPv4Address() & item.Address.GetRawIPv4Address()))
								{
									base.Candidates.StartConnecting(ref pEndPoint, localEndpoints[j], "Connector.ClientLocalAddress");
								}
							}
						}
					}
				}
			}

			public override void Start()
			{
			}

			public override void SwitchRoles(SwitchRolesMessage request)
			{
				base.Session.StartActivity(new DirectConnectionSession.ConnectActivity(base.Session));
			}

			protected override bool ValidateSocket(Socket socket)
			{
				bool flag;
				try
				{
					byte[] numArray = new byte[16];
					if (socket.Receive(numArray, 0, 16, SocketFlags.None) != 16)
					{
						throw new CommunicationException(SRClient.InvalidLengthofReceivedContent);
					}
					if (new Guid(numArray) != base.Session.Id)
					{
						throw new CommunicationException(SRClient.InvalidReceivedSessionId);
					}
					socket.Send(new byte[] { 1 });
					return true;
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					socket.Close();
					flag = false;
				}
				return flag;
			}
		}
	}
}
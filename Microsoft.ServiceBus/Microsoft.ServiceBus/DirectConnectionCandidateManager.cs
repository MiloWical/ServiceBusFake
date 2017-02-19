using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.ServiceBus
{
	internal class DirectConnectionCandidateManager : IDirectConnectionParent, IDisposable
	{
		private readonly object mutex;

		private readonly List<DirectConnectionCandidateManager.Candidate> candidates;

		private readonly IDirectConnectionParent parent;

		private bool complete;

		private bool disposed;

		private int refCount;

		private object ThisLock
		{
			get
			{
				return this.mutex;
			}
		}

		public DirectConnectionCandidateManager(IDirectConnectionParent parent)
		{
			this.parent = parent;
			this.candidates = new List<DirectConnectionCandidateManager.Candidate>();
			this.mutex = new object();
		}

		public void AddRef()
		{
			lock (this.ThisLock)
			{
				DirectConnectionCandidateManager directConnectionCandidateManager = this;
				directConnectionCandidateManager.refCount = directConnectionCandidateManager.refCount + 1;
			}
		}

		public void Dispose()
		{
			lock (this.ThisLock)
			{
				if (!this.disposed)
				{
					this.disposed = true;
					this.complete = true;
					for (int i = 0; i < this.candidates.Count; i++)
					{
						try
						{
							this.candidates[i].Dispose();
						}
						catch
						{
						}
					}
					this.candidates.Clear();
				}
				else
				{
					return;
				}
			}
			GC.SuppressFinalize(this);
		}

		private void Failure(object sender, Exception exception)
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
			DirectConnectionCandidateManager.Candidate candidate = (DirectConnectionCandidateManager.Candidate)sender;
			bool flag = false;
			lock (this.ThisLock)
			{
				this.candidates.Remove(candidate);
				candidate.Dispose();
				if (this.refCount == 0 && this.candidates.Count == 0 && !this.complete)
				{
					if (!this.complete)
					{
						this.complete = true;
						flag = true;
					}
					else
					{
						return;
					}
				}
			}
			if (flag)
			{
				this.Failure(this, new Exception("Connect failed"));
			}
		}

		void Microsoft.ServiceBus.IDirectConnectionParent.Success(object sender, Socket socket)
		{
			DirectConnectionCandidateManager.Candidate candidate = (DirectConnectionCandidateManager.Candidate)sender;
			bool flag = false;
			lock (this.ThisLock)
			{
				this.candidates.Remove(candidate);
				candidate.Dispose();
				if (!this.complete)
				{
					this.complete = true;
					flag = true;
				}
				else
				{
					socket.Close();
					return;
				}
			}
			if (flag)
			{
				this.parent.Success(this, socket);
			}
		}

		public void Release()
		{
			bool flag = false;
			lock (this.ThisLock)
			{
				DirectConnectionCandidateManager directConnectionCandidateManager = this;
				directConnectionCandidateManager.refCount = directConnectionCandidateManager.refCount - 1;
				if (this.refCount == 0 && this.candidates.Count == 0 && !this.complete)
				{
					if (!this.complete)
					{
						this.complete = true;
						flag = true;
					}
					else
					{
						return;
					}
				}
			}
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
			if (flag)
			{
				this.parent.Failure(this, new Exception("Connect failed"));
			}
		}

		public bool StartConnecting(ref IPEndPoint localEndpoint, IPEndPoint serverAddress, string type)
		{
			Exception exception;
			return this.StartConnecting(ref localEndpoint, serverAddress, type, out exception);
		}

		public bool StartConnecting(ref IPEndPoint localEndpoint, IPEndPoint serverAddress, string type, out Exception exception)
		{
			bool flag;
			exception = null;
			DirectConnectionCandidateManager.ConnectCandidate connectCandidate = null;
			lock (this.ThisLock)
			{
				if (!this.complete)
				{
					connectCandidate = new DirectConnectionCandidateManager.ConnectCandidate(type, this);
					this.candidates.Add(connectCandidate);
					return connectCandidate.Start(ref localEndpoint, serverAddress, out exception);
				}
				else
				{
					flag = false;
				}
			}
			return flag;
		}

		public bool StartListening(ref IPEndPoint listenAddress, string type)
		{
			IDisposable disposable;
			return this.StartListening(ref listenAddress, type, out disposable);
		}

		public bool StartListening(ref IPEndPoint listenAddress, string type, out IDisposable candidate)
		{
			bool flag;
			candidate = null;
			lock (this.ThisLock)
			{
				if (!this.complete)
				{
					DirectConnectionCandidateManager.ListenCandidate listenCandidate = new DirectConnectionCandidateManager.ListenCandidate(type, this);
					this.candidates.Add(listenCandidate);
					candidate = listenCandidate;
					return listenCandidate.Start(ref listenAddress);
				}
				else
				{
					flag = false;
				}
			}
			return flag;
		}

		private abstract class Candidate : IDisposable
		{
			private bool disposed;

			private bool complete;

			private object mutex;

			private IDirectConnectionParent parent;

			protected object ThisLock
			{
				get
				{
					return this.mutex;
				}
			}

			public Candidate(string type, IDirectConnectionParent parent)
			{
				this.parent = parent;
				this.mutex = new object();
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
				catch
				{
				}
				GC.SuppressFinalize(this);
			}

			protected void Failure(Exception exception)
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

			protected abstract void OnDispose();

			protected void Success(Socket socket)
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
		}

		private class ConnectCandidate : DirectConnectionCandidateManager.Candidate
		{
			private Socket connectSocket;

			public ConnectCandidate(string type, IDirectConnectionParent parent) : base(type, parent)
			{
			}

			private void ConnectCallback(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				this.ConnectComplete(result, false);
			}

			private void ConnectComplete(IAsyncResult result, bool completedSynchronously)
			{
				Socket socket;
				try
				{
					this.connectSocket.EndConnect(result);
					lock (base.ThisLock)
					{
						socket = this.connectSocket;
						this.connectSocket = null;
					}
					if (socket != null)
					{
						base.Success(socket);
					}
				}
				catch (Exception exception)
				{
					base.Failure(exception);
				}
			}

			protected override void OnDispose()
			{
				lock (base.ThisLock)
				{
					if (this.connectSocket != null)
					{
						this.connectSocket.Close();
					}
				}
			}

			public bool Start(ref IPEndPoint localEndpoint, IPEndPoint serverAddress, out Exception exception)
			{
				bool flag;
				exception = null;
				IAsyncResult asyncResult = null;
				try
				{
					this.connectSocket = new Socket(serverAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					this.connectSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
					this.connectSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
					this.connectSocket.Bind(localEndpoint);
					lock (base.ThisLock)
					{
						asyncResult = this.connectSocket.BeginConnect(serverAddress, new AsyncCallback(this.ConnectCallback), null);
						localEndpoint = (IPEndPoint)this.connectSocket.LocalEndPoint;
					}
					if (asyncResult.CompletedSynchronously)
					{
						this.ConnectComplete(asyncResult, true);
					}
					return true;
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					exception = exception1;
					base.Failure(exception1);
					flag = false;
				}
				return flag;
			}
		}

		private class ListenCandidate : DirectConnectionCandidateManager.Candidate
		{
			private Socket listenSocket;

			public ListenCandidate(string type, IDirectConnectionParent parent) : base(type, parent)
			{
			}

			private void AcceptCallback(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				this.AcceptComplete(result, false);
			}

			private void AcceptComplete(IAsyncResult result, bool completedSynchronously)
			{
				try
				{
					if (this.listenSocket != null)
					{
						Socket socket = this.listenSocket.EndAccept(result);
						if (socket != null)
						{
							base.Success(socket);
						}
						else
						{
							return;
						}
					}
				}
				catch (Exception exception)
				{
					base.Failure(exception);
				}
			}

			private void BeginAccept()
			{
				IAsyncResult asyncResult = null;
				try
				{
					asyncResult = this.listenSocket.BeginAccept(new AsyncCallback(this.AcceptCallback), null);
				}
				catch (Exception exception)
				{
					base.Failure(exception);
					return;
				}
				if (asyncResult.CompletedSynchronously)
				{
					this.AcceptComplete(asyncResult, true);
					return;
				}
			}

			protected override void OnDispose()
			{
				this.listenSocket.Close();
				this.listenSocket = null;
			}

			public bool Start(ref IPEndPoint listenAddress)
			{
				bool flag;
				try
				{
					this.listenSocket = new Socket(listenAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					this.listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
					this.listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
					this.listenSocket.Bind(listenAddress);
					this.listenSocket.Listen(10);
					listenAddress = (IPEndPoint)this.listenSocket.LocalEndPoint;
					this.BeginAccept();
					return true;
				}
				catch (Exception exception)
				{
					base.Failure(exception);
					flag = false;
				}
				return flag;
			}
		}
	}
}
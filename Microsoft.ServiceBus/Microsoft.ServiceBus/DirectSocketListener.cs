using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace Microsoft.ServiceBus
{
	[ServiceBehavior(ConcurrencyMode=ConcurrencyMode.Multiple, InstanceContextMode=InstanceContextMode.Single)]
	internal class DirectSocketListener : Microsoft.ServiceBus.Channels.IConnectionListener, IDirectConnectionControl, IDirectConnectionParent
	{
		private readonly int bufferSize;

		private readonly Dictionary<string, DirectConnectionSession> connectionSessions;

		private readonly object mutex;

		private readonly ProbingClient probingClient;

		private readonly ServiceHost serviceHost;

		private readonly InputQueue<Microsoft.ServiceBus.Channels.IConnection> socketQueue;

		private bool isClosed;

		private EventTraceActivity Activity
		{
			get;
			set;
		}

		private object ThisLock
		{
			get
			{
				return this.mutex;
			}
		}

		public DirectSocketListener(int bufferSize, Uri uri, Binding innerBinding)
		{
			this.bufferSize = bufferSize;
			this.probingClient = new ProbingClient(uri.Host, ConnectConstants.DefaultProbePorts);
			Uri[] uriArray = new Uri[] { uri };
			this.serviceHost = new ConfigurationlessServiceHost(this, uriArray);
			ServiceErrorHandlerBehavior serviceErrorHandlerBehavior = new ServiceErrorHandlerBehavior();
			serviceErrorHandlerBehavior.HandleError += new EventHandler<ServiceErrorEventArgs>((object s, ServiceErrorEventArgs e) => Fx.Exception.TraceHandled(e.Exception, "DirectSocketListener.IErrorHandler.HandleError", null));
			this.serviceHost.Description.Behaviors.Add(serviceErrorHandlerBehavior);
			this.serviceHost.AddServiceEndpoint(typeof(IDirectConnectionControl), innerBinding, "");
			this.connectionSessions = new Dictionary<string, DirectConnectionSession>();
			this.socketQueue = new InputQueue<Microsoft.ServiceBus.Channels.IConnection>();
			this.mutex = new object();
			this.Activity = new EventTraceActivity();
		}

		public void Abort()
		{
			List<DirectConnectionSession> directConnectionSessions;
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.isClosed = true;
					directConnectionSessions = new List<DirectConnectionSession>(this.connectionSessions.Values);
					this.connectionSessions.Clear();
				}
				else
				{
					return;
				}
			}
			for (int i = 0; i < directConnectionSessions.Count; i++)
			{
				try
				{
					directConnectionSessions[i].Abort();
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception, "DirectSocketListener.Abort", this.Activity);
				}
			}
			this.serviceHost.Abort();
		}

		public IAsyncResult BeginAccept(AsyncCallback callback, object state)
		{
			return this.socketQueue.BeginDequeue(Microsoft.ServiceBus.ServiceDefaults.ReceiveTimeout, callback, state);
		}

		public void Close(TimeSpan timeout)
		{
			List<DirectConnectionSession> directConnectionSessions;
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.isClosed = true;
					directConnectionSessions = new List<DirectConnectionSession>(this.connectionSessions.Values);
					this.connectionSessions.Clear();
				}
				else
				{
					return;
				}
			}
			for (int i = 0; i < directConnectionSessions.Count; i++)
			{
				try
				{
					directConnectionSessions[i].Close(timeoutHelper.RemainingTime());
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception, "DirectSocketListener.Close", this.Activity);
					directConnectionSessions[i].Abort();
				}
			}
			this.serviceHost.Close(timeoutHelper.RemainingTime());
		}

		public Microsoft.ServiceBus.Channels.IConnection EndAccept(IAsyncResult result)
		{
			return this.socketQueue.EndDequeue(result);
		}

		void Microsoft.ServiceBus.IDirectConnectionControl.Abort(AbortMessage request)
		{
			DirectConnectionSession directConnectionSession;
			lock (this.ThisLock)
			{
				if (!this.connectionSessions.TryGetValue(request.Id, out directConnectionSession))
				{
					throw Fx.Exception.AsError(new FaultException(SRClient.InvalidID), null);
				}
			}
			((IDirectConnectionControl)directConnectionSession).Abort(request);
		}

		void Microsoft.ServiceBus.IDirectConnectionControl.Connect(DirectConnectMessage request)
		{
			DirectConnectionSession directConnectionSession;
			lock (this.ThisLock)
			{
				if (this.isClosed)
				{
					throw Fx.Exception.AsError(new FaultException(SRClient.EndpointNotFound), null);
				}
				if (this.connectionSessions.ContainsKey(request.Id))
				{
					throw Fx.Exception.AsError(new FaultException(SRClient.DuplicateConnectionID), null);
				}
				directConnectionSession = new DirectConnectionSession(new Guid(request.Id), this.probingClient, this)
				{
					Channel = OperationContext.Current.GetCallbackChannel<IDirectConnectionControl>()
				};
				directConnectionSession.Listen();
				this.connectionSessions.Add(request.Id, directConnectionSession);
			}
			((IDirectConnectionControl)directConnectionSession).Connect(request);
		}

		void Microsoft.ServiceBus.IDirectConnectionControl.ConnectResponse(ConnectResponseMessage request)
		{
			DirectConnectionSession directConnectionSession;
			lock (this.ThisLock)
			{
				if (!this.connectionSessions.TryGetValue(request.Id, out directConnectionSession))
				{
					throw Fx.Exception.AsError(new FaultException(SRClient.InvalidID), null);
				}
			}
			((IDirectConnectionControl)directConnectionSession).ConnectResponse(request);
		}

		void Microsoft.ServiceBus.IDirectConnectionControl.ConnectRetry(ConnectRetryMessage request)
		{
			DirectConnectionSession directConnectionSession;
			lock (this.ThisLock)
			{
				if (!this.connectionSessions.TryGetValue(request.Id, out directConnectionSession))
				{
					throw Fx.Exception.AsError(new FaultException(SRClient.InvalidID), null);
				}
			}
			((IDirectConnectionControl)directConnectionSession).ConnectRetry(request);
		}

		void Microsoft.ServiceBus.IDirectConnectionControl.SwitchRoles(SwitchRolesMessage request)
		{
			DirectConnectionSession directConnectionSession;
			lock (this.ThisLock)
			{
				if (!this.connectionSessions.TryGetValue(request.Id, out directConnectionSession))
				{
					throw Fx.Exception.AsError(new FaultException(SRClient.InvalidID), null);
				}
			}
			((IDirectConnectionControl)directConnectionSession).SwitchRoles(request);
		}

		void Microsoft.ServiceBus.IDirectConnectionParent.Failure(object sender, Exception exception)
		{
			DirectConnectionSession directConnectionSession = (DirectConnectionSession)sender;
			Guid id = directConnectionSession.Id;
			lock (this.ThisLock)
			{
				this.connectionSessions.Remove(id.ToString());
			}
			try
			{
				directConnectionSession.Close(Microsoft.ServiceBus.ServiceDefaults.CloseTimeout);
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				EventTraceActivity eventTraceActivity = new EventTraceActivity(id);
				Fx.Exception.TraceHandled(exception1, "DirectSocketListener.Failure", eventTraceActivity);
				directConnectionSession.Abort();
			}
		}

		void Microsoft.ServiceBus.IDirectConnectionParent.Success(object sender, Socket socket)
		{
			DirectConnectionSession directConnectionSession = (DirectConnectionSession)sender;
			Guid id = directConnectionSession.Id;
			EventTraceActivity eventTraceActivity = new EventTraceActivity(id);
			lock (this.ThisLock)
			{
				this.connectionSessions.Remove(id.ToString());
			}
			try
			{
				directConnectionSession.Close(Microsoft.ServiceBus.ServiceDefaults.CloseTimeout);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "DirectSocketListener.Success", eventTraceActivity);
				directConnectionSession.Abort();
			}
			this.socketQueue.EnqueueAndDispatch(new Microsoft.ServiceBus.Channels.SocketConnection(socket, this.bufferSize, eventTraceActivity));
		}

		public void Open(TimeSpan timeout)
		{
			this.serviceHost.Open(timeout);
		}
	}
}
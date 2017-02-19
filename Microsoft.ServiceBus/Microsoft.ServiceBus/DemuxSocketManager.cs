using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.ServiceBus
{
	internal class DemuxSocketManager
	{
		private const int MaxTypeLength = 100;

		private int bufferSize;

		private IConnectionElement innerElement;

		private IConnectionListener innerListener;

		private Dictionary<string, DemuxSocketListener> listenerTable;

		private Uri uri;

		public DemuxSocketManager(IConnectionElement innerElement, int bufferSize, Uri uri)
		{
			this.innerElement = innerElement;
			this.bufferSize = bufferSize;
			this.uri = uri;
			this.listenerTable = new Dictionary<string, DemuxSocketListener>();
		}

		private static void AcceptCallback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			((DemuxSocketManager)result.AsyncState).AcceptComplete(result, false);
		}

		private void AcceptComplete(IAsyncResult result, bool completedSynchronously)
		{
			IConnection connection;
			try
			{
				try
				{
					connection = this.innerListener.EndAccept(result);
					if (connection == null)
					{
						return;
					}
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					IOThreadScheduler.ScheduleCallbackNoFlow(new Action<object>(DemuxSocketManager.StartAcceptingStatic), this);
					return;
				}
				IOThreadScheduler.ScheduleCallbackNoFlow(new Action<object>(DemuxSocketManager.StartAcceptingStatic), this);
				this.ProcessConnection(connection);
			}
			catch (Exception exception1)
			{
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
			}
		}

		private void OnConnectionDequeued()
		{
		}

		private void ProcessConnection(IConnection connection)
		{
			bool flag = false;
			try
			{
				byte[] numArray = new byte[4];
				if (connection.Read(numArray, 0, 4, TimeSpan.FromSeconds(1)) == 4)
				{
					int num = BitConverter.ToInt32(numArray, 0);
					if (num <= 100)
					{
						byte[] numArray1 = new byte[num];
						if (connection.Read(numArray1, 0, num, TimeSpan.FromSeconds(1)) == num)
						{
							string str = Encoding.UTF8.GetString(numArray1);
							DemuxSocketListener demuxSocketListener = null;
							lock (this.listenerTable)
							{
								if (!this.listenerTable.TryGetValue(str, out demuxSocketListener))
								{
									return;
								}
							}
							demuxSocketListener.EnqueueConnection(connection, new Action(this.OnConnectionDequeued));
							flag = true;
						}
					}
				}
			}
			finally
			{
				if (!flag)
				{
					connection.Close(TimeSpan.FromSeconds(1));
				}
			}
		}

		public void RegisterListener(TimeSpan timeout, DemuxSocketListener listener)
		{
			if (listener.Type.Length > 100)
			{
				throw new ArgumentOutOfRangeException("listener", SRClient.ListenerLengthArgumentOutOfRange);
			}
			lock (this.listenerTable)
			{
				this.listenerTable.Add(listener.Type, listener);
				if (this.listenerTable.Count == 1)
				{
					this.innerListener = this.innerElement.CreateListener(this.bufferSize, this.uri);
					this.innerListener.Open(timeout);
					this.StartAccepting();
				}
			}
		}

		private void StartAccepting()
		{
			try
			{
				IAsyncResult asyncResult = this.innerListener.BeginAccept(new AsyncCallback(DemuxSocketManager.AcceptCallback), this);
				if (asyncResult.CompletedSynchronously)
				{
					this.AcceptComplete(asyncResult, true);
				}
			}
			catch
			{
			}
		}

		private static void StartAcceptingStatic(object state)
		{
			((DemuxSocketManager)state).StartAccepting();
		}

		public void UnregisterListener(TimeSpan timeout, DemuxSocketListener listener)
		{
			DemuxSocketListener demuxSocketListener;
			lock (this.listenerTable)
			{
				if (this.listenerTable.TryGetValue(listener.Type, out demuxSocketListener) && demuxSocketListener == listener)
				{
					this.listenerTable.Remove(listener.Type);
					if (this.listenerTable.Count == 0)
					{
						if (timeout != TimeSpan.Zero)
						{
							this.innerListener.Close(timeout);
						}
						else
						{
							this.innerListener.Abort();
						}
						this.innerListener = null;
					}
				}
			}
		}
	}
}
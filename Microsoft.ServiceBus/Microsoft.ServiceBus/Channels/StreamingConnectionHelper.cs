using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal static class StreamingConnectionHelper
	{
		public static IAsyncResult BeginWriteMessage(Message message, Microsoft.ServiceBus.Channels.IConnection connection, bool isRequest, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings settings, ref TimeoutHelper timeoutHelper, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult(message, connection, isRequest, settings, ref timeoutHelper, callback, state);
		}

		public static void EndWriteMessage(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.End(result);
		}

		public static void WriteMessage(Message message, Microsoft.ServiceBus.Channels.IConnection connection, bool isRequest, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings settings, ref TimeoutHelper timeoutHelper)
		{
			bool flag;
			byte[] envelopeEndBytes = null;
			if (message != null)
			{
				MessageEncoder encoder = settings.MessageEncoderFactory.Encoder;
				byte[] envelopeStartBytes = Microsoft.ServiceBus.Channels.SingletonEncoder.EnvelopeStartBytes;
				if (!isRequest)
				{
					envelopeEndBytes = Microsoft.ServiceBus.Channels.SingletonEncoder.EnvelopeEndBytes;
					flag = Microsoft.ServiceBus.Channels.TransferModeHelper.IsResponseStreamed(settings.TransferMode);
				}
				else
				{
					envelopeEndBytes = Microsoft.ServiceBus.Channels.SingletonEncoder.EnvelopeEndFramingEndBytes;
					flag = Microsoft.ServiceBus.Channels.TransferModeHelper.IsRequestStreamed(settings.TransferMode);
				}
				if (!flag)
				{
					ArraySegment<byte> nums = encoder.WriteMessage(message, 2147483647, settings.BufferManager, (int)envelopeStartBytes.Length + 5);
					nums = Microsoft.ServiceBus.Channels.SingletonEncoder.EncodeMessageFrame(nums);
					Buffer.BlockCopy(envelopeStartBytes, 0, nums.Array, nums.Offset - (int)envelopeStartBytes.Length, (int)envelopeStartBytes.Length);
					connection.Write(nums.Array, nums.Offset - (int)envelopeStartBytes.Length, nums.Count + (int)envelopeStartBytes.Length, true, timeoutHelper.RemainingTime(), settings.BufferManager);
				}
				else
				{
					connection.Write(envelopeStartBytes, 0, (int)envelopeStartBytes.Length, false, timeoutHelper.RemainingTime());
					Microsoft.ServiceBus.Channels.StreamingConnectionHelper.StreamingOutputConnectionStream streamingOutputConnectionStream = new Microsoft.ServiceBus.Channels.StreamingConnectionHelper.StreamingOutputConnectionStream(connection, settings)
					{
						Immediate = !message.Properties.AllowOutputBatching
					};
					encoder.WriteMessage(message, new Microsoft.ServiceBus.Channels.TimeoutStream(streamingOutputConnectionStream, ref timeoutHelper));
				}
			}
			else if (isRequest)
			{
				envelopeEndBytes = Microsoft.ServiceBus.Channels.SingletonEncoder.EndBytes;
			}
			if (envelopeEndBytes != null)
			{
				connection.Write(envelopeEndBytes, 0, (int)envelopeEndBytes.Length, true, timeoutHelper.RemainingTime());
			}
		}

		private class StreamingOutputConnectionStream : Microsoft.ServiceBus.Channels.ConnectionStream
		{
			private byte[] encodedSize;

			public StreamingOutputConnectionStream(Microsoft.ServiceBus.Channels.IConnection connection, IDefaultCommunicationTimeouts timeouts) : base(connection, timeouts)
			{
				this.encodedSize = new byte[5];
			}

			public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				this.WriteChunkSize(count);
				return base.BeginWrite(buffer, offset, count, callback, state);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				this.WriteChunkSize(count);
				base.Write(buffer, offset, count);
			}

			public override void WriteByte(byte value)
			{
				this.WriteChunkSize(1);
				base.WriteByte(value);
			}

			private void WriteChunkSize(int size)
			{
				if (size > 0)
				{
					int num = Microsoft.ServiceBus.Channels.IntEncoder.Encode(size, this.encodedSize, 0);
					base.Connection.Write(this.encodedSize, 0, num, false, TimeoutHelper.FromMilliseconds(this.WriteTimeout));
				}
			}
		}

		private class WriteMessageAsyncResult : AsyncResult
		{
			private Microsoft.ServiceBus.Channels.IConnection connection;

			private MessageEncoder encoder;

			private BufferManager bufferManager;

			private Message message;

			private static AsyncCallback onWriteBufferedMessage;

			private static AsyncCallback onWriteStartBytes;

			private static Action<object> onWriteStartBytesScheduled;

			private static AsyncCallback onWriteEndBytes;

			private byte[] bufferToFree;

			private Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings settings;

			private TimeoutHelper timeoutHelper;

			private byte[] endBytes;

			static WriteMessageAsyncResult()
			{
				Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.onWriteEndBytes = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.OnWriteEndBytes));
			}

			public WriteMessageAsyncResult(Message message, Microsoft.ServiceBus.Channels.IConnection connection, bool isRequest, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings settings, ref TimeoutHelper timeoutHelper, AsyncCallback callback, object state) : base(callback, state)
			{
				bool flag;
				this.connection = connection;
				this.encoder = settings.MessageEncoderFactory.Encoder;
				this.bufferManager = settings.BufferManager;
				this.timeoutHelper = timeoutHelper;
				this.message = message;
				this.settings = settings;
				bool flag1 = true;
				bool flag2 = false;
				if (message != null)
				{
					try
					{
						byte[] envelopeStartBytes = Microsoft.ServiceBus.Channels.SingletonEncoder.EnvelopeStartBytes;
						if (!isRequest)
						{
							this.endBytes = Microsoft.ServiceBus.Channels.SingletonEncoder.EnvelopeEndBytes;
							flag = Microsoft.ServiceBus.Channels.TransferModeHelper.IsResponseStreamed(settings.TransferMode);
						}
						else
						{
							this.endBytes = Microsoft.ServiceBus.Channels.SingletonEncoder.EnvelopeEndFramingEndBytes;
							flag = Microsoft.ServiceBus.Channels.TransferModeHelper.IsRequestStreamed(settings.TransferMode);
						}
						if (!flag)
						{
							ArraySegment<byte> nums = settings.MessageEncoderFactory.Encoder.WriteMessage(message, 2147483647, this.bufferManager, (int)envelopeStartBytes.Length + 5);
							nums = Microsoft.ServiceBus.Channels.SingletonEncoder.EncodeMessageFrame(nums);
							this.bufferToFree = nums.Array;
							Buffer.BlockCopy(envelopeStartBytes, 0, nums.Array, nums.Offset - (int)envelopeStartBytes.Length, (int)envelopeStartBytes.Length);
							if (Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.onWriteBufferedMessage == null)
							{
								Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.onWriteBufferedMessage = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.OnWriteBufferedMessage));
							}
							IAsyncResult asyncResult = connection.BeginWrite(nums.Array, nums.Offset - (int)envelopeStartBytes.Length, nums.Count + (int)envelopeStartBytes.Length, true, timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.onWriteBufferedMessage, this);
							if (asyncResult.CompletedSynchronously)
							{
								flag2 = this.HandleWriteBufferedMessage(asyncResult);
							}
						}
						else
						{
							if (Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.onWriteStartBytes == null)
							{
								Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.onWriteStartBytes = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.OnWriteStartBytes));
							}
							IAsyncResult asyncResult1 = connection.BeginWrite(envelopeStartBytes, 0, (int)envelopeStartBytes.Length, true, timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.onWriteStartBytes, this);
							if (asyncResult1.CompletedSynchronously)
							{
								if (Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.onWriteStartBytesScheduled == null)
								{
									Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.onWriteStartBytesScheduled = new Action<object>(Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.OnWriteStartBytesScheduled);
								}
								IOThreadScheduler.ScheduleCallbackNoFlow(Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.onWriteStartBytesScheduled, asyncResult1);
							}
						}
						flag1 = false;
					}
					finally
					{
						if (flag1)
						{
							this.Cleanup();
						}
					}
				}
				else
				{
					if (isRequest)
					{
						this.endBytes = Microsoft.ServiceBus.Channels.SingletonEncoder.EndBytes;
					}
					flag2 = this.WriteEndBytes();
				}
				if (flag2)
				{
					base.Complete(true);
				}
			}

			private void Cleanup()
			{
				if (this.bufferToFree != null)
				{
					this.bufferManager.ReturnBuffer(this.bufferToFree);
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult>(result);
			}

			private bool HandleWriteBufferedMessage(IAsyncResult result)
			{
				this.connection.EndWrite(result);
				return this.WriteEndBytes();
			}

			private bool HandleWriteEndBytes(IAsyncResult result)
			{
				this.connection.EndWrite(result);
				this.Cleanup();
				return true;
			}

			private bool HandleWriteStartBytes(IAsyncResult result)
			{
				this.connection.EndWrite(result);
				Microsoft.ServiceBus.Channels.StreamingConnectionHelper.StreamingOutputConnectionStream streamingOutputConnectionStream = new Microsoft.ServiceBus.Channels.StreamingConnectionHelper.StreamingOutputConnectionStream(this.connection, this.settings)
				{
					Immediate = !this.message.Properties.AllowOutputBatching
				};
				Stream timeoutStream = new Microsoft.ServiceBus.Channels.TimeoutStream(streamingOutputConnectionStream, ref this.timeoutHelper);
				this.encoder.WriteMessage(this.message, timeoutStream);
				return this.WriteEndBytes();
			}

			private static void OnWriteBufferedMessage(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult asyncState = (Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult)result.AsyncState;
				Exception exception = null;
				bool flag = false;
				bool flag1 = true;
				try
				{
					try
					{
						flag = asyncState.HandleWriteBufferedMessage(result);
						flag1 = false;
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						flag = true;
						exception = exception1;
					}
				}
				finally
				{
					if (flag1)
					{
						asyncState.Cleanup();
					}
				}
				if (flag)
				{
					asyncState.Complete(false, exception);
				}
			}

			private static void OnWriteEndBytes(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult asyncState = (Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult)result.AsyncState;
				Exception exception = null;
				bool flag = false;
				bool flag1 = false;
				try
				{
					try
					{
						flag = asyncState.HandleWriteEndBytes(result);
						flag1 = true;
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						flag = true;
						exception = exception1;
					}
				}
				finally
				{
					if (!flag1)
					{
						asyncState.Cleanup();
					}
				}
				if (flag)
				{
					asyncState.Complete(false, exception);
				}
			}

			private static void OnWriteStartBytes(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.OnWriteStartBytesCallbackHelper(result);
			}

			private static void OnWriteStartBytesCallbackHelper(IAsyncResult result)
			{
				Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult asyncState = (Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult)result.AsyncState;
				Exception exception = null;
				bool flag = false;
				bool flag1 = true;
				try
				{
					try
					{
						flag = asyncState.HandleWriteStartBytes(result);
						flag1 = false;
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						flag = true;
						exception = exception1;
					}
				}
				finally
				{
					if (flag1)
					{
						asyncState.Cleanup();
					}
				}
				if (flag)
				{
					asyncState.Complete(false, exception);
				}
			}

			private static void OnWriteStartBytesScheduled(object state)
			{
				Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.OnWriteStartBytesCallbackHelper((IAsyncResult)state);
			}

			private bool WriteEndBytes()
			{
				if (this.endBytes == null)
				{
					this.Cleanup();
					return true;
				}
				IAsyncResult asyncResult = this.connection.BeginWrite(this.endBytes, 0, (int)this.endBytes.Length, true, this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessageAsyncResult.onWriteEndBytes, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				return this.HandleWriteEndBytes(asyncResult);
			}
		}
	}
}
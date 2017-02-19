using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class ConnectionUpgradeHelper
	{
		public ConnectionUpgradeHelper()
		{
		}

		public static IAsyncResult BeginDecodeFramingFault(Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder, Microsoft.ServiceBus.Channels.IConnection connection, Uri via, string contentType, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFailedUpgradeAsyncResult(decoder, connection, via, contentType, ref timeoutHelper, callback, state);
		}

		public static IAsyncResult BeginInitiateUpgrade(IDefaultCommunicationTimeouts timeouts, EndpointAddress remoteAddress, Microsoft.ServiceBus.Channels.IConnection connection, Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder, StreamUpgradeInitiator upgradeInitiator, string contentType, Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult(timeouts, remoteAddress, connection, decoder, upgradeInitiator, contentType, timeoutHelper, callback, state);
		}

		public static void DecodeFramingFault(Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder, Microsoft.ServiceBus.Channels.IConnection connection, Uri via, string contentType, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper)
		{
			Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.ValidateReadingFaultString(decoder);
			int num = 0;
			byte[] numArray = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.Utility.AllocateByteArray(256);
			int num1 = connection.Read(numArray, num, (int)numArray.Length, timeoutHelper.RemainingTime());
			while (num1 > 0)
			{
				int num2 = decoder.Decode(numArray, num, num1);
				num = num + num2;
				num1 = num1 - num2;
				if (decoder.CurrentState == Microsoft.ServiceBus.Channels.ClientFramingDecoderState.Fault)
				{
					Microsoft.ServiceBus.Channels.ConnectionUtilities.CloseNoThrow(connection, timeoutHelper.RemainingTime());
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(Microsoft.ServiceBus.Channels.FaultStringDecoder.GetFaultException(decoder.Fault, via.ToString(), contentType));
				}
				if (decoder.CurrentState != Microsoft.ServiceBus.Channels.ClientFramingDecoderState.ReadingFaultString)
				{
					throw Fx.AssertAndThrow("invalid framing client state machine");
				}
				if (num1 != 0)
				{
					continue;
				}
				num = 0;
				num1 = connection.Read(numArray, num, (int)numArray.Length, timeoutHelper.RemainingTime());
			}
			throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(decoder.CreatePrematureEOFException());
		}

		public static void EndDecodeFramingFault(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFailedUpgradeAsyncResult.End(result);
		}

		public static Microsoft.ServiceBus.Channels.IConnection EndInitiateUpgrade(IAsyncResult result)
		{
			return Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.End(result);
		}

		public static bool InitiateUpgrade(StreamUpgradeInitiator upgradeInitiator, ref Microsoft.ServiceBus.Channels.IConnection connection, Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder, IDefaultCommunicationTimeouts defaultTimeouts, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper)
		{
			for (string i = upgradeInitiator.GetNextUpgrade(); i != null; i = upgradeInitiator.GetNextUpgrade())
			{
				Microsoft.ServiceBus.Channels.EncodedUpgrade encodedUpgrade = new Microsoft.ServiceBus.Channels.EncodedUpgrade(i);
				connection.Write(encodedUpgrade.EncodedBytes, 0, (int)encodedUpgrade.EncodedBytes.Length, true, timeoutHelper.RemainingTime());
				byte[] numArray = new byte[1];
				if (!Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.ValidateUpgradeResponse(numArray, connection.Read(numArray, 0, (int)numArray.Length, timeoutHelper.RemainingTime()), decoder))
				{
					return false;
				}
				Microsoft.ServiceBus.Channels.ConnectionStream connectionStream = new Microsoft.ServiceBus.Channels.ConnectionStream(connection, defaultTimeouts);
				connection = new Microsoft.ServiceBus.Channels.StreamConnection(upgradeInitiator.InitiateUpgrade(connectionStream), connectionStream);
			}
			return true;
		}

		public static bool ValidatePreambleResponse(byte[] buffer, int count, Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder, Uri via)
		{
			if (count == 0)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string serverRejectedSessionPreamble = Resources.ServerRejectedSessionPreamble;
				object[] objArray = new object[] { via };
				throw exceptionUtility.ThrowHelperError(new ProtocolException(Microsoft.ServiceBus.SR.GetString(serverRejectedSessionPreamble, objArray), decoder.CreatePrematureEOFException()));
			}
			while (decoder.Decode(buffer, 0, count) == 0)
			{
			}
			if (decoder.CurrentState != Microsoft.ServiceBus.Channels.ClientFramingDecoderState.Start)
			{
				return false;
			}
			return true;
		}

		private static void ValidateReadingFaultString(Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder)
		{
			if (decoder.CurrentState != Microsoft.ServiceBus.Channels.ClientFramingDecoderState.ReadingFaultString)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new MessageSecurityException(Microsoft.ServiceBus.SR.GetString(Resources.ServerRejectedUpgradeRequest, new object[0])));
			}
		}

		private static bool ValidateUpgradeResponse(byte[] buffer, int count, Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder)
		{
			if (count == 0)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new MessageSecurityException(Microsoft.ServiceBus.SR.GetString(Resources.ServerRejectedUpgradeRequest, new object[0]), decoder.CreatePrematureEOFException()));
			}
			while (decoder.Decode(buffer, 0, count) == 0)
			{
			}
			if (decoder.CurrentState != Microsoft.ServiceBus.Channels.ClientFramingDecoderState.UpgradeResponse)
			{
				return false;
			}
			return true;
		}

		private class DecodeFailedUpgradeAsyncResult : AsyncResult
		{
			private Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder;

			private Microsoft.ServiceBus.Channels.IConnection connection;

			private Uri via;

			private string contentType;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			private static WaitCallback onReadFaultData;

			static DecodeFailedUpgradeAsyncResult()
			{
				Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFailedUpgradeAsyncResult.onReadFaultData = new WaitCallback(Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFailedUpgradeAsyncResult.OnReadFaultData);
			}

			public DecodeFailedUpgradeAsyncResult(Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder, Microsoft.ServiceBus.Channels.IConnection connection, Uri via, string contentType, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper, AsyncCallback callback, object state) : base(callback, state)
			{
				Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.ValidateReadingFaultString(decoder);
				this.decoder = decoder;
				this.connection = connection;
				this.via = via;
				this.contentType = contentType;
				this.timeoutHelper = timeoutHelper;
				if (connection.BeginRead(0, Math.Min(256, connection.AsyncReadBufferSize), timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFailedUpgradeAsyncResult.onReadFaultData, this) == AsyncReadResult.Queued)
				{
					return;
				}
				this.CompleteReadFaultData();
			}

			private void CompleteReadFaultData()
			{
				int num = 0;
				int num1 = this.connection.EndRead();
				while (num1 > 0)
				{
					int num2 = this.decoder.Decode(this.connection.AsyncReadBuffer, num, num1);
					num = num + num2;
					num1 = num1 - num2;
					if (this.decoder.CurrentState == Microsoft.ServiceBus.Channels.ClientFramingDecoderState.Fault)
					{
						Microsoft.ServiceBus.Channels.ConnectionUtilities.CloseNoThrow(this.connection, this.timeoutHelper.RemainingTime());
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(Microsoft.ServiceBus.Channels.FaultStringDecoder.GetFaultException(this.decoder.Fault, this.via.ToString(), this.contentType));
					}
					if (this.decoder.CurrentState != Microsoft.ServiceBus.Channels.ClientFramingDecoderState.ReadingFaultString)
					{
						Fx.AssertAndThrow("invalid framing client state machine");
					}
					if (num1 != 0)
					{
						continue;
					}
					num = 0;
					if (this.connection.BeginRead(0, Math.Min(256, this.connection.AsyncReadBufferSize), this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFailedUpgradeAsyncResult.onReadFaultData, this) == AsyncReadResult.Queued)
					{
						return;
					}
					num1 = this.connection.EndRead();
				}
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(this.decoder.CreatePrematureEOFException());
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFailedUpgradeAsyncResult>(result);
			}

			private static void OnReadFaultData(object state)
			{
				Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFailedUpgradeAsyncResult decodeFailedUpgradeAsyncResult = (Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFailedUpgradeAsyncResult)state;
				Exception exception = null;
				try
				{
					decodeFailedUpgradeAsyncResult.CompleteReadFaultData();
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					exception = exception1;
				}
				if (exception != null)
				{
					decodeFailedUpgradeAsyncResult.Complete(false, exception);
				}
			}
		}

		private class InitiateUpgradeAsyncResult : AsyncResult
		{
			private IDefaultCommunicationTimeouts defaultTimeouts;

			private Microsoft.ServiceBus.Channels.IConnection connection;

			private Microsoft.ServiceBus.Channels.ConnectionStream connectionStream;

			private string contentType;

			private Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder;

			private static AsyncCallback onInitiateUpgrade;

			private static WaitCallback onReadUpgradeResponse;

			private static AsyncCallback onFailedUpgrade;

			private static AsyncCallback onWriteUpgradeBytes;

			private EndpointAddress remoteAddress;

			private StreamUpgradeInitiator upgradeInitiator;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			static InitiateUpgradeAsyncResult()
			{
				Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.onInitiateUpgrade = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.OnInitiateUpgrade));
				Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.onReadUpgradeResponse = new WaitCallback(Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.OnReadUpgradeResponse);
				Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.onWriteUpgradeBytes = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.OnWriteUpgradeBytes));
			}

			public InitiateUpgradeAsyncResult(IDefaultCommunicationTimeouts timeouts, EndpointAddress remoteAddress, Microsoft.ServiceBus.Channels.IConnection connection, Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder, StreamUpgradeInitiator upgradeInitiator, string contentType, Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper, AsyncCallback callback, object state) : base(callback, state)
			{
				this.defaultTimeouts = timeouts;
				this.decoder = decoder;
				this.upgradeInitiator = upgradeInitiator;
				this.contentType = contentType;
				this.timeoutHelper = timeoutHelper;
				this.connection = connection;
				this.remoteAddress = remoteAddress;
				if (this.Begin())
				{
					base.Complete(true);
				}
			}

			private bool Begin()
			{
				for (string i = this.upgradeInitiator.GetNextUpgrade(); i != null; i = this.upgradeInitiator.GetNextUpgrade())
				{
					Microsoft.ServiceBus.Channels.EncodedUpgrade encodedUpgrade = new Microsoft.ServiceBus.Channels.EncodedUpgrade(i);
					IAsyncResult asyncResult = this.connection.BeginWrite(encodedUpgrade.EncodedBytes, 0, (int)encodedUpgrade.EncodedBytes.Length, true, this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.onWriteUpgradeBytes, this);
					if (!asyncResult.CompletedSynchronously)
					{
						return false;
					}
					if (!this.CompleteWriteUpgradeBytes(asyncResult))
					{
						return false;
					}
				}
				return true;
			}

			private bool CompleteReadUpgradeResponse()
			{
				int num = this.connection.EndRead();
				if (Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.ValidateUpgradeResponse(this.connection.AsyncReadBuffer, num, this.decoder))
				{
					this.connectionStream = new Microsoft.ServiceBus.Channels.ConnectionStream(this.connection, this.defaultTimeouts);
					IAsyncResult asyncResult = this.upgradeInitiator.BeginInitiateUpgrade(this.connectionStream, Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.onInitiateUpgrade, this);
					if (!asyncResult.CompletedSynchronously)
					{
						return false;
					}
					this.CompleteUpgrade(asyncResult);
					return true;
				}
				if (Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.onFailedUpgrade == null)
				{
					Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.onFailedUpgrade = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.OnFailedUpgrade));
				}
				IAsyncResult asyncResult1 = Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.BeginDecodeFramingFault(this.decoder, this.connection, this.remoteAddress.Uri, this.contentType, ref this.timeoutHelper, Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.onFailedUpgrade, this);
				if (asyncResult1.CompletedSynchronously)
				{
					Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.EndDecodeFramingFault(asyncResult1);
				}
				return asyncResult1.CompletedSynchronously;
			}

			private void CompleteUpgrade(IAsyncResult result)
			{
				Stream stream = this.upgradeInitiator.EndInitiateUpgrade(result);
				this.connection = new Microsoft.ServiceBus.Channels.StreamConnection(stream, this.connectionStream);
			}

			private bool CompleteWriteUpgradeBytes(IAsyncResult result)
			{
				this.connection.EndWrite(result);
				if (this.connection.BeginRead(0, (int)Microsoft.ServiceBus.Channels.ServerSessionEncoder.UpgradeResponseBytes.Length, this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult.onReadUpgradeResponse, this) == AsyncReadResult.Queued)
				{
					return false;
				}
				return this.CompleteReadUpgradeResponse();
			}

			public static new Microsoft.ServiceBus.Channels.IConnection End(IAsyncResult result)
			{
				return AsyncResult.End<Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult>(result).connection;
			}

			private static void OnFailedUpgrade(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.EndDecodeFramingFault(result);
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					exception = exception1;
				}
				asyncState.Complete(false, exception);
			}

			private static void OnInitiateUpgrade(IAsyncResult result)
			{
				bool flag;
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					asyncState.CompleteUpgrade(result);
					flag = asyncState.Begin();
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
				if (flag)
				{
					asyncState.Complete(false, exception);
				}
			}

			private static void OnReadUpgradeResponse(object state)
			{
				Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult initiateUpgradeAsyncResult = (Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult)state;
				Exception exception = null;
				bool flag = false;
				try
				{
					if (initiateUpgradeAsyncResult.CompleteReadUpgradeResponse())
					{
						flag = initiateUpgradeAsyncResult.Begin();
					}
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
				if (flag)
				{
					initiateUpgradeAsyncResult.Complete(false, exception);
				}
			}

			private static void OnWriteUpgradeBytes(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgradeAsyncResult)result.AsyncState;
				Exception exception = null;
				bool flag = false;
				try
				{
					if (asyncState.CompleteWriteUpgradeBytes(result))
					{
						flag = asyncState.Begin();
					}
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
				if (flag)
				{
					asyncState.Complete(false, exception);
				}
			}
		}
	}
}
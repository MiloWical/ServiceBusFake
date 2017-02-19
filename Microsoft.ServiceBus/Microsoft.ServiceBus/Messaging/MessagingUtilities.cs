using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class MessagingUtilities
	{
		private static AsyncCallback safeCloseMessageEntityCallback;

		private static AsyncCallback safeCloseCommunicationObjectCallback;

		public static void AbortIfNotNull(ICommunicationObject communicationObject)
		{
			if (communicationObject != null)
			{
				communicationObject.Abort();
			}
		}

		public static void CheckUriSchemeKey(string entityName, string paramName)
		{
			if (string.IsNullOrWhiteSpace(entityName))
			{
				throw Fx.Exception.ArgumentNullOrWhiteSpace(paramName);
			}
			string[] strArrays = new string[] { "@", "?", "#" };
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string str = strArrays[i];
				if (entityName.Contains(str))
				{
					ExceptionTrace exception = Fx.Exception;
					string characterReservedForUriScheme = Resources.CharacterReservedForUriScheme;
					object[] objArray = new object[] { paramName, str };
					throw exception.Argument(paramName, Microsoft.ServiceBus.SR.GetString(characterReservedForUriScheme, objArray));
				}
			}
		}

		public static void CheckValidMessage(BrokeredMessage message, bool validateOnSend)
		{
			if (message == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("message");
			}
			if (validateOnSend && message.IsLockTokenSet)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new InvalidOperationException(SRClient.CannotSendReceivedMessage), null);
			}
		}

		public static void CheckValidMessages(IEnumerable<BrokeredMessage> messages, bool validateOnSend)
		{
			if (messages == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("messages");
			}
			using (IEnumerator<BrokeredMessage> enumerator = messages.GetEnumerator())
			{
				if (!enumerator.MoveNext())
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("messages");
				}
				do
				{
					MessagingUtilities.CheckValidMessage(enumerator.Current, validateOnSend);
				}
				while (enumerator.MoveNext());
			}
		}

		public static void CheckValidSequenceNumbers(IEnumerable<long> sequenceNumbers)
		{
			if (sequenceNumbers == null || !sequenceNumbers.Any<long>())
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("sequenceNumbers");
			}
		}

		public static string ConvertToString(this SecureString str)
		{
			string stringUni;
			if (str == null || str.Length == 0)
			{
				return string.Empty;
			}
			IntPtr zero = IntPtr.Zero;
			try
			{
				zero = Marshal.SecureStringToGlobalAllocUnicode(str);
				stringUni = Marshal.PtrToStringUni(zero);
			}
			finally
			{
				Marshal.ZeroFreeGlobalAllocUnicode(zero);
			}
			return stringUni;
		}

		public static UriBuilder CreateUriBuilderWithHttpsSchemeAndPort(Uri baseUri)
		{
			if (baseUri == null)
			{
				throw Fx.Exception.AsError(new ArgumentNullException("baseUri", SRClient.UseOverloadWithBaseAddress), null);
			}
			UriBuilder uriBuilder = new UriBuilder(baseUri);
			if (uriBuilder.Port == -1)
			{
				uriBuilder.Port = RelayEnvironment.RelayHttpsPort;
			}
			uriBuilder.Scheme = Uri.UriSchemeHttps;
			MessagingUtilities.EnsureTrailingSlash(uriBuilder);
			return uriBuilder;
		}

		public static void EnsureTrailingSlash(UriBuilder uriBuilder)
		{
			if (!uriBuilder.Path.EndsWith("/", StringComparison.Ordinal))
			{
				UriBuilder uriBuilder1 = uriBuilder;
				uriBuilder1.Path = string.Concat(uriBuilder1.Path, "/");
			}
		}

		public static IEnumerable<Uri> GetUriList(IEnumerable<string> addresses)
		{
			if (addresses == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("addresses");
			}
			List<Uri> uris = new List<Uri>();
			foreach (string address in addresses)
			{
				Uri uri = null;
				try
				{
					uri = new Uri(address);
				}
				catch (UriFormatException uriFormatException)
				{
					throw new UriFormatException(SRClient.BadUriFormat(address), uriFormatException);
				}
				MessagingUtilities.ThrowIfNullAddressOrPathExists(uri, "uriAddress");
				uris.Add(uri);
			}
			if (uris.Count == 0)
			{
				throw Fx.Exception.Argument("uriAddresses", SRClient.InvalidAddressPath(uris));
			}
			return uris;
		}

		public static bool IsSubQueue(string path)
		{
			if (path == null)
			{
				return false;
			}
			return path.Contains("$");
		}

		public static void SafeAddClosed(this ICommunicationObject communicationObject, EventHandler handler)
		{
			communicationObject.Closed += handler;
			if (communicationObject.State == CommunicationState.Closed)
			{
				handler(communicationObject, EventArgs.Empty);
			}
		}

		public static void SafeAddClosed(this MessageClientEntity clientEntity, EventHandler handler)
		{
			clientEntity.Closed += handler;
			if (clientEntity.IsClosed)
			{
				handler(clientEntity, EventArgs.Empty);
			}
		}

		public static void SafeAddClosed(this ClientEntity clientEntity, EventHandler handler)
		{
			clientEntity.Closed += handler;
			if (clientEntity.IsClosed)
			{
				handler(clientEntity, EventArgs.Empty);
			}
		}

		public static void SafeAddClosing(this ICommunicationObject communicationObject, EventHandler handler)
		{
			communicationObject.Closing += handler;
			CommunicationState state = communicationObject.State;
			if (state == CommunicationState.Closed || state == CommunicationState.Closing)
			{
				handler(communicationObject, EventArgs.Empty);
			}
		}

		public static void SafeAddFaulted(this ICommunicationObject communicationObject, EventHandler handler)
		{
			communicationObject.Faulted += handler;
			CommunicationState state = communicationObject.State;
			if (state != CommunicationState.Created && state != CommunicationState.Opening && state != CommunicationState.Opened)
			{
				handler(communicationObject, EventArgs.Empty);
			}
		}

		public static void SafeAddFaulted(this MessageClientEntity clientEntity, EventHandler handler)
		{
			clientEntity.Faulted += handler;
			if (clientEntity.IsFaulted)
			{
				handler(clientEntity, EventArgs.Empty);
			}
		}

		public static void SafeAddFaulted(this ClientEntity clientEntity, EventHandler handler)
		{
			clientEntity.Faulted += handler;
			if (clientEntity.IsFaulted)
			{
				handler(clientEntity, EventArgs.Empty);
			}
		}

		public static void SafeClose(this Message message)
		{
			try
			{
				if (message != null && message.State != System.ServiceModel.Channels.MessageState.Closed)
				{
					message.Close();
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "MessagingUtilities.SafeClose(Message)", null);
			}
		}

		public static void SafeClose(this ICommunicationObject communicationObject, TimeSpan? timeout = null)
		{
			try
			{
				if (communicationObject != null && communicationObject.State != CommunicationState.Closed)
				{
					if (MessagingUtilities.safeCloseCommunicationObjectCallback == null)
					{
						MessagingUtilities.safeCloseCommunicationObjectCallback = new AsyncCallback(MessagingUtilities.SafeCloseCommunicationObjectCallback);
					}
					if (!timeout.HasValue)
					{
						communicationObject.BeginClose(MessagingUtilities.safeCloseCommunicationObjectCallback, communicationObject);
					}
					else
					{
						communicationObject.BeginClose(timeout.Value, MessagingUtilities.safeCloseCommunicationObjectCallback, communicationObject);
					}
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "MessagingUtilities.SafeClose(CommunicationObject)", null);
				if (communicationObject != null)
				{
					communicationObject.Abort();
				}
			}
		}

		private static void SafeCloseCommunicationObjectCallback(IAsyncResult asyncResult)
		{
			ICommunicationObject asyncState = (ICommunicationObject)asyncResult.AsyncState;
			try
			{
				asyncState.EndClose(asyncResult);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "MessagingUtilities.SafeCloseCommunicationObjectCallback", null);
				asyncState.Abort();
			}
		}

		private static void SafeCloseMessageEntityCallback(IAsyncResult asyncResult)
		{
			MessageClientEntity asyncState = (MessageClientEntity)asyncResult.AsyncState;
			try
			{
				asyncState.EndClose(asyncResult);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "MessagingUtilities.SafeCloseMessageEntityCallback", null);
			}
		}

		public static void ScheduleCloseOrAbort(this MessageClientEntity clientEntity)
		{
			try
			{
				if (clientEntity != null && !clientEntity.IsClosed)
				{
					if (MessagingUtilities.safeCloseMessageEntityCallback == null)
					{
						MessagingUtilities.safeCloseMessageEntityCallback = new AsyncCallback(MessagingUtilities.SafeCloseMessageEntityCallback);
					}
					clientEntity.BeginClose(MessagingUtilities.safeCloseMessageEntityCallback, clientEntity);
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "MessagingUtilities.ScheduleCloseOrAbort", null);
				if (clientEntity != null)
				{
					clientEntity.Abort();
				}
			}
		}

		public static void ThrowIfContainsSubQueueName(string path)
		{
			if (path.Contains("$"))
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRClient.CannotCreateClientOnSubQueue), null);
			}
		}

		public static void ThrowIfInvalidSubQueueNameString(string path, string paramName)
		{
			string[] strArrays = EntityNameHelper.SplitSubQueuePath(path);
			if ((int)strArrays.Length == 2 && !string.IsNullOrWhiteSpace(strArrays[1]) && !EntityNameHelper.ValidateSubQueueName(strArrays[1]))
			{
				throw new ArgumentException(SRClient.InvalidSubQueueNameString(string.Join(", ", Constants.SupportedSubQueueNames.ToArray())), paramName);
			}
		}

		public static void ThrowIfNullAddressesOrPathExists(IEnumerable<Uri> addresses, string paramName)
		{
			if (addresses == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull(paramName);
			}
			if (addresses.Count<Uri>() == 0)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument("addresses", SRClient.NoAddressesFound(addresses));
			}
			foreach (Uri address in addresses)
			{
				MessagingUtilities.ThrowIfNullAddressOrPathExists(address, "address");
			}
		}

		public static void ThrowIfNullAddressOrPathExists(Uri address, string paramName)
		{
			if (address == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull(paramName);
			}
			if (!string.IsNullOrEmpty(address.AbsolutePath) && (int)address.Segments.Length > 3)
			{
				throw Fx.Exception.Argument(paramName, SRClient.InvalidAddressPath(address.AbsoluteUri));
			}
		}

		public static void ValidateAndSetConsumedMessages(IEnumerable<BrokeredMessage> messages)
		{
			List<BrokeredMessage> brokeredMessages = new List<BrokeredMessage>();
			foreach (BrokeredMessage message in messages)
			{
				if (message.IsConsumed)
				{
					foreach (BrokeredMessage brokeredMessage in brokeredMessages)
					{
						brokeredMessage.IsConsumed = false;
					}
					throw Fx.Exception.AsError(new InvalidOperationException(SRClient.CannotUseSameMessageInstanceInMultipleOperations(message.MessageId)), null);
				}
				brokeredMessages.Add(message);
			}
		}
	}
}
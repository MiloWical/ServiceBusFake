using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal sealed class SaslNegotiator : IIoHandler
	{
		private readonly static string welcome;

		private readonly static Action<TransportAsyncCallbackArgs> onWriteFrameComplete;

		private readonly SaslTransport transport;

		private readonly SaslTransportProvider provider;

		private readonly bool isInitiator;

		private SaslNegotiator.SaslState state;

		private SaslHandler saslHandler;

		private AsyncIO.FrameBufferReader reader;

		private AsyncIO.AsyncBufferWriter writer;

		private Exception completeException;

		private int completeTransport;

		static SaslNegotiator()
		{
			SaslNegotiator.welcome = "Welcome!";
			SaslNegotiator.onWriteFrameComplete = new Action<TransportAsyncCallbackArgs>(SaslNegotiator.OnWriteFrameComplete);
		}

		public SaslNegotiator(SaslTransport transport, SaslTransportProvider provider, bool isInitiator)
		{
			this.transport = transport;
			this.provider = provider;
			this.isInitiator = isInitiator;
			this.state = SaslNegotiator.SaslState.Start;
		}

		public void CompleteNegotiation(SaslCode code, Exception exception)
		{
			this.state = SaslNegotiator.SaslState.End;
			this.completeException = exception;
			if (this.isInitiator)
			{
				this.CompleteTransport();
				return;
			}
			SaslOutcome saslOutcome = new SaslOutcome()
			{
				OutcomeCode = new SaslCode?(code)
			};
			if (code == SaslCode.Ok)
			{
				saslOutcome.AdditionalData = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(SaslNegotiator.welcome));
			}
			this.WriteFrame(saslOutcome, false);
		}

		private void CompleteTransport()
		{
			if (Interlocked.Exchange(ref this.completeTransport, 1) == 0)
			{
				if (this.completeException != null)
				{
					this.transport.OnNegotiationFail(this.completeException);
					return;
				}
				this.transport.OnNegotiationSucceed(this.saslHandler.Principal);
			}
		}

		private void HandleException(string action, Exception exception)
		{
			MessagingClientEtwProvider.TraceClient<SaslNegotiator, string, Exception>((SaslNegotiator source, string op, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, op, ex.Message), this, action, exception);
			this.state = SaslNegotiator.SaslState.End;
			this.completeException = exception;
			this.CompleteTransport();
		}

		private void HandleSaslCommand(Performative command)
		{
			if (command.DescriptorCode == SaslMechanisms.Code)
			{
				this.OnSaslServerMechanisms((SaslMechanisms)command);
				return;
			}
			if (command.DescriptorCode == SaslInit.Code)
			{
				this.OnSaslInit((SaslInit)command);
				return;
			}
			if (command.DescriptorCode == SaslChallenge.Code)
			{
				this.saslHandler.OnChallenge((SaslChallenge)command);
				return;
			}
			if (command.DescriptorCode == SaslResponse.Code)
			{
				this.saslHandler.OnResponse((SaslResponse)command);
				return;
			}
			if (command.DescriptorCode != SaslOutcome.Code)
			{
				throw new AmqpException(AmqpError.NotAllowed, command.ToString());
			}
			this.OnSaslOutcome((SaslOutcome)command);
		}

		void Microsoft.ServiceBus.Messaging.Amqp.IIoHandler.OnIoFault(Exception exception)
		{
			this.HandleException("OnIoFault", exception);
		}

		void Microsoft.ServiceBus.Messaging.Amqp.IIoHandler.OnReceiveBuffer(ByteBuffer buffer)
		{
			Frame frame = new Frame();
			try
			{
				frame.Decode(buffer);
				if (frame.Type != FrameType.Sasl)
				{
					throw new AmqpException(AmqpError.InvalidField, "sasl-frame-type");
				}
				if (frame.Command == null)
				{
					throw new AmqpException(AmqpError.InvalidField, "sasl-frame-body");
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient<SaslNegotiator, Exception>((SaslNegotiator source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "SaslDecode", ex.Message), this, exception);
				this.CompleteNegotiation(SaslCode.Sys, exception);
				return;
			}
			try
			{
				this.HandleSaslCommand(frame.Command);
			}
			catch (UnauthorizedAccessException unauthorizedAccessException1)
			{
				UnauthorizedAccessException unauthorizedAccessException = unauthorizedAccessException1;
				MessagingClientEtwProvider.TraceClient<SaslNegotiator, UnauthorizedAccessException>((SaslNegotiator source, UnauthorizedAccessException ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "Authorize", ex.Message), this, unauthorizedAccessException);
				this.CompleteNegotiation(SaslCode.Auth, unauthorizedAccessException);
			}
			catch (Exception exception3)
			{
				Exception exception2 = exception3;
				if (Fx.IsFatal(exception2))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient<SaslNegotiator, Exception>((SaslNegotiator source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "HandleSaslCommand", ex.Message), this, exception2);
				this.CompleteNegotiation(SaslCode.Sys, exception2);
			}
		}

		private void OnSaslInit(SaslInit init)
		{
			if (this.state != SaslNegotiator.SaslState.WaitingForInit)
			{
				throw new AmqpException(AmqpError.IllegalState, SRAmqp.AmqpIllegalOperationState("R:SASL-INIT", this.state));
			}
			this.state = SaslNegotiator.SaslState.Negotiating;
			SaslTransportProvider saslTransportProvider = this.provider;
			AmqpSymbol mechanism = init.Mechanism;
			this.saslHandler = saslTransportProvider.GetHandler(mechanism.Value, true);
			this.saslHandler.Start(this, init, false);
		}

		private void OnSaslOutcome(SaslOutcome outcome)
		{
			Exception unauthorizedAccessException = null;
			if (outcome.OutcomeCode.Value != SaslCode.Ok)
			{
				SaslCode? outcomeCode = outcome.OutcomeCode;
				unauthorizedAccessException = new UnauthorizedAccessException(outcomeCode.Value.ToString());
			}
			this.CompleteNegotiation(outcome.OutcomeCode.Value, unauthorizedAccessException);
		}

		private void OnSaslServerMechanisms(SaslMechanisms mechanisms)
		{
			if (this.state != SaslNegotiator.SaslState.WaitingForServerMechanisms)
			{
				throw new AmqpException(AmqpError.IllegalState, SRAmqp.AmqpIllegalOperationState("R:SASL-MECH", this.state));
			}
			string str = null;
			using (IEnumerator<string> enumerator = this.provider.Mechanisms.GetEnumerator())
			{
				do
				{
					if (!enumerator.MoveNext())
					{
						break;
					}
					string current = enumerator.Current;
					if (!mechanisms.SaslServerMechanisms.Contains(new AmqpSymbol(current)))
					{
						continue;
					}
					str = current;
					break;
				}
				while (str == null);
			}
			if (str == null)
			{
				throw new AmqpException(AmqpError.NotFound, SRAmqp.AmqpNotSupportMechanism);
			}
			this.state = SaslNegotiator.SaslState.Negotiating;
			this.saslHandler = this.provider.GetHandler(str, true);
			SaslInit saslInit = new SaslInit()
			{
				Mechanism = str
			};
			this.saslHandler.Start(this, saslInit, true);
		}

		private static void OnWriteFrameComplete(TransportAsyncCallbackArgs args)
		{
			SaslNegotiator userToken = (SaslNegotiator)args.UserToken;
			if (args.Exception != null)
			{
				userToken.HandleException("OnWriteFrameComplete", args.Exception);
				return;
			}
			if ((bool)args.UserToken2)
			{
				userToken.ReadFrame();
				return;
			}
			if (userToken.state == SaslNegotiator.SaslState.End)
			{
				userToken.CompleteTransport();
			}
		}

		public void ReadFrame()
		{
			try
			{
				this.reader.ReadFrame();
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.HandleException("ReadFrame", exception);
			}
		}

		private void SendServerMechanisms()
		{
			List<AmqpSymbol> amqpSymbols = new List<AmqpSymbol>();
			foreach (string mechanism in this.provider.Mechanisms)
			{
				amqpSymbols.Add(new AmqpSymbol(mechanism));
			}
			SaslMechanisms saslMechanism = new SaslMechanisms()
			{
				SaslServerMechanisms = new Multiple<AmqpSymbol>(amqpSymbols)
			};
			this.state = SaslNegotiator.SaslState.WaitingForInit;
			this.WriteFrame(saslMechanism, true);
		}

		public bool Start()
		{
			this.reader = new AsyncIO.FrameBufferReader(this, this.transport);
			this.writer = new AsyncIO.AsyncBufferWriter(this.transport);
			if (this.isInitiator)
			{
				this.state = SaslNegotiator.SaslState.WaitingForServerMechanisms;
				MessagingClientEtwProvider.TraceClient<SaslNegotiator>((SaslNegotiator source) => {
				}, this);
				this.ReadFrame();
			}
			else
			{
				this.SendServerMechanisms();
			}
			return false;
		}

		public override string ToString()
		{
			return "sasl-negotiator";
		}

		public void WriteFrame(Performative command, bool needReply)
		{
			try
			{
				ByteBuffer byteBuffer = Frame.EncodeCommand(FrameType.Sasl, 0, command, 0);
				TransportAsyncCallbackArgs transportAsyncCallbackArg = new TransportAsyncCallbackArgs();
				transportAsyncCallbackArg.SetBuffer(byteBuffer);
				transportAsyncCallbackArg.CompletedCallback = SaslNegotiator.onWriteFrameComplete;
				transportAsyncCallbackArg.UserToken = this;
				transportAsyncCallbackArg.UserToken2 = needReply;
				this.writer.WriteBuffer(transportAsyncCallbackArg);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.HandleException("WriteFrame", exception);
			}
		}

		private enum SaslState
		{
			Start,
			WaitingForServerMechanisms,
			WaitingForInit,
			Negotiating,
			End
		}
	}
}
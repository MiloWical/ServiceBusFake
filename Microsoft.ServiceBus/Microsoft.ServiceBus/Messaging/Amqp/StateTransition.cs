using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class StateTransition
	{
		private static StateTransition[] sendHeader;

		private static StateTransition[] sendOpen;

		private static StateTransition[] sendClose;

		private static StateTransition[] receiveHeader;

		private static StateTransition[] receiveOpen;

		private static StateTransition[] receiveClose;

		public AmqpObjectState From
		{
			get;
			private set;
		}

		public static StateTransition[] ReceiveClose
		{
			get
			{
				return StateTransition.receiveClose;
			}
		}

		public static StateTransition[] ReceiveHeader
		{
			get
			{
				return StateTransition.receiveHeader;
			}
		}

		public static StateTransition[] ReceiveOpen
		{
			get
			{
				return StateTransition.receiveOpen;
			}
		}

		public static StateTransition[] SendClose
		{
			get
			{
				return StateTransition.sendClose;
			}
		}

		public static StateTransition[] SendHeader
		{
			get
			{
				return StateTransition.sendHeader;
			}
		}

		public static StateTransition[] SendOpen
		{
			get
			{
				return StateTransition.sendOpen;
			}
		}

		public AmqpObjectState To
		{
			get;
			private set;
		}

		static StateTransition()
		{
			StateTransition[] stateTransition = new StateTransition[] { new StateTransition(AmqpObjectState.Start, AmqpObjectState.HeaderSent), new StateTransition(AmqpObjectState.HeaderReceived, AmqpObjectState.HeaderExchanged) };
			StateTransition.sendHeader = stateTransition;
			StateTransition[] stateTransitionArray = new StateTransition[] { new StateTransition(AmqpObjectState.Start, AmqpObjectState.OpenSent), new StateTransition(AmqpObjectState.OpenReceived, AmqpObjectState.Opened), new StateTransition(AmqpObjectState.HeaderSent, AmqpObjectState.OpenPipe), new StateTransition(AmqpObjectState.HeaderExchanged, AmqpObjectState.OpenSent) };
			StateTransition.sendOpen = stateTransitionArray;
			StateTransition[] stateTransition1 = new StateTransition[] { new StateTransition(AmqpObjectState.Opened, AmqpObjectState.CloseSent), new StateTransition(AmqpObjectState.CloseReceived, AmqpObjectState.End), new StateTransition(AmqpObjectState.OpenSent, AmqpObjectState.ClosePipe), new StateTransition(AmqpObjectState.OpenPipe, AmqpObjectState.OpenClosePipe), new StateTransition(AmqpObjectState.Faulted, AmqpObjectState.Faulted) };
			StateTransition.sendClose = stateTransition1;
			StateTransition[] stateTransitionArray1 = new StateTransition[] { new StateTransition(AmqpObjectState.Start, AmqpObjectState.HeaderReceived), new StateTransition(AmqpObjectState.HeaderSent, AmqpObjectState.HeaderExchanged), new StateTransition(AmqpObjectState.OpenPipe, AmqpObjectState.OpenSent), new StateTransition(AmqpObjectState.OpenClosePipe, AmqpObjectState.ClosePipe) };
			StateTransition.receiveHeader = stateTransitionArray1;
			StateTransition[] stateTransition2 = new StateTransition[] { new StateTransition(AmqpObjectState.Start, AmqpObjectState.OpenReceived), new StateTransition(AmqpObjectState.OpenSent, AmqpObjectState.Opened), new StateTransition(AmqpObjectState.HeaderReceived, AmqpObjectState.OpenReceived), new StateTransition(AmqpObjectState.HeaderExchanged, AmqpObjectState.OpenReceived), new StateTransition(AmqpObjectState.ClosePipe, AmqpObjectState.CloseSent) };
			StateTransition.receiveOpen = stateTransition2;
			StateTransition[] stateTransitionArray2 = new StateTransition[] { new StateTransition(AmqpObjectState.Opened, AmqpObjectState.CloseReceived), new StateTransition(AmqpObjectState.CloseSent, AmqpObjectState.End), new StateTransition(AmqpObjectState.OpenReceived, AmqpObjectState.CloseReceived), new StateTransition(AmqpObjectState.Faulted, AmqpObjectState.End) };
			StateTransition.receiveClose = stateTransitionArray2;
		}

		public StateTransition(AmqpObjectState from, AmqpObjectState to)
		{
			this.From = from;
			this.To = to;
		}
	}
}
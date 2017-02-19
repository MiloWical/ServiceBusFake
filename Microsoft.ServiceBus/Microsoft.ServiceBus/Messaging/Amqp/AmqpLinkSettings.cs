using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpLinkSettings : Attach
	{
		private uint linkCredit;

		public bool AutoSendFlow
		{
			get;
			set;
		}

		public int FlowThreshold
		{
			get;
			set;
		}

		public SettleMode SettleType
		{
			get
			{
				return this.SettleType();
			}
			set
			{
				base.SndSettleMode = null;
				base.RcvSettleMode = null;
				switch (value)
				{
					case SettleMode.SettleOnSend:
					{
						base.SndSettleMode = new byte?(1);
						return;
					}
					case SettleMode.SettleOnReceive:
					{
						return;
					}
					case SettleMode.SettleOnDispose:
					{
						base.RcvSettleMode = new byte?(1);
						return;
					}
					default:
					{
						return;
					}
				}
			}
		}

		public uint TotalLinkCredit
		{
			get
			{
				return this.linkCredit;
			}
			set
			{
				this.linkCredit = value;
				this.FlowThreshold = Math.Min(100, (int)(this.linkCredit * 2 / 3));
			}
		}

		public AmqpLinkSettings()
		{
		}

		public static AmqpLinkSettings Create(Attach attach)
		{
			AmqpLinkSettings amqpLinkSetting = new AmqpLinkSettings()
			{
				LinkName = attach.LinkName,
				Role = new bool?(!attach.Role.Value),
				Source = attach.Source,
				Target = attach.Target,
				SndSettleMode = attach.SndSettleMode,
				RcvSettleMode = attach.RcvSettleMode,
				MaxMessageSize = attach.MaxMessageSize,
				Properties = attach.Properties
			};
			if (!amqpLinkSetting.Role.Value)
			{
				amqpLinkSetting.InitialDeliveryCount = new uint?(0);
			}
			else
			{
				amqpLinkSetting.TotalLinkCredit = 1000;
				amqpLinkSetting.AutoSendFlow = true;
			}
			return amqpLinkSetting;
		}

		public override bool Equals(object obj)
		{
			AmqpLinkSettings amqpLinkSetting = obj as AmqpLinkSettings;
			if (amqpLinkSetting == null || amqpLinkSetting.LinkName == null)
			{
				return false;
			}
			if (!base.LinkName.Equals(amqpLinkSetting.LinkName, StringComparison.CurrentCultureIgnoreCase))
			{
				return false;
			}
			bool? role = base.Role;
			bool? nullable = amqpLinkSetting.Role;
			if (role.GetValueOrDefault() != nullable.GetValueOrDefault())
			{
				return false;
			}
			return role.HasValue == nullable.HasValue;
		}

		public override int GetHashCode()
		{
			bool? role = base.Role;
			return base.LinkName.GetHashCode() * 397 + role.GetHashCode();
		}
	}
}
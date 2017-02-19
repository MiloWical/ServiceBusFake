using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.ServiceBus.Notifications
{
	[AmqpContract(Code=2L)]
	[DataContract(Name="AppleRegistrationDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class AppleRegistrationDescription : RegistrationDescription
	{
		internal const string ExpiryHeader = "ServiceBusNotification-Apns-Expiry";

		internal static Regex DeviceTokenRegex;

		internal override string AppPlatForm
		{
			get
			{
				return "apple";
			}
		}

		[AmqpMember(Order=3, Mandatory=false)]
		[DataMember(Name="DeviceToken", Order=2001, IsRequired=true)]
		public string DeviceToken
		{
			get;
			set;
		}

		internal override string PlatformType
		{
			get
			{
				return "apple";
			}
		}

		internal override string RegistrationType
		{
			get
			{
				return "apple";
			}
		}

		static AppleRegistrationDescription()
		{
			AppleRegistrationDescription.DeviceTokenRegex = new Regex("^[a-fA-F0-9]+$");
		}

		public AppleRegistrationDescription(AppleRegistrationDescription sourceRegistration) : base(sourceRegistration)
		{
			this.DeviceToken = sourceRegistration.DeviceToken;
		}

		public AppleRegistrationDescription(string deviceToken) : this(string.Empty, deviceToken, null)
		{
		}

		public AppleRegistrationDescription(string deviceToken, IEnumerable<string> tags) : this(string.Empty, deviceToken, tags)
		{
		}

		internal AppleRegistrationDescription(string notificationHubPath, string deviceToken, IEnumerable<string> tags) : base(notificationHubPath)
		{
			if (string.IsNullOrWhiteSpace(deviceToken))
			{
				throw new ArgumentNullException("deviceToken");
			}
			this.DeviceToken = deviceToken;
			if (tags != null)
			{
				base.Tags = new HashSet<string>(tags);
			}
		}

		internal override RegistrationDescription Clone()
		{
			return new AppleRegistrationDescription(this);
		}

		internal byte[] GetDeviceTokenBytes()
		{
			return AppleRegistrationDescription.GetDeviceTokenBytes(this.DeviceToken);
		}

		internal static byte[] GetDeviceTokenBytes(string deviceToken)
		{
			if (string.IsNullOrWhiteSpace(deviceToken))
			{
				throw new InvalidDataContractException(SRClient.DeviceTokenIsEmpty);
			}
			if (!AppleRegistrationDescription.DeviceTokenRegex.IsMatch(deviceToken) || deviceToken.Length % 2 != 0)
			{
				throw new InvalidDataContractException(SRClient.DeviceTokenHexaDecimalDigitError);
			}
			byte[] numArray = new byte[deviceToken.Length / 2];
			for (int i = 0; i < (int)numArray.Length; i++)
			{
				numArray[i] = byte.Parse(deviceToken.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			}
			return numArray;
		}

		internal override string GetPnsHandle()
		{
			return this.DeviceToken.ToUpperInvariant();
		}

		internal override void OnValidate(bool allowLocalMockPns, ApiVersion version)
		{
			if (string.IsNullOrWhiteSpace(this.DeviceToken))
			{
				throw new InvalidDataContractException(SRClient.DeviceTokenIsEmpty);
			}
			if (!AppleRegistrationDescription.DeviceTokenRegex.IsMatch(this.DeviceToken) || this.DeviceToken.Length % 2 != 0)
			{
				throw new InvalidDataContractException(SRClient.DeviceTokenHexaDecimalDigitError);
			}
		}

		internal override void SetPnsHandle(string pnsHandle)
		{
			this.DeviceToken = pnsHandle;
		}
	}
}
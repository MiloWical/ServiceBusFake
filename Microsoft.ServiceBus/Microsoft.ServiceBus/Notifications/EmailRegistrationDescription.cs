using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.ServiceBus.Notifications
{
	[AmqpContract(Code=4L)]
	[DataContract(Name="EmailRegistrationDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal class EmailRegistrationDescription : RegistrationDescription
	{
		internal static Regex EmailAddressRegex;

		internal override string AppPlatForm
		{
			get
			{
				return "smtp";
			}
		}

		[AmqpMember(Mandatory=false, Order=3)]
		[DataMember(Name="EmailAddress", IsRequired=true)]
		public string EmailAddress
		{
			get;
			private set;
		}

		internal override string PlatformType
		{
			get
			{
				return "smtp";
			}
		}

		internal override string RegistrationType
		{
			get
			{
				return "smtp";
			}
		}

		static EmailRegistrationDescription()
		{
			EmailRegistrationDescription.EmailAddressRegex = new Regex("^[\\w!#$%&'*+/=?`{|}~^-]+(?:\\.[\\w!#$%&'*+/=?`{|}~^-]+)*@(?:[A-Za-z0-9-]+\\.)+[A-Za-z]{2,6}$");
		}

		public EmailRegistrationDescription(EmailRegistrationDescription description) : this(description.EmailAddress)
		{
		}

		public EmailRegistrationDescription(string emailAddress) : this(string.Empty, emailAddress)
		{
			this.EmailAddress = emailAddress;
		}

		internal EmailRegistrationDescription(string notificationHubPath, string emailAddress) : base(notificationHubPath)
		{
			this.EmailAddress = emailAddress;
		}

		internal override RegistrationDescription Clone()
		{
			return new EmailRegistrationDescription(this);
		}

		internal override string GetPnsHandle()
		{
			return this.EmailAddress;
		}

		internal override void OnValidate(bool allowLocalMockPns, ApiVersion version)
		{
			if (string.IsNullOrWhiteSpace(this.EmailAddress) || !EmailRegistrationDescription.EmailAddressRegex.IsMatch(this.EmailAddress))
			{
				throw new InvalidDataContractException("Email Address is invalid");
			}
		}

		internal override void SetPnsHandle(string pnsHandle)
		{
			this.EmailAddress = pnsHandle;
		}
	}
}
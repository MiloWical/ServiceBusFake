using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="SmtpCredential", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal class SmtpCredential : PnsCredential
	{
		internal const string AppPlatformName = "smtp";

		internal override string AppPlatform
		{
			get
			{
				return "smtp";
			}
		}

		public bool EnableSsl
		{
			get
			{
				bool flag;
				string item = base["EnableSsl"];
				if (string.IsNullOrEmpty(item) || !bool.TryParse(item, out flag))
				{
					throw new InvalidDataContractException("Illegal EnableSsl value");
				}
				return flag;
			}
			set
			{
				base["EnableSsl"] = value.ToString(CultureInfo.InvariantCulture);
			}
		}

		public string FromAddress
		{
			get
			{
				return base["FromAddress"];
			}
			set
			{
				base["FromAddress"] = value;
			}
		}

		public string Password
		{
			get
			{
				return base["Password"];
			}
			set
			{
				base["Password"] = value;
			}
		}

		public ushort Port
		{
			get
			{
				ushort num;
				string item = base["Port"];
				if (string.IsNullOrEmpty(item) || !ushort.TryParse(item, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
				{
					throw new InvalidDataContractException("Illegal Port value");
				}
				return num;
			}
			set
			{
				base["Port"] = value.ToString(CultureInfo.InvariantCulture);
			}
		}

		public string SmtpHost
		{
			get
			{
				return base["SmtpHost"];
			}
			set
			{
				base["SmtpHost"] = value;
			}
		}

		public string UserName
		{
			get
			{
				return base["UserName"];
			}
			set
			{
				base["UserName"] = value;
			}
		}

		public SmtpCredential(string smtpHost, ushort port, string userName, string password, bool enableSsl, string fromAddress)
		{
			this.SmtpHost = smtpHost;
			this.Port = port;
			this.UserName = userName;
			this.Password = password;
			this.EnableSsl = enableSsl;
			this.FromAddress = fromAddress;
		}

		protected override void OnValidate(bool allowLocalMockPns)
		{
			if (base.Properties == null || base.Properties.Count > 6)
			{
				throw new InvalidDataContractException("Only the SmtpHost, Port, EnableSsl, Username, Password and FromAddress should be specified");
			}
			if (base.Properties.Count < 6 || string.IsNullOrWhiteSpace(this.SmtpHost) || this.Port == 0 || string.IsNullOrEmpty(this.UserName) || string.IsNullOrEmpty(this.Password) || this.EnableSsl != this.EnableSsl || string.IsNullOrEmpty(this.FromAddress) || !EmailRegistrationDescription.EmailAddressRegex.IsMatch(this.FromAddress))
			{
				throw new InvalidDataContractException("One or more properties is missing or invalid");
			}
		}
	}
}
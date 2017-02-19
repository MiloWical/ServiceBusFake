using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Notifications
{
	internal class ContinuationToken
	{
		public bool IsValid
		{
			get;
			private set;
		}

		public string NextPartition
		{
			get;
			private set;
		}

		public int Skip
		{
			get;
			private set;
		}

		public string Token
		{
			get;
			private set;
		}

		public ContinuationToken(string nextPartition, int skip)
		{
			if (string.IsNullOrWhiteSpace(nextPartition))
			{
				throw new ArgumentNullException("nextPartition");
			}
			this.NextPartition = nextPartition;
			this.Skip = skip;
			this.Token = ContinuationToken.ToBase64UriEscapeString(string.Concat(nextPartition, ";", skip.ToString(CultureInfo.InvariantCulture)));
			this.IsValid = true;
		}

		public ContinuationToken(string continuationTokenString)
		{
			this.IsValid = false;
			if (string.IsNullOrWhiteSpace(continuationTokenString))
			{
				this.IsValid = true;
				return;
			}
			string str = ContinuationToken.FromBase64UriEscapeString(continuationTokenString);
			if (str == null)
			{
				return;
			}
			string[] strArrays = str.Split(new char[] { ';' });
			if ((int)strArrays.Length != 2)
			{
				return;
			}
			this.NextPartition = strArrays[0];
			int num = 0;
			if (!string.IsNullOrWhiteSpace(strArrays[1]) && !int.TryParse(strArrays[1], out num))
			{
				num = 0;
			}
			this.Skip = num;
			this.Token = continuationTokenString;
			this.IsValid = true;
		}

		private static string FromBase64UriEscapeString(string base64Value)
		{
			string str;
			if (string.IsNullOrWhiteSpace(base64Value))
			{
				return null;
			}
			try
			{
				str = Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(base64Value)));
			}
			catch (ArgumentNullException argumentNullException)
			{
				str = null;
			}
			catch (FormatException formatException)
			{
				str = null;
			}
			return str;
		}

		private static string ToBase64UriEscapeString(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
			return Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(value)));
		}
	}
}
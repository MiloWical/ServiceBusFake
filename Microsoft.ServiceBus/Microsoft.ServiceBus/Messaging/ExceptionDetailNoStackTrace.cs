using Microsoft.ServiceBus.Common;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="ExceptionDetail", Namespace="http://schemas.datacontract.org/2004/07/System.ServiceModel")]
	internal sealed class ExceptionDetailNoStackTrace
	{
		[DataMember]
		public string HelpLink
		{
			get;
			private set;
		}

		[DataMember]
		public ExceptionDetailNoStackTrace InnerException
		{
			get;
			private set;
		}

		[DataMember]
		public string Message
		{
			get;
			private set;
		}

		[DataMember]
		public string StackTrace
		{
			get;
			private set;
		}

		[DataMember]
		public string Type
		{
			get;
			private set;
		}

		public ExceptionDetailNoStackTrace(ExceptionDetailNoStackTrace detail) : this(detail, false)
		{
		}

		public ExceptionDetailNoStackTrace(ExceptionDetailNoStackTrace detail, bool includeInnerException)
		{
			this.HelpLink = string.Empty;
			this.Message = detail.Message;
			this.Type = detail.Type;
			this.StackTrace = string.Empty;
			this.InnerException = null;
			if (includeInnerException && detail.InnerException != null)
			{
				this.InnerException = new ExceptionDetailNoStackTrace(detail.InnerException, includeInnerException);
			}
		}

		public ExceptionDetailNoStackTrace(ExceptionDetail detail) : this(detail, false)
		{
		}

		public ExceptionDetailNoStackTrace(ExceptionDetail detail, bool includeInnerException)
		{
			this.HelpLink = string.Empty;
			this.Message = detail.Message;
			this.Type = detail.Type;
			this.StackTrace = string.Empty;
			this.InnerException = null;
			if (includeInnerException && detail.InnerException != null)
			{
				this.InnerException = new ExceptionDetailNoStackTrace(detail.InnerException, includeInnerException);
			}
		}

		public ExceptionDetailNoStackTrace(Exception exception) : this(exception, false)
		{
		}

		public ExceptionDetailNoStackTrace(Exception exception, bool includeInnerException)
		{
			this.HelpLink = exception.HelpLink;
			this.Message = exception.Message;
			this.Type = exception.GetType().ToString();
			this.StackTrace = string.Empty;
			this.InnerException = null;
			if (includeInnerException && exception.InnerException != null)
			{
				this.InnerException = new ExceptionDetailNoStackTrace(exception.InnerException, includeInnerException);
			}
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(this.GetType().Name);
			stringBuilder.Append(": ");
			stringBuilder.Append(this.Type);
			stringBuilder.Append(": ");
			stringBuilder.Append(this.Message);
			if (this.InnerException != null)
			{
				stringBuilder.Append("--->");
				stringBuilder.AppendLine(this.InnerException.ToString());
				stringBuilder.Append(SRCore.EndOfInnerExceptionStackTrace);
			}
			stringBuilder.AppendLine();
			stringBuilder.Append(this.StackTrace);
			return stringBuilder.ToString();
		}
	}
}
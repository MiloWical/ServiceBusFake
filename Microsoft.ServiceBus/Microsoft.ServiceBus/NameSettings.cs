using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="NameSettings", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	internal class NameSettings : IExtensibleDataObject, IEndpointBehavior, IExtension<OperationContext>
	{
		[DataMember(Name="Uri", IsRequired=false, EmitDefaultValue=false, Order=0)]
		private System.Uri uri;

		[DataMember(Name="DisplayName", IsRequired=false, EmitDefaultValue=false, Order=1)]
		private string displayName;

		[DataMember(Name="Owner", IsRequired=false, EmitDefaultValue=false, Order=2)]
		private string owner;

		[DataMember(Name="ServiceSettings", IsRequired=false, EmitDefaultValue=false, Order=3)]
		private Microsoft.ServiceBus.ServiceSettings serviceSettings;

		private ExtensionDataObject extensionData;

		public string DisplayName
		{
			get
			{
				return this.displayName;
			}
			set
			{
				this.displayName = value;
			}
		}

		internal string Owner
		{
			get
			{
				return this.owner;
			}
			set
			{
				this.owner = value;
			}
		}

		public Microsoft.ServiceBus.ServiceSettings ServiceSettings
		{
			get
			{
				return this.serviceSettings;
			}
			set
			{
				this.serviceSettings = value;
			}
		}

		ExtensionDataObject System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
		{
			get
			{
				return this.extensionData;
			}
			set
			{
				this.extensionData = value;
			}
		}

		public System.Uri Uri
		{
			get
			{
				return this.uri;
			}
			internal set
			{
				this.uri = value;
			}
		}

		public NameSettings()
		{
			this.serviceSettings = new Microsoft.ServiceBus.ServiceSettings();
		}

		internal NameSettings(NameSettings nameSettings)
		{
			this.uri = nameSettings.uri;
			this.displayName = nameSettings.DisplayName;
			this.owner = nameSettings.Owner;
			this.serviceSettings = new Microsoft.ServiceBus.ServiceSettings(nameSettings.ServiceSettings);
		}

		internal bool IsCompatible(NameSettings nameSettings)
		{
			if (this.uri == null && nameSettings.uri != null || this.uri != null && nameSettings.uri == null)
			{
				return false;
			}
			bool flag = (this.uri != null ? false : nameSettings.uri == null);
			if (!flag)
			{
				string str = this.uri.AbsoluteUri.TrimEnd(new char[] { '/' });
				string absoluteUri = nameSettings.uri.AbsoluteUri;
				char[] chrArray = new char[] { '/' };
				flag = str.Equals(absoluteUri.TrimEnd(chrArray), StringComparison.OrdinalIgnoreCase);
			}
			bool flag1 = this.serviceSettings.IsCompatible(nameSettings.serviceSettings);
			if (flag)
			{
				return flag1;
			}
			return false;
		}

		void System.ServiceModel.Description.IEndpointBehavior.AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{
			bindingParameters.Add(this);
		}

		void System.ServiceModel.Description.IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
		}

		void System.ServiceModel.Description.IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
		}

		void System.ServiceModel.Description.IEndpointBehavior.Validate(ServiceEndpoint endpoint)
		{
		}

		void System.ServiceModel.IExtension<System.ServiceModel.OperationContext>.Attach(OperationContext owner)
		{
		}

		void System.ServiceModel.IExtension<System.ServiceModel.OperationContext>.Detach(OperationContext owner)
		{
		}
	}
}
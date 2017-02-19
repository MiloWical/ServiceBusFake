using System;
using System.Configuration;

namespace System.ServiceModel.Configuration
{
	internal class MachineSettingsSection : ConfigurationSection
	{
		private const string enableLoggingKnownPiiKey = "enableLoggingKnownPii";

		private static bool enableLoggingKnownPii;

		private static bool hasInitialized;

		private static object syncRoot;

		private ConfigurationPropertyCollection properties;

		public static bool EnableLoggingKnownPii
		{
			get
			{
				if (!MachineSettingsSection.hasInitialized)
				{
					lock (MachineSettingsSection.syncRoot)
					{
						if (!MachineSettingsSection.hasInitialized)
						{
							MachineSettingsSection section = (MachineSettingsSection)ConfigurationManager.GetSection("system.serviceModel/machineSettings");
							MachineSettingsSection.enableLoggingKnownPii = (bool)section["enableLoggingKnownPii"];
							MachineSettingsSection.hasInitialized = true;
						}
					}
				}
				return MachineSettingsSection.enableLoggingKnownPii;
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection configurationPropertyCollections = new ConfigurationPropertyCollection()
					{
						new ConfigurationProperty("enableLoggingKnownPii", typeof(bool), false, null, null, ConfigurationPropertyOptions.None)
					};
					this.properties = configurationPropertyCollections;
				}
				return this.properties;
			}
		}

		static MachineSettingsSection()
		{
			MachineSettingsSection.syncRoot = new object();
		}

		public MachineSettingsSection()
		{
		}
	}
}
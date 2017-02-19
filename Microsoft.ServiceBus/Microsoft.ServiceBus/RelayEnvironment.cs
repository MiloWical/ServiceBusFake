using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal class RelayEnvironment
	{
		public const string RelayEnvEnvironmentVariable = "RELAYENV";

		public const string StsEnabledEnvironmentVariable = "RELAYSTSENABLED";

		public const string AcsVersionVariable = "ACSVERSION";

		private const int DefaultHttpPort = 80;

		private const int DefaultHttpsPort = 443;

		private const int DefaultNmfPort = 9354;

		private readonly static RelayEnvironment.MutableEnvironment Environment;

		public static string RelayHostRootName
		{
			get
			{
				return RelayEnvironment.Environment.RelayHostRootName;
			}
			set
			{
				RelayEnvironment.Environment.RelayHostRootName = value;
			}
		}

		public static int RelayHttpPort
		{
			get
			{
				return RelayEnvironment.Environment.RelayHttpPort;
			}
		}

		public static int RelayHttpsPort
		{
			get
			{
				return RelayEnvironment.Environment.RelayHttpsPort;
			}
		}

		public static int RelayNmfPort
		{
			get
			{
				return RelayEnvironment.Environment.RelayNmfPort;
			}
		}

		internal static string RelayPathPrefix
		{
			get
			{
				return RelayEnvironment.Environment.RelayPathPrefix;
			}
		}

		public static bool StsEnabled
		{
			get
			{
				return RelayEnvironment.Environment.StsEnabled;
			}
			set
			{
				RelayEnvironment.Environment.StsEnabled = value;
			}
		}

		public static string StsHostName
		{
			get
			{
				return RelayEnvironment.Environment.StsHostName;
			}
		}

		public static int StsHttpPort
		{
			get
			{
				return RelayEnvironment.Environment.StsHttpPort;
			}
		}

		public static int StsHttpsPort
		{
			get
			{
				return RelayEnvironment.Environment.StsHttpsPort;
			}
		}

		static RelayEnvironment()
		{
			string environmentVariable = System.Environment.GetEnvironmentVariable("RELAYENV");
			if (environmentVariable == null)
			{
				RelayEnvironment.ConfigSettings configSetting = new RelayEnvironment.ConfigSettings();
				if (configSetting.HaveSettings)
				{
					RelayEnvironment.Environment = new RelayEnvironment.MutableEnvironment(configSetting);
					return;
				}
				RelayEnvironment.Environment = new RelayEnvironment.MutableEnvironment(new RelayEnvironment.LiveEnvironment());
				return;
			}
			string upperInvariant = environmentVariable.ToUpperInvariant();
			string str = upperInvariant;
			if (upperInvariant != null)
			{
				switch (str)
				{
					case "LIVE":
					{
						RelayEnvironment.Environment = new RelayEnvironment.MutableEnvironment(new RelayEnvironment.LiveEnvironment());
						return;
					}
					case "PPE":
					{
						RelayEnvironment.Environment = new RelayEnvironment.MutableEnvironment(new RelayEnvironment.PpeEnvironment());
						return;
					}
					case "BVT":
					{
						RelayEnvironment.Environment = new RelayEnvironment.MutableEnvironment(new RelayEnvironment.BvtEnvironment());
						return;
					}
					case "INT":
					{
						RelayEnvironment.Environment = new RelayEnvironment.MutableEnvironment(new RelayEnvironment.IntEnvironment());
						return;
					}
					case "LOCAL":
					{
						RelayEnvironment.Environment = new RelayEnvironment.MutableEnvironment(new RelayEnvironment.LocalEnvironment());
						return;
					}
					case "CUSTOM":
					{
						RelayEnvironment.Environment = new RelayEnvironment.MutableEnvironment(new RelayEnvironment.CustomEnvironment());
						return;
					}
				}
			}
			RelayEnvironment.Environment = new RelayEnvironment.MutableEnvironment(new RelayEnvironment.LiveEnvironment());
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { environmentVariable };
			string str1 = string.Format(invariantCulture, "Invalid RELAYENV value: {0}, valid values = LIVE, PPE, INT", objArray);
			EventLog.WriteEntry("MSCSH", str1, EventLogEntryType.Error, 0);
		}

		public RelayEnvironment()
		{
		}

		public static bool GetEnvironmentVariable(string variable, bool defaultValue)
		{
			bool flag;
			string environmentVariable = System.Environment.GetEnvironmentVariable(variable);
			if (environmentVariable != null && bool.TryParse(environmentVariable, out flag))
			{
				return flag;
			}
			return defaultValue;
		}

		public static int GetEnvironmentVariable(string variable, int defaultValue)
		{
			int num;
			string environmentVariable = System.Environment.GetEnvironmentVariable(variable);
			if (!string.IsNullOrEmpty(environmentVariable) && int.TryParse(environmentVariable, out num))
			{
				return num;
			}
			return defaultValue;
		}

		private class BvtEnvironment : RelayEnvironment.EnvironmentBase
		{
			public override string RelayHostRootName
			{
				get
				{
					return "servicebus.windows-bvt.net";
				}
			}

			public override string StsHostName
			{
				get
				{
					return "accesscontrol.windows-ppe.net";
				}
			}

			public BvtEnvironment()
			{
			}
		}

		private class ConfigSettings : RelayEnvironment.IEnvironment
		{
			private const string RelayHostNameElement = "relayHostName";

			private const string RelayHttpPortNameElement = "relayHttpPort";

			private const string RelayHttpsPortNameElement = "relayHttpsPort";

			private const string RelayNmfPortNameElement = "relayNmfPort";

			private const string RelayPathPrefixElement = "relayPathPrefix";

			private const string StsHostNameElement = "stsHostName";

			private const string StsEnabledElement = "stsEnabled";

			private const string StsHttpPortNameElement = "stsHttpPort";

			private const string StsHttpsPortNameElement = "stsHttpsPort";

			private const string V1ConfigFileName = "servicebus.config";

			private const string WebRootPath = "approot\\";

			private readonly string configFileName;

			private bool haveSettings;

			private string relayHostName;

			private int relayHttpPort;

			private int relayHttpsPort;

			private int relayNmfPort;

			private string relayPathPrefix;

			private string stsHostName;

			private bool stsEnabled;

			private int stsHttpPort;

			private int stsHttpsPort;

			public bool HaveSettings
			{
				get
				{
					return this.haveSettings;
				}
			}

			public string RelayHostRootName
			{
				get
				{
					return this.relayHostName;
				}
			}

			public int RelayHttpPort
			{
				get
				{
					return this.relayHttpPort;
				}
			}

			public int RelayHttpsPort
			{
				get
				{
					return this.relayHttpsPort;
				}
			}

			public int RelayNmfPort
			{
				get
				{
					return this.relayNmfPort;
				}
			}

			public string RelayPathPrefix
			{
				get
				{
					return this.relayPathPrefix;
				}
			}

			public bool StsEnabled
			{
				get
				{
					return this.stsEnabled;
				}
			}

			public string StsHostName
			{
				get
				{
					return this.stsHostName;
				}
			}

			public int StsHttpPort
			{
				get
				{
					return this.stsHttpPort;
				}
			}

			public int StsHttpsPort
			{
				get
				{
					return this.stsHttpsPort;
				}
			}

			public ConfigSettings()
			{
				this.configFileName = "servicebus.config";
				this.ReadConfigSettings();
			}

			private void ReadConfigSettings()
			{
				string str;
				int num;
				this.haveSettings = false;
				string str1 = this.configFileName;
				string str2 = Path.Combine("approot\\", this.configFileName);
				if (File.Exists(str1))
				{
					str = str1;
				}
				else if (!File.Exists(str2))
				{
					string directoryName = Path.GetDirectoryName(ConfigurationManager.OpenMachineConfiguration().FilePath);
					str = Path.Combine(directoryName, this.configFileName);
				}
				else
				{
					str = str2;
				}
				RelayEnvironment.LiveEnvironment liveEnvironment = new RelayEnvironment.LiveEnvironment();
				this.relayHostName = liveEnvironment.RelayHostRootName;
				this.relayHttpPort = liveEnvironment.RelayHttpPort;
				this.relayHttpsPort = liveEnvironment.RelayHttpsPort;
				this.relayNmfPort = liveEnvironment.RelayNmfPort;
				this.relayPathPrefix = liveEnvironment.RelayPathPrefix;
				this.stsHostName = liveEnvironment.StsHostName;
				this.stsEnabled = liveEnvironment.StsEnabled;
				this.stsHttpPort = liveEnvironment.StsHttpPort;
				this.stsHttpsPort = liveEnvironment.StsHttpsPort;
				if (File.Exists(str))
				{
					Stream stream = File.OpenRead(str);
					XmlReader xmlReader = XmlReader.Create(stream);
					xmlReader.ReadStartElement("configuration");
					xmlReader.ReadStartElement("Microsoft.ServiceBus");
					while (xmlReader.IsStartElement())
					{
						string name = xmlReader.Name;
						string str3 = xmlReader.ReadElementString();
						string str4 = name;
						string str5 = str4;
						if (str4 == null)
						{
							continue;
						}
						if (<PrivateImplementationDetails>{06D7FFF6-9ACE-4BDE-B357-6278D36DEFF2}.$$method0x6002012-1 == null)
						{
							<PrivateImplementationDetails>{06D7FFF6-9ACE-4BDE-B357-6278D36DEFF2}.$$method0x6002012-1 = new Dictionary<string, int>(8)
							{
								{ "relayHostName", 0 },
								{ "relayHttpPort", 1 },
								{ "relayHttpsPort", 2 },
								{ "relayNmfPort", 3 },
								{ "relayPathPrefix", 4 },
								{ "stsHostName", 5 },
								{ "stsHttpPort", 6 },
								{ "stsHttpsPort", 7 }
							};
						}
						if (!<PrivateImplementationDetails>{06D7FFF6-9ACE-4BDE-B357-6278D36DEFF2}.$$method0x6002012-1.TryGetValue(str5, out num))
						{
							continue;
						}
						switch (num)
						{
							case 0:
							{
								this.relayHostName = str3;
								continue;
							}
							case 1:
							{
								this.relayHttpPort = int.Parse(str3, CultureInfo.InvariantCulture);
								continue;
							}
							case 2:
							{
								this.relayHttpsPort = int.Parse(str3, CultureInfo.InvariantCulture);
								continue;
							}
							case 3:
							{
								this.relayNmfPort = int.Parse(str3, CultureInfo.InvariantCulture);
								continue;
							}
							case 4:
							{
								this.relayPathPrefix = str3;
								if (!this.relayPathPrefix.StartsWith("/", StringComparison.OrdinalIgnoreCase))
								{
									this.relayPathPrefix = string.Concat("/", this.relayPathPrefix);
								}
								if (!this.relayPathPrefix.EndsWith("/", StringComparison.Ordinal))
								{
									continue;
								}
								this.relayPathPrefix = this.relayPathPrefix.Substring(0, this.relayPathPrefix.Length - 1);
								continue;
							}
							case 5:
							{
								this.stsHostName = str3;
								continue;
							}
							case 6:
							{
								this.stsHttpPort = int.Parse(str3, CultureInfo.InvariantCulture);
								continue;
							}
							case 7:
							{
								this.stsHttpsPort = int.Parse(str3, CultureInfo.InvariantCulture);
								continue;
							}
							default:
							{
								continue;
							}
						}
					}
					xmlReader.ReadEndElement();
					xmlReader.ReadEndElement();
					stream.Close();
					this.haveSettings = true;
				}
			}
		}

		private class CustomEnvironment : RelayEnvironment.IEnvironment
		{
			private const string RelayHostEnvironmentVariable = "RELAYHOST";

			private const string RelayHttpPortEnvironmentVariable = "RELAYHTTPPORT";

			private const string RelayHttpsPortEnvironmentVariable = "RELAYHTTPSPORT";

			private const string RelayNmfPortEnvironmentVariable = "RELAYNMFPORT";

			private const string RelayPathPrefixEnvironmentVariable = "RELAYPATHPREFIX";

			private const string StsHostEnvironmentVariable = "STSHOST";

			private const string StsHttpPortEnvironmentVariable = "STSHTTPPORT";

			private const string StsHttpsPortEnvironmentVariable = "STSHTTPSPORT";

			private string relayHostRootName;

			private int relayHttpPort;

			private int relayHttpsPort;

			private string relayPathPrefix;

			private string stsHostName;

			private bool stsEnabled;

			private int stsHttpPort;

			private int stsHttpsPort;

			private int relayNmfPort;

			public string RelayHostRootName
			{
				get
				{
					return this.relayHostRootName;
				}
			}

			public int RelayHttpPort
			{
				get
				{
					return this.relayHttpPort;
				}
			}

			public int RelayHttpsPort
			{
				get
				{
					return this.relayHttpsPort;
				}
			}

			public int RelayNmfPort
			{
				get
				{
					return this.relayNmfPort;
				}
			}

			public string RelayPathPrefix
			{
				get
				{
					return this.relayPathPrefix;
				}
			}

			public bool StsEnabled
			{
				get
				{
					return this.stsEnabled;
				}
			}

			public string StsHostName
			{
				get
				{
					return this.stsHostName;
				}
			}

			public int StsHttpPort
			{
				get
				{
					return this.stsHttpPort;
				}
			}

			public int StsHttpsPort
			{
				get
				{
					return this.stsHttpsPort;
				}
			}

			public CustomEnvironment()
			{
				this.relayHostRootName = System.Environment.GetEnvironmentVariable("RELAYHOST");
				this.relayHttpPort = RelayEnvironment.GetEnvironmentVariable("RELAYHTTPPORT", 80);
				this.relayHttpsPort = RelayEnvironment.GetEnvironmentVariable("RELAYHTTPSPORT", 443);
				this.relayPathPrefix = System.Environment.GetEnvironmentVariable("RELAYPATHPREFIX");
				this.stsHostName = System.Environment.GetEnvironmentVariable("STSHOST");
				this.stsEnabled = true;
				this.stsHttpPort = RelayEnvironment.GetEnvironmentVariable("STSHTTPPORT", 80);
				this.stsHttpsPort = RelayEnvironment.GetEnvironmentVariable("STSHTTPSPORT", 443);
				this.relayNmfPort = RelayEnvironment.GetEnvironmentVariable("RELAYNMFPORT", 9354);
			}
		}

		private abstract class EnvironmentBase : RelayEnvironment.IEnvironment
		{
			public abstract string RelayHostRootName
			{
				get;
			}

			public virtual int RelayHttpPort
			{
				get
				{
					return 80;
				}
			}

			public virtual int RelayHttpsPort
			{
				get
				{
					return 443;
				}
			}

			public int RelayNmfPort
			{
				get
				{
					return 9354;
				}
			}

			public virtual string RelayPathPrefix
			{
				get
				{
					return string.Empty;
				}
			}

			public virtual bool StsEnabled
			{
				get
				{
					return true;
				}
			}

			public abstract string StsHostName
			{
				get;
			}

			public virtual int StsHttpPort
			{
				get
				{
					return 80;
				}
			}

			public virtual int StsHttpsPort
			{
				get
				{
					return 443;
				}
			}

			protected EnvironmentBase()
			{
			}
		}

		private interface IEnvironment
		{
			string RelayHostRootName
			{
				get;
			}

			int RelayHttpPort
			{
				get;
			}

			int RelayHttpsPort
			{
				get;
			}

			int RelayNmfPort
			{
				get;
			}

			string RelayPathPrefix
			{
				get;
			}

			bool StsEnabled
			{
				get;
			}

			string StsHostName
			{
				get;
			}

			int StsHttpPort
			{
				get;
			}

			int StsHttpsPort
			{
				get;
			}
		}

		private class IntEnvironment : RelayEnvironment.EnvironmentBase
		{
			public override string RelayHostRootName
			{
				get
				{
					return "servicebus.windows-int.net";
				}
			}

			public override string StsHostName
			{
				get
				{
					return "accesscontrol.windows-ppe.net";
				}
			}

			public IntEnvironment()
			{
			}
		}

		private class LabsEnvironment : RelayEnvironment.EnvironmentBase
		{
			public override string RelayHostRootName
			{
				get
				{
					return "servicebus.appfabriclabs.com";
				}
			}

			public override string StsHostName
			{
				get
				{
					return "accesscontrol.appfabriclabs.com";
				}
			}

			public LabsEnvironment()
			{
			}
		}

		private class LiveEnvironment : RelayEnvironment.EnvironmentBase
		{
			public override string RelayHostRootName
			{
				get
				{
					return "servicebus.windows.net";
				}
			}

			public override string StsHostName
			{
				get
				{
					return "accesscontrol.windows.net";
				}
			}

			public LiveEnvironment()
			{
			}
		}

		private class LocalEnvironment : RelayEnvironment.EnvironmentBase
		{
			public override string RelayHostRootName
			{
				get
				{
					return "zurich.test.dnsdemo1.com";
				}
			}

			public override bool StsEnabled
			{
				get
				{
					return false;
				}
			}

			public override string StsHostName
			{
				get
				{
					return "zurich.test.dnsdemo1.com";
				}
			}

			public LocalEnvironment()
			{
			}
		}

		private class MutableEnvironment : RelayEnvironment.IEnvironment
		{
			private string relayHostRootName;

			private int relayHttpPort;

			private int relayHttpsPort;

			private string relayPathPrefix;

			private string stsHostName;

			private bool stsEnabled;

			private int stsHttpPort;

			private int stsHttpsPort;

			private int relayNmfPort;

			public string RelayHostRootName
			{
				get
				{
					return get_RelayHostRootName();
				}
				set
				{
					set_RelayHostRootName(value);
				}
			}

			public string get_RelayHostRootName()
			{
				return this.relayHostRootName;
			}

			public void set_RelayHostRootName(string value)
			{
				this.relayHostRootName = value;
			}

			public int RelayHttpPort
			{
				get
				{
					return this.relayHttpPort;
				}
			}

			public int RelayHttpsPort
			{
				get
				{
					return this.relayHttpsPort;
				}
			}

			public int RelayNmfPort
			{
				get
				{
					return this.relayNmfPort;
				}
			}

			public string RelayPathPrefix
			{
				get
				{
					return this.relayPathPrefix;
				}
			}

			public bool StsEnabled
			{
				get
				{
					return get_StsEnabled();
				}
				set
				{
					set_StsEnabled(value);
				}
			}

			public bool get_StsEnabled()
			{
				return this.stsEnabled;
			}

			public void set_StsEnabled(bool value)
			{
				this.stsEnabled = value;
			}

			public string StsHostName
			{
				get
				{
					return this.stsHostName;
				}
			}

			public int StsHttpPort
			{
				get
				{
					return this.stsHttpPort;
				}
			}

			public int StsHttpsPort
			{
				get
				{
					return this.stsHttpsPort;
				}
			}

			public MutableEnvironment(RelayEnvironment.IEnvironment environment)
			{
				this.relayHostRootName = environment.RelayHostRootName;
				this.relayHttpPort = environment.RelayHttpPort;
				this.relayHttpsPort = environment.RelayHttpsPort;
				this.relayPathPrefix = environment.RelayPathPrefix;
				this.stsHostName = environment.StsHostName;
				this.stsEnabled = environment.StsEnabled;
				this.stsHttpPort = environment.StsHttpPort;
				this.stsHttpsPort = environment.StsHttpsPort;
				this.relayNmfPort = environment.RelayNmfPort;
			}
		}

		private class PpeEnvironment : RelayEnvironment.EnvironmentBase
		{
			public override string RelayHostRootName
			{
				get
				{
					return "servicebus.windows-ppe.net";
				}
			}

			public override string StsHostName
			{
				get
				{
					return "accesscontrol.windows-ppe.net";
				}
			}

			public PpeEnvironment()
			{
			}
		}
	}
}
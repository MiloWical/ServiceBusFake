using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.Web.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	internal static class ConfigurationHelpers
	{
		internal static BindingCollectionElement GetAssociatedBindingCollectionElement(ContextInformation evaluationContext, string bindingCollectionName)
		{
			BindingCollectionElement item = null;
			BindingsSection associatedSection = (BindingsSection)Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetAssociatedSection(evaluationContext, Microsoft.ServiceBus.Configuration.ConfigurationStrings.BindingsSectionGroupPath);
			if (associatedSection != null)
			{
				try
				{
					item = associatedSection[bindingCollectionName];
				}
				catch (KeyNotFoundException keyNotFoundException)
				{
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string configBindingExtensionNotFound = Resources.ConfigBindingExtensionNotFound;
					object[] bindingsSectionPath = new object[] { Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetBindingsSectionPath(bindingCollectionName) };
					throw exceptionUtility.ThrowHelperError(new ConfigurationErrorsException(Microsoft.ServiceBus.SR.GetString(configBindingExtensionNotFound, bindingsSectionPath)));
				}
				catch (NullReferenceException nullReferenceException)
				{
					ExceptionUtility exceptionUtility1 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string str = Resources.ConfigBindingExtensionNotFound;
					object[] objArray = new object[] { Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetBindingsSectionPath(bindingCollectionName) };
					throw exceptionUtility1.ThrowHelperError(new ConfigurationErrorsException(Microsoft.ServiceBus.SR.GetString(str, objArray)));
				}
			}
			return item;
		}

		internal static object GetAssociatedSection(ContextInformation evalContext, string sectionPath)
		{
			object section = null;
			if (evalContext == null)
			{
				section = (!(bool)InvokeHelper.InvokeStaticGet(typeof(ServiceHostingEnvironment), "IsHosted") ? ConfigurationManager.GetSection(sectionPath) : Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetSectionFromWebConfigurationManager(sectionPath));
				if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceVerbose)
				{
					TraceUtility.TraceEvent(TraceEventType.Verbose, TraceCode.GetConfigurationSection, new StringTraceRecord("ConfigurationSection", sectionPath), null, null);
				}
			}
			else
			{
				section = evalContext.GetSection(sectionPath);
			}
			if (section == null)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string configSectionNotFound = Resources.ConfigSectionNotFound;
				object[] objArray = new object[] { sectionPath };
				throw exceptionUtility.ThrowHelperError(new ConfigurationErrorsException(Microsoft.ServiceBus.SR.GetString(configSectionNotFound, objArray)));
			}
			return section;
		}

		internal static BindingCollectionElement GetBindingCollectionElement(string bindingCollectionName)
		{
			return Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetAssociatedBindingCollectionElement(null, bindingCollectionName);
		}

		internal static string GetBindingsSectionPath(string sectionName)
		{
			return string.Concat(Microsoft.ServiceBus.Configuration.ConfigurationStrings.BindingsSectionGroupPath, "/", sectionName);
		}

		internal static X509RevocationMode GetCertificateRevocationMode()
		{
			X509RevocationMode x509RevocationMode;
			X509RevocationMode x509RevocationMode1;
			try
			{
				if (Enum.TryParse<X509RevocationMode>(WebConfigurationManager.AppSettings["Microsoft.ServiceBus.X509RevocationMode"], true, out x509RevocationMode))
				{
					x509RevocationMode1 = x509RevocationMode;
					return x509RevocationMode1;
				}
			}
			catch (ConfigurationErrorsException configurationErrorsException)
			{
			}
			try
			{
				if (Enum.TryParse<X509RevocationMode>(ConfigurationManager.AppSettings["Microsoft.ServiceBus.X509RevocationMode"], true, out x509RevocationMode))
				{
					x509RevocationMode1 = x509RevocationMode;
					return x509RevocationMode1;
				}
			}
			catch (ConfigurationErrorsException configurationErrorsException1)
			{
			}
			return X509RevocationMode.NoCheck;
		}

		internal static ContextInformation GetEvaluationContext(Microsoft.ServiceBus.Configuration.IConfigurationContextProviderInternal provider)
		{
			ContextInformation evaluationContext;
			if (provider != null)
			{
				try
				{
					evaluationContext = provider.GetEvaluationContext();
				}
				catch (ConfigurationErrorsException configurationErrorsException)
				{
					return null;
				}
				return evaluationContext;
			}
			return null;
		}

		internal static ContextInformation GetOriginalEvaluationContext(Microsoft.ServiceBus.Configuration.IConfigurationContextProviderInternal provider)
		{
			ContextInformation originalEvaluationContext;
			if (provider != null)
			{
				try
				{
					originalEvaluationContext = provider.GetOriginalEvaluationContext();
				}
				catch (ConfigurationErrorsException configurationErrorsException)
				{
					return null;
				}
				return originalEvaluationContext;
			}
			return null;
		}

		internal static InternalConnectivityMode? GetOverrideConnectivityMode()
		{
			InternalConnectivityMode internalConnectivityMode;
			InternalConnectivityMode? nullable;
			try
			{
				if (Enum.TryParse<InternalConnectivityMode>(WebConfigurationManager.AppSettings["Microsoft.ServiceBus.OverrideAutoDetectMode"], true, out internalConnectivityMode))
				{
					nullable = new InternalConnectivityMode?(internalConnectivityMode);
					return nullable;
				}
			}
			catch (ConfigurationErrorsException configurationErrorsException)
			{
			}
			try
			{
				if (Enum.TryParse<InternalConnectivityMode>(ConfigurationManager.AppSettings["Microsoft.ServiceBus.OverrideAutoDetectMode"], true, out internalConnectivityMode))
				{
					nullable = new InternalConnectivityMode?(internalConnectivityMode);
					return nullable;
				}
			}
			catch (ConfigurationErrorsException configurationErrorsException1)
			{
			}
			return null;
		}

		internal static object GetSection(string sectionPath)
		{
			return Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetAssociatedSection(null, sectionPath);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static object GetSectionFromWebConfigurationManager(string sectionPath)
		{
			string str = (string)InvokeHelper.InvokeStaticGet(typeof(ServiceHostingEnvironment), "CurrentVirtualPath");
			if (str == null)
			{
				return WebConfigurationManager.GetSection(sectionPath);
			}
			return WebConfigurationManager.GetSection(sectionPath, str);
		}

		internal static string GetSectionPath(string sectionName)
		{
			return string.Concat("system.serviceModel", "/", sectionName);
		}

		[SecurityCritical]
		internal static void SetIsPresent(ConfigurationElement element)
		{
			PropertyInfo property = element.GetType().GetProperty("ElementPresent", BindingFlags.Instance | BindingFlags.NonPublic);
			Microsoft.ServiceBus.Configuration.ConfigurationHelpers.SetIsPresentWithAssert(property, element, true);
		}

		[ReflectionPermission(SecurityAction.Assert, MemberAccess=true)]
		[SecurityCritical]
		private static void SetIsPresentWithAssert(PropertyInfo elementPresent, ConfigurationElement element, bool value)
		{
			elementPresent.SetValue(element, value, null);
		}

		[SecurityCritical]
		internal static BindingCollectionElement UnsafeGetAssociatedBindingCollectionElement(ContextInformation evaluationContext, string bindingCollectionName)
		{
			BindingCollectionElement item = null;
			BindingsSection bindingsSection = (BindingsSection)Microsoft.ServiceBus.Configuration.ConfigurationHelpers.UnsafeGetAssociatedSection(evaluationContext, Microsoft.ServiceBus.Configuration.ConfigurationStrings.BindingsSectionGroupPath);
			if (bindingsSection != null)
			{
				try
				{
					item = bindingsSection[bindingCollectionName];
				}
				catch (KeyNotFoundException keyNotFoundException)
				{
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string configBindingExtensionNotFound = Resources.ConfigBindingExtensionNotFound;
					object[] bindingsSectionPath = new object[] { Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetBindingsSectionPath(bindingCollectionName) };
					throw exceptionUtility.ThrowHelperError(new ConfigurationErrorsException(Microsoft.ServiceBus.SR.GetString(configBindingExtensionNotFound, bindingsSectionPath)));
				}
				catch (NullReferenceException nullReferenceException)
				{
					ExceptionUtility exceptionUtility1 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string str = Resources.ConfigBindingExtensionNotFound;
					object[] objArray = new object[] { Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetBindingsSectionPath(bindingCollectionName) };
					throw exceptionUtility1.ThrowHelperError(new ConfigurationErrorsException(Microsoft.ServiceBus.SR.GetString(str, objArray)));
				}
			}
			return item;
		}

		[SecurityCritical]
		internal static object UnsafeGetAssociatedSection(ContextInformation evalContext, string sectionPath)
		{
			object obj = null;
			if (evalContext == null)
			{
				if (!(bool)InvokeHelper.InvokeStaticGet(typeof(ServiceHostingEnvironment), "IsHosted"))
				{
					obj = Microsoft.ServiceBus.Configuration.ConfigurationHelpers.UnsafeGetSectionFromConfigurationManager(sectionPath);
				}
				else
				{
					string str = (string)InvokeHelper.InvokeStaticGet(typeof(ServiceHostingEnvironment), "CurrentVirtualPath");
					obj = (str == null ? Microsoft.ServiceBus.Configuration.ConfigurationHelpers.UnsafeGetSectionFromWebConfigurationManager(sectionPath) : Microsoft.ServiceBus.Configuration.ConfigurationHelpers.UnsafeGetSectionFromWebConfigurationManager(sectionPath, str));
				}
				if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceVerbose)
				{
					TraceUtility.TraceEvent(TraceEventType.Verbose, TraceCode.GetConfigurationSection, new StringTraceRecord("ConfigurationSection", sectionPath), null, null);
				}
			}
			else
			{
				obj = Microsoft.ServiceBus.Configuration.ConfigurationHelpers.UnsafeGetSectionFromContext(evalContext, sectionPath);
			}
			if (obj == null)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string configSectionNotFound = Resources.ConfigSectionNotFound;
				object[] objArray = new object[] { sectionPath };
				throw exceptionUtility.ThrowHelperError(new ConfigurationErrorsException(Microsoft.ServiceBus.SR.GetString(configSectionNotFound, objArray)));
			}
			return obj;
		}

		[SecurityCritical]
		internal static BindingCollectionElement UnsafeGetBindingCollectionElement(string bindingCollectionName)
		{
			return Microsoft.ServiceBus.Configuration.ConfigurationHelpers.UnsafeGetAssociatedBindingCollectionElement(null, bindingCollectionName);
		}

		[SecurityCritical]
		internal static object UnsafeGetSection(string sectionPath)
		{
			return Microsoft.ServiceBus.Configuration.ConfigurationHelpers.UnsafeGetAssociatedSection(null, sectionPath);
		}

		[ConfigurationPermission(SecurityAction.Assert, Unrestricted=true)]
		[SecurityCritical]
		private static object UnsafeGetSectionFromConfigurationManager(string sectionPath)
		{
			return ConfigurationManager.GetSection(sectionPath);
		}

		[ConfigurationPermission(SecurityAction.Assert, Unrestricted=true)]
		[SecurityCritical]
		private static object UnsafeGetSectionFromContext(ContextInformation evalContext, string sectionPath)
		{
			return evalContext.GetSection(sectionPath);
		}

		[ConfigurationPermission(SecurityAction.Assert, Unrestricted=true)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		[SecurityCritical]
		internal static object UnsafeGetSectionFromWebConfigurationManager(string sectionPath)
		{
			return WebConfigurationManager.GetSection(sectionPath);
		}

		[ConfigurationPermission(SecurityAction.Assert, Unrestricted=true)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		[SecurityCritical]
		private static object UnsafeGetSectionFromWebConfigurationManager(string sectionPath, string virtualPath)
		{
			return WebConfigurationManager.GetSection(sectionPath, virtualPath);
		}
	}
}
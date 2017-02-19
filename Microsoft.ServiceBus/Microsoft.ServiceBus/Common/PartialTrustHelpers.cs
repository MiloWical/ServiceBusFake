using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace Microsoft.ServiceBus.Common
{
	internal static class PartialTrustHelpers
	{
		[SecurityCritical]
		private static Type aptca;

		internal static bool ShouldFlowSecurityContext
		{
			[SecurityCritical]
			get
			{
				if (AppDomain.CurrentDomain.IsHomogenous)
				{
					return false;
				}
				return SecurityManager.CurrentThreadRequiresSecurityContextCapture();
			}
		}

		[SecurityCritical]
		internal static SecurityContext CaptureSecurityContextNoIdentityFlow()
		{
			SecurityContext securityContext;
			if (SecurityContext.IsWindowsIdentityFlowSuppressed())
			{
				return SecurityContext.Capture();
			}
			AsyncFlowControl asyncFlowControl = SecurityContext.SuppressFlowWindowsIdentity();
			try
			{
				securityContext = SecurityContext.Capture();
			}
			finally
			{
				((IDisposable)asyncFlowControl).Dispose();
			}
			return securityContext;
		}

		[SecurityCritical]
		internal static bool CheckAppDomainPermissions(PermissionSet permissions)
		{
			if (!AppDomain.CurrentDomain.IsHomogenous)
			{
				return false;
			}
			return permissions.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		[PermissionSet(SecurityAction.Demand, Name="FullTrust")]
		[SecurityCritical]
		private static void DemandForFullTrust()
		{
		}

		[SecurityCritical]
		internal static bool HasEtwPermissions()
		{
			return PartialTrustHelpers.CheckAppDomainPermissions(new PermissionSet(PermissionState.Unrestricted));
		}

		[SecurityCritical]
		private static bool IsAssemblyAptca(Assembly assembly)
		{
			if (PartialTrustHelpers.aptca == null)
			{
				PartialTrustHelpers.aptca = typeof(AllowPartiallyTrustedCallersAttribute);
			}
			return (int)assembly.GetCustomAttributes(PartialTrustHelpers.aptca, false).Length > 0;
		}

		[FileIOPermission(SecurityAction.Assert, Unrestricted=true)]
		[SecurityCritical]
		private static bool IsAssemblySigned(Assembly assembly)
		{
			byte[] publicKeyToken = assembly.GetName().GetPublicKeyToken();
			return publicKeyToken != null & (int)publicKeyToken.Length > 0;
		}

		[SecurityCritical]
		internal static bool IsInFullTrust()
		{
			bool flag;
			if (AppDomain.CurrentDomain.IsHomogenous)
			{
				return AppDomain.CurrentDomain.IsFullyTrusted;
			}
			if (!SecurityManager.CurrentThreadRequiresSecurityContextCapture())
			{
				return true;
			}
			try
			{
				PartialTrustHelpers.DemandForFullTrust();
				flag = true;
			}
			catch (SecurityException securityException)
			{
				flag = false;
			}
			return flag;
		}

		[SecurityCritical]
		internal static bool IsTypeAptca(Type type)
		{
			Assembly assembly = type.Assembly;
			if (PartialTrustHelpers.IsAssemblyAptca(assembly))
			{
				return true;
			}
			return !PartialTrustHelpers.IsAssemblySigned(assembly);
		}

		[SecurityCritical]
		internal static bool UnsafeIsInFullTrust()
		{
			if (AppDomain.CurrentDomain.IsHomogenous)
			{
				return AppDomain.CurrentDomain.IsFullyTrusted;
			}
			return !SecurityManager.CurrentThreadRequiresSecurityContextCapture();
		}
	}
}
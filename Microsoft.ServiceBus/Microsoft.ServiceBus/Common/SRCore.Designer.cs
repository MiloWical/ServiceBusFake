using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Resources;

namespace Microsoft.ServiceBus.Common
{
	internal class SRCore
	{
		private static System.Resources.ResourceManager resourceManager;

		private static CultureInfo resourceCulture;

		internal static string ActionItemIsAlreadyScheduled
		{
			get
			{
				return SRCore.ResourceManager.GetString("ActionItemIsAlreadyScheduled", SRCore.Culture);
			}
		}

		internal static string AsyncCallbackThrewException
		{
			get
			{
				return SRCore.ResourceManager.GetString("AsyncCallbackThrewException", SRCore.Culture);
			}
		}

		internal static string AsyncResultAlreadyEnded
		{
			get
			{
				return SRCore.ResourceManager.GetString("AsyncResultAlreadyEnded", SRCore.Culture);
			}
		}

		internal static string AsyncSemaphoreExitCalledWithoutEnter
		{
			get
			{
				return SRCore.ResourceManager.GetString("AsyncSemaphoreExitCalledWithoutEnter", SRCore.Culture);
			}
		}

		internal static string AsyncTransactionException
		{
			get
			{
				return SRCore.ResourceManager.GetString("AsyncTransactionException", SRCore.Culture);
			}
		}

		internal static string BufferIsNotRightSizeForBufferManager
		{
			get
			{
				return SRCore.ResourceManager.GetString("BufferIsNotRightSizeForBufferManager", SRCore.Culture);
			}
		}

		[GeneratedCode("StrictResXFileCodeGenerator", "4.0.0.0")]
		internal static CultureInfo Culture
		{
			get
			{
				return SRCore.resourceCulture;
			}
			set
			{
				SRCore.resourceCulture = value;
			}
		}

		internal static string EndOfInnerExceptionStackTrace
		{
			get
			{
				return SRCore.ResourceManager.GetString("EndOfInnerExceptionStackTrace", SRCore.Culture);
			}
		}

		internal static string InvalidAsyncResult
		{
			get
			{
				return SRCore.ResourceManager.GetString("InvalidAsyncResult", SRCore.Culture);
			}
		}

		internal static string InvalidAsyncResultImplementationGeneric
		{
			get
			{
				return SRCore.ResourceManager.GetString("InvalidAsyncResultImplementationGeneric", SRCore.Culture);
			}
		}

		internal static string InvalidNullAsyncResult
		{
			get
			{
				return SRCore.ResourceManager.GetString("InvalidNullAsyncResult", SRCore.Culture);
			}
		}

		internal static string InvalidSemaphoreExit
		{
			get
			{
				return SRCore.ResourceManager.GetString("InvalidSemaphoreExit", SRCore.Culture);
			}
		}

		internal static string MustCancelOldTimer
		{
			get
			{
				return SRCore.ResourceManager.GetString("MustCancelOldTimer", SRCore.Culture);
			}
		}

		internal static System.Resources.ResourceManager ResourceManager
		{
			get
			{
				if (object.ReferenceEquals(SRCore.resourceManager, null))
				{
					SRCore.resourceManager = new System.Resources.ResourceManager("Microsoft.ServiceBus.Common.SRCore", typeof(SRCore).Assembly);
				}
				return SRCore.resourceManager;
			}
		}

		internal static string SharedAccessAuthorizationRuleKeyContainsInvalidCharacters
		{
			get
			{
				return SRCore.ResourceManager.GetString("SharedAccessAuthorizationRuleKeyContainsInvalidCharacters", SRCore.Culture);
			}
		}

		internal static string SharedAccessAuthorizationRuleRequiresPrimaryKey
		{
			get
			{
				return SRCore.ResourceManager.GetString("SharedAccessAuthorizationRuleRequiresPrimaryKey", SRCore.Culture);
			}
		}

		internal static string SharedAccessKeyShouldbeBase64
		{
			get
			{
				return SRCore.ResourceManager.GetString("SharedAccessKeyShouldbeBase64", SRCore.Culture);
			}
		}

		private SRCore()
		{
		}

		internal static string ArgumentNullOrEmpty(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("ArgumentNullOrEmpty", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string ArgumentNullOrWhiteSpace(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("ArgumentNullOrWhiteSpace", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string ArgumentOutOfRange(object param0, object param1)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("ArgumentOutOfRange", SRCore.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string ArgumentStringTooBig(object param0, object param1)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("ArgumentStringTooBig", SRCore.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AsyncResultCompletedTwice(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("AsyncResultCompletedTwice", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AutoForwardToSelf(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("AutoForwardToSelf", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string DictionaryKeyIsModified(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("DictionaryKeyIsModified", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string DictionaryKeyNotExist(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("DictionaryKeyNotExist", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string EtwAPIMaxStringCountExceeded(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("EtwAPIMaxStringCountExceeded", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string EtwMaxNumberArgumentsExceeded(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("EtwMaxNumberArgumentsExceeded", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string EtwRegistrationFailed(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("EtwRegistrationFailed", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string FailFastMessage(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("FailFastMessage", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidAsyncResultImplementation(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("InvalidAsyncResultImplementation", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MultipleTransportSettingConfigurationElement(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("MultipleTransportSettingConfigurationElement", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string NullOrEmptyConfigurationAttribute(object param0, object param1)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("NullOrEmptyConfigurationAttribute", SRCore.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string ResourceCountExceeded(object param0, object param1, object param2)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("ResourceCountExceeded", SRCore.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string SharedAccessAuthorizationRuleKeyNameTooBig(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("SharedAccessAuthorizationRuleKeyNameTooBig", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string SharedAccessAuthorizationRuleKeyTooBig(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("SharedAccessAuthorizationRuleKeyTooBig", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string SharedAccessRuleAllowsFixedLengthKeys(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("SharedAccessRuleAllowsFixedLengthKeys", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string ShipAssertExceptionMessage(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("ShipAssertExceptionMessage", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string TimeoutInputQueueDequeue(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("TimeoutInputQueueDequeue", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string TimeoutMustBeNonNegative(object param0, object param1)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("TimeoutMustBeNonNegative", SRCore.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string TimeoutMustBePositive(object param0, object param1)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("TimeoutMustBePositive", SRCore.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string TimeoutOnOperation(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("TimeoutOnOperation", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnsupportedEnumerationValue(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("UnsupportedEnumerationValue", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnsupportedOperation(object param0)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("UnsupportedOperation", SRCore.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnsupportedTransport(object param0, object param1)
		{
			CultureInfo culture = SRCore.Culture;
			string str = SRCore.ResourceManager.GetString("UnsupportedTransport", SRCore.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}
	}
}
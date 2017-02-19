using System;

namespace Microsoft.ServiceBus.Messaging.Filters.FilterLanguage
{
	internal enum Tokens
	{
		error = 48,
		EOF = 49,
		IDENTIFIER = 50,
		DELIMITED_IDENTIFIER = 51,
		PARAMETER = 52,
		INTEGER = 53,
		DECIMAL = 54,
		DOUBLE = 55,
		STRING = 56,
		SET = 57,
		REMOVE = 58,
		AND = 59,
		OR = 60,
		NOT = 61,
		LIKE = 62,
		ESCAPE = 63,
		IN = 64,
		IS = 65,
		EXISTS = 66,
		EQ = 67,
		NEQ = 68,
		LT = 69,
		LTE = 70,
		GT = 71,
		GTE = 72,
		TRUE = 73,
		FALSE = 74,
		NULL = 75,
		RESERVED = 76,
		UPLUS = 77,
		UMINUS = 78
	}
}
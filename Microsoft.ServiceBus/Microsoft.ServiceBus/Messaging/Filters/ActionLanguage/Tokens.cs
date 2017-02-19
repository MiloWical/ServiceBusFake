using System;

namespace Microsoft.ServiceBus.Messaging.Filters.ActionLanguage
{
	internal enum Tokens
	{
		error = 62,
		EOF = 63,
		IDENTIFIER = 64,
		DELIMITED_IDENTIFIER = 65,
		PARAMETER = 66,
		INTEGER = 67,
		DECIMAL = 68,
		DOUBLE = 69,
		STRING = 70,
		SET = 71,
		REMOVE = 72,
		ARGS = 73,
		AND = 74,
		OR = 75,
		NOT = 76,
		LIKE = 77,
		ESCAPE = 78,
		IN = 79,
		IS = 80,
		EXISTS = 81,
		EQ = 82,
		NEQ = 83,
		LT = 84,
		LTE = 85,
		GT = 86,
		GTE = 87,
		TRUE = 88,
		FALSE = 89,
		NULL = 90,
		RESERVED = 91,
		UPLUS = 92,
		UMINUS = 93
	}
}
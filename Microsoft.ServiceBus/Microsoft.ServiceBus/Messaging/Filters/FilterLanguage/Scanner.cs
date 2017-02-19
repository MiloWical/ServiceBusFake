using Babel.ParserGenerator;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Filters;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Filters.FilterLanguage
{
	[GeneratedCode("Lexer", "1.0.0.0")]
	internal sealed class Scanner : ScanBase, IColorScan
	{
		private const int maxAccept = 28;

		private const int initial = 29;

		private const int eofNum = 0;

		private const int goStart = -1;

		private const int INITIAL = 0;

		private const int STRING_LITERAL = 1;

		private const int DELIMITED_IDENTIFIER_TYPE_1 = 2;

		private const int DELIMITED_IDENTIFIER_TYPE_2 = 3;

		public ScanBuff buffer;

		private IErrorHandler handler;

		private int scState;

		private static int parserMax;

		private readonly static Dictionary<string, Tokens> Keywords;

		private StringBuilder builder = new StringBuilder();

		private int state;

		private int currentStart = 29;

		private int chr;

		private int cNum;

		private int lNum;

		private int lineStartNum;

		private int tokPos;

		private int tokNum;

		private int tokLen;

		private int tokCol;

		private int tokLin;

		private int tokEPos;

		private int tokECol;

		private int tokELin;

		private string tokTxt;

		private Stack<int> scStack = new Stack<int>();

		private static int[] startState;

		private static sbyte[] map0;

		private static sbyte[] map2;

		private static sbyte[] map4;

		private static sbyte[] map10;

		private static sbyte[] map12;

		private static sbyte[] map14;

		private static sbyte[] map16;

		private static sbyte[] map18;

		private static sbyte[] map20;

		private static sbyte[] map28;

		private static sbyte[] map30;

		private static sbyte[] map34;

		private static sbyte[] map36;

		private static sbyte[] map38;

		private static sbyte[] map40;

		private static sbyte[] map42;

		private static sbyte[] map44;

		private static sbyte[] map49;

		private static sbyte[] map51;

		private static sbyte[] map53;

		private static sbyte[] map55;

		private static sbyte[] map57;

		private static sbyte[] map61;

		private static sbyte[] map63;

		private static sbyte[] map65;

		private static sbyte[] map67;

		private static sbyte[] map76;

		private static sbyte[] map78;

		private static sbyte[] map80;

		private static sbyte[] map82;

		private static sbyte[] map84;

		private static sbyte[] map89;

		private static sbyte[] map91;

		private static sbyte[] map96;

		private static sbyte[] map98;

		private static sbyte[] map100;

		private static sbyte[] map102;

		private static Scanner.Table[] NxS;

		protected override int CurrentSc
		{
			get
			{
				return this.scState;
			}
			set
			{
				this.scState = value;
				this.currentStart = Scanner.startState[value];
			}
		}

		public IErrorHandler Handler
		{
			get
			{
				return this.handler;
			}
			set
			{
				this.handler = value;
			}
		}

		internal int YY_START
		{
			get
			{
				return this.CurrentSc;
			}
			set
			{
				this.CurrentSc = value;
			}
		}

		private int yycol
		{
			get
			{
				return this.tokCol;
			}
		}

		private int yyleng
		{
			get
			{
				return this.tokLen;
			}
		}

		private int yyline
		{
			get
			{
				return this.tokLin;
			}
		}

		private int yypos
		{
			get
			{
				return this.tokPos;
			}
		}

		public string yytext
		{
			get
			{
				if (this.tokTxt == null)
				{
					this.tokTxt = this.buffer.GetString(this.tokPos, this.tokEPos);
				}
				return this.tokTxt;
			}
		}

		static Scanner()
		{
			Scanner.parserMax = Scanner.GetMaxParseToken();
			Dictionary<string, Tokens> strs = new Dictionary<string, Tokens>(StringComparer.OrdinalIgnoreCase)
			{
				{ "AND", Tokens.AND },
				{ "OR", Tokens.OR },
				{ "NOT", Tokens.NOT },
				{ "EXISTS", Tokens.EXISTS },
				{ "LIKE", Tokens.LIKE },
				{ "ESCAPE", Tokens.ESCAPE },
				{ "IN", Tokens.IN },
				{ "IS", Tokens.IS },
				{ "NULL", Tokens.NULL },
				{ "TRUE", Tokens.TRUE },
				{ "FALSE", Tokens.FALSE },
				{ "SET", Tokens.SET },
				{ "REMOVE", Tokens.REMOVE },
				{ "BEGIN", Tokens.RESERVED },
				{ "END", Tokens.RESERVED },
				{ "BREAK", Tokens.RESERVED },
				{ "CONTINUE", Tokens.RESERVED },
				{ "GOTO", Tokens.RESERVED },
				{ "IF", Tokens.RESERVED },
				{ "ELSE", Tokens.RESERVED },
				{ "WHILE", Tokens.RESERVED },
				{ "TRY", Tokens.RESERVED },
				{ "CATCH", Tokens.RESERVED },
				{ "AS", Tokens.RESERVED },
				{ "BETWEEN", Tokens.RESERVED },
				{ "COALESCE", Tokens.RESERVED },
				{ "CONVERT", Tokens.RESERVED },
				{ "DECLARE", Tokens.RESERVED },
				{ "NULLIF", Tokens.RESERVED }
			};
			Scanner.Keywords = strs;
			Scanner.startState = new int[] { 29, 33, 34, 35, 0 };
			Scanner.map0 = new sbyte[] { 23, 23, 23, 23, 23, 23, 23, 23, 23, 25, 1, 25, 23, 25, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 25, 11, 21, 23, 23, 19, 23, 24, 14, 15, 17, 16, 20, 9, 6, 18, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 23, 23, 12, 10, 13, 23, 4, 2, 2, 2, 2, 8, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 22, 23, 0, 23, 3, 23, 2, 2, 2, 2, 8, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 7, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 23, 23, 23, 2, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23 };
			Scanner.map2 = new sbyte[] { 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 2, 23, 2 };
			Scanner.map4 = new sbyte[] { 2, 2, 2, 2, 2, 23, 2, 2, 23, 23, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 2, 2, 2, 23, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23 };
			Scanner.map10 = new sbyte[] { 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
			Scanner.map12 = new sbyte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 2, 2, 23 };
			Scanner.map14 = new sbyte[] { 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 23, 23, 23, 23, 23, 23, 23, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23 };
			Scanner.map16 = new sbyte[] { 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 23, 23, 23, 23, 2 };
			Scanner.map18 = new sbyte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 23, 23, 23, 2, 2, 2, 2, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 23, 2, 2, 2, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 23, 2, 2, 23, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 23, 2, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 23, 2, 2, 2, 2, 2, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 23, 2, 2, 2, 2, 2, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 23, 2, 2, 2, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 2, 2, 2, 2, 2, 2, 23, 23, 23, 2, 2, 2, 23, 2, 2, 2, 2, 23, 23, 23, 2, 2, 23, 2, 23, 2, 2, 23, 23, 23, 2, 2, 23, 23, 23, 2, 2, 2, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 23, 23, 23, 23, 23, 23, 2, 2, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 2, 2, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 23, 2, 23, 23, 2, 2, 23, 2, 23, 23, 2, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 23, 2, 23, 2, 23, 23, 2, 2, 23, 2, 2, 2, 2, 23, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 23, 2, 2, 2, 2, 2, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2 };
			Scanner.map20 = new sbyte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 2, 2, 2, 2, 23, 23, 23, 2, 23, 23, 23, 2, 2, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 23, 23, 23 };
			Scanner.map28 = new sbyte[] { 23, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 23, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 23, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 23, 23 };
			Scanner.map30 = new sbyte[] { 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23 };
			Scanner.map34 = new sbyte[] { 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23 };
			Scanner.map36 = new sbyte[] { 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 23, 23, 23, 2, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23 };
			Scanner.map38 = new sbyte[] { 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2 };
			Scanner.map40 = new sbyte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
			Scanner.map42 = new sbyte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 };
			Scanner.map44 = new sbyte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
			Scanner.map49 = new sbyte[] { 23, 23, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 23, 2, 23, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 23, 23, 23, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2 };
			Scanner.map51 = new sbyte[] { 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2 };
			Scanner.map53 = new sbyte[] { 2, 23, 23, 23, 23, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 23, 23, 23, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 2, 23, 2, 23, 2, 23, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 23, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2 };
			Scanner.map55 = new sbyte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23 };
			Scanner.map57 = new sbyte[] { 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2 };
			Scanner.map61 = new sbyte[] { 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 2, 2, 23, 23, 23, 23 };
			Scanner.map63 = new sbyte[] { 23, 23, 23, 23, 23, 23, 2, 2, 2, 23 };
			Scanner.map65 = new sbyte[] { 23, 2, 2, 2, 2, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23 };
			Scanner.map67 = new sbyte[] { 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
			Scanner.map76 = new sbyte[] { 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
			Scanner.map78 = new sbyte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23 };
			Scanner.map80 = new sbyte[] { 23, 23, 2, 2 };
			Scanner.map82 = new sbyte[] { 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 23, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
			Scanner.map84 = new sbyte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 };
			Scanner.map89 = new sbyte[] { 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23 };
			Scanner.map91 = new sbyte[] { 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 2, 2, 2, 2, 2, 23, 2, 23, 2, 2, 23, 2, 2, 23 };
			Scanner.map96 = new sbyte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
			Scanner.map98 = new sbyte[] { 2, 2, 2, 2, 2, 23 };
			Scanner.map100 = new sbyte[] { 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23 };
			Scanner.map102 = new sbyte[] { 23, 23, 23, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 2, 2, 2, 2, 23, 23, 2, 2, 2, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23 };
			Scanner.NxS = new Scanner.Table[36];
			Scanner.NxS[0] = new Scanner.Table(0, 0, 0, null);
			Scanner.NxS[1] = new Scanner.Table(0, 0, -1, null);
			sbyte[] numArray = new sbyte[] { 2, -1, 2 };
			Scanner.NxS[2] = new Scanner.Table(25, 3, -1, numArray);
			sbyte[] numArray1 = new sbyte[] { 3, 3, -1, 3, -1, -1, 3 };
			Scanner.NxS[3] = new Scanner.Table(2, 7, -1, numArray1);
			sbyte[] numArray2 = new sbyte[] { 19, -1, -1, -1, -1, -1, 19 };
			Scanner.NxS[4] = new Scanner.Table(2, 7, -1, numArray2);
			sbyte[] numArray3 = new sbyte[] { 5, 30, 31, 31 };
			Scanner.NxS[5] = new Scanner.Table(5, 4, -1, numArray3);
			Scanner.NxS[6] = new Scanner.Table(0, 0, -1, null);
			Scanner.NxS[7] = new Scanner.Table(0, 0, -1, null);
			sbyte[] numArray4 = new sbyte[] { 16 };
			Scanner.NxS[8] = new Scanner.Table(10, 1, -1, numArray4);
			sbyte[] numArray5 = new sbyte[] { 15, -1, -1, 16 };
			Scanner.NxS[9] = new Scanner.Table(10, 4, -1, numArray5);
			sbyte[] numArray6 = new sbyte[] { 14 };
			Scanner.NxS[10] = new Scanner.Table(10, 1, -1, numArray6);
			Scanner.NxS[11] = new Scanner.Table(0, 0, -1, null);
			Scanner.NxS[12] = new Scanner.Table(0, 0, -1, null);
			Scanner.NxS[13] = new Scanner.Table(0, 0, -1, null);
			Scanner.NxS[14] = new Scanner.Table(0, 0, -1, null);
			Scanner.NxS[15] = new Scanner.Table(0, 0, -1, null);
			Scanner.NxS[16] = new Scanner.Table(0, 0, -1, null);
			sbyte[] numArray7 = new sbyte[] { 17 };
			Scanner.NxS[17] = new Scanner.Table(5, 1, -1, numArray7);
			sbyte[] numArray8 = new sbyte[] { 18, -1, 31, 31 };
			Scanner.NxS[18] = new Scanner.Table(5, 4, -1, numArray8);
			sbyte[] numArray9 = new sbyte[] { 19, 19, -1, 19, -1, -1, 19 };
			Scanner.NxS[19] = new Scanner.Table(2, 7, -1, numArray9);
			sbyte[] numArray10 = new sbyte[] { -1 };
			Scanner.NxS[20] = new Scanner.Table(24, 1, 20, numArray10);
			sbyte[] numArray11 = new sbyte[] { 22 };
			Scanner.NxS[21] = new Scanner.Table(24, 1, -1, numArray11);
			Scanner.NxS[22] = new Scanner.Table(0, 0, -1, null);
			sbyte[] numArray12 = new sbyte[] { -1 };
			Scanner.NxS[23] = new Scanner.Table(21, 1, 23, numArray12);
			sbyte[] numArray13 = new sbyte[] { 25 };
			Scanner.NxS[24] = new Scanner.Table(21, 1, -1, numArray13);
			Scanner.NxS[25] = new Scanner.Table(0, 0, -1, null);
			sbyte[] numArray14 = new sbyte[] { 28 };
			Scanner.NxS[26] = new Scanner.Table(0, 1, -1, numArray14);
			sbyte[] numArray15 = new sbyte[] { -1 };
			Scanner.NxS[27] = new Scanner.Table(0, 1, 27, numArray15);
			Scanner.NxS[28] = new Scanner.Table(0, 0, -1, null);
			sbyte[] numArray16 = new sbyte[] { 11, 12, 1, 13, 2, 1, 2, 3, 1, 4, 5, 6, 1, 3, 6, 7, 8, 9, 10 };
			Scanner.NxS[29] = new Scanner.Table(21, 19, 6, numArray16);
			sbyte[] numArray17 = new sbyte[] { 18 };
			Scanner.NxS[30] = new Scanner.Table(5, 1, -1, numArray17);
			sbyte[] numArray18 = new sbyte[] { 17, -1, -1, -1, 32, -1, -1, -1, -1, -1, -1, 32 };
			Scanner.NxS[31] = new Scanner.Table(5, 12, -1, numArray18);
			sbyte[] numArray19 = new sbyte[] { 17 };
			Scanner.NxS[32] = new Scanner.Table(5, 1, -1, numArray19);
			sbyte[] numArray20 = new sbyte[] { 21 };
			Scanner.NxS[33] = new Scanner.Table(24, 1, 20, numArray20);
			sbyte[] numArray21 = new sbyte[] { 24 };
			Scanner.NxS[34] = new Scanner.Table(21, 1, 23, numArray21);
			sbyte[] numArray22 = new sbyte[] { 26 };
			Scanner.NxS[35] = new Scanner.Table(0, 1, 27, numArray22);
		}

		public Scanner(string source)
		{
			this.SetSource(source, 0);
			this.Handler = new ErrorHandler();
			this.yylloc = new LexLocation();
		}

		public Scanner(Stream file)
		{
			this.buffer = Scanner.TextBuff.NewTextBuff(file);
			this.cNum = -1;
			this.chr = 10;
			this.GetChr();
		}

		public Scanner()
		{
		}

		internal void BEGIN(int next)
		{
			this.CurrentSc = next;
		}

		internal void ECHO()
		{
			Console.Out.Write(this.yytext);
		}

		private void GetChr()
		{
			if (this.chr == 10)
			{
				this.lineStartNum = this.cNum + 1;
				Scanner scanner = this;
				scanner.lNum = scanner.lNum + 1;
			}
			this.chr = this.buffer.Read();
			Scanner scanner1 = this;
			scanner1.cNum = scanner1.cNum + 1;
		}

		private static int GetMaxParseToken()
		{
			FieldInfo field = typeof(Tokens).GetField("maxParseToken");
			if (field == null)
			{
				return 2147483647;
			}
			return (int)field.GetValue(null);
		}

		public int GetNext(ref int state, out int start, out int end)
		{
			this.EolState = state;
			Tokens token = (Tokens)this.Scan();
			state = this.EolState;
			start = this.tokPos;
			end = this.tokEPos - 1;
			return (int)token;
		}

		private void LoadYylloc()
		{
			this.yylloc = new LexLocation(this.tokLin, this.tokCol, this.tokLin, this.tokCol + this.tokLen);
		}

		private sbyte Map(int chr)
		{
			if (chr < 8305)
			{
				if (chr < 4520)
				{
					if (chr < 1488)
					{
						if (chr >= 1014)
						{
							if (chr < 1162)
							{
								if (chr < 1015)
								{
									return 23;
								}
								if (chr < 1154)
								{
									return 2;
								}
								return 23;
							}
							if (chr < 1316)
							{
								return 2;
							}
							if (chr >= 1416)
							{
								return 23;
							}
							return Scanner.map10[chr - 1316];
						}
						if (chr >= 751)
						{
							if (chr < 880)
							{
								return 23;
							}
							if (chr >= 931)
							{
								return 2;
							}
							return Scanner.map4[chr - 880];
						}
						if (chr < 248)
						{
							return Scanner.map0[chr];
						}
						if (chr < 706)
						{
							return 2;
						}
						return Scanner.map2[chr - 706];
					}
					if (chr < 2308)
					{
						if (chr >= 1869)
						{
							if (chr < 1958)
							{
								return 2;
							}
							if (chr >= 2043)
							{
								return 23;
							}
							return Scanner.map16[chr - 1958];
						}
						if (chr < 1649)
						{
							return Scanner.map12[chr - 1488];
						}
						if (chr < 1748)
						{
							return 2;
						}
						return Scanner.map14[chr - 1748];
					}
					if (chr >= 4352)
					{
						if (chr < 4447)
						{
							if (chr < 4442)
							{
								return 2;
							}
							return 23;
						}
						if (chr < 4515)
						{
							return 2;
						}
						return 23;
					}
					if (chr < 3980)
					{
						return Scanner.map18[chr - 2308];
					}
					if (chr < 4096)
					{
						return 23;
					}
					return Scanner.map20[chr - 4096];
				}
				if (chr < 6264)
				{
					if (chr < 5024)
					{
						if (chr < 4681)
						{
							if (chr < 4602)
							{
								return 2;
							}
							if (chr < 4608)
							{
								return 23;
							}
							return 2;
						}
						if (chr < 4888)
						{
							return Scanner.map28[chr - 4681];
						}
						if (chr < 4955)
						{
							return 2;
						}
						return Scanner.map30[chr - 4955];
					}
					if (chr < 5741)
					{
						if (chr < 5109)
						{
							return 2;
						}
						if (chr < 5121)
						{
							return 23;
						}
						return 2;
					}
					if (chr < 5867)
					{
						if (chr >= 5792)
						{
							return 2;
						}
						return Scanner.map34[chr - 5741];
					}
					if (chr >= 6176)
					{
						return 2;
					}
					return Scanner.map36[chr - 5867];
				}
				if (chr < 7168)
				{
					if (chr >= 6679)
					{
						if (chr < 6917)
						{
							return 23;
						}
						if (chr >= 7098)
						{
							return 23;
						}
						return Scanner.map42[chr - 6917];
					}
					if (chr < 6315)
					{
						return Scanner.map38[chr - 6264];
					}
					if (chr < 6400)
					{
						return 23;
					}
					return Scanner.map40[chr - 6400];
				}
				if (chr < 7616)
				{
					if (chr < 7294)
					{
						return Scanner.map44[chr - 7168];
					}
					if (chr < 7424)
					{
						return 23;
					}
					return 2;
				}
				if (chr < 7958)
				{
					if (chr < 7680)
					{
						return 23;
					}
					return 2;
				}
				if (chr >= 8189)
				{
					return 23;
				}
				return Scanner.map49[chr - 7958];
			}
			if (chr < 42648)
			{
				if (chr >= 12449)
				{
					if (chr < 19894)
					{
						if (chr < 12687)
						{
							if (chr < 12539)
							{
								return 2;
							}
							if (chr >= 12593)
							{
								return 2;
							}
							return Scanner.map65[chr - 12539];
						}
						if (chr < 12800)
						{
							return Scanner.map67[chr - 12687];
						}
						if (chr < 13312)
						{
							return 23;
						}
						return 2;
					}
					if (chr < 40960)
					{
						if (chr < 19968)
						{
							return 23;
						}
						if (chr < 40900)
						{
							return 2;
						}
						return 23;
					}
					if (chr < 42240)
					{
						if (chr < 42125)
						{
							return 2;
						}
						return 23;
					}
					if (chr < 42509)
					{
						return 2;
					}
					return Scanner.map76[chr - 42509];
				}
				if (chr < 11493)
				{
					if (chr >= 8581)
					{
						if (chr < 11264)
						{
							return 23;
						}
						if (chr >= 11392)
						{
							return 2;
						}
						return Scanner.map55[chr - 11264];
					}
					if (chr < 8341)
					{
						return Scanner.map51[chr - 8305];
					}
					if (chr < 8450)
					{
						return 23;
					}
					return Scanner.map53[chr - 8450];
				}
				if (chr < 11824)
				{
					if (chr < 11743)
					{
						return Scanner.map57[chr - 11493];
					}
					if (chr < 11823)
					{
						return 23;
					}
					return 2;
				}
				if (chr < 12353)
				{
					if (chr < 12293)
					{
						return 23;
					}
					return Scanner.map61[chr - 12293];
				}
				if (chr < 12439)
				{
					return 2;
				}
				return Scanner.map63[chr - 12439];
			}
			if (chr < 64112)
			{
				if (chr < 43335)
				{
					if (chr < 42889)
					{
						if (chr < 42775)
						{
							return 23;
						}
						if (chr >= 42786)
						{
							return 2;
						}
						return Scanner.map78[chr - 42775];
					}
					if (chr < 42893)
					{
						return Scanner.map80[chr - 42889];
					}
					if (chr < 43003)
					{
						return 23;
					}
					return Scanner.map82[chr - 43003];
				}
				if (chr < 44032)
				{
					if (chr < 43520)
					{
						return 23;
					}
					if (chr >= 43610)
					{
						return 23;
					}
					return Scanner.map84[chr - 43520];
				}
				if (chr < 63744)
				{
					if (chr < 55204)
					{
						return 2;
					}
					return 23;
				}
				if (chr < 64046)
				{
					return 2;
				}
				return Scanner.map89[chr - 64046];
			}
			if (chr < 64848)
			{
				if (chr >= 64434)
				{
					if (chr < 64467)
					{
						return 23;
					}
					if (chr < 64830)
					{
						return 2;
					}
					return 23;
				}
				if (chr < 64218)
				{
					return 2;
				}
				if (chr >= 64326)
				{
					return 2;
				}
				return Scanner.map91[chr - 64218];
			}
			if (chr < 65142)
			{
				if (chr < 65020)
				{
					return Scanner.map96[chr - 64848];
				}
				if (chr < 65136)
				{
					return 23;
				}
				return Scanner.map98[chr - 65136];
			}
			if (chr < 65382)
			{
				if (chr < 65277)
				{
					return 2;
				}
				return Scanner.map100[chr - 65277];
			}
			if (chr < 65471)
			{
				return 2;
			}
			return Scanner.map102[chr - 65471];
		}

		private void MarkEnd()
		{
			this.tokTxt = null;
			this.tokLen = this.cNum - this.tokNum;
			this.tokEPos = this.buffer.ReadPos;
			this.tokELin = this.lNum;
			this.tokECol = this.cNum - this.lineStartNum;
		}

		private void MarkToken()
		{
			this.tokPos = this.buffer.ReadPos;
			this.tokNum = this.cNum;
			this.tokLin = this.lNum;
			this.tokCol = this.cNum - this.lineStartNum;
		}

		private int NextState(int qStat)
		{
			int nxS;
			if (this.chr == -1)
			{
				if (qStat > 28 || qStat == this.currentStart)
				{
					return 0;
				}
				return this.currentStart;
			}
			int num = this.Map(this.chr) - Scanner.NxS[qStat].min;
			if (num < 0)
			{
				num = num + 26;
			}
			if (num < Scanner.NxS[qStat].rng)
			{
				nxS = Scanner.NxS[qStat].nxt[num];
			}
			else
			{
				nxS = Scanner.NxS[qStat].dflt;
			}
			if (nxS != -1)
			{
				return nxS;
			}
			return this.currentStart;
		}

		private int NextState()
		{
			int nxS;
			if (this.chr == -1)
			{
				if (this.state > 28 || this.state == this.currentStart)
				{
					return 0;
				}
				return this.currentStart;
			}
			int num = this.Map(this.chr) - Scanner.NxS[this.state].min;
			if (num < 0)
			{
				num = num + 26;
			}
			if (num < Scanner.NxS[this.state].rng)
			{
				nxS = Scanner.NxS[this.state].nxt[num];
			}
			else
			{
				nxS = Scanner.NxS[this.state].dflt;
			}
			if (nxS != -1)
			{
				return nxS;
			}
			return this.currentStart;
		}

		private Scanner.Result Recurse2(Scanner.Context ctx, int next)
		{
			this.SaveStateAndPos(ctx);
			this.state = next;
			if (this.state == 0)
			{
				return Scanner.Result.accept;
			}
			this.GetChr();
			bool flag = false;
			while (true)
			{
				int num = this.NextState();
				next = num;
				if (num == this.currentStart)
				{
					break;
				}
				if (flag && next > 28)
				{
					this.SaveStateAndPos(ctx);
				}
				this.state = next;
				if (this.state == 0)
				{
					return Scanner.Result.accept;
				}
				this.GetChr();
				flag = this.state <= 28;
			}
			if (flag)
			{
				return Scanner.Result.accept;
			}
			return Scanner.Result.noMatch;
		}

		private void RestorePos(Scanner.Context ctx)
		{
			this.buffer.Pos = ctx.bPos;
			this.cNum = ctx.cNum;
		}

		private void RestoreStateAndPos(Scanner.Context ctx)
		{
			this.buffer.Pos = ctx.bPos;
			this.cNum = ctx.cNum;
			this.state = ctx.state;
			this.chr = ctx.cChr;
		}

		private void SaveStateAndPos(Scanner.Context ctx)
		{
			ctx.bPos = this.buffer.Pos;
			ctx.cNum = this.cNum;
			ctx.state = this.state;
			ctx.cChr = this.chr;
		}

		private int Scan()
		{
			Tokens token;
			int num;
			try
			{
				while (true)
				{
					bool flag = false;
					this.state = this.currentStart;
					while (this.NextState() == this.state)
					{
						this.GetChr();
					}
					this.MarkToken();
					while (true)
					{
						int num1 = this.NextState();
						int num2 = num1;
						if (num1 == this.currentStart)
						{
							break;
						}
						if (!flag || num2 <= 28)
						{
							this.state = num2;
							this.GetChr();
							if (this.state <= 28)
							{
								flag = true;
							}
						}
						else
						{
							Scanner.Context context = new Scanner.Context();
							if (this.Recurse2(context, num2) != Scanner.Result.noMatch)
							{
								break;
							}
							this.RestoreStateAndPos(context);
							break;
						}
					}
					if (this.state <= 28)
					{
						this.MarkEnd();
						switch (this.state)
						{
							case 0:
							{
								switch (this.currentStart)
								{
									case 33:
									{
										this.yyerror(SRClient.StringLiteralNotTerminated(this.builder.ToString()), new object[0]);
										num = 48;
										return num;
									}
									case 34:
									{
										this.yyerror(SRClient.DelimitedIdentifierNotTerminated(this.builder.ToString()), new object[0]);
										num = 48;
										return num;
									}
									case 35:
									{
										this.yyerror(SRClient.DelimitedIdentifierNotTerminated(this.builder.ToString()), new object[0]);
										num = 48;
										return num;
									}
								}
								num = 49;
								return num;
							}
							case 1:
							case 4:
							case 8:
							{
								this.yyerror("Unrecognized character. '{0}'", new object[] { this.yytext });
								num = 48;
								return num;
							}
							case 3:
							{
								goto Label2;
							}
							case 5:
							{
								this.yylval.@value = this.yytext;
								num = 53;
								return num;
							}
							case 6:
							{
								num = this.yytext[0];
								return num;
							}
							case 7:
							{
								num = 67;
								return num;
							}
							case 9:
							{
								num = 69;
								return num;
							}
							case 10:
							{
								num = 71;
								return num;
							}
							case 11:
							{
								this.yy_push_state(2);
								this.builder.Clear();
								continue;
							}
							case 12:
							{
								this.yy_push_state(3);
								this.builder.Clear();
								continue;
							}
							case 13:
							{
								this.yy_push_state(1);
								this.builder.Clear();
								continue;
							}
							case 14:
							{
								num = 72;
								return num;
							}
							case 15:
							{
								num = 70;
								return num;
							}
							case 16:
							{
								num = 68;
								return num;
							}
							case 17:
							{
								this.yylval.@value = this.yytext;
								num = 55;
								return num;
							}
							case 18:
							{
								this.yylval.@value = this.yytext;
								num = 55;
								return num;
							}
							case 19:
							{
								this.yylval.@value = this.yytext;
								num = 52;
								return num;
							}
							case 20:
							{
								this.builder.Append(this.yytext);
								continue;
							}
							case 21:
							{
								this.yy_pop_state();
								this.yylval.@value = this.builder.ToString();
								num = 56;
								return num;
							}
							case 22:
							{
								this.builder.Append("'");
								continue;
							}
							case 23:
							{
								this.builder.Append(this.yytext);
								continue;
							}
							case 24:
							{
								this.yy_pop_state();
								this.yylval.@value = this.builder.ToString();
								num = 51;
								return num;
							}
							case 25:
							{
								this.builder.Append("\"");
								continue;
							}
							case 26:
							{
								break;
							}
							case 27:
							{
								this.builder.Append(this.yytext);
								continue;
							}
							case 28:
							{
								this.builder.Append("]");
								continue;
							}
							default:
							{
								continue;
							}
						}
					}
					else
					{
						this.state = this.currentStart;
					}
				}
				this.yy_pop_state();
				this.yylval.@value = this.builder.ToString();
				num = 51;
			}
			finally
			{
				this.LoadYylloc();
			}
			return num;
		Label2:
			if (!Scanner.Keywords.TryGetValue(this.yytext, out token))
			{
				this.yylval.@value = this.yytext;
				num = 50;
				return num;
			}
			else
			{
				if (token == Tokens.RESERVED)
				{
					this.yyerror(SRClient.SqlFilterReservedKeyword(this.yytext), new object[0]);
				}
				num = (int)token;
				return num;
			}
		}

		public void SetSource(string source, int offset)
		{
			this.buffer = new Scanner.StringBuff(source)
			{
				Pos = offset
			};
			this.cNum = offset - 1;
			this.chr = 10;
			this.GetChr();
		}

		internal void yy_clear_stack()
		{
			this.scStack.Clear();
		}

		internal void yy_pop_state()
		{
			if (this.scStack.Count > 0)
			{
				this.CurrentSc = this.scStack.Pop();
			}
		}

		internal void yy_push_state(int state)
		{
			this.scStack.Push(this.CurrentSc);
			this.CurrentSc = state;
		}

		internal int yy_top_state()
		{
			return this.scStack.Peek();
		}

		public override void yyerror(string format, params object[] values)
		{
			if (this.handler != null)
			{
				string str = string.Format(format, values);
				this.handler.AddError(str, this.yytext, this.tokLin, this.tokCol, this.tokLen, 0);
			}
		}

		private void yyless(int n)
		{
			this.buffer.Pos = this.tokPos;
			this.cNum = this.tokNum;
			for (int i = 0; i <= n; i++)
			{
				this.GetChr();
			}
			this.MarkEnd();
		}

		public override int yylex()
		{
			int num;
			do
			{
				num = this.Scan();
			}
			while (num >= Scanner.parserMax);
			return num;
		}

		public sealed class BigEndTextBuff : Scanner.TextBuff
		{
			internal BigEndTextBuff(Stream str) : base(str)
			{
			}

			public override int Read()
			{
				int num = this.bStrm.ReadByte();
				return (num << 8) + this.bStrm.ReadByte();
			}
		}

		internal class Context
		{
			public int bPos;

			public int cNum;

			public int state;

			public int cChr;

			public Context()
			{
			}
		}

		public sealed class LittleEndTextBuff : Scanner.TextBuff
		{
			internal LittleEndTextBuff(Stream str) : base(str)
			{
			}

			public override int Read()
			{
				int num = this.bStrm.ReadByte();
				return (this.bStrm.ReadByte() << 8) + num;
			}
		}

		private enum Result
		{
			accept,
			noMatch,
			contextFound
		}

		public sealed class StreamBuff : ScanBuff
		{
			private BufferedStream bStrm;

			private int delta;

			public override int Pos
			{
				get
				{
					return (int)this.bStrm.Position;
				}
				set
				{
					this.bStrm.Position = (long)value;
				}
			}

			public override int ReadPos
			{
				get
				{
					return (int)this.bStrm.Position - this.delta;
				}
			}

			public StreamBuff(Stream str)
			{
				this.bStrm = new BufferedStream(str);
			}

			public override string GetString(int beg, int end)
			{
				if (end - beg <= 0)
				{
					return "";
				}
				long position = this.bStrm.Position;
				char[] chrArray = new char[end - beg];
				this.bStrm.Position = (long)beg;
				for (int i = 0; i < end - beg; i++)
				{
					chrArray[i] = (char)this.bStrm.ReadByte();
				}
				this.bStrm.Position = position;
				return new string(chrArray);
			}

			public override int Peek()
			{
				int num = this.bStrm.ReadByte();
				this.bStrm.Seek((long)(-this.delta), SeekOrigin.Current);
				return num;
			}

			public override int Read()
			{
				return this.bStrm.ReadByte();
			}
		}

		public sealed class StringBuff : ScanBuff
		{
			private string str;

			private int bPos;

			private int sLen;

			public override int Pos
			{
				get
				{
					return this.bPos;
				}
				set
				{
					this.bPos = value;
				}
			}

			public override int ReadPos
			{
				get
				{
					return this.bPos - 1;
				}
			}

			public StringBuff(string str)
			{
				this.str = str;
				this.sLen = str.Length;
			}

			public override string GetString(int beg, int end)
			{
				if (end > this.sLen)
				{
					end = this.sLen;
				}
				if (end <= beg)
				{
					return "";
				}
				return this.str.Substring(beg, end - beg);
			}

			public override int Peek()
			{
				if (this.bPos >= this.sLen)
				{
					return 10;
				}
				return this.str[this.bPos];
			}

			public override int Read()
			{
				if (this.bPos < this.sLen)
				{
					string str = this.str;
					Scanner.StringBuff stringBuff = this;
					int num = stringBuff.bPos;
					int num1 = num;
					stringBuff.bPos = num + 1;
					return str[num1];
				}
				if (this.bPos != this.sLen)
				{
					return -1;
				}
				Scanner.StringBuff stringBuff1 = this;
				stringBuff1.bPos = stringBuff1.bPos + 1;
				return 10;
			}
		}

		private struct Table
		{
			public int min;

			public int rng;

			public int dflt;

			public sbyte[] nxt;

			public Table(int m, int x, int d, sbyte[] n)
			{
				this.min = m;
				this.rng = x;
				this.dflt = d;
				this.nxt = n;
			}
		}

		public class TextBuff : ScanBuff
		{
			protected BufferedStream bStrm;

			protected int delta;

			public sealed override int Pos
			{
				get
				{
					return (int)this.bStrm.Position;
				}
				set
				{
					this.bStrm.Position = (long)value;
				}
			}

			public sealed override int ReadPos
			{
				get
				{
					return (int)this.bStrm.Position - this.delta;
				}
			}

			protected TextBuff(Stream str)
			{
				this.bStrm = new BufferedStream(str);
			}

			private Exception BadUTF8()
			{
				return new Exception(string.Format("BadUTF8 Character", new object[0]));
			}

			public sealed override string GetString(int beg, int end)
			{
				if (end - beg <= 0)
				{
					return "";
				}
				long position = this.bStrm.Position;
				char[] chrArray = new char[end - beg];
				this.bStrm.Position = (long)beg;
				int num = 0;
				while (this.bStrm.Position < (long)end)
				{
					chrArray[num] = (char)this.Read();
					num++;
				}
				this.bStrm.Position = position;
				return new string(chrArray, 0, num);
			}

			public static Scanner.TextBuff NewTextBuff(Stream strm)
			{
				int num = strm.ReadByte();
				int num1 = strm.ReadByte();
				if (num == 254 && num1 == 255)
				{
					return new Scanner.BigEndTextBuff(strm);
				}
				if (num == 255 && num1 == 254)
				{
					return new Scanner.LittleEndTextBuff(strm);
				}
				int num2 = strm.ReadByte();
				if (num == 239 && num1 == 187 && num2 == 191)
				{
					return new Scanner.TextBuff(strm);
				}
				strm.Seek((long)0, SeekOrigin.Begin);
				return new Scanner.TextBuff(strm);
			}

			public sealed override int Peek()
			{
				int num = this.Read();
				this.bStrm.Seek((long)(-this.delta), SeekOrigin.Current);
				return num;
			}

			public override int Read()
			{
				int num;
				int num1 = this.bStrm.ReadByte();
				if (num1 < 127)
				{
					this.delta = (num1 == -1 ? 0 : 1);
					return num1;
				}
				if ((num1 & 224) == 192)
				{
					this.delta = 2;
					num = this.bStrm.ReadByte();
					if ((num & 192) != 128)
					{
						throw this.BadUTF8();
					}
					return ((num1 & 31) << 6) + (num & 63);
				}
				if ((num1 & 240) != 224)
				{
					throw this.BadUTF8();
				}
				this.delta = 3;
				num = this.bStrm.ReadByte();
				int num2 = this.bStrm.ReadByte();
				if ((num & num2 & 192) != 128)
				{
					throw this.BadUTF8();
				}
				return ((num1 & 15) << 12) + ((num & 63) << 6) + (num2 & 63);
			}
		}
	}
}
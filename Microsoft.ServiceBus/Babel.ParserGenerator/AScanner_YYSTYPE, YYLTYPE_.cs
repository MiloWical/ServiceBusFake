using System;

namespace Babel.ParserGenerator
{
	internal abstract class AScanner<YYSTYPE, YYLTYPE>
	where YYSTYPE : struct
	where YYLTYPE : IMerge<YYLTYPE>
	{
		public YYSTYPE yylval;

		public YYLTYPE yylloc;

		protected AScanner()
		{
		}

		public virtual void yyerror(string format, params object[] args)
		{
		}

		public abstract int yylex();
	}
}
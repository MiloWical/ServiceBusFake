namespace Babel.ParserGenerator
{
	internal interface IMerge<YYLTYPE>
	{
		YYLTYPE Merge(YYLTYPE last);
	}
}
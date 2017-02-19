using System;

namespace Babel.ParserGenerator
{
	internal interface IColorScan
	{
		int GetNext(ref int state, out int start, out int end);

		void SetSource(string source, int offset);
	}
}
using System;

namespace Babel.ParserGenerator
{
	internal class Rule
	{
		public int lhs;

		public int[] rhs;

		public Rule(int lhs, int[] rhs)
		{
			this.lhs = lhs;
			this.rhs = rhs;
		}
	}
}
using System;
using System.Collections.Generic;

namespace Babel.ParserGenerator
{
	internal class State
	{
		public int num;

		public Dictionary<int, int> parser_table;

		public Dictionary<int, int> Goto;

		public int defaultAction;

		public State(int[] actions, int[] gotos) : this(actions)
		{
			this.Goto = new Dictionary<int, int>();
			for (int i = 0; i < (int)gotos.Length; i = i + 2)
			{
				this.Goto.Add(gotos[i], gotos[i + 1]);
			}
		}

		public State(int[] actions)
		{
			this.parser_table = new Dictionary<int, int>();
			for (int i = 0; i < (int)actions.Length; i = i + 2)
			{
				this.parser_table.Add(actions[i], actions[i + 1]);
			}
		}

		public State(int defaultAction)
		{
			this.defaultAction = defaultAction;
		}

		public State(int defaultAction, int[] gotos) : this(defaultAction)
		{
			this.Goto = new Dictionary<int, int>();
			for (int i = 0; i < (int)gotos.Length; i = i + 2)
			{
				this.Goto.Add(gotos[i], gotos[i + 1]);
			}
		}
	}
}
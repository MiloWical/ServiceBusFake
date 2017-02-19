using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Babel.ParserGenerator
{
	internal abstract class ShiftReduceParser<YYSTYPE, YYLTYPE>
	where YYSTYPE : struct
	where YYLTYPE : IMerge<YYLTYPE>
	{
		public bool Trace;

		public AScanner<YYSTYPE, YYLTYPE> scanner;

		protected YYSTYPE yyval;

		protected YYLTYPE yyloc;

		protected YYLTYPE lastL;

		private int next;

		private State current_state;

		private bool recovering;

		private int tokensSinceLastError;

		private ParserStack<State> state_stack;

		protected ParserStack<YYSTYPE> value_stack;

		protected ParserStack<YYLTYPE> location_stack;

		protected string[] nonTerminals;

		protected State[] states;

		protected Rule[] rules;

		protected int errToken;

		protected int eofToken;

		protected ShiftReduceParser()
		{
		}

		protected void AddState(int statenr, State state)
		{
			this.states[statenr] = state;
			state.num = statenr;
		}

		protected string CharToString(char ch)
		{
			object[] objArray;
			CultureInfo invariantCulture;
			switch (ch)
			{
				case '\0':
				{
					return "'\\0'";
				}
				case '\u0001':
				case '\u0002':
				case '\u0003':
				case '\u0004':
				case '\u0005':
				case '\u0006':
				{
					invariantCulture = CultureInfo.InvariantCulture;
					objArray = new object[] { ch };
					return string.Format(invariantCulture, "'{0}'", objArray);
				}
				case '\a':
				{
					return "'\\a'";
				}
				case '\b':
				{
					return "'\\b'";
				}
				case '\t':
				{
					return "'\\t'";
				}
				case '\n':
				{
					return "'\\n'";
				}
				case '\v':
				{
					return "'\\v'";
				}
				case '\f':
				{
					return "'\\f'";
				}
				case '\r':
				{
					return "'\\r'";
				}
				default:
				{
					invariantCulture = CultureInfo.InvariantCulture;
					objArray = new object[] { ch };
					return string.Format(invariantCulture, "'{0}'", objArray);
				}
			}
		}

		public bool DiscardInvalidTokens()
		{
			int currentState = this.current_state.defaultAction;
			if (this.current_state.parser_table == null)
			{
				if (!this.recovering || this.tokensSinceLastError != 0)
				{
					return true;
				}
				if (this.Trace)
				{
					Console.Error.WriteLine("Error: panic discard of {0}", this.TerminalToString(this.next));
				}
				this.next = 0;
				return true;
			}
			while (true)
			{
				if (this.next == 0)
				{
					if (this.Trace)
					{
						Console.Error.Write("Reading a token: ");
					}
					this.next = this.scanner.yylex();
				}
				if (this.Trace)
				{
					Console.Error.WriteLine("Next token is {0}", this.TerminalToString(this.next));
				}
				if (this.next == this.eofToken)
				{
					return false;
				}
				if (this.current_state.parser_table.ContainsKey(this.next))
				{
					currentState = this.current_state.parser_table[this.next];
				}
				if (currentState != 0)
				{
					break;
				}
				if (this.Trace)
				{
					Console.Error.WriteLine("Error: Discarding {0}", this.TerminalToString(this.next));
				}
				this.next = 0;
			}
			return true;
		}

		private void DisplayProduction(Rule rule)
		{
			if ((int)rule.rhs.Length != 0)
			{
				int[] numArray = rule.rhs;
				for (int i = 0; i < (int)numArray.Length; i++)
				{
					int num = numArray[i];
					Console.Error.Write("{0} ", this.SymbolToString(num));
				}
			}
			else
			{
				Console.Error.Write("/* empty */ ");
			}
			Console.Error.WriteLine("-> {0}", this.SymbolToString(rule.lhs));
		}

		private void DisplayRule(int rule_nr)
		{
			Console.Error.Write("Reducing stack by rule {0}, ", rule_nr);
			this.DisplayProduction(this.rules[rule_nr]);
		}

		private void DisplayStack()
		{
			Console.Error.Write("State now");
			for (int i = 0; i < this.state_stack.top; i++)
			{
				Console.Error.Write(" {0}", this.state_stack.array[i].num);
			}
			Console.Error.WriteLine();
		}

		protected abstract void DoAction(int action_nr);

		public bool ErrorRecovery()
		{
			if (!this.recovering)
			{
				this.ReportError();
			}
			if (!this.FindErrorRecoveryState())
			{
				return false;
			}
			this.ShiftErrorToken();
			bool flag = this.DiscardInvalidTokens();
			this.recovering = true;
			this.tokensSinceLastError = 0;
			return flag;
		}

		public bool FindErrorRecoveryState()
		{
			while (this.current_state.parser_table == null || !this.current_state.parser_table.ContainsKey(this.errToken) || this.current_state.parser_table[this.errToken] <= 0)
			{
				if (this.Trace)
				{
					Console.Error.WriteLine("Error: popping state {0}", this.state_stack.Top().num);
				}
				this.state_stack.Pop();
				this.value_stack.Pop();
				this.location_stack.Pop();
				if (this.Trace)
				{
					this.DisplayStack();
				}
				if (this.state_stack.IsEmpty())
				{
					if (this.Trace)
					{
						Console.Error.Write("Aborting: didn't find a state that accepts error token");
					}
					return false;
				}
				this.current_state = this.state_stack.Top();
			}
			return true;
		}

		protected abstract void Initialize();

		public bool Parse()
		{
			int currentState;
			this.Initialize();
			this.next = 0;
			this.current_state = this.states[0];
			this.state_stack.Push(this.current_state);
			this.value_stack.Push(this.yyval);
			this.location_stack.Push(this.yyloc);
			do
			{
			Label0:
				if (this.Trace)
				{
					Console.Error.WriteLine("Entering state {0} ", this.current_state.num);
				}
				currentState = this.current_state.defaultAction;
				if (this.current_state.parser_table != null)
				{
					if (this.next == 0)
					{
						if (this.Trace)
						{
							Console.Error.Write("Reading a token: ");
						}
						this.lastL = this.scanner.yylloc;
						this.next = this.scanner.yylex();
					}
					if (this.Trace)
					{
						Console.Error.WriteLine("Next token is {0}", this.TerminalToString(this.next));
					}
					if (this.current_state.parser_table.ContainsKey(this.next))
					{
						currentState = this.current_state.parser_table[this.next];
					}
				}
				if (currentState <= 0)
				{
					if (currentState >= 0)
					{
						continue;
					}
					this.Reduce(-currentState);
					if (currentState == -1)
					{
						return true;
					}
					else
					{
						goto Label0;
					}
				}
				else
				{
					this.Shift(currentState);
					goto Label0;
				}
			}
			while (currentState != 0 || this.ErrorRecovery());
			return false;
		}

		protected void Reduce(int rule_nr)
		{
			if (this.Trace)
			{
				this.DisplayRule(rule_nr);
			}
			Rule rule = this.rules[rule_nr];
			if ((int)rule.rhs.Length != 1)
			{
				this.yyval = default(YYSTYPE);
			}
			else
			{
				this.yyval = this.value_stack.Top();
			}
			if ((int)rule.rhs.Length == 1)
			{
				this.yyloc = this.location_stack.Top();
			}
			else if ((int)rule.rhs.Length != 0)
			{
				YYLTYPE locationStack = this.location_stack.array[this.location_stack.top - (int)rule.rhs.Length];
				YYLTYPE yYLTYPE = this.location_stack.Top();
				if (locationStack != null && yYLTYPE != null)
				{
					this.yyloc = locationStack.Merge(yYLTYPE);
				}
			}
			else
			{
				this.yyloc = (this.scanner.yylloc != null ? this.scanner.yylloc.Merge(this.lastL) : default(YYLTYPE));
			}
			this.DoAction(rule_nr);
			for (int i = 0; i < (int)rule.rhs.Length; i++)
			{
				this.state_stack.Pop();
				this.value_stack.Pop();
				this.location_stack.Pop();
			}
			if (this.Trace)
			{
				this.DisplayStack();
			}
			this.current_state = this.state_stack.Top();
			if (this.current_state.Goto.ContainsKey(rule.lhs))
			{
				this.current_state = this.states[this.current_state.Goto[rule.lhs]];
			}
			this.state_stack.Push(this.current_state);
			this.value_stack.Push(this.yyval);
			this.location_stack.Push(this.yyloc);
		}

		public void ReportError()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("syntax error, unexpected {0}", this.TerminalToString(this.next));
			if (this.current_state.parser_table.Count < 7)
			{
				bool flag = true;
				foreach (int key in this.current_state.parser_table.Keys)
				{
					if (!flag)
					{
						stringBuilder.Append(", or ");
					}
					else
					{
						stringBuilder.Append(", expecting ");
					}
					stringBuilder.Append(this.TerminalToString(key));
					flag = false;
				}
			}
			this.scanner.yyerror(stringBuilder.ToString(), new object[0]);
		}

		protected void Shift(int state_nr)
		{
			if (this.Trace)
			{
				Console.Error.Write("Shifting token {0}, ", this.TerminalToString(this.next));
			}
			this.current_state = this.states[state_nr];
			this.value_stack.Push(this.scanner.yylval);
			this.state_stack.Push(this.current_state);
			this.location_stack.Push(this.scanner.yylloc);
			if (this.recovering)
			{
				if (this.next != this.errToken)
				{
					ShiftReduceParser<YYSTYPE, YYLTYPE> shiftReduceParser = this;
					shiftReduceParser.tokensSinceLastError = shiftReduceParser.tokensSinceLastError + 1;
				}
				if (this.tokensSinceLastError > 5)
				{
					this.recovering = false;
				}
			}
			if (this.next != this.eofToken)
			{
				this.next = 0;
			}
		}

		public void ShiftErrorToken()
		{
			int num = this.next;
			this.next = this.errToken;
			this.Shift(this.current_state.parser_table[this.next]);
			if (this.Trace)
			{
				Console.Error.WriteLine("Entering state {0} ", this.current_state.num);
			}
			this.next = num;
		}

		private string SymbolToString(int symbol)
		{
			if (symbol >= 0)
			{
				return this.TerminalToString(symbol);
			}
			return this.nonTerminals[-symbol];
		}

		protected abstract string TerminalToString(int terminal);

		protected void yyclearin()
		{
			this.next = 0;
		}

		protected void yyerrok()
		{
			this.recovering = false;
		}
	}
}
using System;

namespace Babel.ParserGenerator
{
	internal class ParserStack<T>
	{
		public T[] array;

		public int top;

		public ParserStack()
		{
		}

		public bool IsEmpty()
		{
			return this.top == 0;
		}

		public T Pop()
		{
			ParserStack<T> parserStack = this;
			int num = parserStack.top - 1;
			int num1 = num;
			parserStack.top = num;
			return this.array[num1];
		}

		public void Push(T value)
		{
			if (this.top >= (int)this.array.Length)
			{
				T[] tArray = new T[(int)this.array.Length * 2];
				Array.Copy(this.array, tArray, this.top);
				this.array = tArray;
			}
			T[] tArray1 = this.array;
			ParserStack<T> parserStack = this;
			int num = parserStack.top;
			int num1 = num;
			parserStack.top = num + 1;
			tArray1[num1] = value;
		}

		public T Top()
		{
			return this.array[this.top - 1];
		}
	}
}
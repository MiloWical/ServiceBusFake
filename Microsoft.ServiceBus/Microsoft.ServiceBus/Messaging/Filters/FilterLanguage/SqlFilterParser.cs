using Babel.ParserGenerator;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Filters;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.ServiceBus.Messaging.Filters.FilterLanguage
{
	internal class SqlFilterParser : ShiftReduceParser<LexValue, LexLocation>
	{
		private readonly ParameterExpression messageParameter;

		private readonly ParameterExpression parametersParameter;

		private readonly HashSet<string> requiredParameters;

		private Expression<Func<BrokeredMessage, IDictionary<string, object>, bool?>> expressionTree;

		public SqlFilterParser()
		{
			this.messageParameter = Expression.Parameter(typeof(BrokeredMessage), "@message");
			this.parametersParameter = Expression.Parameter(typeof(IDictionary<string, object>), "@parameters");
			this.requiredParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}

		protected override void DoAction(int action)
		{
			Expression expression;
			switch (action)
			{
				case 2:
				{
					Expression valueStack = this.value_stack.array[this.value_stack.top - 1].expression;
					ParameterExpression[] parameterExpressionArray = new ParameterExpression[] { this.messageParameter, this.parametersParameter };
					this.expressionTree = Expression.Lambda<Func<BrokeredMessage, IDictionary<string, object>, bool?>>(valueStack, parameterExpressionArray);
					return;
				}
				case 3:
				{
					Expression valueStack1 = this.value_stack.array[this.value_stack.top - 3].expression;
					Expression expression1 = this.value_stack.array[this.value_stack.top - 1].expression;
					this.yyval.expression = ExpressionBuilder.AndAlso(valueStack1, expression1);
					return;
				}
				case 4:
				{
					Expression valueStack2 = this.value_stack.array[this.value_stack.top - 3].expression;
					Expression expression2 = this.value_stack.array[this.value_stack.top - 1].expression;
					this.yyval.expression = ExpressionBuilder.OrElse(valueStack2, expression2);
					return;
				}
				case 5:
				{
					Expression valueStack3 = this.value_stack.array[this.value_stack.top - 1].expression;
					this.yyval.expression = ExpressionBuilder.IsFalse(valueStack3);
					return;
				}
				case 6:
				{
					Expression expression3 = this.value_stack.array[this.value_stack.top - 2].expression;
					this.yyval.expression = expression3;
					return;
				}
				case 7:
				{
					Expression valueStack4 = this.value_stack.array[this.value_stack.top - 2].expression;
					this.yyval.expression = ExpressionBuilder.Exists(valueStack4);
					return;
				}
				case 8:
				{
					Expression expression4 = this.value_stack.array[this.value_stack.top - 3].expression;
					this.yyval.expression = ExpressionBuilder.IsNull(expression4);
					return;
				}
				case 9:
				{
					Expression valueStack5 = this.value_stack.array[this.value_stack.top - 4].expression;
					this.yyval.expression = ExpressionBuilder.IsNotNull(valueStack5);
					return;
				}
				case 10:
				{
					DelayExpression delayExpression = this.value_stack.array[this.value_stack.top - 4].delay;
					Expression expression5 = this.value_stack.array[this.value_stack.top - 2].expression;
					Expression valueStack6 = this.value_stack.array[this.value_stack.top - 1].expression;
					this.yyval.expression = ExpressionBuilder.Like(delayExpression.GetExpression(), expression5, valueStack6);
					return;
				}
				case 11:
				{
					DelayExpression delayExpression1 = this.value_stack.array[this.value_stack.top - 5].delay;
					Expression expression6 = this.value_stack.array[this.value_stack.top - 2].expression;
					Expression valueStack7 = this.value_stack.array[this.value_stack.top - 1].expression;
					this.yyval.expression = ExpressionBuilder.NotLike(delayExpression1.GetExpression(), expression6, valueStack7);
					return;
				}
				case 12:
				{
					DelayExpression delayExpression2 = this.value_stack.array[this.value_stack.top - 5].delay;
					List<Expression> expressions = this.value_stack.array[this.value_stack.top - 2].expressions;
					this.yyval.expression = ExpressionBuilder.In(delayExpression2.GetExpression(), expressions);
					return;
				}
				case 13:
				{
					DelayExpression delayExpression3 = this.value_stack.array[this.value_stack.top - 6].delay;
					List<Expression> expressions1 = this.value_stack.array[this.value_stack.top - 2].expressions;
					this.yyval.expression = ExpressionBuilder.NotIn(delayExpression3.GetExpression(), expressions1);
					return;
				}
				case 14:
				{
					DelayExpression delayExpression4 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression delayExpression5 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.expression = ExpressionBuilder.Equal(delayExpression4.GetExpression(), delayExpression5.GetExpression());
					return;
				}
				case 15:
				{
					DelayExpression delayExpression6 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression delayExpression7 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.expression = ExpressionBuilder.NotEqual(delayExpression6.GetExpression(), delayExpression7.GetExpression());
					return;
				}
				case 16:
				{
					DelayExpression valueStack8 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression delayExpression8 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.expression = ExpressionBuilder.LessThan(valueStack8.GetExpression(), delayExpression8.GetExpression());
					return;
				}
				case 17:
				{
					DelayExpression valueStack9 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression delayExpression9 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.expression = ExpressionBuilder.LessThanOrEqual(valueStack9.GetExpression(), delayExpression9.GetExpression());
					return;
				}
				case 18:
				{
					DelayExpression valueStack10 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression delayExpression10 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.expression = ExpressionBuilder.GreaterThan(valueStack10.GetExpression(), delayExpression10.GetExpression());
					return;
				}
				case 19:
				{
					DelayExpression valueStack11 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression delayExpression11 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.expression = ExpressionBuilder.GreaterThanOrEqual(valueStack11.GetExpression(), delayExpression11.GetExpression());
					return;
				}
				case 20:
				{
					LexLocation locationStack = this.location_stack.array[this.location_stack.top - 1];
					string str = this.value_stack.array[this.value_stack.top - 1].@value;
					this.yyval.delay = DelayExpression.NumericConstant(locationStack, str, TypeCode.Int64);
					return;
				}
				case 21:
				{
					LexLocation lexLocation = this.location_stack.array[this.location_stack.top - 1];
					string str1 = this.value_stack.array[this.value_stack.top - 1].@value;
					this.yyval.delay = DelayExpression.NumericConstant(lexLocation, str1, TypeCode.Decimal);
					return;
				}
				case 22:
				{
					LexLocation locationStack1 = this.location_stack.array[this.location_stack.top - 1];
					string str2 = this.value_stack.array[this.value_stack.top - 1].@value;
					this.yyval.delay = DelayExpression.NumericConstant(locationStack1, str2, TypeCode.Double);
					return;
				}
				case 23:
				{
					this.yyval.delay = DelayExpression.Expression(Expression.Constant(true, typeof(bool)));
					return;
				}
				case 24:
				{
					this.yyval.delay = DelayExpression.Expression(Expression.Constant(false, typeof(bool)));
					return;
				}
				case 25:
				{
					string str3 = this.value_stack.array[this.value_stack.top - 1].@value;
					this.yyval.delay = DelayExpression.Expression(Expression.Constant(str3));
					return;
				}
				case 26:
				{
					this.yyval.delay = DelayExpression.Expression(Expression.Constant(null));
					return;
				}
				case 27:
				{
					string str4 = this.value_stack.array[this.value_stack.top - 1].@value;
					this.yyval.qname = new QualifiedPropertyName(PropertyScope.User, str4);
					return;
				}
				case 28:
				{
					string str5 = this.value_stack.array[this.value_stack.top - 3].@value;
					string str6 = this.value_stack.array[this.value_stack.top - 1].@value;
					if (string.Equals(str5, "sys", StringComparison.OrdinalIgnoreCase))
					{
						this.yyval.qname = new QualifiedPropertyName(PropertyScope.System, str6);
						return;
					}
					if (string.Equals(str5, "user", StringComparison.OrdinalIgnoreCase))
					{
						this.yyval.qname = new QualifiedPropertyName(PropertyScope.User, str6);
						return;
					}
					this.scanner.yyerror(SRClient.FilterScopeNotSupported(str5), new object[0]);
					return;
				}
				case 29:
				{
					this.yyval.@value = this.value_stack.array[this.value_stack.top - 1].@value;
					return;
				}
				case 30:
				{
					this.yyval.@value = this.value_stack.array[this.value_stack.top - 1].@value;
					return;
				}
				case 31:
				{
					QualifiedPropertyName qualifiedPropertyName = this.value_stack.array[this.value_stack.top - 1].qname;
					this.yyval.expression = new GetPropertyExpression(this.messageParameter, qualifiedPropertyName);
					return;
				}
				case 32:
				{
					this.yyval.delay = this.value_stack.array[this.value_stack.top - 1].delay;
					return;
				}
				case 33:
				{
					this.yyval.delay = DelayExpression.Expression(this.value_stack.array[this.value_stack.top - 1].expression);
					return;
				}
				case 34:
				{
					this.yyval.delay = DelayExpression.Expression(this.value_stack.array[this.value_stack.top - 1].expression);
					return;
				}
				case 35:
				{
					this.yyval.delay = DelayExpression.Expression(this.value_stack.array[this.value_stack.top - 1].expression);
					return;
				}
				case 36:
				{
					DelayExpression valueStack12 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression delayExpression12 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.delay = DelayExpression.Expression(ExpressionBuilder.AddChecked(valueStack12.GetExpression(), delayExpression12.GetExpression()));
					return;
				}
				case 37:
				{
					DelayExpression valueStack13 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression delayExpression13 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.delay = DelayExpression.Expression(ExpressionBuilder.SubtractChecked(valueStack13.GetExpression(), delayExpression13.GetExpression()));
					return;
				}
				case 38:
				{
					DelayExpression valueStack14 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression delayExpression14 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.delay = DelayExpression.Expression(ExpressionBuilder.MultiplyChecked(valueStack14.GetExpression(), delayExpression14.GetExpression()));
					return;
				}
				case 39:
				{
					DelayExpression valueStack15 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression delayExpression15 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.delay = DelayExpression.Expression(ExpressionBuilder.Divide(valueStack15.GetExpression(), delayExpression15.GetExpression()));
					return;
				}
				case 40:
				{
					DelayExpression valueStack16 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression delayExpression16 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.delay = DelayExpression.Expression(ExpressionBuilder.Modulo(valueStack16.GetExpression(), delayExpression16.GetExpression()));
					return;
				}
				case 41:
				{
					DelayExpression valueStack17 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.delay = DelayExpression.Expression(valueStack17.GetExpression());
					return;
				}
				case 42:
				{
					DelayExpression delayExpression17 = this.value_stack.array[this.value_stack.top - 1].delay;
					if (!delayExpression17.TryGetConstantNumericLiteral(ExpressionType.NegateChecked, out expression))
					{
						expression = ExpressionBuilder.NegateChecked(delayExpression17.GetExpression());
					}
					this.yyval.delay = DelayExpression.Expression(expression);
					return;
				}
				case 43:
				{
					DelayExpression valueStack18 = this.value_stack.array[this.value_stack.top - 2].delay;
					this.yyval.delay = DelayExpression.Expression(valueStack18.GetExpression());
					return;
				}
				case 44:
				{
					string str7 = this.value_stack.array[this.value_stack.top - 1].@value;
					this.requiredParameters.Add(str7);
					this.yyval.expression = ExpressionBuilder.GetParameter(this.parametersParameter, str7);
					return;
				}
				case 45:
				{
					string str8 = this.value_stack.array[this.value_stack.top - 4].@value;
					List<Expression> expressions2 = this.value_stack.array[this.value_stack.top - 2].expressions;
					this.yyval.expression = ExpressionBuilder.MakeFunction(this.messageParameter, str8, expressions2);
					return;
				}
				case 46:
				{
					this.yyval.expressions = new List<Expression>();
					return;
				}
				case 47:
				{
					Expression expression7 = this.value_stack.array[this.value_stack.top - 1].delay.GetExpression();
					List<Expression> expressions3 = new List<Expression>()
					{
						ExpressionBuilder.ConvertToObject(expression7)
					};
					this.yyval.expressions = expressions3;
					return;
				}
				case 48:
				{
					List<Expression> expressions4 = this.value_stack.array[this.value_stack.top - 3].expressions;
					Expression expression8 = this.value_stack.array[this.value_stack.top - 1].delay.GetExpression();
					expressions4.Add(ExpressionBuilder.ConvertToObject(expression8));
					this.yyval.expressions = expressions4;
					return;
				}
				case 49:
				{
					this.yyval.expression = this.value_stack.array[this.value_stack.top - 1].delay.GetExpression();
					return;
				}
				case 50:
				{
					this.yyval.expression = Expression.Constant(null);
					return;
				}
				case 51:
				{
					this.yyval.expression = this.value_stack.array[this.value_stack.top - 1].delay.GetExpression();
					return;
				}
				case 52:
				{
					DelayExpression delayExpression18 = this.value_stack.array[this.value_stack.top - 1].delay;
					List<Expression> expressions5 = new List<Expression>()
					{
						ExpressionBuilder.ConvertToObject(delayExpression18.GetExpression())
					};
					this.yyval.expressions = expressions5;
					return;
				}
				case 53:
				{
					List<Expression> expressions6 = this.value_stack.array[this.value_stack.top - 3].expressions;
					DelayExpression valueStack19 = this.value_stack.array[this.value_stack.top - 1].delay;
					expressions6.Add(ExpressionBuilder.ConvertToObject(valueStack19.GetExpression()));
					this.yyval.expressions = expressions6;
					return;
				}
				default:
				{
					return;
				}
			}
		}

		protected override void Initialize()
		{
			this.errToken = 48;
			this.eofToken = 49;
			this.states = new State[99];
			base.AddState(0, new State(new int[] { 61, 8, 40, 10, 66, 89, 50, 41, 51, 53, 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 43, 54, 45, 56 }, new int[] { -3, 1, -4, 3, -6, 93, -2, 48, -1, 49, -12, 98, -13, 30, -5, 38, -7, 40 }));
			int[] numArray = new int[] { 49, 2 };
			base.AddState(1, new State(numArray));
			base.AddState(2, new State(-1));
			base.AddState(3, new State(new int[] { 59, 4, 60, 6, 49, -2 }));
			base.AddState(4, new State(new int[] { 61, 8, 40, 10, 66, 89, 50, 41, 51, 53, 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 43, 54, 45, 56 }, new int[] { -4, 5, -6, 93, -2, 48, -1, 49, -12, 98, -13, 30, -5, 38, -7, 40 }));
			base.AddState(5, new State(-3));
			base.AddState(6, new State(new int[] { 61, 8, 40, 10, 66, 89, 50, 41, 51, 53, 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 43, 54, 45, 56 }, new int[] { -4, 7, -6, 93, -2, 48, -1, 49, -12, 98, -13, 30, -5, 38, -7, 40 }));
			base.AddState(7, new State(new int[] { 59, 4, 60, -4, 49, -4, 41, -4 }));
			base.AddState(8, new State(new int[] { 61, 8, 40, 10, 66, 89, 50, 41, 51, 53, 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 43, 54, 45, 56 }, new int[] { -4, 9, -6, 93, -2, 48, -1, 49, -12, 98, -13, 30, -5, 38, -7, 40 }));
			base.AddState(9, new State(-5));
			base.AddState(10, new State(new int[] { 61, 8, 40, 10, 66, 89, 50, 41, 51, 53, 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 43, 54, 45, 56 }, new int[] { -4, 11, -12, 13, -6, 93, -2, 48, -1, 49, -13, 30, -5, 38, -7, 40 }));
			base.AddState(11, new State(new int[] { 41, 12, 59, 4, 60, 6 }));
			base.AddState(12, new State(-6));
			base.AddState(13, new State(new int[] { 41, 14, 62, 15, 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 61, 62, 64, 73, 67, 77, 68, 79, 69, 81, 70, 83, 71, 85, 72, 87 }));
			base.AddState(14, new State(-43));
			base.AddState(15, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -8, 16, -12, 61, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			int[] numArray1 = new int[] { 63, 18, 59, -50, 60, -50, 49, -50, 41, -50 };
			int[] numArray2 = new int[] { -9, 17 };
			base.AddState(16, new State(numArray1, numArray2));
			base.AddState(17, new State(-10));
			base.AddState(18, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 19, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(19, new State(new int[] { 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 59, -51, 60, -51, 49, -51, 41, -51 }));
			base.AddState(20, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 21, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(21, new State(new int[] { 43, -36, 45, -36, 42, 24, 47, 26, 37, 28, 62, -36, 61, -36, 64, -36, 67, -36, 68, -36, 69, -36, 70, -36, 71, -36, 72, -36, 41, -36, 63, -36, 59, -36, 60, -36, 49, -36, 44, -36 }));
			base.AddState(22, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 23, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(23, new State(new int[] { 43, -37, 45, -37, 42, 24, 47, 26, 37, 28, 62, -37, 61, -37, 64, -37, 67, -37, 68, -37, 69, -37, 70, -37, 71, -37, 72, -37, 41, -37, 63, -37, 59, -37, 60, -37, 49, -37, 44, -37 }));
			base.AddState(24, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 25, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(25, new State(-38));
			base.AddState(26, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 27, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(27, new State(-39));
			base.AddState(28, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 29, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(29, new State(-40));
			base.AddState(30, new State(-32));
			base.AddState(31, new State(-20));
			base.AddState(32, new State(-21));
			base.AddState(33, new State(-22));
			base.AddState(34, new State(-23));
			base.AddState(35, new State(-24));
			base.AddState(36, new State(-25));
			base.AddState(37, new State(-26));
			base.AddState(38, new State(-33));
			base.AddState(39, new State(-44));
			base.AddState(40, new State(-34));
			base.AddState(41, new State(new int[] { 40, 42, 46, -29, 65, -29, 62, -29, 43, -29, 45, -29, 42, -29, 47, -29, 37, -29, 61, -29, 64, -29, 67, -29, 68, -29, 69, -29, 70, -29, 71, -29, 72, -29, 41, -29, 63, -29, 59, -29, 60, -29, 49, -29, 44, -29 }));
			base.AddState(42, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58, 41, -46, 44, -46 }, new int[] { -11, 43, -12, 60, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(43, new State(new int[] { 41, 44, 44, 45 }));
			base.AddState(44, new State(-45));
			base.AddState(45, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 46, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(46, new State(new int[] { 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 41, -48, 44, -48 }));
			base.AddState(47, new State(-35));
			base.AddState(48, new State(-31));
			base.AddState(49, new State(new int[] { 46, 50, 65, -27, 62, -27, 43, -27, 45, -27, 42, -27, 47, -27, 37, -27, 61, -27, 64, -27, 67, -27, 68, -27, 69, -27, 70, -27, 71, -27, 72, -27, 41, -27, 63, -27, 59, -27, 60, -27, 49, -27, 44, -27 }));
			int[] numArray3 = new int[] { 50, 52, 51, 53 };
			int[] numArray4 = new int[] { -1, 51 };
			base.AddState(50, new State(numArray3, numArray4));
			base.AddState(51, new State(-28));
			base.AddState(52, new State(-29));
			base.AddState(53, new State(-30));
			base.AddState(54, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 55, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(55, new State(-41));
			base.AddState(56, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 57, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(57, new State(-42));
			base.AddState(58, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 59, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(59, new State(new int[] { 41, 14, 43, 20, 45, 22, 42, 24, 47, 26, 37, 28 }));
			base.AddState(60, new State(new int[] { 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 41, -47, 44, -47 }));
			base.AddState(61, new State(new int[] { 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 63, -49, 59, -49, 60, -49, 49, -49, 41, -49 }));
			base.AddState(62, new State(new int[] { 62, 63, 64, 66 }));
			base.AddState(63, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -8, 64, -12, 61, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			int[] numArray5 = new int[] { 63, 18, 59, -50, 60, -50, 49, -50, 41, -50 };
			int[] numArray6 = new int[] { -9, 65 };
			base.AddState(64, new State(numArray5, numArray6));
			base.AddState(65, new State(-11));
			int[] numArray7 = new int[] { 40, 67 };
			base.AddState(66, new State(numArray7));
			base.AddState(67, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -10, 68, -12, 72, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(68, new State(new int[] { 41, 69, 44, 70 }));
			base.AddState(69, new State(-13));
			base.AddState(70, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 71, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(71, new State(new int[] { 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 41, -53, 44, -53 }));
			base.AddState(72, new State(new int[] { 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 41, -52, 44, -52 }));
			int[] numArray8 = new int[] { 40, 74 };
			base.AddState(73, new State(numArray8));
			base.AddState(74, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -10, 75, -12, 72, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(75, new State(new int[] { 41, 76, 44, 70 }));
			base.AddState(76, new State(-12));
			base.AddState(77, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 78, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(78, new State(new int[] { 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 59, -14, 60, -14, 49, -14, 41, -14 }));
			base.AddState(79, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 80, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(80, new State(new int[] { 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 59, -15, 60, -15, 49, -15, 41, -15 }));
			base.AddState(81, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 82, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(82, new State(new int[] { 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 59, -16, 60, -16, 49, -16, 41, -16 }));
			base.AddState(83, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 84, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(84, new State(new int[] { 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 59, -17, 60, -17, 49, -17, 41, -17 }));
			base.AddState(85, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 86, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(86, new State(new int[] { 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 59, -18, 60, -18, 49, -18, 41, -18 }));
			base.AddState(87, new State(new int[] { 53, 31, 54, 32, 55, 33, 73, 34, 74, 35, 56, 36, 75, 37, 52, 39, 50, 41, 51, 53, 43, 54, 45, 56, 40, 58 }, new int[] { -12, 88, -13, 30, -5, 38, -7, 40, -6, 47, -2, 48, -1, 49 }));
			base.AddState(88, new State(new int[] { 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 59, -19, 60, -19, 49, -19, 41, -19 }));
			int[] numArray9 = new int[] { 40, 90 };
			base.AddState(89, new State(numArray9));
			base.AddState(90, new State(new int[] { 50, 52, 51, 53 }, new int[] { -6, 91, -2, 48, -1, 49 }));
			int[] numArray10 = new int[] { 41, 92 };
			base.AddState(91, new State(numArray10));
			base.AddState(92, new State(-7));
			base.AddState(93, new State(new int[] { 65, 94, 62, -35, 43, -35, 45, -35, 42, -35, 47, -35, 37, -35, 61, -35, 64, -35, 67, -35, 68, -35, 69, -35, 70, -35, 71, -35, 72, -35, 41, -35 }));
			base.AddState(94, new State(new int[] { 75, 95, 61, 96 }));
			base.AddState(95, new State(-8));
			int[] numArray11 = new int[] { 75, 97 };
			base.AddState(96, new State(numArray11));
			base.AddState(97, new State(-9));
			base.AddState(98, new State(new int[] { 62, 15, 43, 20, 45, 22, 42, 24, 47, 26, 37, 28, 61, 62, 64, 73, 67, 77, 68, 79, 69, 81, 70, 83, 71, 85, 72, 87 }));
			this.rules = new Rule[54];
			Rule[] rule = this.rules;
			int[] numArray12 = new int[] { -3, 49 };
			rule[1] = new Rule(-14, numArray12);
			Rule[] ruleArray = this.rules;
			int[] numArray13 = new int[] { -4 };
			ruleArray[2] = new Rule(-3, numArray13);
			this.rules[3] = new Rule(-4, new int[] { -4, 59, -4 });
			this.rules[4] = new Rule(-4, new int[] { -4, 60, -4 });
			Rule[] rule1 = this.rules;
			int[] numArray14 = new int[] { 61, -4 };
			rule1[5] = new Rule(-4, numArray14);
			this.rules[6] = new Rule(-4, new int[] { 40, -4, 41 });
			this.rules[7] = new Rule(-4, new int[] { 66, 40, -6, 41 });
			this.rules[8] = new Rule(-4, new int[] { -6, 65, 75 });
			this.rules[9] = new Rule(-4, new int[] { -6, 65, 61, 75 });
			this.rules[10] = new Rule(-4, new int[] { -12, 62, -8, -9 });
			this.rules[11] = new Rule(-4, new int[] { -12, 61, 62, -8, -9 });
			this.rules[12] = new Rule(-4, new int[] { -12, 64, 40, -10, 41 });
			this.rules[13] = new Rule(-4, new int[] { -12, 61, 64, 40, -10, 41 });
			this.rules[14] = new Rule(-4, new int[] { -12, 67, -12 });
			this.rules[15] = new Rule(-4, new int[] { -12, 68, -12 });
			this.rules[16] = new Rule(-4, new int[] { -12, 69, -12 });
			this.rules[17] = new Rule(-4, new int[] { -12, 70, -12 });
			this.rules[18] = new Rule(-4, new int[] { -12, 71, -12 });
			this.rules[19] = new Rule(-4, new int[] { -12, 72, -12 });
			Rule[] ruleArray1 = this.rules;
			int[] numArray15 = new int[] { 53 };
			ruleArray1[20] = new Rule(-13, numArray15);
			Rule[] rule2 = this.rules;
			int[] numArray16 = new int[] { 54 };
			rule2[21] = new Rule(-13, numArray16);
			Rule[] ruleArray2 = this.rules;
			int[] numArray17 = new int[] { 55 };
			ruleArray2[22] = new Rule(-13, numArray17);
			Rule[] rule3 = this.rules;
			int[] numArray18 = new int[] { 73 };
			rule3[23] = new Rule(-13, numArray18);
			Rule[] ruleArray3 = this.rules;
			int[] numArray19 = new int[] { 74 };
			ruleArray3[24] = new Rule(-13, numArray19);
			Rule[] rule4 = this.rules;
			int[] numArray20 = new int[] { 56 };
			rule4[25] = new Rule(-13, numArray20);
			Rule[] ruleArray4 = this.rules;
			int[] numArray21 = new int[] { 75 };
			ruleArray4[26] = new Rule(-13, numArray21);
			Rule[] rule5 = this.rules;
			int[] numArray22 = new int[] { -1 };
			rule5[27] = new Rule(-2, numArray22);
			this.rules[28] = new Rule(-2, new int[] { -1, 46, -1 });
			Rule[] ruleArray5 = this.rules;
			int[] numArray23 = new int[] { 50 };
			ruleArray5[29] = new Rule(-1, numArray23);
			Rule[] rule6 = this.rules;
			int[] numArray24 = new int[] { 51 };
			rule6[30] = new Rule(-1, numArray24);
			Rule[] ruleArray6 = this.rules;
			int[] numArray25 = new int[] { -2 };
			ruleArray6[31] = new Rule(-6, numArray25);
			Rule[] rule7 = this.rules;
			int[] numArray26 = new int[] { -13 };
			rule7[32] = new Rule(-12, numArray26);
			Rule[] ruleArray7 = this.rules;
			int[] numArray27 = new int[] { -5 };
			ruleArray7[33] = new Rule(-12, numArray27);
			Rule[] rule8 = this.rules;
			int[] numArray28 = new int[] { -7 };
			rule8[34] = new Rule(-12, numArray28);
			Rule[] ruleArray8 = this.rules;
			int[] numArray29 = new int[] { -6 };
			ruleArray8[35] = new Rule(-12, numArray29);
			this.rules[36] = new Rule(-12, new int[] { -12, 43, -12 });
			this.rules[37] = new Rule(-12, new int[] { -12, 45, -12 });
			this.rules[38] = new Rule(-12, new int[] { -12, 42, -12 });
			this.rules[39] = new Rule(-12, new int[] { -12, 47, -12 });
			this.rules[40] = new Rule(-12, new int[] { -12, 37, -12 });
			Rule[] rule9 = this.rules;
			int[] numArray30 = new int[] { 43, -12 };
			rule9[41] = new Rule(-12, numArray30);
			Rule[] ruleArray9 = this.rules;
			int[] numArray31 = new int[] { 45, -12 };
			ruleArray9[42] = new Rule(-12, numArray31);
			this.rules[43] = new Rule(-12, new int[] { 40, -12, 41 });
			Rule[] rule10 = this.rules;
			int[] numArray32 = new int[] { 52 };
			rule10[44] = new Rule(-5, numArray32);
			this.rules[45] = new Rule(-7, new int[] { 50, 40, -11, 41 });
			this.rules[46] = new Rule(-11, new int[0]);
			Rule[] ruleArray10 = this.rules;
			int[] numArray33 = new int[] { -12 };
			ruleArray10[47] = new Rule(-11, numArray33);
			this.rules[48] = new Rule(-11, new int[] { -11, 44, -12 });
			Rule[] rule11 = this.rules;
			int[] numArray34 = new int[] { -12 };
			rule11[49] = new Rule(-8, numArray34);
			this.rules[50] = new Rule(-9, new int[0]);
			Rule[] ruleArray11 = this.rules;
			int[] numArray35 = new int[] { 63, -12 };
			ruleArray11[51] = new Rule(-9, numArray35);
			Rule[] rule12 = this.rules;
			int[] numArray36 = new int[] { -12 };
			rule12[52] = new Rule(-10, numArray36);
			this.rules[53] = new Rule(-10, new int[] { -10, 44, -12 });
			string[] strArrays = new string[] { "", "property_name_part", "property", "production", "predicate", "get_parameter", "get_property", "function", "pattern", "optional_escape", "expression_list", "argument_list", "expression", "literal", "$accept" };
			this.nonTerminals = strArrays;
		}

		public static SqlFilterParserOutput Parse(string sqlExpression)
		{
			SqlFilterParser sqlFilterParser = new SqlFilterParser()
			{
				scanner = new Scanner(sqlExpression)
			};
			sqlFilterParser.Parse();
			return new SqlFilterParserOutput(sqlFilterParser.expressionTree, sqlFilterParser.requiredParameters);
		}

		protected override string TerminalToString(int terminal)
		{
			if ((Tokens)terminal.ToString() != terminal.ToString())
			{
				return (Tokens)terminal.ToString();
			}
			return base.CharToString((char)terminal);
		}

		public static void Validate(string sqlExpression)
		{
			SqlFilterParser sqlFilterParser = new SqlFilterParser()
			{
				scanner = new Scanner(sqlExpression)
			};
			sqlFilterParser.Parse();
		}
	}
}
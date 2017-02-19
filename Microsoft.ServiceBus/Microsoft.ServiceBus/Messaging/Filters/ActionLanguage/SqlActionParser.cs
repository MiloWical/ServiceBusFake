using Babel.ParserGenerator;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Filters;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.ServiceBus.Messaging.Filters.ActionLanguage
{
	internal class SqlActionParser : ShiftReduceParser<LexValue, LexLocation>
	{
		private readonly ParameterExpression messageParameter;

		private readonly ParameterExpression parametersParameter;

		private readonly HashSet<string> requiredParameters;

		private Expression<Action<BrokeredMessage, IDictionary<string, object>>> expressionTree;

		private IList<Expression> expressionList;

		public Expression<Action<BrokeredMessage, IDictionary<string, object>>> ExpressionTree
		{
			get
			{
				if (this.expressionTree == null)
				{
					BlockExpression blockExpression = Expression.Block(this.expressionList);
					ParameterExpression[] parameterExpressionArray = new ParameterExpression[] { this.messageParameter, this.parametersParameter };
					this.expressionTree = Expression.Lambda<Action<BrokeredMessage, IDictionary<string, object>>>(blockExpression, parameterExpressionArray);
				}
				return this.expressionTree;
			}
		}

		public SqlActionParser()
		{
			this.messageParameter = Expression.Parameter(Constants.MessageType, "@message");
			this.parametersParameter = Expression.Parameter(typeof(IDictionary<string, object>), "@parameters");
			this.expressionList = new List<Expression>();
			this.requiredParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}

		protected override void DoAction(int action)
		{
			Expression expression;
			switch (action)
			{
				case 2:
				{
					this.expressionList = this.value_stack.array[this.value_stack.top - 1].expressions;
					return;
				}
				case 3:
				{
					List<Expression> expressions = new List<Expression>()
					{
						this.value_stack.array[this.value_stack.top - 1].expression
					};
					this.yyval.expressions = expressions;
					return;
				}
				case 4:
				{
					List<Expression> valueStack = this.value_stack.array[this.value_stack.top - 2].expressions;
					valueStack.Add(this.value_stack.array[this.value_stack.top - 1].expression);
					this.yyval.expressions = valueStack;
					return;
				}
				case 5:
				{
					this.yyval.expression = this.value_stack.array[this.value_stack.top - 2].expression;
					return;
				}
				case 6:
				{
					this.yyval.expression = this.value_stack.array[this.value_stack.top - 1].expression;
					return;
				}
				case 7:
				{
					this.yyval.expression = this.value_stack.array[this.value_stack.top - 1].expression;
					return;
				}
				case 8:
				case 9:
				{
					return;
				}
				case 10:
				{
					QualifiedPropertyName qualifiedPropertyName = this.value_stack.array[this.value_stack.top - 3].qname;
					DelayExpression delayExpression = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.expression = new SetPropertyExpression(this.messageParameter, qualifiedPropertyName, ExpressionBuilder.ConvertToObject(delayExpression.GetExpression()));
					return;
				}
				case 11:
				{
					QualifiedPropertyName valueStack1 = this.value_stack.array[this.value_stack.top - 1].qname;
					this.yyval.expression = new RemovePropertyExpression(this.messageParameter, valueStack1);
					return;
				}
				case 12:
				{
					this.yyval.delay = this.value_stack.array[this.value_stack.top - 1].delay;
					return;
				}
				case 13:
				{
					this.yyval.delay = DelayExpression.Expression(this.value_stack.array[this.value_stack.top - 1].expression);
					return;
				}
				case 14:
				{
					this.yyval.delay = DelayExpression.Expression(this.value_stack.array[this.value_stack.top - 1].expression);
					return;
				}
				case 15:
				{
					this.yyval.delay = DelayExpression.Expression(this.value_stack.array[this.value_stack.top - 1].expression);
					return;
				}
				case 16:
				{
					DelayExpression delayExpression1 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression valueStack2 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.delay = DelayExpression.Expression(ExpressionBuilder.AddChecked(delayExpression1.GetExpression(), valueStack2.GetExpression()));
					return;
				}
				case 17:
				{
					DelayExpression delayExpression2 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression valueStack3 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.delay = DelayExpression.Expression(ExpressionBuilder.SubtractChecked(delayExpression2.GetExpression(), valueStack3.GetExpression()));
					return;
				}
				case 18:
				{
					DelayExpression delayExpression3 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression valueStack4 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.delay = DelayExpression.Expression(ExpressionBuilder.MultiplyChecked(delayExpression3.GetExpression(), valueStack4.GetExpression()));
					return;
				}
				case 19:
				{
					DelayExpression delayExpression4 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression valueStack5 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.delay = DelayExpression.Expression(ExpressionBuilder.Divide(delayExpression4.GetExpression(), valueStack5.GetExpression()));
					return;
				}
				case 20:
				{
					DelayExpression delayExpression5 = this.value_stack.array[this.value_stack.top - 3].delay;
					DelayExpression valueStack6 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.delay = DelayExpression.Expression(ExpressionBuilder.Modulo(delayExpression5.GetExpression(), valueStack6.GetExpression()));
					return;
				}
				case 21:
				{
					DelayExpression delayExpression6 = this.value_stack.array[this.value_stack.top - 1].delay;
					this.yyval.delay = DelayExpression.Expression(delayExpression6.GetExpression());
					return;
				}
				case 22:
				{
					DelayExpression valueStack7 = this.value_stack.array[this.value_stack.top - 1].delay;
					if (!valueStack7.TryGetConstantNumericLiteral(ExpressionType.NegateChecked, out expression))
					{
						expression = ExpressionBuilder.NegateChecked(valueStack7.GetExpression());
					}
					this.yyval.delay = DelayExpression.Expression(expression);
					return;
				}
				case 23:
				{
					DelayExpression delayExpression7 = this.value_stack.array[this.value_stack.top - 2].delay;
					this.yyval.delay = DelayExpression.Expression(delayExpression7.GetExpression());
					return;
				}
				case 24:
				{
					LexLocation locationStack = this.location_stack.array[this.location_stack.top - 1];
					string str = this.value_stack.array[this.value_stack.top - 1].@value;
					this.yyval.delay = DelayExpression.NumericConstant(locationStack, str, TypeCode.Int64);
					return;
				}
				case 25:
				{
					LexLocation lexLocation = this.location_stack.array[this.location_stack.top - 1];
					string str1 = this.value_stack.array[this.value_stack.top - 1].@value;
					this.yyval.delay = DelayExpression.NumericConstant(lexLocation, str1, TypeCode.Decimal);
					return;
				}
				case 26:
				{
					LexLocation locationStack1 = this.location_stack.array[this.location_stack.top - 1];
					string str2 = this.value_stack.array[this.value_stack.top - 1].@value;
					this.yyval.delay = DelayExpression.NumericConstant(locationStack1, str2, TypeCode.Double);
					return;
				}
				case 27:
				{
					this.yyval.delay = DelayExpression.Expression(Expression.Constant(true, typeof(bool)));
					return;
				}
				case 28:
				{
					this.yyval.delay = DelayExpression.Expression(Expression.Constant(false, typeof(bool)));
					return;
				}
				case 29:
				{
					string str3 = this.value_stack.array[this.value_stack.top - 1].@value;
					this.yyval.delay = DelayExpression.Expression(Expression.Constant(str3));
					return;
				}
				case 30:
				{
					this.yyval.delay = DelayExpression.Expression(Expression.Constant(null));
					return;
				}
				case 31:
				{
					string str4 = this.value_stack.array[this.value_stack.top - 4].@value;
					List<Expression> expressions1 = this.value_stack.array[this.value_stack.top - 2].expressions;
					this.yyval.expression = ExpressionBuilder.MakeFunction(this.messageParameter, str4, expressions1);
					return;
				}
				case 32:
				{
					this.yyval.expressions = new List<Expression>();
					return;
				}
				case 33:
				{
					DelayExpression valueStack8 = this.value_stack.array[this.value_stack.top - 1].delay;
					List<Expression> expressions2 = new List<Expression>()
					{
						ExpressionBuilder.ConvertToObject(valueStack8.GetExpression())
					};
					this.yyval.expressions = expressions2;
					return;
				}
				case 34:
				{
					List<Expression> expressions3 = this.value_stack.array[this.value_stack.top - 3].expressions;
					DelayExpression delayExpression8 = this.value_stack.array[this.value_stack.top - 1].delay;
					expressions3.Add(ExpressionBuilder.ConvertToObject(delayExpression8.GetExpression()));
					this.yyval.expressions = expressions3;
					return;
				}
				case 35:
				{
					string str5 = this.value_stack.array[this.value_stack.top - 1].@value;
					this.requiredParameters.Add(str5);
					this.yyval.expression = ExpressionBuilder.GetParameter(this.parametersParameter, str5);
					return;
				}
				case 36:
				{
					QualifiedPropertyName qualifiedPropertyName1 = this.value_stack.array[this.value_stack.top - 1].qname;
					this.yyval.expression = new GetPropertyExpression(this.messageParameter, qualifiedPropertyName1);
					return;
				}
				case 37:
				{
					string str6 = this.value_stack.array[this.value_stack.top - 1].@value;
					this.yyval.qname = new QualifiedPropertyName(PropertyScope.User, str6);
					return;
				}
				case 38:
				{
					string str7 = this.value_stack.array[this.value_stack.top - 3].@value;
					string str8 = this.value_stack.array[this.value_stack.top - 1].@value;
					if (string.Equals(str7, "sys", StringComparison.OrdinalIgnoreCase))
					{
						this.yyval.qname = new QualifiedPropertyName(PropertyScope.System, str8);
						return;
					}
					if (string.Equals(str7, "user", StringComparison.OrdinalIgnoreCase))
					{
						this.yyval.qname = new QualifiedPropertyName(PropertyScope.User, str8);
						return;
					}
					this.scanner.yyerror(SRClient.FilterScopeNotSupported(str7), new object[0]);
					return;
				}
				case 39:
				{
					this.yyval.@value = this.value_stack.array[this.value_stack.top - 1].@value;
					return;
				}
				case 40:
				{
					this.yyval.@value = this.value_stack.array[this.value_stack.top - 1].@value;
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
			this.errToken = 62;
			this.eofToken = 63;
			this.states = new State[59];
			base.AddState(0, new State(new int[] { 71, 9, 72, 56 }, new int[] { -10, 1, -11, 3, -3, 58, -4, 5, -5, 8, -6, 55 }));
			int[] numArray = new int[] { 63, 2 };
			base.AddState(1, new State(numArray));
			base.AddState(2, new State(-1));
			base.AddState(3, new State(new int[] { 71, 9, 72, 56, 63, -2 }, new int[] { -3, 4, -4, 5, -5, 8, -6, 55 }));
			base.AddState(4, new State(-4));
			int[] numArray1 = new int[] { 59, 7, 71, -8, 72, -8, 63, -8 };
			int[] numArray2 = new int[] { -16, 6 };
			base.AddState(5, new State(numArray1, numArray2));
			base.AddState(6, new State(-5));
			base.AddState(7, new State(-9));
			base.AddState(8, new State(-6));
			base.AddState(9, new State(new int[] { 64, 45, 65, 46 }, new int[] { -2, 10, -1, 42 }));
			int[] numArray3 = new int[] { 61, 11 };
			base.AddState(10, new State(numArray3));
			base.AddState(11, new State(new int[] { 67, 24, 68, 25, 69, 26, 88, 27, 89, 28, 70, 29, 90, 30, 64, 32, 66, 39, 65, 46, 43, 47, 45, 49, 40, 51 }, new int[] { -13, 12, -14, 23, -7, 31, -9, 38, -8, 40, -2, 41, -1, 42 }));
			base.AddState(12, new State(new int[] { 43, 13, 45, 15, 42, 17, 47, 19, 37, 21, 59, -10, 71, -10, 72, -10, 63, -10 }));
			base.AddState(13, new State(new int[] { 67, 24, 68, 25, 69, 26, 88, 27, 89, 28, 70, 29, 90, 30, 64, 32, 66, 39, 65, 46, 43, 47, 45, 49, 40, 51 }, new int[] { -13, 14, -14, 23, -7, 31, -9, 38, -8, 40, -2, 41, -1, 42 }));
			base.AddState(14, new State(new int[] { 43, -16, 45, -16, 42, 17, 47, 19, 37, 21, 59, -16, 71, -16, 72, -16, 63, -16, 41, -16, 44, -16 }));
			base.AddState(15, new State(new int[] { 67, 24, 68, 25, 69, 26, 88, 27, 89, 28, 70, 29, 90, 30, 64, 32, 66, 39, 65, 46, 43, 47, 45, 49, 40, 51 }, new int[] { -13, 16, -14, 23, -7, 31, -9, 38, -8, 40, -2, 41, -1, 42 }));
			base.AddState(16, new State(new int[] { 43, -17, 45, -17, 42, 17, 47, 19, 37, 21, 59, -17, 71, -17, 72, -17, 63, -17, 41, -17, 44, -17 }));
			base.AddState(17, new State(new int[] { 67, 24, 68, 25, 69, 26, 88, 27, 89, 28, 70, 29, 90, 30, 64, 32, 66, 39, 65, 46, 43, 47, 45, 49, 40, 51 }, new int[] { -13, 18, -14, 23, -7, 31, -9, 38, -8, 40, -2, 41, -1, 42 }));
			base.AddState(18, new State(-18));
			base.AddState(19, new State(new int[] { 67, 24, 68, 25, 69, 26, 88, 27, 89, 28, 70, 29, 90, 30, 64, 32, 66, 39, 65, 46, 43, 47, 45, 49, 40, 51 }, new int[] { -13, 20, -14, 23, -7, 31, -9, 38, -8, 40, -2, 41, -1, 42 }));
			base.AddState(20, new State(-19));
			base.AddState(21, new State(new int[] { 67, 24, 68, 25, 69, 26, 88, 27, 89, 28, 70, 29, 90, 30, 64, 32, 66, 39, 65, 46, 43, 47, 45, 49, 40, 51 }, new int[] { -13, 22, -14, 23, -7, 31, -9, 38, -8, 40, -2, 41, -1, 42 }));
			base.AddState(22, new State(-20));
			base.AddState(23, new State(-12));
			base.AddState(24, new State(-24));
			base.AddState(25, new State(-25));
			base.AddState(26, new State(-26));
			base.AddState(27, new State(-27));
			base.AddState(28, new State(-28));
			base.AddState(29, new State(-29));
			base.AddState(30, new State(-30));
			base.AddState(31, new State(-13));
			base.AddState(32, new State(new int[] { 40, 33, 46, -39, 43, -39, 45, -39, 42, -39, 47, -39, 37, -39, 59, -39, 71, -39, 72, -39, 63, -39, 41, -39, 44, -39 }));
			base.AddState(33, new State(new int[] { 67, 24, 68, 25, 69, 26, 88, 27, 89, 28, 70, 29, 90, 30, 64, 32, 66, 39, 65, 46, 43, 47, 45, 49, 40, 51, 41, -32, 44, -32 }, new int[] { -12, 34, -13, 54, -14, 23, -7, 31, -9, 38, -8, 40, -2, 41, -1, 42 }));
			base.AddState(34, new State(new int[] { 41, 35, 44, 36 }));
			base.AddState(35, new State(-31));
			base.AddState(36, new State(new int[] { 67, 24, 68, 25, 69, 26, 88, 27, 89, 28, 70, 29, 90, 30, 64, 32, 66, 39, 65, 46, 43, 47, 45, 49, 40, 51 }, new int[] { -13, 37, -14, 23, -7, 31, -9, 38, -8, 40, -2, 41, -1, 42 }));
			base.AddState(37, new State(new int[] { 43, 13, 45, 15, 42, 17, 47, 19, 37, 21, 41, -34, 44, -34 }));
			base.AddState(38, new State(-14));
			base.AddState(39, new State(-35));
			base.AddState(40, new State(-15));
			base.AddState(41, new State(-36));
			base.AddState(42, new State(new int[] { 46, 43, 61, -37, 43, -37, 45, -37, 42, -37, 47, -37, 37, -37, 59, -37, 71, -37, 72, -37, 63, -37, 41, -37, 44, -37 }));
			int[] numArray4 = new int[] { 64, 45, 65, 46 };
			int[] numArray5 = new int[] { -1, 44 };
			base.AddState(43, new State(numArray4, numArray5));
			base.AddState(44, new State(-38));
			base.AddState(45, new State(-39));
			base.AddState(46, new State(-40));
			base.AddState(47, new State(new int[] { 67, 24, 68, 25, 69, 26, 88, 27, 89, 28, 70, 29, 90, 30, 64, 32, 66, 39, 65, 46, 43, 47, 45, 49, 40, 51 }, new int[] { -13, 48, -14, 23, -7, 31, -9, 38, -8, 40, -2, 41, -1, 42 }));
			base.AddState(48, new State(-21));
			base.AddState(49, new State(new int[] { 67, 24, 68, 25, 69, 26, 88, 27, 89, 28, 70, 29, 90, 30, 64, 32, 66, 39, 65, 46, 43, 47, 45, 49, 40, 51 }, new int[] { -13, 50, -14, 23, -7, 31, -9, 38, -8, 40, -2, 41, -1, 42 }));
			base.AddState(50, new State(-22));
			base.AddState(51, new State(new int[] { 67, 24, 68, 25, 69, 26, 88, 27, 89, 28, 70, 29, 90, 30, 64, 32, 66, 39, 65, 46, 43, 47, 45, 49, 40, 51 }, new int[] { -13, 52, -14, 23, -7, 31, -9, 38, -8, 40, -2, 41, -1, 42 }));
			base.AddState(52, new State(new int[] { 41, 53, 43, 13, 45, 15, 42, 17, 47, 19, 37, 21 }));
			base.AddState(53, new State(-23));
			base.AddState(54, new State(new int[] { 43, 13, 45, 15, 42, 17, 47, 19, 37, 21, 41, -33, 44, -33 }));
			base.AddState(55, new State(-7));
			base.AddState(56, new State(new int[] { 64, 45, 65, 46 }, new int[] { -2, 57, -1, 42 }));
			base.AddState(57, new State(-11));
			base.AddState(58, new State(-3));
			this.rules = new Rule[41];
			Rule[] rule = this.rules;
			int[] numArray6 = new int[] { -10, 63 };
			rule[1] = new Rule(-15, numArray6);
			Rule[] ruleArray = this.rules;
			int[] numArray7 = new int[] { -11 };
			ruleArray[2] = new Rule(-10, numArray7);
			Rule[] rule1 = this.rules;
			int[] numArray8 = new int[] { -3 };
			rule1[3] = new Rule(-11, numArray8);
			Rule[] ruleArray1 = this.rules;
			int[] numArray9 = new int[] { -11, -3 };
			ruleArray1[4] = new Rule(-11, numArray9);
			Rule[] rule2 = this.rules;
			int[] numArray10 = new int[] { -4, -16 };
			rule2[5] = new Rule(-3, numArray10);
			Rule[] ruleArray2 = this.rules;
			int[] numArray11 = new int[] { -5 };
			ruleArray2[6] = new Rule(-4, numArray11);
			Rule[] rule3 = this.rules;
			int[] numArray12 = new int[] { -6 };
			rule3[7] = new Rule(-4, numArray12);
			this.rules[8] = new Rule(-16, new int[0]);
			Rule[] ruleArray3 = this.rules;
			int[] numArray13 = new int[] { 59 };
			ruleArray3[9] = new Rule(-16, numArray13);
			this.rules[10] = new Rule(-5, new int[] { 71, -2, 61, -13 });
			Rule[] rule4 = this.rules;
			int[] numArray14 = new int[] { 72, -2 };
			rule4[11] = new Rule(-6, numArray14);
			Rule[] ruleArray4 = this.rules;
			int[] numArray15 = new int[] { -14 };
			ruleArray4[12] = new Rule(-13, numArray15);
			Rule[] rule5 = this.rules;
			int[] numArray16 = new int[] { -7 };
			rule5[13] = new Rule(-13, numArray16);
			Rule[] ruleArray5 = this.rules;
			int[] numArray17 = new int[] { -9 };
			ruleArray5[14] = new Rule(-13, numArray17);
			Rule[] rule6 = this.rules;
			int[] numArray18 = new int[] { -8 };
			rule6[15] = new Rule(-13, numArray18);
			this.rules[16] = new Rule(-13, new int[] { -13, 43, -13 });
			this.rules[17] = new Rule(-13, new int[] { -13, 45, -13 });
			this.rules[18] = new Rule(-13, new int[] { -13, 42, -13 });
			this.rules[19] = new Rule(-13, new int[] { -13, 47, -13 });
			this.rules[20] = new Rule(-13, new int[] { -13, 37, -13 });
			Rule[] ruleArray6 = this.rules;
			int[] numArray19 = new int[] { 43, -13 };
			ruleArray6[21] = new Rule(-13, numArray19);
			Rule[] rule7 = this.rules;
			int[] numArray20 = new int[] { 45, -13 };
			rule7[22] = new Rule(-13, numArray20);
			this.rules[23] = new Rule(-13, new int[] { 40, -13, 41 });
			Rule[] ruleArray7 = this.rules;
			int[] numArray21 = new int[] { 67 };
			ruleArray7[24] = new Rule(-14, numArray21);
			Rule[] rule8 = this.rules;
			int[] numArray22 = new int[] { 68 };
			rule8[25] = new Rule(-14, numArray22);
			Rule[] ruleArray8 = this.rules;
			int[] numArray23 = new int[] { 69 };
			ruleArray8[26] = new Rule(-14, numArray23);
			Rule[] rule9 = this.rules;
			int[] numArray24 = new int[] { 88 };
			rule9[27] = new Rule(-14, numArray24);
			Rule[] ruleArray9 = this.rules;
			int[] numArray25 = new int[] { 89 };
			ruleArray9[28] = new Rule(-14, numArray25);
			Rule[] rule10 = this.rules;
			int[] numArray26 = new int[] { 70 };
			rule10[29] = new Rule(-14, numArray26);
			Rule[] ruleArray10 = this.rules;
			int[] numArray27 = new int[] { 90 };
			ruleArray10[30] = new Rule(-14, numArray27);
			this.rules[31] = new Rule(-7, new int[] { 64, 40, -12, 41 });
			this.rules[32] = new Rule(-12, new int[0]);
			Rule[] rule11 = this.rules;
			int[] numArray28 = new int[] { -13 };
			rule11[33] = new Rule(-12, numArray28);
			this.rules[34] = new Rule(-12, new int[] { -12, 44, -13 });
			Rule[] ruleArray11 = this.rules;
			int[] numArray29 = new int[] { 66 };
			ruleArray11[35] = new Rule(-9, numArray29);
			Rule[] rule12 = this.rules;
			int[] numArray30 = new int[] { -2 };
			rule12[36] = new Rule(-8, numArray30);
			Rule[] ruleArray12 = this.rules;
			int[] numArray31 = new int[] { -1 };
			ruleArray12[37] = new Rule(-2, numArray31);
			this.rules[38] = new Rule(-2, new int[] { -1, 46, -1 });
			Rule[] rule13 = this.rules;
			int[] numArray32 = new int[] { 64 };
			rule13[39] = new Rule(-1, numArray32);
			Rule[] ruleArray13 = this.rules;
			int[] numArray33 = new int[] { 65 };
			ruleArray13[40] = new Rule(-1, numArray33);
			string[] strArrays = new string[] { "", "property_name_part", "property", "statement", "action", "set_action", "remove_action", "function", "get_property", "get_parameter", "production", "statements", "argument_list", "expression", "literal", "$accept", "optional_seperator" };
			this.nonTerminals = strArrays;
		}

		public static SqlActionParserOutput Parse(string sqlExpression)
		{
			SqlActionParser sqlActionParser = SqlActionParser.ParseAndValidate(sqlExpression);
			return new SqlActionParserOutput(sqlActionParser.ExpressionTree, sqlActionParser.requiredParameters);
		}

		private static SqlActionParser ParseAndValidate(string sqlExpression)
		{
			SqlActionParser sqlActionParser = new SqlActionParser()
			{
				scanner = new Scanner(sqlExpression)
			};
			sqlActionParser.Parse();
			if (sqlActionParser.expressionList.Count > 32)
			{
				throw new RuleActionException(SRClient.FilterActionTooManyStatements(sqlActionParser.expressionList.Count, 32));
			}
			return sqlActionParser;
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
			SqlActionParser.ParseAndValidate(sqlExpression);
		}
	}
}
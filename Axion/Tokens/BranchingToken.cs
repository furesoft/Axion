﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axion.Tokens
{
	internal class BranchingToken : Token
	{
		internal readonly List<OperationToken>                          Conditions;
		internal readonly Dictionary<List<OperationToken>, List<Token>> ElseIfs;
		internal readonly List<Token>                                   ElseTokens;
		internal readonly List<Token>                                   ThenTokens;

		internal BranchingToken(List<OperationToken> conditions, List<Token> thenTokens)
		{
			Conditions = conditions;
			ThenTokens = thenTokens;
			ElseIfs    = new Dictionary<List<OperationToken>, List<Token>>();
			ElseTokens = new List<Token>();
		}

		public override string ToString(int tabLevel)
		{
			var tabs = "";
			for (var i = 0; i < tabLevel; i++)
			{
				tabs += "  ";
			}

			var str = new StringBuilder();
			str.AppendLine($"{tabs}(If,");
			str.AppendLine($"{tabs}  (Conditions,");
			for (var i = 0; i < Conditions.Count; i++)
			{
				if (i == Conditions.Count - 1)
				{
					str.AppendLine($"{Conditions[i].ToString(tabLevel + 2)}");
					break;
				}

				str.AppendLine($"{Conditions[i].ToString(tabLevel + 2)},");
			}

			str.AppendLine($"{tabs}  ),");
			str.AppendLine($"{tabs}  (Then,");
			for (var i = 0; i < ThenTokens.Count; i++)
			{
				if (i == ThenTokens.Count - 1)
				{
					str.AppendLine($"{ThenTokens[i].ToString(tabLevel + 2)}");
					break;
				}

				str.AppendLine($"{ThenTokens[i].ToString(tabLevel + 2)},");
			}

			str.AppendLine($"{tabs}  ),");

			for (var i = 0; i < ElseIfs.Count; i++)
			{
				str.AppendLine($"{tabs}  (ElseIf,");
				str.AppendLine($"{tabs}    (Conditions,");
				for (var I = 0; I < Conditions.Count; I++)
				{
					if (I == Conditions.Count - 1)
					{
						str.AppendLine($"{Conditions[I].ToString(tabLevel + 3)}");
						break;
					}

					str.AppendLine($"{Conditions[I].ToString(tabLevel + 3)},");
				}

				str.AppendLine($"{tabs}    ),");
				str.AppendLine($"{tabs}    (Then,");
				List<Token> actions = ElseIfs.ElementAt(i).Value;
				for (var I = 0; I < actions.Count; I++)
				{
					if (I == actions.Count - 1)
					{
						str.AppendLine($"{actions[I].ToString(tabLevel + 3)}");
						break;
					}

					str.AppendLine($"{actions[I].ToString(tabLevel + 3)},");
				}

				str.AppendLine($"{tabs}    ),");
				str.AppendLine($"{tabs}  ),");
			}

			str.AppendLine($"{tabs}  (Else,");
			for (var i = 0; i < ElseTokens.Count; i++)
			{
				if (i == ElseTokens.Count - 1)
				{
					str.AppendLine($"{ElseTokens[i].ToString(tabLevel + 2)}");
					break;
				}

				str.AppendLine($"{ElseTokens[i].ToString(tabLevel + 2)},");
			}

			str.AppendLine($"{tabs}  )");
			str.Append($"{tabs})");
			return str.ToString();
		}
	}
}
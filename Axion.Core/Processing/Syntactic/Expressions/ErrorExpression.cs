using Axion.Core.Processing.CodeGen;
using Axion.Core.Processing.Lexical.Tokens;

namespace Axion.Core.Processing.Syntactic.Expressions {
    public class ErrorExpression : Expression {
        public readonly Token ErrorToken;

        public ErrorExpression(SyntaxTreeNode parent) : base(parent) {
            ErrorToken = Token;
        }

        public override void ToAxionCode(CodeBuilder c) {
            c.Write(ErrorToken);
        }

        public override void ToCSharpCode(CodeBuilder c) {
            c.Write(ErrorToken);
        }
    }
}
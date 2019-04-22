using Axion.Core.Processing.CodeGen;
using Axion.Core.Processing.Lexical.Tokens;
using Axion.Core.Specification;

namespace Axion.Core.Processing.Syntactic.Expressions {
    public class UnaryOperationExpression : Expression {
        public readonly OperatorToken Operator;
        private         Expression    val;

        public Expression Value {
            get => val;
            set => SetNode(ref val, value);
        }

        public UnaryOperationExpression(
            SyntaxTreeNode parent,
            OperatorToken  op,
            Expression     expression
        ) : base(parent) {
            Operator = op;
            Value    = expression;

            MarkPosition(op, expression);
        }

        public UnaryOperationExpression(
            OperatorToken op,
            Expression    expression
        ) {
            Operator = op;
            Value    = expression;

            MarkPosition(op, expression);
        }

        public UnaryOperationExpression(
            SyntaxTreeNode parent,
            TokenType      opType,
            Expression     expression
        ) : base(parent) {
            Operator = new OperatorToken(opType);
            Value    = expression;

            MarkPosition(Value);
        }

        public UnaryOperationExpression(
            TokenType  opType,
            Expression expression
        ) {
            Operator = new OperatorToken(opType);
            Value    = expression;

            MarkPosition(Value);
        }

        public override void ToAxionCode(CodeBuilder c) {
            if (Operator.Properties.InputSide == InputSide.Left) {
                c.Write(Value, Operator.Value);
            }

            c.Write(Operator.Value, Value);
        }

        public override void ToCSharpCode(CodeBuilder c) {
            if (Operator.Properties.InputSide == InputSide.Left) {
                c.Write(Value, Operator.Value);
            }

            c.Write(Operator.Value, Value);
        }
    }
}
using Newtonsoft.Json;

namespace Axion.Core.Processing.Syntax.Tree.Expressions {
    public class ParenthesisExpression : Expression {
        private Expression expression;

        [JsonProperty]
        internal Expression Expression {
            get => expression;
            set {
                value.Parent = this;
                expression   = value;
            }
        }

        internal ParenthesisExpression(Expression expression) {
            Expression = expression;
            MarkPosition(expression);
        }

        public override string ToString() {
            return ToAxionCode();
        }

        private string ToAxionCode() {
            return "(" + Expression + ")";
        }
    }
}
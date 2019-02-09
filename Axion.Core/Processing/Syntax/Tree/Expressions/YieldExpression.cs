using Newtonsoft.Json;

namespace Axion.Core.Processing.Syntax.Tree.Expressions {
    public class YieldExpression : Expression {
        private Expression expression;

        internal YieldExpression(Expression expression, bool isYieldFrom, Position start, Position end) : base(
            start,
            end
        ) {
            Expression  = expression;
            IsYieldFrom = isYieldFrom;
        }

        [JsonProperty]
        internal Expression Expression {
            get => expression;
            set {
                value.Parent = this;
                expression   = value;
            }
        }

        internal bool IsYieldFrom { get; }

        public override string ToString() {
            return ToAxionCode();
        }

        private string ToAxionCode() {
            return "yield " + Expression;
        }
    }
}
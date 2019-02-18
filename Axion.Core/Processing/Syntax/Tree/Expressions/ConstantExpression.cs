using Axion.Core.Processing.Lexical.Tokens;
using Axion.Core.Specification;
using Newtonsoft.Json;

namespace Axion.Core.Processing.Syntax.Tree.Expressions {
    public class ConstantExpression : Expression {
        [JsonProperty]
        internal Token Value { get; }

        internal override string CannotAssignReason => Spec.ERR_InvalidAssignmentTarget;

        internal ConstantExpression(Token value) {
            Value = value;
            MarkPosition(value);
        }

        internal ConstantExpression(Token value, Position start, Position end) : base(start, end) {
            Value = value;
        }

        public override string ToString() {
            return ToAxionCode();
        }

        private string ToAxionCode() {
            return Value.ToString();
        }
    }
}
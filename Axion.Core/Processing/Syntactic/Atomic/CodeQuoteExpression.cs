using System;
using Axion.Core.Processing.CodeGen;
using Axion.Core.Processing.Syntactic.TypeNames;
using static Axion.Core.Specification.TokenType;

namespace Axion.Core.Processing.Syntactic.Atomic {
    /// <summary>
    ///     <c>
    ///         code_quote_expr:
    ///             '{{' expr '}}';
    ///     </c>
    /// </summary>
    public class CodeQuoteExpression : Expression {
        private Expression val;

        public Expression Value {
            get => val;
            set => SetNode(ref val, value);
        }

        public override TypeName ValueType => Value.ValueType;

        /// <summary>
        ///     Expression is constructed from tokens stream
        ///     that belongs to <see cref="parent"/>'s AST.
        /// </summary>
        internal CodeQuoteExpression(Expression parent) {
            Construct(parent, () => {
                Eat(DoubleOpenBrace);
                Value = ParseAny();
                Eat(DoubleCloseBrace);
            });
        }

        internal override void ToAxionCode(CodeBuilder c) {
            c.Write("{{", Value, "}}");
        }

        internal override void ToCSharpCode(CodeBuilder c) {
            throw new NotImplementedException();
        }
    }
}
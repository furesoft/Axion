using Axion.Core.Processing.CodeGen;
using Axion.Core.Processing.Syntactic.Expressions;
using Axion.Core.Specification;

namespace Axion.Core.Processing.Syntactic.Statements {
    /// <summary>
    ///     <c>
    ///         for_in_stmt:
    ///             'for' list_primary 'in' list_test block
    ///     </c>
    /// </summary>
    public class ForInStatement : LoopStatement {
        private Expression item;

        public Expression Item {
            get => item;
            set => SetNode(ref item, value);
        }

        private Expression iterable;

        public Expression Iterable {
            get => iterable;
            set => SetNode(ref iterable, value);
        }

        #region Constructors

        /// <summary>
        ///     Constructs new <see cref="ForInStatement"/> from tokens.
        /// </summary>
        internal ForInStatement(SyntaxTreeNode parent) : base(parent) {
            MarkStart(TokenType.KeywordFor);

            Item = Expression.ParseExpression(
                parent,
                Expression.ParsePrimaryExpr,
                typeof(SimpleNameExpression)
            );
            if (MaybeEat(TokenType.OpIn)) {
                Iterable = Expression.ParseExpression(
                    parent,
                    Expression.ParseTestExpr
                );
                Block = new BlockStatement(this, BlockType.Loop);
                if (MaybeEat(TokenType.KeywordElse)) {
                    NoBreakBlock = new BlockStatement(this);
                }
            }

            MarkEnd(Token);
        }

        /// <summary>
        ///     Constructs plain <see cref="ForInStatement"/> without position in source.
        /// </summary>
        public ForInStatement(
            Expression     item,
            Expression     iterable,
            BlockStatement block,
            BlockStatement noBreakBlock
        ) : base(block, noBreakBlock) {
            Item     = item;
            Iterable = iterable;
        }

        #endregion

        #region Code converters

        public override void ToAxionCode(CodeBuilder c) {
            c.Write(
                "for ",
                Item,
                " in ",
                Iterable,
                " ",
                Block
            );
            if (NoBreakBlock != null) {
                c.Write(" nobreak ", NoBreakBlock);
            }
        }

        public override void ToCSharpCode(CodeBuilder c) {
            c.Write(
                "foreach (",
                Item,
                " in ",
                Iterable,
                ") ",
                Block
            );
            if (NoBreakBlock != null) {
                Unit.ReportError("C# doesn't support 'nobreak' block", NoBreakBlock);
            }
        }

        #endregion
    }
}
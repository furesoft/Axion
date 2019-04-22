using System;
using System.Linq;
using Axion.Core.Processing.Lexical.Tokens;
using Axion.Core.Processing.Syntactic.Expressions.Binary;
using Axion.Core.Processing.Syntactic.Expressions.Multiple;
using Axion.Core.Processing.Syntactic.Expressions.TypeNames;
using Axion.Core.Specification;
using static Axion.Core.Specification.TokenType;

namespace Axion.Core.Processing.Syntactic.Expressions {
    public abstract class Expression : SyntaxTreeNode {
        protected Expression(SyntaxTreeNode parent) : base(parent) { }
        protected Expression() { }

        /// <summary>
        ///     <c>
        ///         primary
        ///             : ID
        ///             | await_expr
        ///             | yield_expr
        ///             | new_expr
        ///             | parenthesis_expr
        ///             | list_expr
        ///             | hash_collection
        ///             | CONSTANT
        ///     </c>
        /// </summary>
        internal static Expression ParsePrimaryExpr(SyntaxTreeNode parent) {
            Expression value;
            switch (parent.Peek.Type) {
                case Identifier: {
                    value = NameExpression.ParseName(parent);
                    break;
                }

                case KeywordAwait: {
                    value = new AwaitExpression(parent);
                    break;
                }

                case KeywordYield: {
                    value = new YieldExpression(parent);
                    break;
                }

                case KeywordNew: {
                    value = new TypeInitializerExpression(parent);
                    break;
                }

                case OpenParenthesis: {
                    value = ParseExpression(parent, expectedTypes: Spec.PrimaryExprs);
                    break;
                }

                case OpenBracket: {
                    value = new ListInitializerExpression(parent);
                    break;
                }

                case OpenBrace: {
                    value = new HashCollectionExpression(parent);
                    break;
                }

                default: {
                    parent.Move();
                    if (Spec.Literals.Contains(parent.Token.Type)) {
                        // TODO add pre-concatenation of literals
                        value = new ConstantExpression(parent);
                    }
                    else {
                        parent.Unit.ReportError(Spec.ERR_PrimaryExpected, parent.Token);
                        value = new ErrorExpression(parent);
                    }

                    break;
                }
            }

            return value;
        }

        /// <summary>
        ///     <c>
        ///         extended:
        ///             (pipeline | { member | call_expr | index_expr })
        ///             ['++' | '--']
        ///         pipeline:
        ///             primary {'|>' primary }
        ///     </c>
        /// </summary>
        internal static Expression ParseExtendedExpr(SyntaxTreeNode parent) {
            Expression value = ParsePrimaryExpr(parent);
            if (parent.MaybeEat(RightPipeline)) {
                do {
                    value = new FunctionCallExpression(
                        parent,
                        ParseExtendedExprInternal(parent, ParsePrimaryExpr(parent)),
                        new CallArgument(parent, value)
                    );
                } while (parent.MaybeEat(RightPipeline));

                return value;
            }

            return ParseExtendedExprInternal(parent, value);
        }

        private static Expression ParseExtendedExprInternal(
            SyntaxTreeNode parent,
            Expression     value
        ) {
            while (true) {
                if (parent.Peek.Is(Dot)) {
                    value = new MemberAccessExpression(parent, value);
                }
                else if (parent.Peek.Is(OpenParenthesis)) {
                    value = new FunctionCallExpression(parent, value, true);
                }
                else if (parent.Peek.Is(OpenBracket)) {
                    value = new IndexerExpression(parent, value);
                }
                else {
                    break;
                }
            }

            if (parent.MaybeEat(OpIncrement, OpDecrement)) {
                var op = (OperatorToken) value.Token;
                op.Properties.InputSide = InputSide.Right;
                value                   = new UnaryOperationExpression(parent, op, value);
            }

            return value;
        }

        /// <summary>
        ///     <c>
        ///         unary_left:
        ///             (UNARY_LEFT unary_left) | trailer
        ///     </c>
        /// </summary>
        internal static Expression ParseUnaryLeftExpr(SyntaxTreeNode parent) {
            if (parent.MaybeEat(Spec.UnaryLeftOperators)) {
                var op = (OperatorToken) parent.Token;
                op.Properties.InputSide = InputSide.Left;
                return new UnaryOperationExpression(parent, op, ParseUnaryLeftExpr(parent));
            }

            return ParseExtendedExpr(parent);
        }

        /// <summary>
        ///     <c>
        ///         priority_expr:
        ///             factor OPERATOR priority_expr
        ///     </c>
        /// </summary>
        internal static Expression ParseOperation(SyntaxTreeNode parent, int precedence = 0) {
            Expression expr = ParseUnaryLeftExpr(parent);
            while (parent.Peek is OperatorToken op && op.Properties.Precedence >= precedence) {
                parent.Move();
                expr = new BinaryOperationExpression(
                    parent,
                    expr,
                    op,
                    ParseOperation(parent, op.Properties.Precedence + 1)
                );
            }

            return expr;
        }

        /// <summary>
        ///     <c>
        ///         test:
        ///             priority_expr [cond_expr]
        ///             | lambda_def
        ///     </c>
        /// </summary>
        internal static Expression ParseTestExpr(SyntaxTreeNode parent) {
            Expression expr = ParseOperation(parent);
            if (parent.Peek.Is(KeywordIf, KeywordUnless)
                && parent.Token.Type != Newline) {
                expr = new ConditionalExpression(parent, expr);
            }

            return expr;
        }

        /// <summary>
        ///     <c>
        ///         expr:
        ///             test | (['let'] assignable [':' type] ['=' assign_value])
        ///         assign_value:
        ///             yield_expr | test_list
        ///     </c>
        /// </summary>
        internal static Expression ParseSingleExpr(SyntaxTreeNode parent) {
            bool isImmutable = parent.MaybeEat(KeywordLet);

            Expression expr = ParseExpression(parent, ParseTestExpr);

            // ['let'] name '=' expr
            // name is undefined
            if (expr is BinaryOperationExpression bin
                && bin.Left is SimpleNameExpression name
                && bin.Operator.Is(OpAssign)
                && !bin.ParentBlock.HasVariable(name)) {
                return new VariableDefinitionExpression(parent, bin.Left, null, bin.Right) {
                    IsImmutable = isImmutable
                };
            }

            if (!parent.Peek.Is(Colon)) {
                return expr;
            }

            if (!Spec.VariableLeftExprs.Contains(expr.GetType())) {
                parent.Unit.ReportError(Spec.ERR_InvalidAssignmentTarget, expr);
            }

            TypeName   type  = null;
            Expression value = null;
            if (parent.MaybeEat(Colon)) {
                type = TypeName.ParseTypeName(parent);
            }

            if (parent.MaybeEat(OpAssign)) {
                value = ParseExpression(parent, expectedTypes: Spec.TestExprs);
            }

            return new VariableDefinitionExpression(parent, expr, type, value) {
                IsImmutable = isImmutable
            };
        }

        /// <summary>
        ///     <c>
        ///         ['('] %expr {',' %expr} [')']
        ///     </c>
        ///     Helper for parsing multiple comma-separated
        ///     expressions with optional parenthesis
        ///     (e.g. tuples)
        /// </summary>
        internal static Expression ParseExpression(
            SyntaxTreeNode                   parent,
            Func<SyntaxTreeNode, Expression> parserFunc = null,
            params Type[]                    expectedTypes
        ) {
            if (expectedTypes.Length == 0 || parserFunc == ParseSingleExpr) {
                expectedTypes = Spec.AllExprs;
            }

            parserFunc??=ParseSingleExpr;
            bool  parens = parent.MaybeEat(OpenParenthesis);
            Token start  = parent.Token;
            // empty tuple
            if (parent.MaybeEat(CloseParenthesis)) {
                return new TupleExpression(parent, start, parent.Token);
            }

            var list = new NodeList<Expression>(parent) {
                parserFunc(parent)
            };

            // tuple
            if (parent.MaybeEat(Comma)) {
                bool trailingComma;
                do {
                    list.Add(parserFunc(parent));
                    trailingComma = parent.MaybeEat(Comma);
                } while (trailingComma);
            }
            // generator | comprehension
            else if (parent.Peek.Is(KeywordFor)) {
                list[0] = new ForComprehension(parent, list[0]);
                if (parens) {
                    list[0] = new GeneratorExpression(parent, (ForComprehension) list[0]);
                }
            }
            // parenthesized
            else {
                if (parens
                    && !(list[0] is ParenthesizedExpression)
                    && !(list[0] is TupleExpression)) {
                    list[0] = new ParenthesizedExpression(start, list[0]);
                }
            }

            if (parens) {
                parent.Eat(CloseParenthesis);
            }

            if (expectedTypes.Length > 0) {
                for (var i = 0; i < list.Count; i++) {
                    Type itemType = list[i].GetType();
                    if (!expectedTypes.Contains(itemType)) {
                        if (expectedTypes.Length > 1) {
                            parent.Unit.ReportError(
                                "Expected "
                                + "'"
                                + parserFunc.Method.Name
                                + "'"
                                + ", got "
                                + Utilities.GetExprFriendlyName(itemType),
                                list[i]
                            );
                        }
                        else {
                            parent.Unit.ReportError(
                                "Expected "
                                + Utilities.GetExprFriendlyName(expectedTypes[0])
                                + ", got "
                                + Utilities.GetExprFriendlyName(itemType),
                                list[i]
                            );
                        }
                    }
                }
            }

            return MaybeTuple(parent, list);
        }

        internal static Expression MaybeTuple(
            SyntaxTreeNode       parent,
            NodeList<Expression> expressions
        ) {
            if (expressions.Count == 1) {
                return expressions[0];
            }

            return new TupleExpression(parent, expressions);
        }
    }
}
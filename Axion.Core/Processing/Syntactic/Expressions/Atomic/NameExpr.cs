using System.Collections.Generic;
using Axion.Core.Processing.Lexical.Tokens;
using Axion.Core.Processing.Syntactic.Expressions.Common;
using Axion.Core.Processing.Syntactic.Expressions.TypeNames;
using Axion.Core.Processing.Traversal;
using static Axion.Core.Processing.Lexical.Tokens.TokenType;

namespace Axion.Core.Processing.Syntactic.Expressions.Atomic {
    /// <summary>
    ///     <c>
    ///         name-expr:
    ///             ID {'.' ID};
    ///     </c>
    /// </summary>
    public class NameExpr : AtomExpr {
        private NodeList<Token> tokens = null!;

        public NodeList<Token> Tokens {
            get => tokens;
            set => tokens = Bind(value);
        }

        public List<Token> Qualifiers => Tokens.OfType(Identifier);
        public bool        IsSimple   => Qualifiers.Count == 1;

        [NoPathTraversing]
        public override TypeName ValueType =>
            ((Expr) GetParent<ScopeExpr>().GetDefByName(this))?.ValueType;

        public NameExpr(string name) {
            Tokens = new NodeList<Token>(this);
            if (name.Contains('.')) {
                string[] qs = name.Split('.');
                for (var i = 0; i < qs.Length; i++) {
                    string q = qs[i];
                    Tokens.Add(new Token(Source, Identifier, q));
                    if (i != qs.Length - 1) {
                        Tokens.Add(new Token(Source, OpDot, "."));
                    }
                }
            }
            else {
                Tokens.Add(new Token(Source, Identifier, name));
            }
        }

        public NameExpr(Node parent) : base(parent) { }

        public NameExpr Parse(bool simple = false) {
            Tokens ??= new NodeList<Token>(this);
            Stream.Eat(Identifier);
            Tokens.Add(Stream.Token);
            if (simple) {
                return this;
            }
            while (Stream.MaybeEat(OpDot)) {
                Tokens.Add(Stream.Token);
                if (Stream.Eat(Identifier) != null) {
                    Tokens.Add(Stream.Token);
                }
            }
            return this;
        }
    }
}

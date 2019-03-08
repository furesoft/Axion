﻿using System;
using System.Collections.Generic;
using Axion.Core.Specification;
using Newtonsoft.Json;

namespace Axion.Core.Processing.Lexical.Tokens {
    /// <summary>
    ///     Represents a 'string' literal.
    /// </summary>
    public class StringToken : Token {
        public StringToken(
            Position             startPosition,
            StringLiteralOptions options,
            string               value,
            string               rawValue       = null,
            List<Interpolation>  interpolations = null,
            bool                 isUnclosed     = false
        ) : base(TokenType.String, startPosition, value) {
            if (rawValue == null) {
                rawValue = value;
            }
            Options        = options;
            Interpolations = interpolations ?? new List<Interpolation>();
            RawValue       = rawValue;
            IsUnclosed     = isUnclosed;
            int endLine = Span.StartPosition.Line;
            int endCol  = Span.EndPosition.Column;

            // addition of quotes length:
            // compute count of quotes on token end line:
            // Multiline:  6 quotes on 1  line,  (3 * 2);
            //             3 quotes on 2+ lines, (3 * 1);
            // One-line:   2 quotes on 1  line,  (1 * 2);
            //             1 quote  on 2+ lines, (1 * 1).
            string[] lines = RawValue.Split(Spec.EndOfLines, StringSplitOptions.None);
            if (lines.Length == 1) {
                endCol =  Span.StartPosition.Column;
                endCol += lines[lines.Length - 1].Length;
                // if 1 line: add 1 for each prefix letter
                if (Options.IsRaw) {
                    endCol++;
                }
                if (Options.IsFormatted) {
                    endCol++;
                }

                if (IsUnclosed) {
                    endCol += Options.QuotesCount;
                }
                else {
                    endCol += Options.QuotesCount * 2;
                }
            }
            else if (lines.Length > 1) {
                endCol = lines[lines.Length - 1].Length;
                if (!IsUnclosed) {
                    endCol += Options.QuotesCount;
                }
                endLine += lines.Length - 1;
            }
            Span = new Span(Span.StartPosition, (endLine, endCol));
        }

        [JsonProperty]
        public string RawValue { get; }

        [JsonProperty]
        public bool IsUnclosed { get; }

        [JsonProperty]
        internal StringLiteralOptions Options { get; }

        [JsonProperty]
        internal List<Interpolation> Interpolations { get; }

        public override string ToAxionCode() {
            var result = "";
            if (Options.IsFormatted) {
                result += "f";
            }
            if (Options.IsRaw) {
                result += "r";
            }
            int quotesCount = Options.QuotesCount;
            result += new string(Options.Quote, quotesCount) + RawValue;
            if (!IsUnclosed) {
                result += new string(Options.Quote, quotesCount);
            }
            else {
                result += Options.TrailingQuotes;
            }
            return result + Whitespaces;
        }
    }

    public sealed class Interpolation {
        internal readonly List<Token> Tokens = new List<Token>();
        internal readonly int         StartIndex;
        internal          int         EndIndex;

        public Interpolation(int startIndex) {
            StartIndex = startIndex;
        }

        internal int Length => EndIndex - StartIndex;
    }

    public class StringLiteralOptions {
        internal bool IsMultiline;

        internal readonly bool IsLineEndsNormalized;

        public StringLiteralOptions(
            char quote                = '"',
            bool isMultiline          = false,
            bool isLineEndsNormalized = false,
            bool isFormatted          = false,
            bool isRaw                = false
        ) {
            Quote                = quote;
            IsMultiline          = isMultiline;
            IsLineEndsNormalized = isLineEndsNormalized;
            IsFormatted          = isFormatted;
            IsRaw                = isRaw;
        }

        public bool HasPrefixes => IsFormatted || IsRaw;

        public int QuotesCount => IsMultiline ? 3 : 1;

        internal char Quote { get; set; }

        internal string TrailingQuotes { get; set; }

        internal bool IsFormatted { get; private set; }

        internal bool IsRaw { get; private set; }

        public void AppendPrefix(char c, out bool valid, out bool duplicated) {
            duplicated = false;
            valid      = true;
            if (c == 'f' || c == 'F') {
                if (IsFormatted) {
                    duplicated = true;
                }
                IsFormatted = true;
            }
            else if (c == 'r' || c == 'R') {
                if (IsRaw) {
                    duplicated = true;
                }
                IsRaw = true;
            }
            else {
                valid = false;
            }
        }

        public string GetPrefixes() {
            var result = "";
            if (IsFormatted) {
                result += "f";
            }
            if (IsRaw) {
                result += "r";
            }
            return result;
        }
    }
}
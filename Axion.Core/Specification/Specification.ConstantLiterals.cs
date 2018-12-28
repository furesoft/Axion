using System;
using System.Collections.Generic;
using System.Numerics;
using Axion.Core.Processing.Lexical.Tokens;

namespace Axion.Core.Specification {
    public partial class Spec {
        internal static readonly TokenType[] ConstantValueTypes = {
            TokenType.String,
            TokenType.Character,
            TokenType.Number,
            TokenType.KeywordTrue,
            TokenType.KeywordFalse,
            TokenType.KeywordNull
        };

        #region Language string and character literals

        internal const char CharLiteralQuote = '`';

        /// <summary>
        ///     Contains all valid quotes for string literals.
        /// </summary>
        internal static readonly char[] StringQuotes = { '"', '\'' };

        /// <summary>
        ///     Contains all valid escape sequences in string and character literals.
        /// </summary>
        internal static readonly Dictionary<char, string> EscapeSequences = new Dictionary<char, string> {
            { '0', "\u0000" },
            { 'a', "\u0007" },
            { 'b', "\u0008" },
            { 'f', "\u000c" },
            { 'n', "\u000a" },
            { 'r', "\u000d" },
            { 't', "\u0009" },
            { 'v', "\u000b" },
            { '\\', "\\" },
            { '\'', "\'" },
            { '\"', "\"" },
            { '`', "\\`" }
        };

        #endregion

        #region Language number literals

        internal static readonly string[] NumberTypes = {
            nameof(Int64),
            nameof(Double),
            nameof(BigInteger),
            nameof(Complex)
        };

        internal static readonly char[] NumberPostfixes = {
            'f', 'F', // float
            'l', 'L', // long
            'i', 'I', // int (followed by bit rate)
            'u', 'U', // unsigned
            'j', 'J'  // complex
        };

        internal const int MinNumberBitRate = 8;
        internal const int MaxNumberBitRate = 64;

        internal static readonly int[] IntegerBitRates = {
            MinNumberBitRate, 16, 32, MaxNumberBitRate
        };

        internal static readonly int[] FloatBitRates = {
            32, 64, 128
        };

        #endregion
    }
}
// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     A parsed rule expression, used to decide which individual findings satisfy the rule rather than
///     only whether the rule matched at all.
///     Mirrors the evaluation model of the underlying engine: operators fold strictly left to right with
///     no precedence, and grouping is expressed only with parentheses.
/// </summary>
internal sealed class RuleExpression
{
    /// <summary>
    ///     Parsing recurses once per level of parenthesis nesting, as does the underlying engine's own
    ///     evaluator, so nesting is capped well above anything an author would write by hand. Deeply
    ///     nested input would otherwise exhaust the stack, which cannot be caught and takes the process
    ///     down with it.
    /// </summary>
    internal const int MaxNestingDepth = 64;

    private static readonly string[] Operators = { "AND", "OR", "XOR", "NAND", "NOR" };

    private readonly Node _root;

    private RuleExpression(Node root)
    {
        _root = root;
    }

    /// <summary>
    ///     Counts the deepest parenthesis nesting in an expression without parsing it, so callers can
    ///     reject one before it reaches a recursive evaluator.
    /// </summary>
    internal static int MaxNestingOf(string expression)
    {
        var depth = 0;
        var deepest = 0;

        foreach (var character in expression)
            if (character == '(')
            {
                depth++;
                if (depth > deepest)
                {
                    deepest = depth;
                }
            }
            else if (character == ')')
            {
                depth--;
            }

        return deepest;
    }

    /// <summary>
    ///     Parses an expression, returning null if it is malformed or nested past <see cref="MaxNestingDepth" />.
    /// </summary>
    public static RuleExpression? TryParse(string expression)
    {
        var tokens = Tokenize(expression);
        if (tokens.Count == 0)
        {
            return null;
        }

        var index = 0;
        var root = ParseSequence(tokens, ref index, 0);
        return root is null || index != tokens.Count ? null : new RuleExpression(root);
    }

    public bool Evaluate(Func<string, bool> labelValue)
    {
        return _root.Evaluate(labelValue);
    }

    private static List<Token> Tokenize(string expression)
    {
        List<Token> tokens = new();

        foreach (var raw in expression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var remaining = raw;

            while (remaining.StartsWith("(", StringComparison.Ordinal))
            {
                tokens.Add(new Token(TokenKind.OpenParen, "("));
                remaining = remaining.Substring(1);
            }

            var closers = 0;
            while (remaining.EndsWith(")", StringComparison.Ordinal))
            {
                closers++;
                remaining = remaining.Substring(0, remaining.Length - 1);
            }

            if (remaining.Length > 0)
            {
                if (remaining.Equals("NOT", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Add(new Token(TokenKind.Not, remaining));
                }
                else if (Operators.Contains(remaining, StringComparer.OrdinalIgnoreCase))
                {
                    tokens.Add(new Token(TokenKind.Operator, remaining.ToUpperInvariant()));
                }
                else
                {
                    tokens.Add(new Token(TokenKind.Label, remaining));
                }
            }

            for (var i = 0; i < closers; i++) tokens.Add(new Token(TokenKind.CloseParen, ")"));
        }

        return tokens;
    }

    private static Node? ParseSequence(List<Token> tokens, ref int index, int depth)
    {
        var left = ParseTerm(tokens, ref index, depth);
        if (left is null)
        {
            return null;
        }

        while (index < tokens.Count && tokens[index].Kind == TokenKind.Operator)
        {
            var op = tokens[index].Text;
            index++;

            var right = ParseTerm(tokens, ref index, depth);
            if (right is null)
            {
                return null;
            }

            left = new BinaryNode(op, left, right);
        }

        return left;
    }

    private static Node? ParseTerm(List<Token> tokens, ref int index, int depth)
    {
        var negate = false;
        while (index < tokens.Count && tokens[index].Kind == TokenKind.Not)
        {
            negate = !negate;
            index++;
        }

        if (index >= tokens.Count)
        {
            return null;
        }

        Node? inner;

        if (tokens[index].Kind == TokenKind.OpenParen)
        {
            if (depth >= MaxNestingDepth)
            {
                return null;
            }

            index++;
            inner = ParseSequence(tokens, ref index, depth + 1);
            if (inner is null || index >= tokens.Count || tokens[index].Kind != TokenKind.CloseParen)
            {
                return null;
            }

            index++;
        }
        else if (tokens[index].Kind == TokenKind.Label)
        {
            inner = new LabelNode(tokens[index].Text);
            index++;
        }
        else
        {
            return null;
        }

        return negate ? new NotNode(inner) : inner;
    }

    private enum TokenKind
    {
        OpenParen,
        CloseParen,
        Not,
        Operator,
        Label
    }

    private readonly struct Token
    {
        public Token(TokenKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }

        public TokenKind Kind { get; }
        public string Text { get; }
    }

    private abstract class Node
    {
        public abstract bool Evaluate(Func<string, bool> labelValue);
    }

    private sealed class LabelNode : Node
    {
        private readonly string _label;

        public LabelNode(string label)
        {
            _label = label;
        }

        public override bool Evaluate(Func<string, bool> labelValue)
        {
            return labelValue(_label);
        }
    }

    private sealed class NotNode : Node
    {
        private readonly Node _inner;

        public NotNode(Node inner)
        {
            _inner = inner;
        }

        public override bool Evaluate(Func<string, bool> labelValue)
        {
            return !_inner.Evaluate(labelValue);
        }
    }

    private sealed class BinaryNode : Node
    {
        private readonly Node _left;
        private readonly string _operator;
        private readonly Node _right;

        public BinaryNode(string op, Node left, Node right)
        {
            _operator = op;
            _left = left;
            _right = right;
        }

        public override bool Evaluate(Func<string, bool> labelValue)
        {
            var left = _left.Evaluate(labelValue);
            var right = _right.Evaluate(labelValue);

            return _operator switch
            {
                "AND" => left && right,
                "OR" => left || right,
                "XOR" => left ^ right,
                "NAND" => !(left && right),
                "NOR" => !(left || right),
                _ => false
            };
        }
    }
}

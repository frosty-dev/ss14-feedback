﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Syntax;

/// <summary>
/// A "run" of commands. Not a true expression.
/// </summary>
public sealed class CommandRun
{
    public readonly List<(ParsedCommand, Vector2i)> Commands;
    private readonly string _originalExpr;

    public static bool TryParse(
            bool blockMode,
            bool doAutocomplete,
            ForwardParser parser,
            Type? pipedType,
            Type? targetOutput,
            bool once,
            [NotNullWhen(true)] out CommandRun? expr,
            out ValueTask<(CompletionResult?, IConError?)>? autocomplete,
            out IConError? error
        )
    {
        autocomplete = null;
        error = null;
        var cmds = new List<(ParsedCommand, Vector2i)>();
        var start = parser.Index;
        var noCommand = false;
        parser.Consume(char.IsWhiteSpace);

        while ((!once || cmds.Count < 1) && ParsedCommand.TryParse(doAutocomplete, parser, pipedType, out var cmd, out error, out noCommand, out autocomplete, targetOutput))
        {
            var end = parser.Index;
            pipedType = cmd.ReturnType;
            cmds.Add((cmd, (start, end)));
            parser.Consume(char.IsWhiteSpace);
            start = parser.Index;

            if (blockMode && parser.PeekChar() == '}')
                break;
        }

        if (error is OutOfInputError && noCommand)
            error = null;

        if (error is not null and not OutOfInputError || error is OutOfInputError && !noCommand || cmds.Count == 0)
        {
            expr = null;
            return false;
        }

        if (!(cmds.Last().Item1.ReturnType?.IsAssignableTo(targetOutput) ?? false) && targetOutput is not null)
        {
            error = new ExpressionOfWrongType(targetOutput, cmds.Last().Item1.ReturnType!, once);
            expr = null;
            return false;
        }

        expr = new CommandRun(cmds, parser.Input);
        return true;
    }

    public object? Invoke(object? input, IInvocationContext ctx, bool reportErrors = true)
    {
        var ret = input;
        foreach (var (cmd, span) in Commands)
        {
            ret = cmd.Invoke(ret, ctx);
            if (ctx.GetErrors().Any())
            {
                // Got an error, we need to report it and break out.
                foreach (var err in ctx.GetErrors())
                {
                    err.Contextualize(_originalExpr, span);
                    ctx.WriteLine(err.Describe());
                }

                return null;
            }
        }

        return ret;
    }


    private CommandRun(List<(ParsedCommand, Vector2i)> commands, string originalExpr)
    {
        Commands = commands;
        _originalExpr = originalExpr;
    }
}

public sealed class CommandRun<TIn, TOut>
{
    internal readonly CommandRun InnerCommandRun;

    public static bool TryParse(bool blockMode, bool doAutoComplete, ForwardParser parser, bool once,
        [NotNullWhen(true)] out CommandRun<TIn, TOut>? expr,
        out ValueTask<(CompletionResult?, IConError?)>? autocomplete, out IConError? error)
    {
        if (!CommandRun.TryParse(blockMode, doAutoComplete, parser, typeof(TIn), typeof(TOut), once, out var innerExpr, out autocomplete, out error))
        {
            expr = null;
            return false;
        }

        expr = new CommandRun<TIn, TOut>(innerExpr);
        return true;
    }

    public TOut? Invoke(object? input, IInvocationContext ctx)
    {
        var res = InnerCommandRun.Invoke(input, ctx);
        if (res is null)
            return default;
        return (TOut?) res;
    }

    private CommandRun(CommandRun commandRun)
    {
        InnerCommandRun = commandRun;
    }
}

public sealed class CommandRun<TRes>
{
    internal readonly CommandRun _innerCommandRun;

    public static bool TryParse(bool blockMode, bool doAutoComplete, ForwardParser parser, Type? pipedType, bool once,
        [NotNullWhen(true)] out CommandRun<TRes>? expr, out ValueTask<(CompletionResult?, IConError?)>? completion,
        out IConError? error)
    {
        if (!CommandRun.TryParse(blockMode, doAutoComplete, parser, pipedType, typeof(TRes), once, out var innerExpr, out completion, out error))
        {
            expr = null;
            return false;
        }

        expr = new CommandRun<TRes>(innerExpr);
        return true;
    }

    public TRes? Invoke(object? input, IInvocationContext ctx)
    {
        var res = _innerCommandRun.Invoke(input, ctx);
        if (res is null)
            return default;
        return (TRes?) res;
    }

    private CommandRun(CommandRun commandRun)
    {
        _innerCommandRun = commandRun;
    }
}

public record struct ExpressionOfWrongType(Type Expected, Type Got, bool Once) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = FormattedMessage.FromMarkup(
            $"Expected an expression of type {Expected.PrettyName()}, but got {Got.PrettyName()}");

        if (Once)
        {
            msg.PushNewline();
            msg.AddText("Note: A single command is expected here, if you were trying to chain commands please surround the run with { } to form a block.");
        }

        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

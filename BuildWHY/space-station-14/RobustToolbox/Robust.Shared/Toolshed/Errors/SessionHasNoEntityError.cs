﻿using System.Diagnostics;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Errors;

public record struct SessionHasNoEntityError(ICommonSession Session) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup($"The user {Session.Name} has no attached entity.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

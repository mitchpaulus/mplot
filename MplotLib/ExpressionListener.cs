namespace MplotLib;

public class ExpressionListener : MplotBaseListener
{
    private readonly List<string> _stringArgs;

    public ExpressionListener(List<string> stringArgs)
    {
        _stringArgs = stringArgs;
    }

    public readonly List<MplotToken> Tokens = new();
    // Check out awk -f to_listener.awk Mplot.g4

    public override void EnterIntExp(MplotParser.IntExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.INT, context.GetText()));
    }

    public override void EnterFloatExp(MplotParser.FloatExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.FLOAT, context.GetText()));
    }

    public override void EnterStringExp(MplotParser.StringExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.STRING, context.argString().ToMPlotString().GetString(_stringArgs)));
    }

    public override void EnterExpressionColExp(MplotParser.ExpressionColExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.EXPRESSIONCOL, context.GetText()));
    }

    public override void EnterExpressionColXExp(MplotParser.ExpressionColXExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.EXPRESSIONCOLX, context.GetText()));
    }

    public override void EnterExpressionColYExp(MplotParser.ExpressionColYExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.EXPRESSIONCOLY, context.GetText()));
    }

    public override void EnterPlusExp(MplotParser.PlusExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.PLUS, context.GetText()));
    }

    public override void EnterMinusExp(MplotParser.MinusExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.MINUS, context.GetText()));
    }

    public override void EnterAsteriskExp(MplotParser.AsteriskExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.ASTERISK, context.GetText()));
    }

    public override void EnterForwardSlashExp(MplotParser.ForwardSlashExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.FORWARDSLASH, context.GetText()));
    }

    public override void EnterLessThanExp(MplotParser.LessThanExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.LESSTHAN, context.GetText()));
    }

    public override void EnterGreaterThanExp(MplotParser.GreaterThanExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.GREATERTHAN, context.GetText()));
    }

    public override void EnterLessOrEqualExp(MplotParser.LessOrEqualExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.LESSOREQUAL, context.GetText()));
    }

    public override void EnterGreaterOrEqualExp(MplotParser.GreaterOrEqualExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.GREATEROREQUAL, context.GetText()));
    }

    public override void EnterEqualExp(MplotParser.EqualExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.EQUAL, context.GetText()));
    }

    public override void EnterNotEqualExp(MplotParser.NotEqualExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.NOTEQUAL, context.GetText()));
    }

    public override void EnterAndExp(MplotParser.AndExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.AND, context.GetText()));
    }

    public override void EnterOrExp(MplotParser.OrExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.OR, context.GetText()));
    }

    public override void EnterExpressionIdentifierExp(MplotParser.ExpressionIdentifierExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.EXPRESSIONIDENTIFIER, context.GetText()));
    }

    public override void EnterCycleExp(MplotParser.CycleExpContext context)
    {
        Tokens.Add(new(ExpressionTokenType.CYCLE, context.GetText()));
    }
}

public class MplotToken
{
    public ExpressionTokenType TokenType;
    public string Contents;

    public MplotToken(ExpressionTokenType tokenType, string contents)
    {
        TokenType = tokenType;
        Contents = contents;
    }
}

public enum ExpressionTokenType
{
    INT,
    FLOAT,
    STRING,
    EXPRESSIONCOL,
    EXPRESSIONCOLX,
    EXPRESSIONCOLY,
    PLUS,
    MINUS,
    ASTERISK,
    FORWARDSLASH,
    LESSTHAN,
    GREATERTHAN,
    LESSOREQUAL,
    GREATEROREQUAL,
    EQUAL,
    NOTEQUAL,
    AND,
    OR,
    EXPRESSIONIDENTIFIER,
    CYCLE,
}

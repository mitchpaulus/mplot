using MplotLib;

namespace MplotGui.ViewModels;

public enum SeriesStyleOption
{
    Scatter,
    Line,
    Boxes,
    FStep
}

public enum MarkerOption
{
    Auto,
    None,
    Circle,
    FilledCircle,
    Square,
    FilledSquare,
    Plus,
    X,
    Line
}

public enum SeparatorOption
{
    Auto,
    Comma,
    Tab,
    Whitespace
}

public enum DashTypeOption
{
    Solid,
    Simple
}

public enum YAxisSideOption
{
    Left,
    Right
}

public enum DataSourceOption
{
    File,
    Inline
}

// Legacy SeriesViewModel removed in favour of independent array editors. Enums remain for reuse across the UI.

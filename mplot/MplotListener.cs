using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mplot;

internal class MplotListener : MplotBaseListener
{
    Stack<Color> ColorStack = new Stack<Color>();

    public string XLabel { get; set; }
    public string YLabel { get; set; }

    public Stack<int> Ycols { get; set; }
    public Stack<int> Xcols { get; set; }

    public override void EnterXlabel(MplotParser.XlabelContext context)
    {
        XLabel = context.STRING().GetText();
    }
    public override void EnterYlabel(MplotParser.YlabelContext context)
    {
        YLabel = context.STRING().GetText();
    }

    public override void EnterYcol(MplotParser.YcolContext context) => Ycols.Push(int.Parse(context.INT().GetText()));
    public override void EnterXcol(MplotParser.XcolContext context) => Xcols.Push(int.Parse(context.INT().GetText()));

}
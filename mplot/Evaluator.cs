using System.Globalization;
using System.Text;
using Antlr4.Runtime.Atn;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using OneOf;

namespace mplot;

public class Evaluator
{
    public Evaluator()
    {
    }

    public static string EvaluateGnuplot(List<MplotToken> tokens) => EvaluateGnuplot(tokens, null, null);

    public static string EvaluateGnuplot(List<MplotToken> tokens, string? currentX, string? currentY)
    {
        Stack<string> expStack = new();

        foreach (var token in tokens)
        {
            if (token.TokenType == ExpressionTokenType.PLUS)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} + {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.MINUS)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} - {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.ASTERISK)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} * {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.FORWARDSLASH)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} / {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.STRING)
            {
                expStack.Push($"column(\"{token.Contents.RemoveQuotes()}\")");
            }
            else if (token.TokenType == ExpressionTokenType.EXPRESSIONCOL)
            {
                // This is '$' INT, example: $2. Passed as is to Gnuplot
                expStack.Push(token.Contents);
            }
            else if (token.TokenType == ExpressionTokenType.INT)
            {
                // The '.0' here is key.
                // Gnuplot will do integer division otherwise, and this is almost certainly unintended.
                expStack.Push(token.Contents + ".0");
            }
            else if (token.TokenType == ExpressionTokenType.FLOAT)
            {
                expStack.Push(token.Contents);
            }
            else if (token.TokenType == ExpressionTokenType.LESSTHAN)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} < {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.GREATERTHAN)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} > {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.LESSOREQUAL)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} <= {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.GREATEROREQUAL)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} >= {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.EQUAL)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} == {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.AND)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} && {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.OR)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} || {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.EXPRESSIONCOLX)
            {
                if (string.IsNullOrWhiteSpace(currentX)) throw new InvalidDataException($"Current x expression not set.");
                expStack.Push($"({currentX})");
            }
            else if (token.TokenType == ExpressionTokenType.EXPRESSIONCOLY)
            {
                if (string.IsNullOrWhiteSpace(currentY)) throw new InvalidDataException($"Current y expression not set.");
                expStack.Push($"({currentY})");
            }
            else
            {
                throw new NotImplementedException($"Token type {token.TokenType} not implemented yet");
            }
        }

        return expStack.Pop();
    }

    public static AwkEval EvaluateAwk(List<MplotToken> tokens)
    {
        Stack<string> expStack = new();

        bool needsHead = false;

        foreach (var token in tokens)
        {
            if (token.TokenType == ExpressionTokenType.PLUS)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} + {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.MINUS)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} - {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.ASTERISK)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} * {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.FORWARDSLASH)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} / {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.STRING)
            {
                expStack.Push($"$headers[\"{token.Contents.RemoveQuotes()}\"");
                needsHead = true;
            }
            else if (token.TokenType == ExpressionTokenType.EXPRESSIONCOL)
            {
                // This is '$' INT, example: $2. Passed as is to awk
                expStack.Push(token.Contents);
            }
            else if (token.TokenType == ExpressionTokenType.INT)
            {
                expStack.Push(token.Contents);
            }
            else if (token.TokenType == ExpressionTokenType.FLOAT)
            {
                expStack.Push(token.Contents);
            }
            else if (token.TokenType == ExpressionTokenType.LESSTHAN)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} < {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.GREATERTHAN)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} > {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.LESSOREQUAL)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} <= {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.GREATEROREQUAL)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} >= {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.EQUAL)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} == {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.AND)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} && {arg2})");
            }
            else if (token.TokenType == ExpressionTokenType.OR)
            {
                if (expStack.Count < 2) throw new ArgumentException();
                string arg2 = expStack.Pop();
                string arg1 = expStack.Pop();
                expStack.Push($"({arg1} || {arg2})");
            }
            else
            {
                throw new NotImplementedException($"Token type {token.TokenType} not implemented yet");
            }
        }

        return new (expStack.Pop(), needsHead);
    }


    public static ExpressionArray Evaluate(List<MplotToken> atoms, IDataSource source)
    {
        Stack<ExpressionArray> dataStack = new();

        foreach (var token in atoms)
        {
            if (token.TokenType == ExpressionTokenType.PLUS)
            {
                Operations.Add(dataStack);
            }
            else if (token.TokenType == ExpressionTokenType.MINUS)
            {
                Operations.Subtract(dataStack);
            }
            else if (token.TokenType == ExpressionTokenType.ASTERISK)
            {
                Operations.Multiply(dataStack);
            }
            else if (token.TokenType == ExpressionTokenType.FORWARDSLASH)
            {
                Operations.Divide(dataStack);
            }
            else if (token.TokenType == ExpressionTokenType.EXPRESSIONCOL)
            {
                var dataArray = source.Get(int.Parse(token.Contents[1..]));
                dataStack.Push(new StringArray(dataArray));
            }
            else if (token.TokenType == ExpressionTokenType.STRING)
            {
                var dataArray = source.Get(token.Contents.RemoveQuotes());
                dataStack.Push(new StringArray(dataArray));
            }
            else if (token.TokenType == ExpressionTokenType.LESSTHAN)
            {
                Operations.Comparison(dataStack, token.TokenType);
            }
            else if (token.TokenType == ExpressionTokenType.GREATERTHAN)
            {
                Operations.Comparison(dataStack, token.TokenType);
            }
            else if (token.TokenType == ExpressionTokenType.LESSOREQUAL)
            {
                Operations.Comparison(dataStack, token.TokenType);
            }
            else if (token.TokenType == ExpressionTokenType.GREATEROREQUAL)
            {
                Operations.Comparison(dataStack, token.TokenType);
            }
            else if (token.TokenType == ExpressionTokenType.AND)
            {

            }
            else
            {
                throw new NotImplementedException();
            }
        }

        return dataStack.Pop();
    }

    public static ExpressionArray Evaluate(List<Atom> atoms, IDataSource source)
    {
        Stack<ExpressionArray> dataStack = new();

        foreach (var atom in atoms)
        {
            if (atom.TryPickT0(out var op, out var colSpec))
            {
                if (op.Id == Operator.Add.Id)
                {
                    Operations.Add(dataStack);
                }
                else if (op.Id == Operator.Subtract.Id)
                {
                    Operations.Subtract(dataStack);
                }
                else if (op.Id == Operator.Multiply.Id)
                {
                    Operations.Multiply(dataStack);
                }
                else if (op.Id == Operator.Divide.Id)
                {
                    Operations.Divide(dataStack);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                List<string> array = colSpec.Match(
                    colNumSpec => source.Get(colNumSpec.OneBasedColumn),
                    nameColSpec => source.Get(nameColSpec.Name)
                );
                dataStack.Push(new StringArray(array));
            }
        }

        return dataStack.Pop();
    }
}

public class Atom : OneOfBase<Operator, ExpressionColSpec>
{
    public Atom(OneOf<Operator, ExpressionColSpec> input) : base(input)
    {
    }
}

public class ExpressionColSpec : OneOfBase<ColNumSpec, NameColSpec>
{
    public ExpressionColSpec(OneOf<ColNumSpec, NameColSpec> input) : base(input)
    {
    }
}

public class ColNumSpec
{
    public ColNumSpec(int oneBasedColumn)
    {
        OneBasedColumn = oneBasedColumn;
    }

    public int OneBasedColumn { get; }
}

public class NameColSpec
{
    public NameColSpec(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

public interface IOperator
{

}

public class Operator
{
    public int Id { get; }

    public static Operator LessThan = new(1);
    public static Operator GreaterThan = new(2);
    public static Operator Add = new(3);
    public static Operator Subtract = new(4);
    public static Operator Multiply = new(5);
    public static Operator Divide = new(6);

    public Operator(int id)
    {
        Id = id;
    }
}

public abstract class ExpressionArray
{
    public abstract int Length { get; }

    public abstract DoubleArray ToDoubleArray();
    public abstract List<string> ToStringList();
}



public class StringArray : ExpressionArray
{
    public override int Length => Array.Count;

    public List<string> Array { get; }

    public StringArray(List<string> array)
    {
        Array = array;
    }

    public override DoubleArray ToDoubleArray()
    {
        return new DoubleArray(Array.Select(double.Parse).ToList());
    }

    public override List<string> ToStringList() => Array;
}

public class DoubleArray : ExpressionArray
{
    public List<double> Array { get; }

    public override int Length => Array.Count;

    public double this[int i] => Array[i];

    public override DoubleArray ToDoubleArray()
    {
        return this;
    }

    public override List<string> ToStringList()
    {
        return Array.Select(d => d.ToString(CultureInfo.InvariantCulture)).ToList();
    }

    public DoubleArray(List<double> array)
    {
        Array = array;
    }
}

public class BoolArray : ExpressionArray
{
    private readonly List<bool> _array;

    public override int Length => _array.Count;
    public override DoubleArray ToDoubleArray()
    {
        throw new NotImplementedException();
    }

    public override List<string> ToStringList()
    {
        // Not sure what to do here
        return _array.Select(b => b ? "1" : "0").ToList();
    }

    public BoolArray(List<bool> array)
    {
        _array = array;
    }
}

public class IntArray : ExpressionArray
{
    private readonly List<int> _array;

    public override int Length => _array.Count;
    public override DoubleArray ToDoubleArray() => new(_array.Select(i => (double) i).ToList());
    public override List<string> ToStringList()
    {
        return _array.Select(i => i.ToString()).ToList();
    }

    public IntArray(List<int> array)
    {
        _array = array;
    }
}

public static class Operations
{
    public static bool Add(Stack<ExpressionArray> stack)
    {
        if (stack.Count < 2) return false;

        var array1 = stack.Pop();
        if (array1 is BoolArray) return false;

        var array2 = stack.Pop();
        if (array2 is BoolArray) return false;

        if (array1.Length != array2.Length) return false;

        var doubleArray1 = array1.ToDoubleArray();
        var doubleArray2 = array2.ToDoubleArray();

        var newArray = new List<double>();

        for (int i = 0; i < array1.Length; i++)
        {
            newArray.Add(doubleArray1[i] + doubleArray2[i]);
        }

        stack.Push(new DoubleArray(newArray));

        return true;
    }

    public static bool Subtract(Stack<ExpressionArray> stack)
    {
        if (stack.Count < 2) return false;

        var array1 = stack.Pop();
        if (array1 is BoolArray) return false;

        var array2 = stack.Pop();
        if (array2 is BoolArray) return false;

        if (array1.Length != array2.Length) return false;

        var doubleArray1 = array1.ToDoubleArray();
        var doubleArray2 = array2.ToDoubleArray();

        var newArray = new List<double>();

        for (int i = 0; i < array1.Length; i++)
        {
            newArray.Add(doubleArray1[i] - doubleArray2[i]);
        }

        stack.Push(new DoubleArray(newArray));

        return true;
    }

    public static bool Multiply(Stack<ExpressionArray> stack)
    {
        if (stack.Count < 2) return false;

        var array1 = stack.Pop();
        if (array1 is BoolArray) return false;

        var array2 = stack.Pop();
        if (array2 is BoolArray) return false;

        if (array1.Length != array2.Length) return false;

        var doubleArray1 = array1.ToDoubleArray();
        var doubleArray2 = array2.ToDoubleArray();

        var newArray = new List<double>();

        for (int i = 0; i < array1.Length; i++)
        {
            newArray.Add(doubleArray1[i] * doubleArray2[i]);
        }

        stack.Push(new DoubleArray(newArray));

        return true;
    }

    public static bool Divide(Stack<ExpressionArray> stack)
    {
        if (stack.Count < 2) return false;

        var array1 = stack.Pop();
        if (array1 is BoolArray) return false;

        var array2 = stack.Pop();
        if (array2 is BoolArray) return false;

        if (array1.Length != array2.Length) return false;

        var doubleArray1 = array1.ToDoubleArray();
        var doubleArray2 = array2.ToDoubleArray();

        var newArray = new List<double>();

        for (int i = 0; i < array1.Length; i++)
        {
            if (doubleArray2[i] == 0)
            {
                newArray.Add(0);
            }
            else
            {
                newArray.Add(doubleArray1[i] / doubleArray2[i]);
            }
        }

        stack.Push(new DoubleArray(newArray));

        return true;
    }

    public static bool Comparison(Stack<ExpressionArray> stack, ExpressionTokenType type)
    {
        if (stack.Count < 2) return false;

        var array1 = stack.Pop();
        if (array1 is BoolArray) return false;

        var array2 = stack.Pop();
        if (array2 is BoolArray) return false;

        if (array1.Length != array2.Length) return false;

        var doubleArray1 = array1.ToDoubleArray();
        var doubleArray2 = array2.ToDoubleArray();

        var newArray = new List<bool>();

        Func<double, double, bool> comparer = type switch
        {
            ExpressionTokenType.LESSTHAN => (a, b) => a < b,
            ExpressionTokenType.GREATERTHAN => (a, b) => a > b,
            ExpressionTokenType.LESSOREQUAL => (a, b) => a <= b,
            ExpressionTokenType.GREATEROREQUAL => (a, b) => a >= b,
            _ => throw new NotImplementedException()
        };

        for (int i = 0; i < array1.Length; i++)
        {
            newArray.Add(comparer(doubleArray1[i], doubleArray2[i]));
        }

        stack.Push(new BoolArray(newArray));

        return true;
    }
}

public class AwkEval
{
    public string AwkExpression;
    public bool NeedsHeaderRead;

    public AwkEval(string awkExpression, bool needsHeaderRead)
    {
        AwkExpression = awkExpression;
        NeedsHeaderRead = needsHeaderRead;
    }
}

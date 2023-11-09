using Antlr4.Runtime.Atn;
using OneOf;

namespace mplot;

public class Evaluator
{
    public Evaluator()
    {
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

    public BoolArray(List<bool> array)
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
}

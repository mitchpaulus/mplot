using Antlr4.Runtime.Atn;
using OneOf;

namespace mplot;

public class Evaluator
{

    public Evaluator(DataSource source, Separator2 sep)
    {
        
    }


    public List<string> Evaluate(List<Atom> atoms)
    {
        Stack<List<string>> dataStack = new();

        foreach (var atom in atoms)
        {
            if (atom.TryPickT0(out var op, out var colSpec))
            {
                
            }
            else
            {
                
            }
        }
        
        throw new NotImplementedException();
    }
}

public class Atom : OneOfBase<Operator, ExpressionColSpec>
{
    protected Atom(OneOf<Operator, ExpressionColSpec> input) : base(input)
    {
    }
}

public class ExpressionColSpec
{
}

public class Operator
{
    public int Id { get; }

    public static Operator LessThan = new(1);
    public static Operator GreaterThan = new(2);
    

    public Operator(int id)
    {
        Id = id;
    }
    
    
    
    
    
    
}
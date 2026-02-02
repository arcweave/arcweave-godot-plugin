namespace Arcweave.Interpreter
{
public class RuntimeException : System.Exception
{
    public RuntimeException(string message) : base(message)
    {
    }
    
    public RuntimeException(string message, System.Exception innerException) : base(message, innerException)
    {
    }
}

public class ParseException : System.Exception
{
    public ParseException(string message) : base(message)
    {
    }
    
    public ParseException(string message, System.Exception innerException) : base(message, innerException)
    {
    }
}
}


using System.Globalization;

namespace Arcweave.Interpreter
{
public class ArcscriptExpressionBase
{
    public NumberFormatInfo NumberFormat { get; private set; }
    public ArcscriptExpressionBase()
    {
        NumberFormat = new NumberFormatInfo
        {
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = ","
        };
    }
}
}


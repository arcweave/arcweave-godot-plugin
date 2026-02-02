#nullable enable
using System;
using System.Collections;

namespace Arcweave.Interpreter
{
    public class Expression : ArcscriptExpressionBase, IComparable
    {
        public object Value { get; private set; }
        public Expression(object value) { Value = value; }
        public Expression(object value, Type type) { Value = Convert.ChangeType(value, type); } 
        public Expression(Expression expression) { Value = expression.Value; }
        public void SetValue(object value) { Value = value; }
        public Type Type() { return Value.GetType(); }
        public static Expression operator +(Expression e) { return e; }
        public static Expression operator -(Expression e)
        {
            if (e.Value is int i)
            {
                return new Expression(-i);
            }
            else if (e.Value is double value)
            {
                return new Expression(-value);
            }
            return e;
        }
        public static Expression operator +(Expression first, Expression second)
        {
            if (first.Type() == typeof(string) || second.Type() == typeof(string))
            {
                return new Expression(first.ToString() + second.ToString());
            }
            var doubleValues = GetDoubleValues(first.Value, second.Value);
            if (!doubleValues.HasDouble)
            {
                var intValue = (int)(doubleValues.Value1 + doubleValues.Value2);
                return new Expression(intValue);
            }
            else
            {
                return new Expression(doubleValues.Value1 + doubleValues.Value2);
            }
        }

        public static Expression operator -(Expression first, Expression second)
        {
            if (first.Type() == typeof(string) || second.Type() == typeof(string))
            {
                throw new InvalidOperationException("Subtraction is not supported for string types.");
            }
            var doubleValues = GetDoubleValues(first.Value, second.Value);
            if (!doubleValues.HasDouble)
            {
                var intValue = (int) (doubleValues.Value1 - doubleValues.Value2);
                return new Expression(intValue);
            } else
            {
                return new Expression(doubleValues.Value1 - doubleValues.Value2);
            }
        }

        public static Expression operator *(Expression first, Expression second)
        {
            if (first.Type() == typeof(string) || second.Type() == typeof(string))
            {
                throw new InvalidOperationException("Multiplication is not supported for string types.");
            }
            var doubleValues = GetDoubleValues(first.Value, second.Value);
            if (!doubleValues.HasDouble)
            {
                var intValue = (int)(doubleValues.Value1 * doubleValues.Value2);
                return new Expression(intValue);
            }
            else
            {
                return new Expression(doubleValues.Value1 * doubleValues.Value2);
            }
        }

        public static Expression operator /(Expression first, Expression second)
        {
            if (first.Type() == typeof(string) || second.Type() == typeof(string))
            {
                throw new InvalidOperationException("Division is not supported for string types.");
            }
            var doubleValues = GetDoubleValues(first.Value, second.Value);
            var result = doubleValues.Value1 / doubleValues.Value2;
            if (double.IsInfinity(result))
            {
                throw new DivideByZeroException("Division by zero.");
            }
            return new Expression(result);
        }
        
        public static Expression operator %(Expression first, Expression second)
        {
            if (first.Type() == typeof(string) || second.Type() == typeof(string))
            {
                throw new InvalidOperationException("Modulo is not supported for string types.");
            }
            var doubleValues = GetDoubleValues(first.Value, second.Value);
            if (doubleValues.Value2 == 0)
            {
                throw new DivideByZeroException("Modulo by zero.");
            }
            if (!doubleValues.HasDouble)
            {
                var intValue = (int)(doubleValues.Value1 % doubleValues.Value2);
                return new Expression(intValue);
            }
            else
            {
                return new Expression(doubleValues.Value1 % doubleValues.Value2);
            }
        }

        public static bool operator ==(Expression first, Expression second)
        {
            System.Type type1 = first.Type();
            if (type1 == typeof(int) || type1 == typeof(double))
            {
                DoubleValues doubleValues = GetDoubleValues(first.Value, second.Value);
                return doubleValues.Value1 == doubleValues.Value2;
            }
            if (type1 == typeof(bool))
            {
                return (bool)first.Value == (bool)second.Value;
            }
            if (type1 == typeof(string))
            {
                return (string)first.Value == (string)second.Value;
            }
            return false;
        }

        public int CompareTo(object? other)
        {
            if (other == null || !(other is Expression))
            {
                throw new ArgumentException("Object is not an Expression");
            }
            Expression o = (Expression)other;
            DoubleValues fValues = GetDoubleValues(this.Value, o.Value);
            double result = fValues.Value1 - fValues.Value2;
            if (result < 0)
            {
                return -1;
            } else if (result > 0)
            {
                return 1;
            }
            return 0;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is Expression))
            {
                return false;
            }
            Expression e = (Expression)obj;

            System.Type type1 = Type();
            if (type1 == typeof(int) || type1 == typeof(double))
            {
                DoubleValues doubleValues = GetDoubleValues(Value, e.Value);
                return doubleValues.Value1 == doubleValues.Value2;
            }
            if (type1 == typeof(bool))
            {
                return (bool)Value == (bool)e.Value;
            }
            if (type1 == typeof(string))
            {
                return (string)Value == (string)e.Value;
            }
            return false;
        }

        public static bool operator !=(Expression first, Expression second)
        {
            System.Type type1 = first.Type();
            if (type1 == typeof(int) || type1 == typeof(double))
            {
                DoubleValues doubleValues = GetDoubleValues(first.Value, second.Value);
                return doubleValues.Value1 != doubleValues.Value2;
            }
            if (type1 == typeof(bool))
            {
                return (bool)first.Value != (bool)second.Value;
            }
            if (type1 == typeof(string))
            {
                return (string)first.Value != (string)second.Value;
            }
            return true;
        }
        public static bool operator ==(Expression first, bool second)
        {
            return GetBoolValue(first.Value) == second;
        }
        public static bool operator !=(Expression first, bool second)
        {
            return GetBoolValue(first.Value) != second;
        }
        public static bool operator ==(Expression first, int second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second);
            return doubleValues.Value1 == doubleValues.Value2;
        }
        public static bool operator !=(Expression first, int second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second);
            return doubleValues.Value1 != doubleValues.Value2;
        }
        public static bool operator ==(Expression first, double second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second);
            return doubleValues.Value1 == doubleValues.Value2;
        }
        public static bool operator !=(Expression first, double second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second);
            return doubleValues.Value1 != doubleValues.Value2;
        }
        public static bool operator ==(Expression first, string second)
        {
            return (string)first.Value == second;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
        public static bool operator !=(Expression first, string second)
        {
            return (string)first.Value == second;
        }

        public static bool operator >(Expression first, Expression second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second.Value);
            return doubleValues.Value1 > doubleValues.Value2;
        }
        public static bool operator <(Expression first, Expression second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second.Value);
            return doubleValues.Value1 < doubleValues.Value2;
        }
        public static bool operator >=(Expression first, Expression second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second.Value);
            return doubleValues.Value1 >= doubleValues.Value2;
        }
        public static bool operator <=(Expression first, Expression second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second.Value);
            return doubleValues.Value1 <= doubleValues.Value2;
        }
        public static bool operator >(Expression first, int second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second);
            return doubleValues.Value1 > doubleValues.Value2;
        }
        public static bool operator <(Expression first, int second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second);
            return doubleValues.Value1 < doubleValues.Value2;
        }
        public static bool operator >=(Expression first, int second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second);
            return doubleValues.Value1 >= doubleValues.Value2;
        }
        public static bool operator <=(Expression first, int second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second);
            return doubleValues.Value1 <= doubleValues.Value2;
        }
        public static bool operator >(Expression first, double second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second);
            return doubleValues.Value1 > doubleValues.Value2;
        }
        public static bool operator <(Expression first, double second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second);
            return doubleValues.Value1 < doubleValues.Value2;
        }
        public static bool operator >=(Expression first, double second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second);
            return doubleValues.Value1 >= doubleValues.Value2;
        }
        public static bool operator <=(Expression first, double second)
        {
            DoubleValues doubleValues = GetDoubleValues(first.Value, second);
            return doubleValues.Value1 <= doubleValues.Value2;
        }
        public static Expression operator !(Expression e)
        {
            return new Expression(!GetBoolValue(e.Value));
        }
        public static bool operator true(Expression e)
        {
            return GetBoolValue(e.Value);
        }
        public static bool operator false(Expression e)
        {
            return !GetBoolValue(e.Value);
        }
        public static Expression operator &(Expression first, Expression second)
        {
            if (GetBoolValue(first.Value) && GetBoolValue(second.Value))
            {
                return new Expression(true);
            }
            return new Expression(false);
        }

        public static Expression operator |(Expression first, Expression second)
        {
            if (GetBoolValue(first.Value) || GetBoolValue(second.Value))
            {
                return new Expression(true);
            }
            return new Expression(false);
        }

        public override string ToString()
        {
            if (Value.GetType() == typeof(bool))
            {
                if ((bool)Value)
                {
                    return "true";
                }
                return "false";
            }

            if (Value.GetType() == typeof(double))
            {
                return ((double)Value).ToString(NumberFormat);
            }
            return Value.ToString();
        }

        private struct DoubleValues
        {
            public double Value1;
            public double Value2;
            public bool HasDouble;

            public DoubleValues(double val1, double val2, bool hasDouble) {  Value1 = val1; Value2 = val2; HasDouble = hasDouble; }
        }

        private static DoubleValues GetDoubleValues(object first, object second) {
            bool hasDouble = false;
            int value1, value2;
            double flValue1 = 0, flValue2 = 0;
            if (first.GetType() == typeof(int))
            {
                value1 = (int)first;
                flValue1 = value1;
            } else if (first.GetType() == typeof(double))
            {
                hasDouble = true;
                flValue1 = (double)first;
            } else if (first.GetType() == typeof(bool))
            {
                flValue1 = (bool)first ? 1 : 0;
            }

            if (second.GetType() == typeof(int))
            {
                value2 = (int)second;
                flValue2 = value2;
            } else if (second.GetType() == typeof(double))
            {
                hasDouble = true;
                flValue2 = (double)second;
            } else if (second.GetType() == typeof(bool))
            {
                flValue2 = (bool)second ? 1 : 0;
            }

            return new DoubleValues(flValue1, flValue2, hasDouble);
        }

        internal static bool GetBoolValue(object value)
        {
            if (value.GetType() == typeof(bool)) { return (bool)value; }
            if (value.GetType() == typeof(string)) { return ((string)value).Length > 0; }
            if (value.GetType() == typeof(int)) { return (int)value > 0; }
            if (value.GetType() == typeof(double)) { return (double)value > 0; }
            return false;
        }
    }
}

using System;

namespace Arcweave.Interpreter
{
    public class Expression
    {
        public object Value { get; set; }
        public Expression() { Value = null; }
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
            else if (e.Value is float value)
            {
                return new Expression(-value);
            }
            return e;
        }
        public static Expression operator +(Expression first, Expression second)
        {
            if (first.Type() == typeof(string) || second.Type() == typeof(string))
            {
                return new Expression(first.Value.ToString() + second.Value.ToString());
            }
            var floatValues = GetFloatValues(first.Value, second.Value);
            if (!floatValues.HasFloat)
            {
                var intValue = (int)(floatValues.Value1 + floatValues.Value2);
                return new Expression(intValue);
            }
            else
            {
                return new Expression(floatValues.Value1 + floatValues.Value2);
            }
        }

        public static Expression operator -(Expression first, Expression second)
        {
            var floatValues = GetFloatValues(first.Value, second.Value);
            if (!floatValues.HasFloat)
            {
                var intValue = (int) (floatValues.Value1 - floatValues.Value2);
                return new Expression(intValue);
            } else
            {
                return new Expression(floatValues.Value1 - floatValues.Value2);
            }
        }

        public static Expression operator *(Expression first, Expression second)
        {
            var floatValues = GetFloatValues(first.Value, second.Value);
            if (!floatValues.HasFloat)
            {
                var intValue = (int)(floatValues.Value1 * floatValues.Value2);
                return new Expression(intValue);
            }
            else
            {
                return new Expression(floatValues.Value1 * floatValues.Value2);
            }
        }

        public static Expression operator /(Expression first, Expression second)
        {
            var floatValues = GetFloatValues(first.Value, second.Value);
            return new Expression(floatValues.Value1 / floatValues.Value2);
        }

        public static bool operator ==(Expression first, Expression second)
        {
            System.Type type1 = first.Type();
            if (type1 == typeof(int) || type1 == typeof(float))
            {
                FloatValues floatValues = GetFloatValues(first.Value, second.Value);
                return floatValues.Value1 == floatValues.Value2;
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

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Expression))
            {
                return false;
            }
            Expression e = (Expression)obj;

            System.Type type1 = Type();
            if (type1 == typeof(int) || type1 == typeof(float))
            {
                FloatValues floatValues = GetFloatValues(Value, e.Value);
                return floatValues.Value1 == floatValues.Value2;
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
            if (type1 == typeof(int) || type1 == typeof(float))
            {
                FloatValues floatValues = GetFloatValues(first.Value, second.Value);
                return floatValues.Value1 != floatValues.Value2;
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
            FloatValues floatValues = GetFloatValues(first.Value, second);
            return floatValues.Value1 == floatValues.Value2;
        }
        public static bool operator !=(Expression first, int second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second);
            return floatValues.Value1 != floatValues.Value2;
        }
        public static bool operator ==(Expression first, float second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second);
            return floatValues.Value1 == floatValues.Value2;
        }
        public static bool operator !=(Expression first, float second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second);
            return floatValues.Value1 != floatValues.Value2;
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
            FloatValues floatValues = GetFloatValues(first.Value, second.Value);
            return floatValues.Value1 > floatValues.Value2;
        }
        public static bool operator <(Expression first, Expression second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second.Value);
            return floatValues.Value1 < floatValues.Value2;
        }
        public static bool operator >=(Expression first, Expression second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second.Value);
            return floatValues.Value1 >= floatValues.Value2;
        }
        public static bool operator <=(Expression first, Expression second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second.Value);
            return floatValues.Value1 <= floatValues.Value2;
        }
        public static bool operator >(Expression first, int second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second);
            return floatValues.Value1 > floatValues.Value2;
        }
        public static bool operator <(Expression first, int second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second);
            return floatValues.Value1 < floatValues.Value2;
        }
        public static bool operator >=(Expression first, int second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second);
            return floatValues.Value1 >= floatValues.Value2;
        }
        public static bool operator <=(Expression first, int second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second);
            return floatValues.Value1 <= floatValues.Value2;
        }
        public static bool operator >(Expression first, float second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second);
            return floatValues.Value1 > floatValues.Value2;
        }
        public static bool operator <(Expression first, float second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second);
            return floatValues.Value1 < floatValues.Value2;
        }
        public static bool operator >=(Expression first, float second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second);
            return floatValues.Value1 >= floatValues.Value2;
        }
        public static bool operator <=(Expression first, float second)
        {
            FloatValues floatValues = GetFloatValues(first.Value, second);
            return floatValues.Value1 <= floatValues.Value2;
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
            return Value.ToString();
        }

        private struct FloatValues
        {
            public float Value1;
            public float Value2;
            public bool HasFloat;

            public FloatValues(float val1, float val2, bool hasFloat) {  Value1 = val1; Value2 = val2; HasFloat = hasFloat; }
        }

        private static FloatValues GetFloatValues(object first, object second) {
            bool hasFloat = false;
            int value1, value2;
            float flValue1 = 0, flValue2 = 0;
            if (first.GetType() == typeof(int))
            {
                value1 = (int)first;
                flValue1 = value1;
            } else if (first.GetType() == typeof(float))
            {
                hasFloat = true;
                flValue1 = (float)first;
            } else if (first.GetType() == typeof(bool))
            {
                flValue1 = (bool)first ? 1 : 0;
            }

            if (second.GetType() == typeof(int))
            {
                value2 = (int)second;
                flValue2 = value2;
            } else if (second.GetType() == typeof(float))
            {
                hasFloat = true;
                flValue2 = (float)second;
            } else if (second.GetType() == typeof(bool))
            {
                flValue2 = (bool)second ? 1 : 0;
            }

            return new FloatValues(flValue1, flValue2, hasFloat);
        }

        private static bool GetBoolValue(object value)
        {
            if (value.GetType() == typeof(bool)) { return (bool)value; }
            if (value.GetType() == typeof(string)) { return ((string)value).Length > 0; }
            if (value.GetType() == typeof(int)) { return (int)value > 0; }
            if (value.GetType() == typeof(float)) { return (float)value > 0; }
            return false;
        }
    }
}
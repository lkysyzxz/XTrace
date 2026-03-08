using System;

namespace ModelContextProtocol
{
    internal static class Throw
    {
        public static void IfNull(object arg, string paramName = null)
        {
            if (arg is null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void IfNullOrWhiteSpace(string arg, string paramName = null)
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", paramName);
            }
        }

        public static void IfNegative(int arg, string paramName = null)
        {
            if (arg < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, "Value must not be negative.");
            }
        }
    }
}

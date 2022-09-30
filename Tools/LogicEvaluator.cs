using FrogSharp.Common;

namespace FrogSharp.Tools
{
    public static class LogicEvaluator
    {
        public static bool ParseLogic(float a, float b, string logic)
        {
            switch (logic)
            {
                case Constants.Equals:
                    return a.Equals(b);
                case Constants.Greater:
                    return a > b;
                case Constants.Less:
                    return a < b;
                case Constants.GreaterOrEquals:
                    return a >= b;
                case Constants.LessOrEquals:
                    return a <= b;
            }

            return false;
        }
    }
}
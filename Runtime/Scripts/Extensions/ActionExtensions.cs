using System;


namespace KyLib.Extensions
{
    /// <summary>
    /// Actionägí£ÉÅÉ\ÉbÉh
    /// </summary>
    public static class ActionExtensions
    {
        public static void SafeInvoke(this Action action)
        {
            action?.Invoke();
        }

        public static void SafeInvoke<T>(this Action<T> action, T arg)
        {
            action?.Invoke(arg);
        }

        public static void SafeInvoke<T1, T2>(this Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            action?.Invoke(arg1, arg2);
        }
    }
}

using UnityEngine;


namespace KyLib.Extensions
{
    /// <summary>
    /// •¶š—ñŠg’£ƒƒ\ƒbƒh
    /// </summary>
    public static class StringExtensions
    {

        public static bool IsNullOrEmpty(this string str)
        {
            return str == null || str == "";
        }

    }
}

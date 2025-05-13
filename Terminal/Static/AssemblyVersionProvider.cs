using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Static
{
    public static class AssemblyVersionProvider
    {
        /// <summary>
        /// Возвращает версию сборки для указанного типа T.
        /// </summary>
        public static string GetVersion<T>()
        {
            try
            {
                var version = typeof(T).Assembly.GetName().Version;
                return version?.ToString() ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// Возвращает версию сборки для переданного типа.
        /// </summary>
        public static string GetVersion(Type type)
        {
            try
            {
                var version = type.Assembly.GetName().Version;
                return version?.ToString() ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}

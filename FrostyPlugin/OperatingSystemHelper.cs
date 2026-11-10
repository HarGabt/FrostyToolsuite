using System.Runtime.InteropServices;

namespace FrostyModManager
{
    public static class OperatingSystemHelper
    {
        private const bool forceTrue = false;
        private static bool? _value = null;

        public static bool IsWine(bool useForce = true)
        {
            if (forceTrue && useForce)
            {
                return true;
            }

            if (_value != null)
            {
                return _value ?? true;
            }

            try
            {
                var version = GetWineVersion();
                _value = true;

                return true;
            }
            catch { }

            _value = false;

            return false;
        }

        [DllImport("ntdll.dll", EntryPoint = "wine_get_version", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern string GetWineVersion();
    }
}
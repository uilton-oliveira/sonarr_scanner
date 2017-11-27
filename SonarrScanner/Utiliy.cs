using System;

namespace Sonarr_Scanner
{
    class Utiliy
    {
        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }
    }
}

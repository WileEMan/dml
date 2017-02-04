#if false       // Not used at the moment..
#if !AnyCPU

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace WileyBlack.Dml.Platforms
{
    public class NativeSupplement
    {
        public static string GetLastError()
        {
            StringBuilder sb = new StringBuilder(4096);
            DLLGetLastError(sb, sb.Capacity + 1);
            return sb.ToString();
        }

        public static void Throwable(uint DLLResult)
        {
            if (DLLResult == 0) return;
            throw new Exception(GetLastError());
        }

        [DllImport("Dml Native Supplement.dll")]
        private static extern bool DLLGetLastError(StringBuilder s, int sCapacity);
    }
}

#endif
#endif
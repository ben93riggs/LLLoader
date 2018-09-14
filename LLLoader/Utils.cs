using System;

namespace LLLoader
{
    public class Utils
    {
        public static IntPtr CreateRemotePointer(IntPtr hProcess, byte[] pData, int flProtect)
        {
            if (pData == null || hProcess == IntPtr.Zero)
                return IntPtr.Zero;

            IntPtr lpAddress = PInvoke.VirtualAllocEx(hProcess, IntPtr.Zero, (uint)pData.Length, 12288, flProtect);
            if (lpAddress != IntPtr.Zero && PInvoke.WriteProcessMemory(hProcess, lpAddress, pData, pData.Length, out uint lpNumberOfBytesRead) && lpNumberOfBytesRead == pData.Length || lpAddress == IntPtr.Zero)
                return lpAddress;

            PInvoke.VirtualFreeEx(hProcess, lpAddress, 0, 32768);
            lpAddress = IntPtr.Zero;

            return lpAddress;
        }

        //will return -1 (uint.MaxValue == -1 as a signed integer) if it fails.
        public static uint RunThread(IntPtr hProcess, IntPtr lpStartAddress, IntPtr lpParam, int timeout = 1000)
        {
            uint dwThreadRet = uint.MaxValue;

            IntPtr hThread = PInvoke.CreateRemoteThread(hProcess, 0, 0, lpStartAddress, lpParam, 0, 0);

            if (hThread == IntPtr.Zero)
                return dwThreadRet;

            if (PInvoke.WaitForSingleObject(hThread, timeout) == 0x0L) //wait for a response
                PInvoke.GetExitCodeThread(hThread, out dwThreadRet);

            return dwThreadRet;
        }

        public static uint GetLastErrorEx(IntPtr hProcess)
        {
            IntPtr fnGetLastError = PInvoke.GetProcAddress(PInvoke.GetModuleHandleA("kernel32.dll"), "GetLastError");
            return RunThread(hProcess, fnGetLastError, IntPtr.Zero);
        }
    }
}

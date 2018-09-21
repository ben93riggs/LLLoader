using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace LLLoader.Tools
{
    public static class Utils
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

        /// <summary>
        /// Attempts to write some data to a temp file on disk.
        /// </summary>
        /// <param name="data">Data to write to disk</param>
        /// <returns>The path to the temporary disk location if successful, null otherwise.</returns>
        /// <exception cref="ArgumentNullException(string)">The 'data' parameter is null</exception>
        /// <remarks>
        /// First, the function attempts to obtain a temp file name
        /// from the system, but if that fails a randomly-named file
        /// will be created in the same folder as the application
        /// </remarks>
        public static string WriteTempData(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string path;
            try
            {
                path = Path.GetTempFileName();
            }
            catch (IOException)
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());
            }

            try { File.WriteAllBytes(path, data); }
            catch { path = null; }

            return path;
        }

        /// <summary>
        /// Deep clone a managed object to create an exact replica.
        /// </summary>
        /// <typeparam name="T">A formattable type (must be compatible with a BinaryFormatter)</typeparam>
        /// <param name="obj">The object to clone</param>
        /// <returns>A clone of the Object 'obj'.</returns>
        public static T DeepClone<T>(T obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }

        public static uint SizeOf(this Type t)
        {
            //I'm super lazy, and I hate looking at ugly casts everywhere.
            return (uint)Marshal.SizeOf(t);
        }
    }
}

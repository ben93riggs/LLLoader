using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace LLInjector
{
    public partial class Form1 : Form
    {
        private IntPtr _loadedModule = IntPtr.Zero;

        public Form1()
        {
            InitializeComponent();
            PInvoke.AllocConsole();
            timer1.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var proc = Process.GetProcessesByName(textBox2.Text)[0];

            _loadedModule = InjectDll(textBox1.Text, proc.Id);
        }

        private IntPtr InjectDll(string dllPath, int id)
        {
            IntPtr hProcess = PInvoke.OpenProcess(ExtendedTypes.ProcessAccessFlags.All, false, id);

            try
            {
                IntPtr hModule;

                var fnLoadLibraryW = PInvoke.GetProcAddress(PInvoke.GetModuleHandleA("kernel32.dll"), "LoadLibraryW");

                if (fnLoadLibraryW == IntPtr.Zero)
                    throw new Exception("Unable to locate the LoadLibraryW entry point");

                // Create a wchar_t * in the remote process which points to the unicode version of the dll path.
                var pLib = Utils.CreateRemotePointer(hProcess, Encoding.Unicode.GetBytes(dllPath + "\0"), 0x04);

                if (pLib == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to allocate memory in the remote process");

                try
                {
                    // Call LoadLibraryW in the remote process by using CreateRemoteThread.
                    uint hMod = Utils.RunThread(hProcess, fnLoadLibraryW, (uint)pLib.ToInt32(), 10000);

                    if (hMod == uint.MaxValue)
                        throw new Exception("Error occurred when calling function in the remote process");
                    else if (hMod == 0)
                        throw new Exception("Failed to load module into remote process. Error code: " + Utils.GetLastErrorEx(hProcess).ToString());
                    else
                        hModule = new IntPtr((int)hMod);
                }
                finally
                {
                    // Cleanup in all cases.
                    PInvoke.VirtualFreeEx(hProcess, pLib, 0, 0x8000);
                }
                return hModule;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return IntPtr.Zero;
            }
        }

        private bool UnloadDll(int id, IntPtr hModule)
        {
            try
            {
                IntPtr hProcess = PInvoke.OpenProcess(ExtendedTypes.ProcessAccessFlags.All, false, id);

                IntPtr fnFreeLibrary = PInvoke.GetProcAddress(PInvoke.GetModuleHandleA("kernel32.dll"), "FreeLibrary");

                if (fnFreeLibrary == IntPtr.Zero)
                    throw new Exception("Unable to find necessary function entry points in the remote process");

                var hMod = Utils.RunThread(hProcess, fnFreeLibrary, (uint)hModule, 10000);

                if (hMod == uint.MaxValue)
                    throw new Exception("Error occurred when calling function in the remote process");

                if (hMod == 0)
                    throw new Exception("Failed to load module into remote process. Error code: " + Utils.GetLastErrorEx(hProcess));

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            var proc = Process.GetProcessesByName(textBox2.Text)[0];

            Console.WriteLine(UnloadDll(proc.Id, _loadedModule) ? "Success!" : "Failure");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            button2.Enabled = _loadedModule != IntPtr.Zero;
        }
    }
}

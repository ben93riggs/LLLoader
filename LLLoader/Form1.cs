using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;

namespace LLLoader
{
    public partial class Form1 : MaterialForm
    {
        private IntPtr _loadedModule = IntPtr.Zero;

        public Form1()
        {
            MaterialSkinManager materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            //materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey800, Primary.BlueGrey900, Primary.BlueGrey500, Accent.LightBlue200, TextShade.WHITE);

            InitializeComponent();
            timer2.Start();
        }

        private IntPtr LoadDll(string dllPath, int id)
        {
            try
            {
                IntPtr hProcess = PInvoke.OpenProcess(ExtendedTypes.ProcessAccessFlags.All, false, id);

                IntPtr hModule;

                IntPtr fnLoadLibraryW = PInvoke.GetProcAddress(PInvoke.GetModuleHandleA("kernel32.dll"), "LoadLibraryW");

                if (fnLoadLibraryW == IntPtr.Zero)
                    throw new Exception("Unable to locate the LoadLibraryW entry point");

                // Create a wchar_t * in the remote process which points to the unicode version of the dll path.
                IntPtr pLib = Utils.CreateRemotePointer(hProcess, Encoding.Unicode.GetBytes(dllPath + "\0"), 0x04);

                if (pLib == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to allocate memory in the remote process");

                try
                {
                    // Call LoadLibraryW in the remote process by using CreateRemoteThread.
                    uint hMod = Utils.RunThread(hProcess, fnLoadLibraryW, pLib, 10000);

                    if (hMod == uint.MaxValue)
                        throw new Exception("Error occurred when calling function in the remote process");
                    else if (hMod == 0)
                        throw new Exception("Failed to load module into remote process. Error code: " + Utils.GetLastErrorEx(hProcess).ToString());
                    else
                        hModule = new IntPtr(hMod);
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
                logBox.AppendText("\n" + e.Message);
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

                uint hMod = Utils.RunThread(hProcess, fnFreeLibrary, hModule, 10000);

                if (hMod == uint.MaxValue)
                    throw new Exception("Error occurred when calling function in the remote process");

                if (hMod == 0)
                    throw new Exception("Failed to load module into remote process. Error code: " + Utils.GetLastErrorEx(hProcess));

                return true;
            }
            catch (Exception e)
            {
                logBox.AppendText("\n" + e.Message);
                return false;
            }
            
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            try
            {
                //should make sure GetProcessesByName returns an array of length > 0 or throw a descriptive exception.
                Process proc = Process.GetProcessesByName(processTextbox.Text)[0];

                _loadedModule = LoadDll(dllPathTextbox.Text, proc.Id);
            }
            catch (Exception ex)
            {
                logBox.AppendText("\n" + ex.Message);
                return;
            }

            unloadButton.Visible = true;

            logBox.AppendText("\n" + $@"Successfully loaded dll into [{processTextbox.Text}]. Handle: [{_loadedModule}]");
        }

        private void unloadButton_Click(object sender, EventArgs e)
        {
            try
            {
                Process proc = Process.GetProcessesByName(processTextbox.Text)[0];
                logBox.AppendText("\n" + (UnloadDll(proc.Id, _loadedModule) ? "Success!" : "Failure"));
            }
            catch (Exception exception)
            {
                logBox.AppendText("\n" + exception.Message);
                return;
            }

            unloadButton.Visible = false;

            logBox.AppendText("\n" + $@"Successfully called ProcessDetach in [{processTextbox.Text}] for the previously loaded module.");
        }

        private void processListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            processTextbox.Text = processListbox.Items[processListbox.SelectedIndex].ToString();
        }

        private void dllPathTextbox_DoubleClick(object sender, EventArgs e)
        {
            var file = new OpenFileDialog();
            if (file.ShowDialog() == DialogResult.OK)
            {
                dllPathTextbox.Text = file.FileName;
            }
        }
        private void timer2_Tick(object sender, EventArgs e)
        {
            //refresh the process listbox
            var processList = Process.GetProcesses();
            foreach (Process process in processList)
            {
                if (string.IsNullOrEmpty(process.ProcessName))
                    continue;

                if (process.Id <= 0)
                    continue;

                if (processListbox.Items.Contains(process.ProcessName))
                    continue;

                processListbox.Items.Add(process.ProcessName);
            }

            //check if the process closed so we can reset

            if (Process.GetProcessesByName(processTextbox.Text).Length > 0) //if we found the target process
            {
                if (_loadedModule != IntPtr.Zero) //and our handle to the module is still alive
                    unloadButton.Visible = true;
            }
            else
            {
                _loadedModule = IntPtr.Zero;
                unloadButton.Visible = false;
            }

        }
    }
}

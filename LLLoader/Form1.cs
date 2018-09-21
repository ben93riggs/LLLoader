using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using LLLoader.Tools;
using MaterialSkin;
using MaterialSkin.Controls;
using LLLoader.PortableExecutable;

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

            InitializeComponent();
            timer2.Start();
        }

        // DllMain function call stub.
        private static readonly byte[] DLLMAIN_STUB =
        {
            #if PE64
                0x48, 0x83, 0xEC, 0x28,                                     //sub rsp, 0x28
                0x48, 0xB9, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //movabs rcx, 0x0
                0x48, 0xC7, 0xC2, 0x01, 0x00, 0x00, 0x00,                   //mov rdx, 0x1
                0x4D, 0x31, 0xC0,                                           //xor r8, r8
                0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //movabs rax, 0x0
                0xFF, 0xD0,                                                 //call rax
                0x48, 0x83, 0xC4, 0x28,                                     //add rsp, 0x28
                0xC3                                                        //ret
            #else
                0x68, 0x00, 0x00, 0x00, 0x00, //push lpReserved
                0x68, 0x01, 0x00, 0x00, 0x00, //push dwReason
                0x68, 0x00, 0x00, 0x00, 0x00, //push hModule
                0xFF, 0x54, 0x24, 0x10,       //call [esp + 10h]
                0xC3                          //ret
            #endif
        };

        private IntPtr MapDll(string dllPath, int id)
        {
            IntPtr hModule = IntPtr.Zero;

            try
            {
                IntPtr hProcess = PInvoke.OpenProcess(ExtendedTypes.ProcessAccessFlags.All, false, id);

                using (PortableExecutable.PortableExecutable portableExecutable = new PortableExecutable.PortableExecutable(dllPath))
                {
                    PortableExecutable.PortableExecutable image = Utils.DeepClone(portableExecutable);

                    IntPtr pStub = IntPtr.Zero;

                    //allocate memory for the image to load into the remote process.
                    hModule = PInvoke.VirtualAllocEx(hProcess, IntPtr.Zero, image.NTHeader.OptionalHeader.SizeOfImage, 0x1000 | 0x2000, 0x04);
                    if (hModule == IntPtr.Zero)
                        throw new InvalidOperationException("Unable to allocate memory in the remote process.");

                    PatchRelocations(image, hModule);
                    LoadDependencies(image, hProcess, id);
                    PatchImports(image, hProcess, id);

                    uint nBytes;
                    if (preserveHeaders)
                    {
                        long szHeader = (image.DOSHeader.e_lfanew + Marshal.SizeOf(typeof(IMAGE_FILE_HEADER)) + sizeof(uint) + image.NTHeader.FileHeader.SizeOfOptionalHeader);
                        byte[] header = new byte[szHeader];
                        if (image.Read(0, SeekOrigin.Begin, header))
                            PInvoke.WriteProcessMemory(hProcess, hModule, header, header.Length, out nBytes);
                    }

                    MapSections(image, hProcess, hModule);

                    // some modules don't have an entry point and are purely libraries, mapping them and keeping the handle is just fine
                    // an unlikely scenario with forced injection, but you never know.
                    if (image.NTHeader.OptionalHeader.AddressOfEntryPoint > 0)
                    {
                        var stub = (byte[])DLLMAIN_STUB.Clone();
                        BitConverter.GetBytes(hModule.ToInt32()).CopyTo(stub, 0x0B);

                        pStub = PInvoke.VirtualAllocEx(hProcess, IntPtr.Zero, (uint)DLLMAIN_STUB.Length, 0x1000 | 0x2000, 0x40);
                        if (pStub.IsNull() || (!PInvoke.WriteProcessMemory(hProcess, pStub, stub, stub.Length, out nBytes) || nBytes != (uint)stub.Length))
                            throw new InvalidOperationException("Unable to write stub to the remote process.");

                        IntPtr hStubThread = PInvoke.CreateRemoteThread(hProcess, 0, 0, pStub, (uint)(hModule.Add(image.NTHeader.OptionalHeader.AddressOfEntryPoint).ToInt32()), 0, 0);
                        if (PInvoke.WaitForSingleObject(hStubThread, 5000) == 0x0L)
                        {
                            PInvoke.GetExitCodeThread(hStubThread, out nBytes);
                            if (nBytes == 0)
                            {
                                PInvoke.VirtualFreeEx(hProcess, hModule, 0, 0x8000);
                                throw new Exception("Entry method of module reported a failure " + Marshal.GetLastWin32Error().ToString());
                            }
                            PInvoke.VirtualFreeEx(hProcess, pStub, 0, 0x8000);
                            PInvoke.CloseHandle(hStubThread);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                logBox.AppendText("\n" + ex.Message);
                return IntPtr.Zero;
            }

                return pModule;
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
                    byte[] blankArray = new byte[Encoding.Unicode.GetBytes(dllPath + "\0").Length];
                    PInvoke.WriteProcessMemory(hProcess, pLib, blankArray, Encoding.Unicode.GetBytes(dllPath + "\0").Length, out uint nBytesRead);
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

                _loadedModule = MapDll(dllPathTextbox.Text, proc.Id);
                //_loadedModule = LoadDll(dllPathTextbox.Text, proc.Id);
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
            OpenFileDialog file = new OpenFileDialog();
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

        private static void PatchRelocations(PortableExecutable.PortableExecutable image, IntPtr pAlloc)
        {
            // Base relocations are essentially Microsofts ingenious way of preserving portability in images.
            // for all absolute address calls/jmps/references...etc, an entry is made into the base relocation
            // table telling the loader exactly where an "absolute" address is being used. This allows the loader
            // to iterate through the relocations and patch these absolute values to ensure they are correct when
            // the image is loaded somewhere that isn't its preferred base address.
            IMAGE_DATA_DIRECTORY relocDir = image.NTHeader.OptionalHeader.DataDirectory[(int)DATA_DIRECTORIES.BaseRelocTable];

            if (relocDir.Size <= 0)
                return;

            uint n = 0;
            var delta = ((ulong)pAlloc.ToInt64() - image.NTHeader.OptionalHeader.ImageBase); //The difference in loaded/preferred addresses.
            var pReloc = image.GetPtrFromRVA(relocDir.VirtualAddress);
            var szReloc = (uint)Marshal.SizeOf(typeof(IMAGE_BASE_RELOCATION));
            IMAGE_BASE_RELOCATION reloc;

            while (n < relocDir.Size && image.Read(pReloc, SeekOrigin.Begin, out reloc))
            {
                // A relocation block consists of an IMAGE_BASE_RELOCATION, and an array of WORDs.
                // To calculate the number of relocations (represented by WORDs), just do some simple math.
                int nrelocs = (int)((reloc.SizeOfBlock - szReloc) / sizeof(ushort));
                uint pageVa = image.GetPtrFromRVA(reloc.VirtualAddress); //The Page RVA for this set of relocations (usually a 4K boundary).
                ushort vreloc;
                uint old;

                for (int i = 0; i < nrelocs; i++)
                {
                    // There are only 2 types of relocations on Intel machines: ABSOLUTE (padding, nothing needs to be done) and HIGHLOW (0x03)
                    // Highlow means that all 32 bits of the "delta" value need to be added to the relocation value.
                    if (image.Read(pReloc + szReloc + (i << 1), SeekOrigin.Begin, out vreloc) && (vreloc >> 12 & 3) != 0)
                    {
                        uint vp = (uint)(pageVa + (vreloc & 0x0FFF));
                        if (image.Read<uint>(vp, SeekOrigin.Begin, out old))
                            image.Write<uint>(-4, SeekOrigin.Current, (uint)(old + delta));
                        else
                            throw image.GetLastError(); //unlikely, but I hate crashing targets because something in the PE was messed up.
                    }
                }
                n += reloc.SizeOfBlock;
                pReloc += reloc.SizeOfBlock;
            }
        }

        /*
         * Handles loading of all dependent modules. Iterates the IAT entries and attempts to load (using LoadLibrary) all 
         * of the necessary modules for the main module to function. The manifest is extracted and activation contexts used to
         * ensure correct loading of Side-By-Side dependencies.
         */
        private static bool LoadDependencies(PortableExecutable.PortableExecutable image, IntPtr hProcess, int processId)
        {
            List<string> neededDependencies = new List<string>();
            string curdep = string.Empty;
            bool success = false;

            foreach (var desc in image.EnumImports())
            {
                if (image.ReadString(image.GetPtrFromRVA(desc.Name), SeekOrigin.Begin, out curdep) && !string.IsNullOrEmpty(curdep))
                {
                    if (GetRemoteModuleHandle(curdep, processId).IsNull())
                        neededDependencies.Add(curdep);
                }
            }

            if (neededDependencies.Count > 0) //do we actually need to load any new modules?
            {
                byte[] bManifest = ExtractManifest(image);
                string pathManifest = string.Empty;

                if (bManifest == null) // no internal manifest, may be an external manifest or none at all?
                {
                    if (!string.IsNullOrEmpty(image.FileLocation) && File.Exists(Path.Combine(Path.GetDirectoryName(image.FileLocation), Path.GetFileName(image.FileLocation) + ".manifest")))
                    {
                        pathManifest = Path.Combine(Path.GetDirectoryName(image.FileLocation), Path.GetFileName(image.FileLocation) + ".manifest");
                    }
                    else // no internal or external manifest, presume no side-by-side dependencies.
                    {
                        var standard = InjectionMethod.Create(InjectionMethodType.Standard);
                        var results = standard.InjectAll(neededDependencies.ToArray(), hProcess);

                        foreach (var result in results)
                            if (result.IsNull())
                                return false; // failed to inject a dependecy, abort mission.

                        return true; // done loading dependencies.
                    }
                }
                else
                {
                    pathManifest = Utils.WriteTempData(bManifest);
                }

                if (string.IsNullOrEmpty(pathManifest))
                    return false;

                IntPtr pResolverStub = PInvoke.VirtualAllocEx(hProcess, IntPtr.Zero, (uint)RESOLVER_STUB.Length, 0x1000 | 0x2000, 0x40);
                IntPtr pManifest = PInvoke.CreateRemotePointer(hProcess, Encoding.ASCII.GetBytes(pathManifest + "\0"), 0x04);
                IntPtr pModules = PInvoke.CreateRemotePointer(hProcess, Encoding.ASCII.GetBytes(string.Join("\0", neededDependencies.ToArray()) + "\0"), 0x04);

                if (!pResolverStub.IsNull())
                {
                    var resolverStub = (byte[])RESOLVER_STUB.Clone();
                    uint nBytes = 0;

                    // Call patching. Patch the empty function addresses with the runtime addresses.
                    BitConverter.GetBytes(FN_CREATEACTCTXA.Subtract(pResolverStub.Add(0x3F)).ToInt32()).CopyTo(resolverStub, 0x3B);
                    BitConverter.GetBytes(FN_ACTIVATEACTCTX.Subtract(pResolverStub.Add(0x58)).ToInt32()).CopyTo(resolverStub, 0x54);
                    BitConverter.GetBytes(FN_GETMODULEHANDLEA.Subtract(pResolverStub.Add(0x84)).ToInt32()).CopyTo(resolverStub, 0x80);
                    BitConverter.GetBytes(FN_LOADLIBRARYA.Subtract(pResolverStub.Add(0x92)).ToInt32()).CopyTo(resolverStub, 0x8E);
                    BitConverter.GetBytes(FN_DEACTIVATEACTCTX.Subtract(pResolverStub.Add(0xC8)).ToInt32()).CopyTo(resolverStub, 0xC4);
                    BitConverter.GetBytes(FN_RELEASEACTCTX.Subtract(pResolverStub.Add(0xD1)).ToInt32()).CopyTo(resolverStub, 0xCD);

                    // Parameter patching
                    BitConverter.GetBytes(pManifest.ToInt32()).CopyTo(resolverStub, 0x1F);
                    BitConverter.GetBytes(neededDependencies.Count).CopyTo(resolverStub, 0x28);
                    BitConverter.GetBytes(pModules.ToInt32()).CopyTo(resolverStub, 0x31);

                    if (PInvoke.WriteProcessMemory(hProcess, pResolverStub, resolverStub, resolverStub.Length, out nBytes) && nBytes == (uint)resolverStub.Length)
                    {
                        uint result = PInvoke.RunThread(hProcess, pResolverStub, 0, 5000);
                        success = (result != uint.MaxValue && result != 0);
                    }

                    // Cleanup
                    PInvoke.VirtualFreeEx(hProcess, pModules, 0, 0x8000);
                    PInvoke.VirtualFreeEx(hProcess, pManifest, 0, 0x8000);
                    PInvoke.VirtualFreeEx(hProcess, pResolverStub, 0, 0x8000);
                }
            }

            return success;
        }
    }
}

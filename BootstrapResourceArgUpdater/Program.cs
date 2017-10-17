using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

// This is a bastardization of what Epic does to edit resources in the bootstrap exe

namespace BootstrapResourceArgUpdater
{
    public class ModuleResourceUpdate : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr BeginUpdateResource(string pFileName, [MarshalAs(UnmanagedType.Bool)]bool bDeleteExistingResources);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, IntPtr lpData, uint cbData);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

        IntPtr UpdateHandle;
        List<IntPtr> UnmanagedPointers = new List<IntPtr>();

        public ModuleResourceUpdate(string OutputFile)
        {
            UpdateHandle = BeginUpdateResource(OutputFile, false);
        }

        public void SetData(int ResourceId, int Type, byte[] Data)
        {
            IntPtr UnmanagedPointer = Marshal.AllocHGlobal(Data.Length);
            UnmanagedPointers.Add(UnmanagedPointer);

            Marshal.Copy(Data, 0, UnmanagedPointer, Data.Length);

            if (!UpdateResource(UpdateHandle, new IntPtr(Type), new IntPtr(ResourceId), 1033, UnmanagedPointer, (uint)Data.Length))
            {
                throw new Exception("Couldn't update resource");
            }
        }

        public void Dispose()
        {
            EndUpdateResource(UpdateHandle, false);
            foreach (IntPtr UnmanagedPointer in UnmanagedPointers)
            {
                Marshal.FreeHGlobal(UnmanagedPointer);
            }
            UnmanagedPointers.Clear();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() != 2)
            {
                Console.WriteLine("Usage: [bootstrap filename] [new arg string]");
                return;
            }

            using (ModuleResourceUpdate Update = new ModuleResourceUpdate(args[0]))
            {
                Update.SetData(202, 10, Encoding.Unicode.GetBytes(args[1] + "\0"));
            }
        }
    }
}

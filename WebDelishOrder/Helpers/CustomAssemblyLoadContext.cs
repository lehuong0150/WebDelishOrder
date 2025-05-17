using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;


public class CustomAssemblyLoadContext : AssemblyLoadContext
{
    public IntPtr LoadUnmanagedLibrary(string absolutePath)
    {
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Không tìm thấy thư viện unmanaged tại đường dẫn: {absolutePath}");
        }

        return LoadUnmanagedDll(absolutePath);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllPath)
    {
        return LoadLibrary(unmanagedDllPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        return null;
    }

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);
}
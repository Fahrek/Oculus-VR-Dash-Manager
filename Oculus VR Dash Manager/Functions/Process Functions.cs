﻿using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace OVR_Dash_Manager.Functions
{
    public static class Process_Functions
    {
        public static bool IsCurrentProcess_Elevated()
        {
            var vIdentity = GetWindowsIdentity();
            var vPrincipal = GetWindowsPrincipal(vIdentity);

            var pReturn = vPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            vIdentity.Dispose();
            return pReturn;
        }

        static WindowsIdentity GetWindowsIdentity() => WindowsIdentity.GetCurrent();

        static WindowsPrincipal GetWindowsPrincipal(WindowsIdentity pIdentity)
        {
            return new WindowsPrincipal(pIdentity);
        }

        public static string GetCurrentProcessDirectory()
        {
            var Current = Process.GetCurrentProcess();
            return Path.GetDirectoryName(Current.MainModule.FileName);
        }

        public static Process StartProcess(string Path, string Arguments = "")
        {
            if (File.Exists(Path))
            {
                return Process.Start(Path, Arguments);
                // File
            }
            else
            {
                // try and build full url - else returns same as input
                var URL = StringFunctions.GetFullUrl(Path);
                return Process.Start(URL, Arguments);
                // Web Site
            }
        }
    }
}
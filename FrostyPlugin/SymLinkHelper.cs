using FrostyModManager;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Frosty.Core
{
    public static class SymLinkHelper
    {
        private static IntPtr _symlinkModule = IntPtr.Zero;
        private static ConvertWindowsPathDelegate _convertWindowsPathDelegate;
        private static IsWindowsPathSymlinkDelegate _isWindowsPathSymlinkDelegate;
        private static DeleteWindowsPathDelegate _deleteWindowsPathDelegate;
        private static CreateWindowsSymlinkDelegate _createWindowsSymlinkDelegate;

        public const int BatchSize = 8;

        private const string registryPath = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Session Manager\\Environment";
        private const string envVarName = "PATHEXT";

        private static bool _areHardLinksSupported = false;
        public static bool AreHardLinksSupported => _areHardLinksSupported;

        private static bool _areSymLinksLinuxSupported = false;
        public static bool AreSymLinksSupported => !OperatingSystemHelper.IsWine() || _areSymLinksLinuxSupported;

        public static void Initialize(string path)
        {
            TestHardLinks(path);

            if (!UpdateRegistry())
            {
                FileLogger.Info("Registry update failed.");
                _areSymLinksLinuxSupported = false;
                return;
            }

            if (!UpdateEnviromentalVariables())
            {
                FileLogger.Info($"Missing value for env var {envVarName}.");
                _areSymLinksLinuxSupported = false;
                return;
            }

            if (!InitializeSymlinksLibrary())
            {
                FileLogger.Info("Could not initialize symlink library.");
                _areSymLinksLinuxSupported = false;
                return;
            }

            _areSymLinksLinuxSupported = true;
        }

        private static bool InitializeSymlinksLibrary()
        {
            _symlinkModule = SafeLoadLibrary("ThirdParty/wine-symlink.dll.so");
            if (_symlinkModule == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                FileLogger.Info($"Failed to load wine-symlink.dll.so with error code {errorCode}.");
                return false;
            }

            IntPtr pAddressOfFunctionToCall = GetProcAddress(_symlinkModule, "ConvertWindowsPath");
            if (pAddressOfFunctionToCall == IntPtr.Zero)
            {
                FileLogger.Info("Failed to find ConvertWindowsPath in wine-symlink.dll.so.");
                return false;
            }

            _convertWindowsPathDelegate = Marshal.GetDelegateForFunctionPointer<ConvertWindowsPathDelegate>(pAddressOfFunctionToCall);

            pAddressOfFunctionToCall = GetProcAddress(_symlinkModule, "IsWindowsPathSymlink");
            if (pAddressOfFunctionToCall == IntPtr.Zero)
            {
                FileLogger.Info("Failed to find IsWindowsPathSymlink in wine-symlink.dll.so.");
                return false;
            }

            _isWindowsPathSymlinkDelegate = Marshal.GetDelegateForFunctionPointer<IsWindowsPathSymlinkDelegate>(pAddressOfFunctionToCall);

            pAddressOfFunctionToCall = GetProcAddress(_symlinkModule, "DeleteWindowsPath");
            if (pAddressOfFunctionToCall == IntPtr.Zero)
            {
                FileLogger.Info("Failed to find DeleteWindowsPath in wine-symlink.dll.so.");
                return false;
            }

            _deleteWindowsPathDelegate = Marshal.GetDelegateForFunctionPointer<DeleteWindowsPathDelegate>(pAddressOfFunctionToCall);

            pAddressOfFunctionToCall = GetProcAddress(_symlinkModule, "CreateWindowsSymlink");
            if (pAddressOfFunctionToCall == IntPtr.Zero)
            {
                FileLogger.Info("Failed to find CreateWindowsSymlink in wine-symlink.dll.so.");
                return false;
            }

            _createWindowsSymlinkDelegate = Marshal.GetDelegateForFunctionPointer<CreateWindowsSymlinkDelegate>(pAddressOfFunctionToCall);

            return true;
        }

        public static void DeleteDirectorySafe(string path)
        {
            if (!Directory.Exists(path))
            {
                FileLogger.Info($"Directory delete aborted. Path '{path}' does not exists.");
                return;
            }

            if (_areSymLinksLinuxSupported && !_deleteWindowsPathDelegate(path))
            {
                FileLogger.Info($"Could not safely remove directory '{path}'.");

                throw new Exception($"Could not safely remove directory '{path}'.");
            }

            if (IsSymbolicLink(path))
            {
                Directory.Delete(path, true);
                return;
            }

            DeleteDirectoryRecursivly(path);
        }

        public static void DeleteFileSafe(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            if (!_areSymLinksLinuxSupported)
            {
                File.Delete(path);
                return;
            }

            if (!_deleteWindowsPathDelegate(path))
            {
                FileLogger.Info($"Could not safely remove file '{path}'.");

                throw new Exception($"Could not safely remove file '{path}'.");
            }
        }

        public static bool IsSymbolicLink(string path, bool strict = false)
        {
            if (!strict && (IsHardLink(path) > 0))
            {
                return true;
            }
            else if (strict && (IsHardLink(path) == 1))
            {
                return true;
            }

            if (_areSymLinksLinuxSupported)
            {
                return _isWindowsPathSymlinkDelegate(path);
            }

            var isDirectory = Directory.Exists(path);

            if (!isDirectory && !File.Exists(path))
            {
                return false;
            }

            FileAttributes attributes;

            if (isDirectory)
            {
                var di = new DirectoryInfo(path);
                attributes = di.Attributes;
            }
            else
            {
                var fi = new FileInfo(path);
                attributes = fi.Attributes;
            }

            return (attributes & FileAttributes.ReparsePoint) != 0;
        }

        public static bool DoesDirectoryContainSymLinks(string path, bool strict = false)
        {
            if (!Directory.Exists(path))
            {
                return false;
            }

            if (IsSymbolicLink(path, strict))
            {
                return true;
            }

            var queue = new Queue<string>();
            queue.Enqueue(path);

            while (queue.Count > 0)
            {
                var dirPath = queue.Dequeue();
                var files = Directory.GetFiles(dirPath);

                if (files.Any(f => IsSymbolicLink(f, strict)))
                {
                    return true;
                }

                var subDirs = Directory.GetDirectories(dirPath);
                if (subDirs.Any(s => IsSymbolicLink(s, strict)))
                {
                    return true;
                }

                foreach (var subDir in subDirs)
                {
                    queue.Enqueue(subDir);
                }
            }

            return false;
        }

        public static void CreateSymlinkLinux(string source, string destination)
        {
            if (string.IsNullOrWhiteSpace(source) ||  string.IsNullOrWhiteSpace(destination))
            {
                FileLogger.Info($"Symbolic Link aborted. Invalid source '{source}' or destination '{destination}'.");
                return;
            }

            var isDirectory = Directory.Exists(source);

            if (!isDirectory && !File.Exists(source))
            {
                FileLogger.Info($"Symbolic Link aborted. Source '{source}' does not exists.");
                return;
            }

            if (!_areSymLinksLinuxSupported)
            {
                return;
            }

            if (!_createWindowsSymlinkDelegate(source, destination))
            {
                FileLogger.Info($"Could not create symlink for source '{source}' and destination '{destination}'.");
                throw new Exception($"Could not create symlink for source '{source}' and destination '{destination}'.");
            }
        }

        public static string GetLinuxPath(string path)
        {
            if (!_areSymLinksLinuxSupported)
            {
                return path;
            }

            var sb = new StringBuilder(2048);
            if (!_convertWindowsPathDelegate(path, sb, sb.Capacity))
            {
                FileLogger.Info("Convert delegate returned false");
                return path;
            }

            var linuxPath = sb.ToString();

            linuxPath = Regex.Replace(linuxPath.Replace('\\', '/'), "/{2,}", "/");

            return linuxPath;
        }

        private static void DeleteDirectoryRecursivly(string path)
        {
            var queue = new Queue<string>();
            queue.Enqueue(path);

            while (queue.Count > 0) 
            {
                var current = queue.Dequeue();
                if (!Directory.Exists(current))
                {
                    continue;
                }

                var files = Directory.GetFiles(current);
                foreach (var file in files)
                {
                    var hardlinks = IsHardLink(file);

                    //if (hardlinks > 0)
                    //{
                    //    FileLogger.Info($"File [{file}] has {hardlinks} hard links.");
                    //}

                    if (hardlinks == 1)
                    {
                        FileLogger.Info($"Could not safely remove file [{file}].\nPlease remove whole ModData folder manually.");
                        throw new FileNotFoundException($"Could not safely remove file [{file}].\nPlease remove whole ModData folder manually.");
                    }

                    File.Delete(file);
                }

                var dirs = Directory.GetDirectories(current);
                foreach (var dir in dirs)
                {
                    queue.Enqueue(dir);
                }
            }

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private static uint IsHardLink(string path)
        {
            if (Directory.Exists(path))
            {
                return 0;
            }

            if (!File.Exists(path))
            {
                return 0;
            }

            var orgPath = RemoveModDataSegment(path);
            if (string.IsNullOrWhiteSpace(orgPath))
            {
                return 0;
            }

            if (!File.Exists(orgPath)) 
            {
                return 0;
            }

            var orgInfo = GetFileInfo(orgPath);
            var info = GetFileInfo(path);

            var res =
                orgInfo.VolumeSerialNumber == info.VolumeSerialNumber &&
                orgInfo.FileIndexHigh == info.FileIndexHigh &&
                orgInfo.FileIndexLow == info.FileIndexLow;

            if (!res)
            {
                return 0;
            }

            return info.NumberOfLinks;
        }

        private static string RemoveModDataSegment(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            // Normalize separators
            var normalized = path.Replace('/', '\\');

            var parts = normalized.Split('\\').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            // Find "ModData"
            int modDataIndex = Array.FindIndex(
                parts,
                p => string.Equals(p, "ModData", StringComparison.OrdinalIgnoreCase));

            // Must have ModData + one folder after it
            if (modDataIndex < 0 || modDataIndex + 1 >= parts.Length)
            {
                return string.Empty;
            }

            // Remove ModData and the folder after it
            var resultParts = parts
                .Where((_, i) => i != modDataIndex && i != modDataIndex + 1)
                .ToArray();

            // Preserve drive letter or UNC prefix
            string prefix = Path.IsPathRooted(normalized)
                ? Path.GetPathRoot(normalized)
                : string.Empty;

            if (string.IsNullOrWhiteSpace(prefix))
            {
                return Path.Combine(resultParts);
            }

            if (resultParts.Length > 0)
            {
                resultParts[0] = string.Empty;
            }

            return prefix + Path.Combine(resultParts);
        }

        private static void TestHardLinks(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var orgFile = Path.Combine(path, "hard_link_test.txt");
            var linkFile = Path.Combine(path, "hard_link_test_link.txt");

            try
            {
                if (!File.Exists(orgFile))
                {
                    File.Create(orgFile).Close();
                }
            }
            catch (Exception ex)
            {
                FileLogger.Info($"Exception when testing hard links. Details: {ex.Message}");
                _areHardLinksSupported = false;
                return;
            }

            if (!File.Exists(orgFile))
            {
                FileLogger.Info("Could not create test file for hard linking.");
                _areHardLinksSupported = false;
                return;
            }

            CreateHardLink(orgFile, linkFile);

            if (!File.Exists(linkFile))
            {
                FileLogger.Info("Could not create hard link to the test file.");
                File.Delete(orgFile);
                _areHardLinksSupported = false;
                return;
            }

            File.Delete(linkFile);
            File.Delete(orgFile);

            _areHardLinksSupported = true;
        }

        private static bool UpdateEnviromentalVariables()
        {
            var envVar = Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrWhiteSpace(envVar))
            {
                _areSymLinksLinuxSupported = false;
                return false;
            }

            if (envVar != "." && !envVar.Contains(".;") && !envVar.EndsWith(";."))
            {
                if (envVar.EndsWith(";"))
                {
                    Environment.SetEnvironmentVariable(envVarName, envVar + ".");
                }
                else
                {
                    Environment.SetEnvironmentVariable(envVarName, envVar + ";.");
                }
            }

            return true;
        }

        private static bool UpdateRegistry()
        {
            if (!OperatingSystemHelper.IsWine())
            {
                return true;
            }

            var valueObj = Registry.GetValue(registryPath, envVarName, string.Empty);

            if (valueObj == null || !(valueObj is string) || string.IsNullOrWhiteSpace((string)valueObj))
            {
                FileLogger.Info("Missing registry key 'HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Session Manager\\Environment\\PATHEXT'.");

                return false;
            }

            var value = (string)valueObj;

            if (value == "." || value.Contains(".;") || value.EndsWith(";."))
            {
                return true;
            }

            if (value.EndsWith(";"))
            {
                value += ".";
            }
            else
            {
                value += ";.";
            }

            Registry.SetValue(registryPath, envVarName, value);

            FileLogger.Info("Registry updated.");

            return true;
        }

        public static void HandleAggregateException(AggregateException ax)
        {
            var ex = ax.Flatten().InnerExceptions.FirstOrDefault();

            if (ex == null)
            {
                FileLogger.Info($"Retrieved empty Aggregate Exception. Details:\n{ax}");
                return;
            }

            FileLogger.Info($"Retrieved Aggregate Exception with following exception. Details:\n{ex}");

            throw ex;
        }

        public static void CreateHardLink(string source, string destination)
        {
            if (!File.Exists(source))
            {
                FileLogger.Info($"Hard Link aborted. Source '{source}' does not exists.");
                return;
            }

            CreateHardLink(destination, source, IntPtr.Zero);
        }

        public static IntPtr SafeLoadLibrary(string path)
        {
            // save old error mode
            uint oldMode;
            SetThreadErrorMode(SEM_FAILCRITICALERRORS | SEM_NOOPENFILEERRORBOX, out oldMode);

            try
            {
                return LoadLibrary(path);
            }
            finally
            {
                // restore previous mode
                SetThreadErrorMode(oldMode, out _);
            }
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes
        );

        // Structs and data for hard link infos
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetFileInformationByHandle(
        SafeFileHandle hFile,
        out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [StructLayout(LayoutKind.Sequential)]
        struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        static BY_HANDLE_FILE_INFORMATION GetFileInfo(string path)
        {
            using (var fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete))
            {
                if (!GetFileInformationByHandle(fs.SafeFileHandle, out var info))
                {
                    throw new IOException("Failed to get file info");
                }

                return info;
            }
        }

        // LoadLibrary and GetProcAddress are from kernel32
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll")]
        private static extern uint SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

        private const uint SEM_FAILCRITICALERRORS = 0x0001;
        private const uint SEM_NOOPENFILEERRORBOX = 0x8000;

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        // Delegate that matches the signature of your function
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private delegate bool ConvertWindowsPathDelegate(
            [MarshalAs(UnmanagedType.LPStr)]  string path,
            [MarshalAs(UnmanagedType.LPStr)]  StringBuilder buffer,
            int bufferSize
        );

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private delegate bool IsWindowsPathSymlinkDelegate(string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private delegate bool DeleteWindowsPathDelegate(string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private delegate bool CreateWindowsSymlinkDelegate(string source, string destination);
    }
}

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace SharpWorker.Health.Native
{
    internal class SystemMemoryInfo
    {
        private readonly PerformanceCounter _monoAvailableMemoryCounter;
        private readonly PerformanceCounter _monoTotalMemoryCounter;
        private readonly PerformanceCounter _netAvailableMemoryCounter;

        private ulong _availablePhysicalMemory;
        private ulong _totalPhysicalMemory;

        public SystemMemoryInfo()
        {
            try
            {
                if (PerformanceCounterCategory.Exists("Mono Memory"))
                {
                    _monoAvailableMemoryCounter = new PerformanceCounter("Mono Memory", "Available Physical Memory");
                    _monoTotalMemoryCounter = new PerformanceCounter("Mono Memory", "Total Physical Memory");
                }
                else if (PerformanceCounterCategory.Exists("Memory"))
                {
                    _netAvailableMemoryCounter = new PerformanceCounter("Memory", "Available Bytes");
                }
            }
            catch
            {
                // ignored
            }
        }

        public ulong AvailablePhysicalMemory
        {
            [SecurityCritical]
            get
            {
                Refresh();

                return _availablePhysicalMemory;
            }
        }

        public ulong TotalPhysicalMemory
        {
            [SecurityCritical]
            get
            {
                Refresh();

                return _totalPhysicalMemory;
            }
        }

        [SecurityCritical]
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void GlobalMemoryStatus(ref MemoryStatus lpBuffer);

        [SecurityCritical]
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

        [SecurityCritical]
        // ReSharper disable once ExcessiveIndentation
        private void Refresh()
        {
            // Try mono's performance counter
            try
            {
                if (_monoTotalMemoryCounter != null && _monoAvailableMemoryCounter != null)
                {
                    _totalPhysicalMemory = (ulong) _monoTotalMemoryCounter.NextValue();
                    _availablePhysicalMemory = (ulong) _monoAvailableMemoryCounter.NextValue();
                }
            }
            catch
            {
                // ignore
            }

            if (Environment.OSVersion.Platform != PlatformID.Win32Windows &&
                Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return;
            }

            // Windows XP and later
            if (Environment.OSVersion.Version.Major >= 5)
            {
                try
                {
                    var memoryStatusEx = MemoryStatusEx.Initialize();

                    if (GlobalMemoryStatusEx(ref memoryStatusEx))
                    {
                        _availablePhysicalMemory = memoryStatusEx.AvailablePhysical;
                        _totalPhysicalMemory = memoryStatusEx.TotalPhysical;

                        return;
                    }
                }
                catch
                {
                    // ignore
                }
            }

            // Windows NT and earlier
            try
            {
                var memoryStatus = MemoryStatus.Initialize();
                GlobalMemoryStatus(ref memoryStatus);

                if (memoryStatus.TotalPhysical > 0)
                {
                    _availablePhysicalMemory = memoryStatus.AvailablePhysical;
                    _totalPhysicalMemory = memoryStatus.TotalPhysical;

                    return;
                }
            }
            catch
            {
                // ignore
            }

            // Try performance counter
            if (_netAvailableMemoryCounter != null)
            {
                _availablePhysicalMemory = (ulong) _netAvailableMemoryCounter.NextValue();
            }
        }
    }
}
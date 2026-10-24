using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace TaskManagerTrayWidget;

public sealed class SystemMetricsSampler
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(
        out FILETIME lpIdleTime,
        out FILETIME lpKernelTime,
        out FILETIME lpUserTime);

    private ulong _prevIdleTime;
    private ulong _prevKernelTime;
    private ulong _prevUserTime;
    private bool _hasPrevTimes;

    public SystemMetricsSampler()
    {
        // Initial sample
        SampleTimes(out _prevIdleTime, out _prevKernelTime, out _prevUserTime);
        _hasPrevTimes = true;
    }

    private static ulong ToUInt64(FILETIME ft)
    {
        return ((ulong)(uint)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
    }

    private static bool SampleTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime)
    {
        if (GetSystemTimes(out FILETIME ftIdle, out FILETIME ftKernel, out FILETIME ftUser))
        {
            idleTime = ToUInt64(ftIdle);
            kernelTime = ToUInt64(ftKernel);
            userTime = ToUInt64(ftUser);
            return true;
        }

        idleTime = 0;
        kernelTime = 0;
        userTime = 0;
        return false;
    }

    /// <summary>
    /// Returns current CPU utilization percentage (0.0 - 100.0).
    /// </summary>
    public double GetCpuUsage()
    {
        if (!SampleTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime))
        {
            return 0.0;
        }

        if (!_hasPrevTimes)
        {
            _prevIdleTime = idleTime;
            _prevKernelTime = kernelTime;
            _prevUserTime = userTime;
            _hasPrevTimes = true;
            return 0.0;
        }

        ulong idleDiff = idleTime - _prevIdleTime;
        ulong kernelDiff = kernelTime - _prevKernelTime;
        ulong userDiff = userTime - _prevUserTime;

        _prevIdleTime = idleTime;
        _prevKernelTime = kernelTime;
        _prevUserTime = userTime;

        // In Windows, kernel time includes idle time
        ulong totalDiff = kernelDiff + userDiff;
        if (totalDiff == 0)
        {
            return 0.0;
        }

        double cpuPercent = (1.0 - ((double)idleDiff / totalDiff)) * 100.0;
        return Math.Clamp(cpuPercent, 0.0, 100.0);
    }

    /// <summary>
    /// Returns current RAM memory usage percentage (0.0 - 100.0), total memory (bytes), and used memory (bytes).
    /// </summary>
    public (double UsagePercentage, ulong TotalBytes, ulong UsedBytes) GetMemoryUsage()
    {
        var memStatus = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(memStatus))
        {
            ulong total = memStatus.ullTotalPhys;
            ulong avail = memStatus.ullAvailPhys;
            ulong used = total >= avail ? total - avail : 0;
            double percent = total > 0 ? ((double)used / total) * 100.0 : memStatus.dwMemoryLoad;
            return (Math.Clamp(percent, 0.0, 100.0), total, used);
        }

        return (0.0, 0, 0);
    }
}


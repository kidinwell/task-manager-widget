using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TaskManagerTrayWidget;

static class Program
{
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);
    private const int ATTACH_PARENT_PROCESS = -1;

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length > 0 && (args[0] == "--test-metrics" || args[0] == "--test-all"))
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
            var sampler = new SystemMetricsSampler();
            Thread.Sleep(500);
            double cpu = sampler.GetCpuUsage();
            var (memPercent, totalBytes, usedBytes) = sampler.GetMemoryUsage();
            Console.WriteLine($"[METRICS_TEST] CPU: {cpu:F1}%, RAM: {memPercent:F1}% (Used: {usedBytes / (1024 * 1024)}MB / Total: {totalBytes / (1024 * 1024)}MB)");
            if (cpu < 0 || cpu > 100 || memPercent <= 0 || memPercent > 100)
            {
                Console.WriteLine("[METRICS_TEST] ERROR: Metrics out of bounds.");
                Environment.Exit(1);
            }
            Console.WriteLine("[METRICS_TEST] SUCCESS: Metrics within expected ranges.");

            if (args[0] == "--test-metrics")
            {
                Environment.Exit(0);
                return;
            }
        }

        if (args.Length > 0 && (args[0] == "--test-icon" || args[0] == "--test-all"))
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
            try
            {
                // Test threshold color mappings
                Color normalColor = IconRenderer.GetThresholdColor(50.0);
                Color warnColor = IconRenderer.GetThresholdColor(85.0);
                Color critColor = IconRenderer.GetThresholdColor(95.0);

                if (normalColor.IsEmpty || warnColor.IsEmpty || critColor.IsEmpty)
                {
                    Console.WriteLine("[ICON_TEST] FAILED: Threshold color is empty.");
                    Environment.Exit(1);
                }

                // Test creating icons for all display modes and thresholds
                var modes = new[] { DisplayMode.Dual, DisplayMode.CpuOnly, DisplayMode.RamOnly };
                var percentages = new[] { 10.0, 50.0, 85.0, 95.0 };

                foreach (var mode in modes)
                {
                    foreach (var cpu in percentages)
                    {
                        foreach (var ram in percentages)
                        {
                            using Icon icon = IconRenderer.CreateChartIcon(cpu, ram, mode);
                            if (icon.Width != 32 || icon.Height != 32)
                            {
                                Console.WriteLine($"[ICON_TEST] FAILED: Invalid icon dimensions {icon.Width}x{icon.Height}");
                                Environment.Exit(1);
                            }
                        }
                    }
                }

                Console.WriteLine("[ICON_TEST] SUCCESS: Icons successfully created and disposed across all modes & thresholds.");

                if (args[0] == "--test-icon")
                {
                    Environment.Exit(0);
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ICON_TEST] EXCEPTION: {ex}");
                Environment.Exit(1);
                return;
            }
        }

        if (args.Length > 0 && (args[0] == "--test-e2e" || args[0] == "--test-all"))
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
            try
            {
                ApplicationConfiguration.Initialize();
                using var context = new TrayApplicationContext();

                // Run multiple update cycles
                for (int i = 0; i < 3; i++)
                {
                    Thread.Sleep(300);
                    context.UpdateMetrics();
                }

                Console.WriteLine("[E2E_TEST] SUCCESS: TrayApplicationContext initialized and executed metric updates without error.");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[E2E_TEST] EXCEPTION: {ex}");
                Environment.Exit(1);
            }
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}

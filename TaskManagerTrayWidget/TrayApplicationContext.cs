using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace TaskManagerTrayWidget;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _pollingTimer;
    private readonly SystemMetricsSampler _sampler;
    private Icon? _currentIcon;
    private DisplayMode _displayMode = DisplayMode.Dual;

    private readonly ToolStripMenuItem _dualMenuItem;
    private readonly ToolStripMenuItem _cpuMenuItem;
    private readonly ToolStripMenuItem _ramMenuItem;

    public TrayApplicationContext()
    {
        _sampler = new SystemMetricsSampler();

        // Build context menu
        var contextMenu = new ContextMenuStrip();

        var openTaskMgrItem = new ToolStripMenuItem("Task Manager", null, (s, e) => LaunchTaskManager())
        {
            Font = new Font(contextMenu.Font, FontStyle.Bold)
        };
        contextMenu.Items.Add(openTaskMgrItem);
        contextMenu.Items.Add(new ToolStripSeparator());

        _dualMenuItem = new ToolStripMenuItem("Display: Dual (CPU & RAM)", null, (s, e) => SetDisplayMode(DisplayMode.Dual))
        {
            Checked = true
        };
        _cpuMenuItem = new ToolStripMenuItem("Display: CPU Only", null, (s, e) => SetDisplayMode(DisplayMode.CpuOnly));
        _ramMenuItem = new ToolStripMenuItem("Display: RAM Only", null, (s, e) => SetDisplayMode(DisplayMode.RamOnly));

        contextMenu.Items.Add(_dualMenuItem);
        contextMenu.Items.Add(_cpuMenuItem);
        contextMenu.Items.Add(_ramMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => ExitApplication());
        contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = contextMenu,
            Visible = true,
            Text = "Task Manager Widget (Initializing...)"
        };

        _notifyIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                LaunchTaskManager();
            }
        };

        // Polling timer: 1.5 seconds interval
        _pollingTimer = new System.Windows.Forms.Timer
        {
            Interval = 1500
        };
        _pollingTimer.Tick += (s, e) => UpdateMetrics();

        // Initial update
        UpdateMetrics();
        _pollingTimer.Start();
    }

    private void SetDisplayMode(DisplayMode mode)
    {
        _displayMode = mode;
        _dualMenuItem.Checked = mode == DisplayMode.Dual;
        _cpuMenuItem.Checked = mode == DisplayMode.CpuOnly;
        _ramMenuItem.Checked = mode == DisplayMode.RamOnly;
        UpdateMetrics();
    }

    public void UpdateMetrics()
    {
        double cpu = _sampler.GetCpuUsage();
        var (ram, totalBytes, usedBytes) = _sampler.GetMemoryUsage();

        // Generate dynamic icon
        Icon newIcon = IconRenderer.CreateChartIcon(cpu, ram, _displayMode);
        Icon? oldIcon = _currentIcon;
        _currentIcon = newIcon;
        _notifyIcon.Icon = newIcon;

        // Dispose previous icon to avoid GDI handle accumulation
        oldIcon?.Dispose();

        // Update tooltip (under 63 chars for Windows NotifyIcon compatibility)
        string modeLabel = _displayMode switch
        {
            DisplayMode.CpuOnly => "CPU",
            DisplayMode.RamOnly => "RAM",
            _ => "Dual"
        };
        string tooltip = $"CPU: {cpu:F0}% | RAM: {ram:F0}% [{modeLabel}]";
        if (tooltip.Length > 63)
        {
            tooltip = tooltip.Substring(0, 63);
        }
        _notifyIcon.Text = tooltip;
    }

    public static void LaunchTaskManager()
    {
        try
        {
            Process.Start(new ProcessStartInfo("taskmgr.exe")
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to start Task Manager: {ex.Message}");
        }
    }

    private void ExitApplication()
    {
        _pollingTimer.Stop();
        _notifyIcon.Visible = false;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollingTimer.Stop();
            _pollingTimer.Dispose();

            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();

            _currentIcon?.Dispose();
            _currentIcon = null;
        }
        base.Dispose(disposing);
    }
}


using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TaskManagerTrayWidget;

public enum DisplayMode
{
    Dual,
    CpuOnly,
    RamOnly
}

public static class IconRenderer
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// Returns threshold color:
    /// &lt;80%: Normal (Task Manager vibrant green)
    /// 80%-89%: Warning (amber/yellow)
    /// &gt;=90%: Critical (alert red)
    /// </summary>
    public static Color GetThresholdColor(double percentage)
    {
        if (percentage >= 90.0)
        {
            return Color.FromArgb(239, 83, 80); // Critical: Red
        }
        if (percentage >= 80.0)
        {
            return Color.FromArgb(255, 179, 0); // Warning: Amber/Yellow
        }
        return Color.FromArgb(0, 230, 118);     // Normal: Task Manager vibrant green
    }

    /// <summary>
    /// Generates a 32x32 GDI Icon styled like the Windows Task Manager system tray icon.
    /// Features a dark gray border frame with side-by-side vertical bar graphs.
    /// Caller is responsible for disposing the returned Icon.
    /// </summary>
    public static Icon CreateChartIcon(double cpuPercent, double ramPercent, DisplayMode mode = DisplayMode.Dual)
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.None;
            g.Clear(Color.Transparent);

            // Outer frame (dark border & dark charcoal background)
            using var borderPen = new Pen(Color.FromArgb(100, 100, 100), 1);
            using var bgBrush = new SolidBrush(Color.FromArgb(24, 24, 24));
            g.FillRectangle(bgBrush, 1, 1, 30, 30);
            g.DrawRectangle(borderPen, 1, 1, 29, 29);

            const int barTop = 3;
            const int barBottom = 28;
            const int barMaxHeight = barBottom - barTop + 1; // 26px

            // Grid lines for authentic Task Manager look
            using var gridPen = new Pen(Color.FromArgb(36, 46, 36), 1);
            int[] gridYs = { 9, 15, 22 };

            if (mode == DisplayMode.Dual)
            {
                // Left column: CPU (x: 3..14, width 12)
                // Right column: RAM (x: 17..28, width 12)
                // Center separator (x: 15..16)
                using var barBgBrush = new SolidBrush(Color.FromArgb(16, 20, 16));
                g.FillRectangle(barBgBrush, 3, barTop, 12, barMaxHeight);
                g.FillRectangle(barBgBrush, 17, barTop, 12, barMaxHeight);

                // Draw subtle grid lines across both columns
                foreach (int gy in gridYs)
                {
                    g.DrawLine(gridPen, 3, gy, 14, gy);
                    g.DrawLine(gridPen, 17, gy, 28, gy);
                }

                // Center divider
                using var dividerPen = new Pen(Color.FromArgb(60, 60, 60), 1);
                g.DrawLine(dividerPen, 15, 2, 15, 29);
                g.DrawLine(dividerPen, 16, 2, 16, 29);

                // Render CPU bar (Left)
                int cpuHeight = (int)Math.Round(Math.Clamp(cpuPercent, 0.0, 100.0) / 100.0 * barMaxHeight);
                if (cpuHeight > 0)
                {
                    int cpuY = barBottom - cpuHeight + 1;
                    using var cpuBrush = new SolidBrush(GetThresholdColor(cpuPercent));
                    g.FillRectangle(cpuBrush, 3, cpuY, 12, cpuHeight);
                }

                // Render RAM bar (Right)
                int ramHeight = (int)Math.Round(Math.Clamp(ramPercent, 0.0, 100.0) / 100.0 * barMaxHeight);
                if (ramHeight > 0)
                {
                    int ramY = barBottom - ramHeight + 1;
                    using var ramBrush = new SolidBrush(GetThresholdColor(ramPercent));
                    g.FillRectangle(ramBrush, 17, ramY, 12, ramHeight);
                }
            }
            else
            {
                // Single bar mode (full width bar)
                bool isCpu = mode == DisplayMode.CpuOnly;
                double percent = isCpu ? cpuPercent : ramPercent;

                using var barBgBrush = new SolidBrush(Color.FromArgb(16, 20, 16));
                g.FillRectangle(barBgBrush, 3, barTop, 26, barMaxHeight);

                foreach (int gy in gridYs)
                {
                    g.DrawLine(gridPen, 3, gy, 28, gy);
                }

                int barHeight = (int)Math.Round(Math.Clamp(percent, 0.0, 100.0) / 100.0 * barMaxHeight);
                if (barHeight > 0)
                {
                    int barY = barBottom - barHeight + 1;
                    using var barBrush = new SolidBrush(GetThresholdColor(percent));
                    g.FillRectangle(barBrush, 3, barY, 26, barHeight);
                }
            }
        }

        IntPtr hIcon = bitmap.GetHicon();
        try
        {
            using var tempIcon = Icon.FromHandle(hIcon);
            return (Icon)tempIcon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }
}

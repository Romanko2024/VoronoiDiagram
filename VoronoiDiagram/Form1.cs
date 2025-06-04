using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VoronoiDiagram
{
    public partial class Form1 : Form
    {
        private Bitmap canvas;
        private string currentMetric = "Евклідова";
        private readonly Dictionary<string, Func<PointF, PointF, double>> metrics = new Dictionary<string, Func<PointF, PointF, double>>
        {
            ["Евклідова"] = (p1, p2) => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)),
            ["Манхетенська"] = (p1, p2) => Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y),
            ["Чебишева"] = (p1, p2) => Math.Max(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y))
        };
        public MainForm()
        {
            InitializeComponent();
            canvas = new Bitmap(pictureBox.Width, pictureBox.Height);
            pictureBox.Image = canvas;
            comboBoxMetric.SelectedIndex = 0;
        }
        private void RenderVoronoi()
        {
            if (vertices.Count == 0)
            {
                using (var g = Graphics.FromImage(canvas))
                    g.Clear(Color.White);
                pictureBox.Refresh();
                return;
            }

            var timer = Stopwatch.StartNew();
            var cpuStart = Process.GetCurrentProcess().TotalProcessorTime;
            var memStart = GC.GetTotalMemory(false);

            locusSizes.Clear();
            var colors = AssignColors();

            if (useParallel)
            {
                int parts = Environment.ProcessorCount;
                var partitions = PartitionCanvas(parts);

                Parallel.ForEach(partitions, rect =>
                {
                    ProcessRegion(rect, colors);
                });
            }

            timer.Stop();
            var cpuTime = (Process.GetCurrentProcess().TotalProcessorTime - cpuStart).TotalMilliseconds;
            var memUsed = (GC.GetTotalMemory(false) - memStart) / 1024.0;

            labelStats.Text = $"Час: {timer.ElapsedMilliseconds} мс | CPU: {cpuTime:F2} мс | Пам'ять: {memUsed:F2} КБ";
            pictureBox.Refresh();
        }
    }
}

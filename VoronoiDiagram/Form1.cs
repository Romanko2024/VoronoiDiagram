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
        private PictureBox pictureBox;
        private ComboBox comboBoxMetric;
        private NumericUpDown numericUpDownPoints;
        private NumericUpDown numericUpDownRemove;
        private Button buttonGenerate;
        private Button buttonClear;
        private Button buttonRemove;
        private RadioButton radioButtonSingle;
        private RadioButton radioButtonParallel;
        private Label labelStats;

        private List<PointF> vertices = new List<PointF>();
        private Bitmap canvas;
        private string currentMetric = "Евклідова";
        private bool useParallel = false;
        private Dictionary<PointF, int> locusSizes = new Dictionary<PointF, int>();
        private readonly Dictionary<string, Func<PointF, PointF, double>> metrics = new Dictionary<string, Func<PointF, PointF, double>>
        {
            ["Евклідова"] = (p1, p2) => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)),
            ["Манхетенська"] = (p1, p2) => Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y),
            ["Чебишева"] = (p1, p2) => Math.Max(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y))
        };
        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }
        private Dictionary<PointF, Color> AssignColors()
        {
            var rnd = new Random();
            return vertices.ToDictionary(
                v => v,
                v => Color.FromArgb(rnd.Next(200, 256), rnd.Next(200, 256), rnd.Next(200, 256)) // Світлі кольори
            );
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

        private void ProcessRegion(Rectangle rect, Dictionary<PointF, Color> colors)
        {
            double maxDist = Math.Sqrt(Math.Pow(rect.Width, 2) + Math.Pow(rect.Height, 2)) * 2;

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    var point = new PointF(x, y);
                    var closest = FindClosestVertex(point, rect, maxDist);
                    if (closest.HasValue)
                    {
                        lock (canvas)
                        {
                            canvas.SetPixel(x, y, colors[closest.Value]);
                        }
                        lock (locusSizes)
                        {
                            if (!locusSizes.ContainsKey(closest.Value))
                                locusSizes[closest.Value] = 0;
                            locusSizes[closest.Value]++;
                        }
                    }
                }
            }
        }

        private bool IsVertexRelevant(PointF vertex, Rectangle region, double maxDist)
        {
            
        }
        private List<Rectangle> PartitionCanvas(int parts)
        {
            
        }
        private PointF? FindClosestVertex(PointF point, Rectangle region, double maxDist)
        {
            PointF? closest = null;
            double minDist = double.MaxValue;

            foreach (var vertex in vertices)
            {
                if (!IsVertexRelevant(vertex, region, maxDist))
                    continue;

                double dist = metrics[currentMetric](point, vertex);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = vertex;
                }
            }
            return closest;
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Діаграма Вороного";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // PictureBox
            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            pictureBox.MouseClick += PictureBox_MouseClick;

            // ComboBox для вибору метрики
            comboBoxMetric = new ComboBox
            {
                Items = { "Евклідова", "Манхетенська", "Чебишева" },
                SelectedIndex = 0,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(10, 10),
                Width = 120
            };
            comboBoxMetric.SelectedIndexChanged += ComboBoxMetric_SelectedIndexChanged;

            // NumericUpDown для кількості точок
            numericUpDownPoints = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10000,
                Value = 100,
                Location = new Point(140, 10),
                Width = 60
            };

            //gen
            buttonGenerate = new Button
            {
                Text = "Згенерувати",
                Location = new Point(210, 9),
                Width = 100
            };
            buttonGenerate.Click += ButtonGenerate_Click;

            //del
            buttonClear = new Button
            {
                Text = "Очистити",
                Location = new Point(320, 9),
                Width = 80
            };
            buttonClear.Click += ButtonClear_Click;

            // NumericUpDown для відсотка видалення
            numericUpDownRemove = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100,
                Value = 30,
                Location = new Point(410, 10),
                Width = 60
            };

            //видалення малих локусів
            buttonRemove = new Button
            {
                Text = "Вилучити малі локуси",
                Location = new Point(480, 9),
                Width = 150
            };
            buttonRemove.Click += ButtonRemove_Click;

            //стат
            labelStats = new Label
            {
                Text = "Статистика: ",
                Dock = DockStyle.Bottom,
                Height = 20,
                BorderStyle = BorderStyle.FixedSingle
            };

            //controlPanel
            Panel controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40
            };
            controlPanel.Controls.AddRange(new Control[] {
                comboBoxMetric, numericUpDownPoints, buttonGenerate, buttonClear,
                numericUpDownRemove, buttonRemove, radioButtonSingle, radioButtonParallel
            });
            radioButtonSingle = new RadioButton
            {
                Text = "Один потік",
                Checked = true,
                Location = new Point(640, 11),
                AutoSize = true
            };

            radioButtonParallel = new RadioButton
            {
                Text = "Паралельно",
                Location = new Point(720, 11),
                AutoSize = true
            };

            //додавання елементів
            this.Controls.Add(pictureBox);
            this.Controls.Add(controlPanel);
            this.Controls.Add(labelStats);

            //ыныц полотна
            canvas = new Bitmap(pictureBox.Width, pictureBox.Height);
            pictureBox.Image = canvas;
        }
        private void PictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                vertices.Add(e.Location);
                RenderVoronoi();
            }
            else if (e.Button == MouseButtons.Right && vertices.Count > 0)
            {
                var closest = vertices.OrderBy(v => metrics[currentMetric](v, e.Location)).First();
                vertices.Remove(closest);
                RenderVoronoi();
            }
        }

        private void ButtonGenerate_Click(object sender, EventArgs e)
        {
            var rnd = new Random();
            vertices.Clear();
            for (int i = 0; i < numericUpDownPoints.Value; i++)
            {
                vertices.Add(new PointF(
                    rnd.Next(pictureBox.Width),
                    rnd.Next(pictureBox.Height)
                ));
            }
            RenderVoronoi();
        }

        private void ButtonClear_Click(object sender, EventArgs e)
        {
            vertices.Clear();
            using (var g = Graphics.FromImage(canvas))
                g.Clear(Color.White);
            pictureBox.Refresh();
            labelStats.Text = "Статистика: ";
        }
        private void ComboBoxMetric_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentMetric = comboBoxMetric.SelectedItem.ToString();
            if (vertices.Count > 0) RenderVoronoi();
        }

        private void RadioButtonParallel_CheckedChanged(object sender, EventArgs e)
        {
            useParallel = radioButtonParallel.Checked;
            if (vertices.Count > 0) RenderVoronoi();
        }

        private void ButtonRemove_Click(object sender, EventArgs e)
        {
            if (locusSizes.Count == 0 || vertices.Count == 0) return;

            double threshold = (double)numericUpDownRemove.Value / 100.0;
            var avgSize = locusSizes.Values.Average();
            var toRemove = locusSizes
                .Where(kvp => kvp.Value < avgSize * threshold)
                .Select(kvp => kvp.Key)
                .ToList();

            vertices.RemoveAll(toRemove.Contains);
            RenderVoronoi();
        }

    }
}

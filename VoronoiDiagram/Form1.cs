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
        private string currentMetric = "��������";
        private bool useParallel = false;
        private Dictionary<PointF, int> locusSizes = new Dictionary<PointF, int>();
        private readonly Dictionary<string, Func<PointF, PointF, double>> metrics = new Dictionary<string, Func<PointF, PointF, double>>
        {
            ["��������"] = (p1, p2) => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)),
            ["������������"] = (p1, p2) => Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y),
            ["��������"] = (p1, p2) => Math.Max(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y))
        };

        // ����������: ����������� �� ��'� Form1 (����� �����)
        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private Dictionary<PointF, Color> AssignColors()
        {
            var rnd = new Random();
            return vertices.ToDictionary(
                v => v,
                v => Color.FromArgb(rnd.Next(200, 256), rnd.Next(200, 256), rnd.Next(200, 256))
            );
        }

        private void RenderVoronoi()
        {
            if (vertices.Count == 0)
            {
                using (var g = Graphics.FromImage(canvas))
                    g.Clear(Color.White);
                pictureBox.Refresh();
                labelStats.Text = "����������: ";
                return;
            }

            var timer = Stopwatch.StartNew();
            var cpuStart = Process.GetCurrentProcess().TotalProcessorTime;
            var memStart = GC.GetTotalMemory(false);

            locusSizes.Clear();
            var colors = AssignColors();

            //�������� �������
            using (var g = Graphics.FromImage(canvas))
                g.Clear(Color.White);

            if (useParallel)
            {
                int parts = Environment.ProcessorCount;
                var partitions = PartitionCanvas(parts);

                Parallel.ForEach(partitions, rect =>
                {
                    ProcessRegion(rect, colors);
                });
            }
            else
            {
                ProcessRegion(new Rectangle(0, 0, canvas.Width, canvas.Height), colors);
            }

            //��������� ������
            using (var g = Graphics.FromImage(canvas))
            {
                foreach (var vertex in vertices)
                {
                    g.FillEllipse(Brushes.Black, vertex.X - 2, vertex.Y - 2, 4, 4);
                }
            }

            timer.Stop();
            var cpuTime = (Process.GetCurrentProcess().TotalProcessorTime - cpuStart).TotalMilliseconds;
            var memUsed = (GC.GetTotalMemory(false) - memStart) / 1024.0;

            labelStats.Text = $"���: {timer.ElapsedMilliseconds} �� | CPU: {cpuTime:F2} �� | ���'���: {memUsed:F2} ��";
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
            //���� ������� �������� ������
            if (region.Contains((int)vertex.X, (int)vertex.Y))
                return true;

            //������� �� ��������� ����� ������
            float closestX = Math.Clamp(vertex.X, region.Left, region.Right - 1);
            float closestY = Math.Clamp(vertex.Y, region.Top, region.Bottom - 1);
            double dist = metrics[currentMetric](vertex, new PointF(closestX, closestY));

            return dist <= maxDist;
        }

        private List<Rectangle> PartitionCanvas(int parts)
        {
            int cols = (int)Math.Sqrt(parts);
            int rows = (parts + cols - 1) / cols;
            var partitions = new List<Rectangle>();
            int width = canvas.Width / cols;
            int height = canvas.Height / rows;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    int rectX = x * width;
                    int rectY = y * height;
                    int rectWidth = (x == cols - 1) ? canvas.Width - x * width : width;
                    int rectHeight = (y == rows - 1) ? canvas.Height - y * height : height;

                    partitions.Add(new Rectangle(rectX, rectY, rectWidth, rectHeight));
                }
            }
            return partitions;
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
            this.Text = "ĳ������ ��������";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // PictureBox
            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            pictureBox.MouseClick += PictureBox_MouseClick;

            // ComboBox ��� ������ �������
            comboBoxMetric = new ComboBox
            {
                Items = { "��������", "������������", "��������" },
                SelectedIndex = 0,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(10, 10),
                Width = 120
            };
            comboBoxMetric.SelectedIndexChanged += ComboBoxMetric_SelectedIndexChanged;

            // NumericUpDown ��� ������� �����
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
                Text = "�����������",
                Location = new Point(210, 9),
                Width = 100
            };
            buttonGenerate.Click += ButtonGenerate_Click;

            //del
            buttonClear = new Button
            {
                Text = "��������",
                Location = new Point(320, 9),
                Width = 80
            };
            buttonClear.Click += ButtonClear_Click;

            // NumericUpDown ��� ������� ���������
            numericUpDownRemove = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100,
                Value = 30,
                Location = new Point(410, 10),
                Width = 60
            };

            //��������� ����� ������
            buttonRemove = new Button
            {
                Text = "�������� ��� ������",
                Location = new Point(480, 9),
                Width = 150
            };
            buttonRemove.Click += ButtonRemove_Click;

            //����
            labelStats = new Label
            {
                Text = "����������: ",
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

            radioButtonSingle = new RadioButton
            {
                Text = "���� ����",
                Checked = true,
                Location = new Point(640, 11),
                AutoSize = true
            };
            radioButtonSingle.CheckedChanged += RadioButtonParallel_CheckedChanged;

            radioButtonParallel = new RadioButton
            {
                Text = "����������",
                Location = new Point(720, 11),
                AutoSize = true
            };
            radioButtonParallel.CheckedChanged += RadioButtonParallel_CheckedChanged;

            controlPanel.Controls.AddRange(new Control[] {
                comboBoxMetric, numericUpDownPoints, buttonGenerate, buttonClear,
                numericUpDownRemove, buttonRemove, radioButtonSingle, radioButtonParallel
            });
            //��������� ��������
            this.Controls.Add(pictureBox);
            this.Controls.Add(controlPanel);
            this.Controls.Add(labelStats);

            //��� �������
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
            int count = (int)numericUpDownPoints.Value;
            for (int i = 0; i < count; i++)
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
            labelStats.Text = "����������: ";
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

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
        private string currentMetric = "��������";
        private readonly Dictionary<string, Func<PointF, PointF, double>> metrics = new Dictionary<string, Func<PointF, PointF, double>>
        {
            ["��������"] = (p1, p2) => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)),
            ["������������"] = (p1, p2) => Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y),
            ["��������"] = (p1, p2) => Math.Max(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y))
        };
        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
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

            labelStats.Text = $"���: {timer.ElapsedMilliseconds} �� | CPU: {cpuTime:F2} �� | ���'���: {memUsed:F2} ��";
            pictureBox.Refresh();
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
            controlPanel.Controls.AddRange(new Control[] {
                comboBoxMetric, numericUpDownPoints, buttonGenerate, buttonClear,
                numericUpDownRemove, buttonRemove, radioButtonSingle, radioButtonParallel
            });

            //��������� ��������
            this.Controls.Add(pictureBox);
            this.Controls.Add(controlPanel);
            this.Controls.Add(labelStats);

            //���� �������
            canvas = new Bitmap(pictureBox.Width, pictureBox.Height);
            pictureBox.Image = canvas;
        }
    }
}

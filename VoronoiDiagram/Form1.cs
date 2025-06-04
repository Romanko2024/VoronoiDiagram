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
    }
}

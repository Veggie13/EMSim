using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlexDraw;

namespace EMSim
{
    public partial class Form1 : Form
    {
        const double K = 1000000;

        const int nPoints = 7;
        class Charge : IDrawable
        {
            public double Q = 1;
            public PointD Pt = new PointD();

            public void Draw(IDrawAPI api)
            {
                api.FillEllipse(new RectangleD(
                    Pt.Offset(new PointD(-MapSize.Width / 300, -MapSize.Width / 300)),
                    Pt.Offset(new PointD(MapSize.Width / 300, MapSize.Width / 300))),
                    Q > 0 ? Color.Red : Color.Blue);
            }

            public bool Visible
            {
                get { return true; }
            }

            public PointD Origin
            {
                get { return new PointD(0, 0); }
            }

            public RectangleD Bounds
            {
                get { return new RectangleD(Pt, Pt); }
            }

            public RectangleD LastBounds
            {
                get { return Bounds; }
            }

            public event DrawableModifiedEvent Modified { add { } remove { } }
        }
        List<Charge> _pts = new List<Charge>();

        static RectangleD MapSize = new RectangleD(-500, 1000, -750, 750);

        class ForceField : IDrawable
        {
            public Form1 Parent { get; set; }
            public bool JustMap { get; set; }

            List<Charge> test = new List<Charge>();
            List<Charge> test2 = new List<Charge>(), test3 = new List<Charge>();
            public void Update()
            {
                test.Clear();
                test2.Clear();
                test3.Clear();

                var bounds = Bounds;
                double xStep = Bounds.Width / 400f;
                double yStep = Bounds.Height / 400f;
                for (double x = Bounds.Left + xStep; x < Bounds.Right; x += xStep)
                    for (double y = Bounds.Bottom + yStep; y < Bounds.Top; y += yStep)
                    {
                        double M = Force(new PointD(x, y));
                        if (double.IsNaN(M)) continue;
                        Charge n = new Charge();
                        n.Q = M;
                        n.Pt = new PointD(x, y);
                        test.Add(n);
                    }
                double Max = test.Max<Charge, double>((q) => q.Q);
                int[] hist = new int[10001];
                foreach (Charge q in test)
                {
                    double m = q.Q / Max;
                    int v = (int)(10000 * m);
                    if (v > 1)
                        continue;
                    test2.Add(q);
                    //int mm = 255 - (int)(255 * m);//(double)v / 100);
                    //Color c = Color.FromArgb(mm, mm, mm);
                    //_surface.DrawPixel(q.Pt, c);
                }
            }

            public double Force(PointD pt)
            {
                double ax = 0, ay = 0;
                foreach (Charge q in Parent._pts)
                {
                    double dx = (q.Pt.X - pt.X);
                    double dy = (q.Pt.Y - pt.Y);
                    double r2 = dx * dx + dy * dy;
                    //r2 = Math.Pow(r2, 0.75);
                    r2 *= Math.Sqrt(r2);
                    ax += K * q.Q * dx / r2;
                    ay += K * q.Q * dy / r2;
                }
                return Math.Sqrt(ax * ax + ay * ay);
            }
            
            public void Draw(IDrawAPI api)
            {
                if (JustMap)
                {
                    double deltaX = Parent._viewport.View.Width / 400;
                    double deltaY = Parent._viewport.View.Height / 400;
                    for (double xx = Parent._viewport.View.Left + deltaX; xx < Parent._viewport.View.Right; xx += deltaX)
                    {
                        for (double yy = Parent._viewport.View.Bottom + deltaY; yy < Parent._viewport.View.Top; yy += deltaY)
                        {
                            PointD pt = new PointD(xx, yy);
                            PointD mapPt = Parent.mapPoint(pt);
                            int x = (int)Math.Floor(mapPt.X), y = (int)Math.Floor(mapPt.Y);
                            if (x < 0 || y < 0 || x > Parent._map.Width - 2 || y > Parent._map.Height - 2)
                                continue;
                            Color pp = Parent._map.GetPixel(x, y);
                            Color pq = Parent._map.GetPixel(x, y + 1);
                            Color qp = Parent._map.GetPixel(x + 1, y);
                            Color qq = Parent._map.GetPixel(x + 1, y + 1);
                            double dx = x - mapPt.X, dy = y - mapPt.Y;
                            Color bc = Color.FromArgb(
                                (int)Math.Round(dx * dy * (pp.R - pq.R - qp.R + qq.R) + dx * (pp.R - qp.R) + dy * (pp.R - pq.R) + pp.R),
                                (int)Math.Round(dx * dy * (pp.G - pq.G - qp.G + qq.G) + dx * (pp.G - qp.G) + dy * (pp.G - pq.G) + pp.G),
                                (int)Math.Round(dx * dy * (pp.B - pq.B - qp.B + qq.B) + dx * (pp.B - qp.B) + dy * (pp.B - pq.B) + pp.B));
                            api.FillRectangle(new RectangleD(
                                pt.Offset(new PointD(-MapSize.Width / 800, -MapSize.Width / 800)),
                                pt.Offset(new PointD(MapSize.Width / 800, MapSize.Width / 800))),
                                bc);
                        }
                    }
                    return;
                }

                if (test2.Count < 1)
                    return;
                double Max = test2.Max<Charge, double>((q) => q.Q);
                foreach (Charge q in test2)
                {
                    PointD mapPt = Parent.mapPoint(q.Pt);
                    Color bc = Color.White;
                    if (!(mapPt.X < 0 || mapPt.Y < 0 || mapPt.X > Parent._map.Width - 2 || mapPt.Y > Parent._map.Height - 2))
                    {
                        int x = (int)Math.Floor(mapPt.X), y = (int)Math.Floor(mapPt.Y);
                        Color pp = Parent._map.GetPixel(x, y);
                        Color pq = Parent._map.GetPixel(x, y + 1);
                        Color qp = Parent._map.GetPixel(x + 1, y);
                        Color qq = Parent._map.GetPixel(x + 1, y + 1);
                        double dx = x - mapPt.X, dy = y - mapPt.Y;
                        bc = Color.FromArgb(
                            (int)Math.Round(dx * dy * (pp.R - pq.R - qp.R + qq.R) + dx * (pp.R - qp.R) + dy * (pp.R - pq.R) + pp.R),
                            (int)Math.Round(dx * dy * (pp.G - pq.G - qp.G + qq.G) + dx * (pp.G - qp.G) + dy * (pp.G - pq.G) + pp.G),
                            (int)Math.Round(dx * dy * (pp.B - pq.B - qp.B + qq.B) + dx * (pp.B - qp.B) + dy * (pp.B - pq.B) + pp.B));
                    }
                    double m = 1 - Math.Pow(q.Q / Max, 0.2);
                    int mm = (int)(255 * m);
                    Color c = Color.FromArgb((int)(m * bc.R), (int)(m * bc.G), (int)(m * bc.B));
                    api.FillRectangle(new RectangleD(
                        q.Pt.Offset(new PointD(-MapSize.Width / 800, -MapSize.Width / 800)),
                        q.Pt.Offset(new PointD(MapSize.Width / 800, MapSize.Width / 800))),
                        c);
                }
            }

            public bool Visible
            {
                get { return true; }
            }

            public PointD Origin
            {
                get { return new PointD(0, 0); }
            }

            public RectangleD Bounds
            {
                get { return MapSize; }
            }

            public RectangleD LastBounds
            {
                get { return Bounds; }
            }

            public event DrawableModifiedEvent Modified { add { } remove { } }
        }
        ForceField _field = new ForceField();

        class SteepestDescent : IDrawable
        {
            public Form1 Parent { get; set; }

            List<PointD> _guesses = new List<PointD>();
            
            public void Update(PointD start)
            {
                _guesses.Clear();
                PointD prev = new PointD();
                prev.Copy(start);
                _guesses.Add(prev);
                double prevF = double.MaxValue;
                PointD cur = new PointD();
                cur.Copy(prev);

                double P = 100;
                while (prevF > 0.1 && _guesses.Count < 1000 && P > 0.001)
                {
                    double ax = 0, ay = 0, daxdx = 0, daydx = 0, daxdy = 0, daydy = 0;
                    foreach (Charge q in Parent._pts)
                    {
                        double rx = cur.X - q.Pt.X;
                        double ry = cur.Y - q.Pt.Y;
                        double r2 = rx * rx + ry * ry;
                        double fx = K * q.Q * rx / Math.Pow(r2, 1.5);
                        double fy = K * q.Q * ry / Math.Pow(r2, 1.5);
                        double C = Math.Sqrt(r2) / (K * q.Q);
                        double dfxdx = C * (fy * fy - 2 * fx * fx);
                        double dfydx = C * (-3 * fx * fy);
                        double dfxdy = dfydx;
                        double dfydy = C * (fx * fx - 2 * fy * fy);

                        ax += fx;
                        ay += fy;
                        daxdx += dfxdx;
                        daydx += dfydx;
                        daxdy += dfxdy;
                        daydy += dfydy;
                    }

                    double F = Math.Sqrt(ax * ax + ay * ay);
                    double dFdx = (ax * daxdx + ay * daydx) / F;
                    double dFdy = (ax * daxdy + ay * daydy) / F;

                    if (F > prevF)
                    {
                        P /= 10;
                        cur.Copy(prev);
                    }
                    else
                    {
                        prevF = F;
                        prev = cur;
                        cur = cur.Offset(new PointD(-P * dFdx, -P * dFdy));
                        _guesses.Add(prev);
                    }
                }
            }

            public void Draw(IDrawAPI api)
            {
                for (int i = 1; i < _guesses.Count; i++)
                {
                    double portion = (double)i / _guesses.Count;
                    Color c = Color.FromArgb(0, 255 - (int)(200 * (1 - portion)), 0);
                    api.DrawLine(_guesses[i - 1], _guesses[i], c);
                }
            }

            public bool Visible
            {
                get { return true; }
            }

            public PointD Origin
            {
                get { return new PointD(0, 0); }
            }

            public RectangleD Bounds
            {
                get { return MapSize; }
            }

            public RectangleD LastBounds
            {
                get { return Bounds; }
            }

            public event DrawableModifiedEvent Modified { add { } remove { } }
        }
        SteepestDescent _descent = new SteepestDescent();

        class Charges : IDrawable
        {
            public Form1 Parent { get; set; }

            public void Draw(IDrawAPI api)
            {
                foreach (Charge q in Parent._pts)
                    q.Draw(api);
            }

            public bool Visible
            {
                get { return true; }
            }

            public PointD Origin
            {
                get { return new PointD(0, 0); }
            }

            public RectangleD Bounds
            {
                get { return MapSize; }
            }

            public RectangleD LastBounds
            {
                get { return Bounds; }
            }

            public event DrawableModifiedEvent Modified { add { } remove { } }
        }

        private GCViewport _viewport = new GCViewport();
        private DrawSurface _surface = new DrawSurface();
        private Bitmap _map;
        private double _curCharge = 1;
        public Form1()
        {
            InitializeComponent();

            _viewport.View = MapSize;
            _viewport.IsYUp = true;

            _surface.Viewports.Add(_viewport);

            this.SizeChanged += new EventHandler(Form1_SizeChanged);
            this.Paint += new PaintEventHandler(Form1_Paint);
            this.MouseClick += new MouseEventHandler(Form1_MouseClick);
            this.MouseMove += new MouseEventHandler(Form1_MouseMove);
            this.KeyPress += new KeyPressEventHandler(Form1_KeyPress);
            this.DoubleBuffered = true;

            _map = Properties.Resources.map;

            /*Random r = new Random();
            for (int i = 0; i < nPoints; i++)
            {
                _pts[i] = new Charge();
                _pts[i].Q = (r.NextDouble() > 0.5) ? 1 : -1;
                _pts[i].Pt.X = 2 * r.NextDouble() - 1;
                _pts[i].Pt.Y = 2 * r.NextDouble() - 1;
            }*/
            _pts.Add(makeCharge(1, 0f, 0f));
            _pts.Add(makeCharge(-1, 148f, 189f));
            _pts.Add(makeCharge(-1, 190f, 160f));
            _pts.Add(makeCharge(1, 213f, 168f));
            _pts.Add(makeCharge(1, 282f, 147f));
            _pts.Add(makeCharge(-1, 524f, -125f));
            _pts.Add(makeCharge(-1, 325f, -463f));
            _pts.Add(makeCharge(-1, -328f, 527f));
            _pts.Add(makeCharge(-1, 242.5f, 81.8f));
            
            _field.Parent = this;
            _field.Update();

            Random rand = new Random();
            double relX = rand.NextDouble();
            double relY = rand.NextDouble();
            _descent.Parent = this;
            _descent.Update(new PointD(MapSize.Left + relX * MapSize.Width, MapSize.Bottom + relY * MapSize.Height));

            _surface.Items.Add(_field);
            _surface.Items.Add(_descent);
            var charges = new Charges();
            charges.Parent = this;
            _surface.Items.Add(charges);
        }

        void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case '\b':
                    _pts.RemoveAt(_pts.Count - 1);
                    _field.Update();
                    Invalidate();
                    break;
                case '+':
                    {
                        PointD mousePos = _viewport.Transform(PointToClient(Cursor.Position));
                        double width = _viewport.View.Width;
                        double height = _viewport.View.Height;
                        _viewport.View = new RectangleD(
                            mousePos.Offset(new PointD(-width / 4, -height / 4)),
                            mousePos.Offset(new PointD(width / 4, height / 4))
                            );
                        Invalidate();
                    }
                    break;
                case '-':
                    {
                        double left = _viewport.View.Left;
                        double right = _viewport.View.Right;
                        double top = _viewport.View.Top;
                        double bottom = _viewport.View.Bottom;
                        _viewport.View = new RectangleD(
                            new PointD((3 * left - right) / 2, (3 * top - bottom) / 2),
                            new PointD((3 * right - left) / 2, (3 * bottom - top) / 2)
                            );
                        Invalidate();
                    }
                    break;
                case 'C':
                case 'c':
                    {
                        _viewport.View = MapSize;
                        Invalidate();
                    }
                    break;
                case ' ':
                    _field.JustMap = !_field.JustMap;
                    Invalidate();
                    break;
                default:
                    break;
            }
        }

        void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = _viewport.Transform(e.Location);
            string lbl = string.Format("Mouse: {0:F6}, {1:F6}", mousePos.X, mousePos.Y);
            _lblMousePos.Text = lbl;
            double force = _field.Force(mousePos);
            lbl = string.Format("Value: {0:F6}", force);
            _lblValue.Text = lbl;
        }

        void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            var mousePos = _viewport.Transform(e.Location);
            if ((e.Button & System.Windows.Forms.MouseButtons.Right) == System.Windows.Forms.MouseButtons.Right)
            {
                _descent.Update(mousePos);
                Invalidate();
            }
            else if ((e.Button & System.Windows.Forms.MouseButtons.Middle) == System.Windows.Forms.MouseButtons.Middle)
            {
                _curCharge *= -1;
                _lblCharge.Text = string.Format("Charge: {0}", _curCharge);
            }
            else
            {
                _pts.Add(makeCharge(_curCharge, mousePos.X, mousePos.Y));
                _field.Update();
                Invalidate();
            }
        }

        PointD mapPoint(PointD pt)
        {
            return new PointD(2 * pt.X + 786, 1580 - 2 * pt.Y);
        }

        Charge makeCharge(double q, double x, double y)
        {
            Charge qq = new Charge();
            qq.Q = q;
            qq.Pt.X = x;
            qq.Pt.Y = y;
            return qq;
        }

        void Form1_Paint(object sender, PaintEventArgs e)
        {
            _viewport.GC = e.Graphics;

            e.Graphics.FillRectangle(Brushes.Black, this.DisplayRectangle);

            _surface.Draw();
        }

        void Form1_SizeChanged(object sender, EventArgs e)
        {
            DoResize();
        }

        private void DoResize()
        {
            _viewport.Window = this.DisplayRectangle;
            Invalidate();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DoResize();
        }
    }
}

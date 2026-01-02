using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace AreaSelector
{
    public class SelectionForm : Form
    {
        private Point startPoint;
        private Rectangle selectionRect;
        private bool isSelecting;
        private Pen borderPen;
        private SolidBrush fillBrush;
        private SolidBrush overlayBrush;

        public SelectionForm()
        {
            // Fullscreen transparent form
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;
            this.Opacity = 0.25;
            this.Cursor = Cursors.Cross;

            borderPen = new Pen(Color.FromArgb(242, 7, 86), 2);
            fillBrush = new SolidBrush(Color.FromArgb(34, 242, 7, 86));
            overlayBrush = new SolidBrush(Color.FromArgb(64, 0, 0, 0));

            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            this.KeyDown += OnKeyDown;
            this.Paint += OnPaint;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                startPoint = e.Location;
                isSelecting = true;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                int width = Math.Abs(e.X - startPoint.X);
                int height = Math.Abs(e.Y - startPoint.Y);
                selectionRect = new Rectangle(x, y, width, height);
                this.Invalidate();
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (isSelecting && selectionRect.Width > 5 && selectionRect.Height > 5)
            {
                // Output the selection coordinates and exit
                Console.WriteLine(selectionRect.X + "," + selectionRect.Y + "," + selectionRect.Width + "," + selectionRect.Height);
                this.Close();
            }
            else
            {
                isSelecting = false;
                selectionRect = Rectangle.Empty;
                this.Invalidate();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Console.WriteLine("CANCELLED");
                this.Close();
            }
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                // Draw selection rectangle
                e.Graphics.FillRectangle(fillBrush, selectionRect);
                e.Graphics.DrawRectangle(borderPen, selectionRect);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (borderPen != null) borderPen.Dispose();
                if (fillBrush != null) fillBrush.Dispose();
                if (overlayBrush != null) overlayBrush.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SelectionForm());
        }
    }
}

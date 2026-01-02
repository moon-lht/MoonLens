using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace TranslationPopup
{
    public class PopupForm : Form
    {
        private RichTextBox textBox;
        private Button copyButton;
        private Button closeButton;
        private Button pinButton;
        private bool isPinned = false;
        private Point dragOffset;
        private bool isDragging = false;
        private Panel headerPanel;
        private string translatedText;
        private System.Windows.Forms.Timer fadeTimer;
        private System.Windows.Forms.Timer autoCloseTimer;
        private double currentOpacity = 1.0;
        private bool isFading = false;
        private static Mutex mutex;
        private NamedPipeServerStream pipeServer;
        private Thread pipeThread;
        private bool isClosing = false;

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        public PopupForm(string text)
        {
            translatedText = text;
            InitializeComponent();
            StartPipeServer();
        }

        private void StartPipeServer()
        {
            pipeThread = new Thread(() => {
                while (!isClosing)
                {
                    try
                    {
                        pipeServer = new NamedPipeServerStream("MoonLensPopupPipe", PipeDirection.In);
                        pipeServer.WaitForConnection();
                        
                        using (StreamReader reader = new StreamReader(pipeServer))
                        {
                            string newText = reader.ReadToEnd();
                            if (!string.IsNullOrEmpty(newText))
                            {
                                this.Invoke((MethodInvoker)delegate {
                                    UpdateText(newText);
                                });
                            }
                        }
                        pipeServer.Close();
                    }
                    catch { }
                }
            });
            pipeThread.IsBackground = true;
            pipeThread.Start();
        }

        private void UpdateText(string newText)
        {
            translatedText = newText;
            textBox.Text = translatedText;
            
            // Reset fade and timer
            currentOpacity = 1.0;
            this.Opacity = 1.0;
            isFading = false;
            
            if (autoCloseTimer != null)
            {
                autoCloseTimer.Stop();
                if (!isPinned)
                {
                    autoCloseTimer.Start();
                }
            }
            
            // Bring to front
            this.TopMost = true;
            this.BringToFront();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(420, 280);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(
                Screen.PrimaryScreen.WorkingArea.Width - 440,
                Screen.PrimaryScreen.WorkingArea.Height - 300
            );
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(45, 45, 45);
            this.Opacity = 1.0;

            // Header panel for dragging
            headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 32;
            headerPanel.BackColor = Color.FromArgb(30, 30, 30);
            headerPanel.MouseDown += Header_MouseDown;
            headerPanel.MouseMove += Header_MouseMove;
            headerPanel.MouseUp += Header_MouseUp;

            Label titleLabel = new Label();
            titleLabel.Text = "MoonLens";
            titleLabel.ForeColor = Color.White;
            titleLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            titleLabel.Location = new Point(10, 6);
            titleLabel.AutoSize = true;
            headerPanel.Controls.Add(titleLabel);

            // Pin button
            pinButton = new Button();
            pinButton.Text = "\ud83d\udccc";
            pinButton.Size = new Size(28, 24);
            pinButton.Location = new Point(this.Width - 90, 4);
            pinButton.FlatStyle = FlatStyle.Flat;
            pinButton.FlatAppearance.BorderSize = 0;
            pinButton.BackColor = Color.FromArgb(30, 30, 30);
            pinButton.ForeColor = Color.Gray;
            pinButton.Click += PinButton_Click;
            headerPanel.Controls.Add(pinButton);

            // Copy button
            copyButton = new Button();
            copyButton.Text = "\ud83d\udccb";
            copyButton.Size = new Size(28, 24);
            copyButton.Location = new Point(this.Width - 60, 4);
            copyButton.FlatStyle = FlatStyle.Flat;
            copyButton.FlatAppearance.BorderSize = 0;
            copyButton.BackColor = Color.FromArgb(30, 30, 30);
            copyButton.ForeColor = Color.White;
            copyButton.Click += CopyButton_Click;
            headerPanel.Controls.Add(copyButton);

            // Close button
            closeButton = new Button();
            closeButton.Text = "\u2715";
            closeButton.Size = new Size(28, 24);
            closeButton.Location = new Point(this.Width - 30, 4);
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.BackColor = Color.FromArgb(30, 30, 30);
            closeButton.ForeColor = Color.White;
            closeButton.Click += CloseButton_Click;
            headerPanel.Controls.Add(closeButton);

            // Text box with scroll
            textBox = new RichTextBox();
            textBox.Text = translatedText;
            textBox.ForeColor = Color.White;
            textBox.BackColor = Color.FromArgb(45, 45, 45);
            textBox.Font = new Font("Segoe UI", 11);
            textBox.Location = new Point(12, 40);
            textBox.Size = new Size(this.Width - 24, this.Height - 52);
            textBox.BorderStyle = BorderStyle.None;
            textBox.ReadOnly = true;
            textBox.ScrollBars = RichTextBoxScrollBars.Vertical;

            this.Controls.Add(headerPanel);
            this.Controls.Add(textBox);

            // Fade timer
            fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 30;
            fadeTimer.Tick += FadeTimer_Tick;

            // Auto close after 10 seconds if not pinned
            autoCloseTimer = new System.Windows.Forms.Timer();
            autoCloseTimer.Interval = 8000;
            autoCloseTimer.Tick += AutoCloseTimer_Tick;
            autoCloseTimer.Start();

            // Escape to close
            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) StartFadeOut();
            };
        }

        private void AutoCloseTimer_Tick(object sender, EventArgs e)
        {
            if (!isPinned)
            {
                autoCloseTimer.Stop();
                StartFadeOut();
            }
        }

        private void StartFadeOut()
        {
            if (!isFading)
            {
                isFading = true;
                fadeTimer.Start();
            }
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            currentOpacity -= 0.05;
            if (currentOpacity <= 0)
            {
                fadeTimer.Stop();
                isClosing = true;
                this.Close();
            }
            else
            {
                this.Opacity = currentOpacity;
            }
        }

        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragOffset = e.Location;
            }
        }

        private void Header_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = this.PointToScreen(e.Location);
                this.Location = new Point(newLocation.X - dragOffset.X, newLocation.Y - dragOffset.Y);
            }
        }

        private void Header_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void PinButton_Click(object sender, EventArgs e)
        {
            isPinned = !isPinned;
            pinButton.ForeColor = isPinned ? Color.Yellow : Color.Gray;
            
            if (isPinned)
            {
                autoCloseTimer.Stop();
                isFading = false;
                fadeTimer.Stop();
                currentOpacity = 1.0;
                this.Opacity = 1.0;
            }
            else
            {
                autoCloseTimer.Start();
            }
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(translatedText);
            copyButton.Text = "\u2713";
            copyButton.ForeColor = Color.LightGreen;
            System.Windows.Forms.Timer resetTimer = new System.Windows.Forms.Timer();
            resetTimer.Interval = 1500;
            resetTimer.Tick += (s, ev) => {
                copyButton.Text = "\ud83d\udccb";
                copyButton.ForeColor = Color.White;
                resetTimer.Stop();
            };
            resetTimer.Start();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            StartFadeOut();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isClosing = true;
            if (pipeServer != null)
            {
                try { pipeServer.Close(); } catch { }
            }
            base.OnFormClosing(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }
    }

    class Program
    {
        static Mutex mutex;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: TranslationPopup.exe \"text to display\"");
                return;
            }

            string text = string.Join(" ", args);
            
            // Check if popup already exists
            bool createdNew;
            mutex = new Mutex(true, "MoonLensPopupMutex", out createdNew);
            
            if (!createdNew)
            {
                // Popup already running, send text via pipe
                try
                {
                    using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "MoonLensPopupPipe", PipeDirection.Out))
                    {
                        pipeClient.Connect(1000);
                        using (StreamWriter writer = new StreamWriter(pipeClient))
                        {
                            writer.Write(text);
                            writer.Flush();
                        }
                    }
                }
                catch { }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PopupForm(text));
            
            mutex.ReleaseMutex();
        }
    }
}

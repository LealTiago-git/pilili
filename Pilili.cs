// Toca o som da urna a cada Enter, em qualquer programa. Fica na bandeja do sistema.
// Compilar (gera um Pilili.exe único, com som e ícone embutidos):
// C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /out:Pilili.exe /win32icon:pilili.ico /resource:fim-som-da-urna.mp3,som.mp3 /resource:pilili.ico,pilili.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll Pilili.cs
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

static class Program
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

    [STAThread]
    static void Main()
    {
        bool isFirstInstance;
        using (new Mutex(true, "PililiEnterSom", out isFirstInstance))
        {
            if (!isFirstInstance) return;

            SetProcessDPIAware();
            Sound.Open();
            EnterKeyHook.Install(Sound.Play);

            var panel = new TrayPanel();
            var tray = new NotifyIcon { Icon = LoadIcon(SystemInformation.SmallIconSize.Width), Text = "Pilili", Visible = true };
            tray.MouseUp += (sender, e) => panel.Toggle();

            Application.Run();

            tray.Visible = false;
            EnterKeyHook.Uninstall();
            Sound.Close();
        }
    }

    public static Stream Resource(string name)
    {
        return typeof(Program).Assembly.GetManifestResourceStream(name);
    }

    public static Icon LoadIcon(int size)
    {
        using (var stream = Resource("pilili.ico")) return new Icon(stream, size, size);
    }
}

static class Sound
{
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)] static extern int mciSendString(string command, StringBuilder result, int length, IntPtr callback);

    static readonly string Mp3Path = Path.Combine(Path.GetTempPath(), "fim-som-da-urna.mp3");
    static readonly string VolumeFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PililiVolume.txt");

    public static int Volume { get; private set; }

    public static void Open()
    {
        // O MCI só toca arquivos, então o mp3 embutido é copiado para a pasta temp.
        using (var mp3 = Program.Resource("som.mp3"))
        using (var file = File.Create(Mp3Path)) mp3.CopyTo(file);

        Mci("open \"" + Mp3Path + "\" type mpegvideo alias som");
        SetVolume(ReadSavedVolume());
    }

    public static void Play() { Mci("play som from 0"); }

    public static void Close() { Mci("close som"); }

    public static void SetVolume(int percent)
    {
        Volume = Math.Max(0, Math.Min(100, percent));
        Mci("setaudio som volume to " + Volume * 10); // MCI vai de 0 a 1000
        SaveVolume();
    }

    // Salva a cada mudança, então fechar, travar ou desligar o PC não perde o volume.
    // Grava num .tmp, força ir ao disco e só então troca: queda de energia no meio não corrompe o arquivo.
    static void SaveVolume()
    {
        string temp = VolumeFile + ".tmp";
        try
        {
            byte[] content = Encoding.ASCII.GetBytes(Volume.ToString());
            using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 16, FileOptions.WriteThrough))
            {
                file.Write(content, 0, content.Length);
                file.Flush(true);
            }
            if (File.Exists(VolumeFile)) File.Replace(temp, VolumeFile, null);
            else File.Move(temp, VolumeFile);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    static int ReadSavedVolume()
    {
        int saved;
        try { return int.TryParse(File.ReadAllText(VolumeFile), out saved) ? saved : 100; }
        catch (IOException) { return 100; }
    }

    static void Mci(string command) { mciSendString(command, null, 0, IntPtr.Zero); }
}

static class EnterKeyHook
{
    delegate IntPtr HookProc(int code, IntPtr message, IntPtr keyInfo);
    [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int hookType, HookProc callback, IntPtr module, uint threadId);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr keyInfo);
    [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string name);

    const int LowLevelKeyboard = 13, KeyDown = 0x100, KeyUp = 0x101, SysKeyDown = 0x104, SysKeyUp = 0x105, EnterKey = 0x0D;

    static readonly HookProc callback = OnKey; // mantém o delegate vivo enquanto o Windows o chama
    static IntPtr hook;
    static Action onPress;
    static bool isHeld;

    public static void Install(Action action)
    {
        onPress = action;
        hook = SetWindowsHookEx(LowLevelKeyboard, callback, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
    }

    public static void Uninstall() { UnhookWindowsHookEx(hook); }

    static IntPtr OnKey(int code, IntPtr message, IntPtr keyInfo)
    {
        if (code >= 0 && Marshal.ReadInt32(keyInfo) == EnterKey)
        {
            int msg = (int)message;
            bool pressed = msg == KeyDown || msg == SysKeyDown;
            if (pressed && !isHeld) onPress(); // segurar Enter não repete o som
            if (pressed || msg == KeyUp || msg == SysKeyUp) isHeld = pressed;
        }
        return CallNextHookEx(IntPtr.Zero, code, message, keyInfo);
    }
}

class Palette
{
    public Color Background, Text, SubText, Divider, Hover, Rail, Knob, Accent;

    public static readonly Palette Light = new Palette
    {
        Background = Color.FromArgb(249, 249, 249), Text = Color.FromArgb(26, 26, 26), SubText = Color.FromArgb(96, 96, 96),
        Divider = Color.FromArgb(229, 229, 229), Hover = Color.FromArgb(234, 234, 234), Rail = Color.FromArgb(200, 200, 200),
        Knob = Color.White, Accent = Color.FromArgb(46, 158, 79)
    };

    public static readonly Palette Dark = new Palette
    {
        Background = Color.FromArgb(44, 44, 44), Text = Color.White, SubText = Color.FromArgb(160, 160, 160),
        Divider = Color.FromArgb(64, 64, 64), Hover = Color.FromArgb(58, 58, 58), Rail = Color.FromArgb(96, 96, 96),
        Knob = Color.FromArgb(69, 69, 69), Accent = Color.FromArgb(76, 194, 110)
    };

    public static Palette FromWindowsTheme()
    {
        object light = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", 1);
        return light is int && (int)light == 0 ? Dark : Light;
    }
}

// Painel da bandeja no estilo do Windows 11. Coordenadas em 96 dpi, escaladas para a tela.
class TrayPanel : Form
{
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);

    const int PanelWidth = 300, PanelHeight = 216, KnobRadius = 10;
    const int DwmCornerPreference = 33, DwmRoundCorners = 2, DwmBorderColor = 34, DropShadow = 0x20000;

    static readonly RectangleF VolumeSlider = new RectangleF(52, 94, 232, 28);
    static readonly RectangleF TestButton = new RectangleF(6, 136, 288, 36);
    static readonly RectangleF QuitButton = new RectangleF(6, 174, 288, 36);

    readonly float scale;
    readonly Bitmap logo = Program.LoadIcon(64).ToBitmap();
    readonly Font titleFont = new Font("Segoe UI Semibold", 11f), textFont = new Font("Segoe UI", 9.5f),
        smallFont = new Font("Segoe UI", 8.5f), iconFont = new Font("Segoe Fluent Icons", 12f);
    Palette colors = Palette.Light;
    RectangleF hovered;
    bool draggingVolume;
    DateTime hiddenAt;

    public TrayPanel()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        KeyPreview = true;
        DoubleBuffered = true;
        using (var screen = Graphics.FromHwnd(IntPtr.Zero)) scale = screen.DpiX / 96f;
        ClientSize = new Size((int)(PanelWidth * scale), (int)(PanelHeight * scale));
    }

    protected override CreateParams CreateParams
    {
        get { var p = base.CreateParams; p.ClassStyle |= DropShadow; return p; }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int round = DwmRoundCorners;
        DwmSetWindowAttribute(Handle, DwmCornerPreference, ref round, 4);
    }

    public void Toggle()
    {
        // Clicar no ícone com o painel aberto já o fecha via Deactivate; não reabrir em seguida.
        bool justClosed = (DateTime.Now - hiddenAt).TotalMilliseconds < 250;
        if (Visible || justClosed) { Hide(); return; }

        colors = Palette.FromWindowsTheme();
        BackColor = colors.Background;
        int border = ColorTranslator.ToWin32(colors.Divider);
        DwmSetWindowAttribute(Handle, DwmBorderColor, ref border, 4);

        Location = PositionNearCursor();
        hovered = RectangleF.Empty;
        Show();
        Activate();
        SetForegroundWindow(Handle);
    }

    Point PositionNearCursor()
    {
        Point cursor = Cursor.Position;
        Rectangle area = Screen.FromPoint(cursor).WorkingArea;
        int x = Math.Max(area.Left, Math.Min(cursor.X - Width / 2, area.Right - Width - 8));
        int y = Math.Max(area.Top, Math.Min(cursor.Y - Height - 8, area.Bottom - Height - 8));
        return new Point(x, y);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        draggingVolume = false;
        Hide();
        hiddenAt = DateTime.Now;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) Hide();
        if (e.KeyCode == Keys.Left) ChangeVolume(Sound.Volume - 5);
        if (e.KeyCode == Keys.Right) ChangeVolume(Sound.Volume + 5);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (!VolumeSlider.Contains(ToPanel(e))) return;
        draggingVolume = Capture = true;
        SetVolumeAt(ToPanel(e).X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (draggingVolume) SetVolumeAt(ToPanel(e).X);
        else UpdateHover(ToPanel(e));
    }

    protected override void OnMouseLeave(EventArgs e) { UpdateHover(PointF.Empty); }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        PointF point = ToPanel(e);
        if (draggingVolume) { draggingVolume = Capture = false; Sound.Play(); }
        else if (TestButton.Contains(point)) Sound.Play();
        else if (QuitButton.Contains(point)) Application.Exit();
    }

    PointF ToPanel(MouseEventArgs e) { return new PointF(e.X / scale, e.Y / scale); }

    void UpdateHover(PointF point)
    {
        RectangleF target = TestButton.Contains(point) ? TestButton : QuitButton.Contains(point) ? QuitButton : RectangleF.Empty;
        Cursor = target.IsEmpty && !VolumeSlider.Contains(point) ? Cursors.Default : Cursors.Hand;
        if (target == hovered) return;
        hovered = target;
        Invalidate();
    }

    void SetVolumeAt(float x)
    {
        float railStart = VolumeSlider.Left + KnobRadius, railLength = VolumeSlider.Width - 2 * KnobRadius;
        ChangeVolume((int)Math.Round((x - railStart) / railLength * 100));
    }

    void ChangeVolume(int percent)
    {
        Sound.SetVolume(percent);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.ScaleTransform(scale, scale);

        DrawHeader(g);
        DrawDivider(g, 62);
        DrawVolume(g);
        DrawDivider(g, 130);
        DrawButton(g, TestButton, "\uE768", "Testar som");
        DrawButton(g, QuitButton, "\uE7E8", "Sair");
    }

    void DrawHeader(Graphics g)
    {
        g.DrawImage(logo, 16, 16, 32, 32);
        DrawText(g, "Pilili", titleFont, colors.Text, 58, 12);
        FillCircle(g, colors.Accent, 63, 42, 3.5f);
        DrawText(g, "Ativo  ·  pressione Enter para tocar", smallFont, colors.SubText, 70, 34);
    }

    void DrawVolume(Graphics g)
    {
        DrawText(g, "Volume", smallFont, colors.SubText, 16, 72);
        using (var alignRight = new StringFormat { Alignment = StringAlignment.Far })
        using (var brush = new SolidBrush(colors.Text))
            g.DrawString(Sound.Volume + "%", smallFont, brush, PanelWidth - 16, 72, alignRight);

        string speaker = Sound.Volume == 0 ? "\uE74F" : "\uE767";
        DrawText(g, speaker, iconFont, colors.Text, 18, 98);

        float centerY = VolumeSlider.Top + VolumeSlider.Height / 2;
        float railStart = VolumeSlider.Left + KnobRadius, railLength = VolumeSlider.Width - 2 * KnobRadius;
        float knobX = railStart + railLength * Sound.Volume / 100f;
        FillRounded(g, colors.Rail, new RectangleF(railStart, centerY - 2, railLength, 4), 2);
        FillRounded(g, colors.Accent, new RectangleF(railStart, centerY - 2, knobX - railStart, 4), 2);
        FillCircle(g, colors.Knob, knobX, centerY, KnobRadius);
        using (var outline = new Pen(colors.Divider)) g.DrawEllipse(outline, knobX - KnobRadius, centerY - KnobRadius, 2 * KnobRadius, 2 * KnobRadius);
        FillCircle(g, colors.Accent, knobX, centerY, 6);
    }

    void DrawButton(Graphics g, RectangleF area, string icon, string label)
    {
        if (area == hovered) FillRounded(g, colors.Hover, area, 4);
        DrawText(g, icon, iconFont, colors.Text, area.X + 10, area.Y + 9);
        DrawText(g, label, textFont, colors.Text, area.X + 42, area.Y + 8);
    }

    void DrawDivider(Graphics g, float y)
    {
        using (var pen = new Pen(colors.Divider)) g.DrawLine(pen, 0, y, PanelWidth, y);
    }

    static void DrawText(Graphics g, string text, Font font, Color color, float x, float y)
    {
        using (var brush = new SolidBrush(color)) g.DrawString(text, font, brush, x, y);
    }

    static void FillCircle(Graphics g, Color color, float centerX, float centerY, float radius)
    {
        using (var brush = new SolidBrush(color)) g.FillEllipse(brush, centerX - radius, centerY - radius, 2 * radius, 2 * radius);
    }

    static void FillRounded(Graphics g, Color color, RectangleF area, float radius)
    {
        if (area.Width <= 0) return;
        float diameter = Math.Min(2 * radius, Math.Min(area.Width, area.Height));
        using (var shape = new GraphicsPath())
        using (var brush = new SolidBrush(color))
        {
            shape.AddArc(area.X, area.Y, diameter, diameter, 180, 90);
            shape.AddArc(area.Right - diameter, area.Y, diameter, diameter, 270, 90);
            shape.AddArc(area.Right - diameter, area.Bottom - diameter, diameter, diameter, 0, 90);
            shape.AddArc(area.X, area.Bottom - diameter, diameter, diameter, 90, 90);
            g.FillPath(brush, shape);
        }
    }
}

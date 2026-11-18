namespace FoxBatteryTaskbar;

/// <summary>Asks the user for their Fox Cloud API key.</summary>
public sealed class ApiKeyDialog : Form
{
    readonly TextBox keyBox = new() { Width = 360, UseSystemPasswordChar = true };

    public string ApiKey => keyBox.Text.Trim();

    public ApiKeyDialog()
    {
        Text = "Fox Battery Taskbar – API key";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = MinimizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        var help = new LinkLabel
        {
            Text = "Generate a key at foxesscloud.com → User Profile → API Management.",
            AutoSize = true,
            MaximumSize = new Size(360, 0),
        };
        help.LinkArea = new LinkArea(help.Text.IndexOf("foxesscloud.com", StringComparison.Ordinal), "foxesscloud.com".Length);
        help.LinkClicked += (_, _) => OpenUrl("https://www.foxesscloud.com/login");

        var show = new CheckBox { Text = "Show key", AutoSize = true };
        show.CheckedChanged += (_, _) => keyBox.UseSystemPasswordChar = !show.Checked;

        var fromFile = new Button { Text = "Load from file…", AutoSize = true };
        fromFile.Click += (_, _) => LoadFromFile();

        var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        AcceptButton = ok;
        CancelButton = cancel;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.AddRange([cancel, ok, fromFile]);

        var layout = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(help);
        layout.Controls.Add(keyBox);
        layout.Controls.Add(show);
        layout.Controls.Add(buttons);
        Controls.Add(layout);
    }

    void LoadFromFile()
    {
        using var dlg = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*" };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            keyBox.Text = File.ReadAllText(dlg.FileName).Trim();
    }

    internal static void OpenUrl(string url) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
}

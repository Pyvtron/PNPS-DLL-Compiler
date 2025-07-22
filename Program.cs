using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WinFormsLabel = System.Windows.Forms.Label;
using System.Drawing;

public class MainForm : Form
{
    private TabControl tabControl;
    private TabPage tabCompile;
    private TabPage tabInfo;
    private TabPage tabLanguage;

    private Button btnSelectFile;
    private TextBox txtFilePath;
    private Button btnCompile;
    private TextBox txtOutput;
    private WinFormsLabel lblStatus;

    private WinFormsLabel lblInfo;
    private ComboBox languageSelector;

    private Dictionary<string, Dictionary<string, string>> translations;
    private string currentLanguage = "en";
    private const string settingsFile = "settings.json";

    public MainForm()
    {
        LoadLanguageSetting();
        InitializeTranslations();

        this.Text = "DLL Compiler";
        this.Width = 600;
        this.Height = 400;

        try
        {
            this.Icon = new Icon("Icon.ico");
        }
        catch
        {

        }

        tabControl = new TabControl { Dock = DockStyle.Fill };

        tabCompile = new TabPage();
        btnSelectFile = new Button { Left = 10, Top = 10, Width = 120 };
        btnSelectFile.Click += BtnSelectFile_Click;

        txtFilePath = new TextBox { Left = 140, Top = 12, Width = 430 };

        btnCompile = new Button { Left = 10, Top = 50, Width = 120 };
        btnCompile.Click += BtnCompile_Click;

        lblStatus = new WinFormsLabel { Left = 140, Top = 55, Width = 430 };

        txtOutput = new TextBox
        {
            Left = 10,
            Top = 90,
            Width = 560,
            Height = 180,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true
        };

        tabCompile.Controls.AddRange(new Control[] { btnSelectFile, txtFilePath, btnCompile, lblStatus, txtOutput });

        tabInfo = new TabPage();

        lblInfo = new WinFormsLabel
        {
            AutoSize = true,
            Left = 10,
            Top = 10
        };

        tabInfo.Controls.Add(lblInfo);

        tabLanguage = new TabPage();

        languageSelector = new ComboBox
        {
            Left = 10,
            Top = 10,
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        languageSelector.Items.AddRange(new string[] { "Deutsch", "English" });
        languageSelector.SelectedIndexChanged += LanguageSelector_SelectedIndexChanged;
        tabLanguage.Controls.Add(languageSelector);

        tabControl.TabPages.AddRange(new TabPage[] { tabCompile, tabInfo, tabLanguage });
        this.Controls.Add(tabControl);

        ApplyLanguage();
    }

    private void BtnSelectFile_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog();
        ofd.Filter = Translate("openfile_filter");
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            txtFilePath.Text = ofd.FileName;
            lblStatus.Text = Translate("status_file_selected");
            txtOutput.Clear();
        }
    }

    private void BtnCompile_Click(object? sender, EventArgs e)
    {
        string inputFile = txtFilePath.Text;
        if (!File.Exists(inputFile))
        {
            MessageBox.Show(Translate("error_file_not_found"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var sfd = new SaveFileDialog();
        sfd.Filter = Translate("savefile_filter");
        sfd.FileName = Path.GetFileNameWithoutExtension(inputFile) + ".dll";

        if (sfd.ShowDialog() != DialogResult.OK) return;

        string outputDll = sfd.FileName;
        lblStatus.Text = Translate("status_compiling");
        txtOutput.Clear();

        try
        {
            string sourceCode = File.ReadAllText(inputFile);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToList();

            var compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(outputDll),
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var result = compilation.Emit(outputDll);

            if (result.Success)
            {
                lblStatus.Text = Translate("status_success");
                txtOutput.Text = Translate("output_success") + outputDll;
            }
            else
            {
                lblStatus.Text = Translate("status_failed");
                txtOutput.Text = string.Join("\r\n", result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString()));
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = Translate("status_exception");
            txtOutput.Text = ex.ToString();
        }
    }

    private void LanguageSelector_SelectedIndexChanged(object? sender, EventArgs e)
    {
        currentLanguage = languageSelector.SelectedIndex == 0 ? "de" : "en";
        SaveLanguageSetting();
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        tabCompile.Text = Translate("tab_compile");
        tabInfo.Text = Translate("tab_info");
        tabLanguage.Text = Translate("tab_language");

        btnSelectFile.Text = Translate("btn_select_file");
        btnCompile.Text = Translate("btn_compile");
        lblInfo.Text = Translate("info_text");
        lblStatus.Text = Translate("status_ready");

        languageSelector.SelectedIndex = currentLanguage == "de" ? 0 : 1;
    }

    private void InitializeTranslations()
    {
        translations = new Dictionary<string, Dictionary<string, string>>()
        {
            ["de"] = new Dictionary<string, string>()
            {
                ["tab_compile"] = "Kompilieren",
                ["tab_info"] = "Info",
                ["tab_language"] = "Sprache",
                ["btn_select_file"] = "Datei auswählen",
                ["btn_compile"] = "Kompilieren",
                ["status_ready"] = "Status: Bereit",
                ["status_file_selected"] = "Status: Datei ausgewählt",
                ["status_compiling"] = "Status: Kompiliere...",
                ["status_success"] = "Status: Kompiliert!",
                ["status_failed"] = "Status: Kompilierung fehlgeschlagen",
                ["status_exception"] = "Status: Ausnahme beim Kompilieren",
                ["error_file_not_found"] = "Die Datei existiert nicht.",
                ["output_success"] = "DLL erfolgreich erstellt:\r\n",
                ["info_text"] = "PNPS DLL Compiler\nVersion: 1.0.0\nAutor: Pyvtron\nQuelle: https://pyvtron-projects.netlify.app/\nWebsite: Pyvtron Projects\nLizenz: Alle Rechte Vorbehalten\nPNPS Bedeutung: Pyvtron Projects",
                ["openfile_filter"] = "C# Dateien (*.cs)|*.cs|Alle Dateien (*.*)|*.*",
                ["savefile_filter"] = "DLL-Dateien (Dynamic Link Library) (*.dll)|*.dll"
            },
            ["en"] = new Dictionary<string, string>()
            {
                ["tab_compile"] = "Compile",
                ["tab_info"] = "Info",
                ["tab_language"] = "Language",
                ["btn_select_file"] = "Select File",
                ["btn_compile"] = "Compile",
                ["status_ready"] = "Status: Ready",
                ["status_file_selected"] = "Status: File selected",
                ["status_compiling"] = "Status: Compiling...",
                ["status_success"] = "Status: Compiled!",
                ["status_failed"] = "Status: Compilation failed",
                ["status_exception"] = "Status: Exception while compiling",
                ["error_file_not_found"] = "The file does not exist.",
                ["output_success"] = "DLL successfully created:\r\n",
                ["info_text"] = "PNPS DLL Compiler\nVersion: 1.0.0\nAuthor: Pyvtron\nSource: https://pyvtron-projects.netlify.app/\nWebsite: Pyvtron Projects\nLicense: All Rights Reserved\nPNPS Meaning: Pyvtron Projects",
                ["openfile_filter"] = "C# Files (*.cs)|*.cs|All Files (*.*)|*.*",
                ["savefile_filter"] = "DLL (Dynamic Link Library) Files (*.dll)|*.dll"
            }
        };
    }

    private string Translate(string key)
    {
        if (translations.TryGetValue(currentLanguage, out var lang) && lang.TryGetValue(key, out var val))
            return val;
        return key;
    }

    private void SaveLanguageSetting()
    {
        File.WriteAllText(settingsFile, JsonSerializer.Serialize(new { language = currentLanguage }));
    }

    private void LoadLanguageSetting()
    {
        if (File.Exists(settingsFile))
        {
            try
            {
                var json = File.ReadAllText(settingsFile);
                var doc = JsonDocument.Parse(json);
                currentLanguage = doc.RootElement.GetProperty("language").GetString() ?? "en";
            }
            catch
            {
                currentLanguage = "en";
            }
        }
        else
        {
            currentLanguage = "en";
        }
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new MainForm());
    }
}
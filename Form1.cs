using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Launcher;
using FluentTransitions;
using MSession = CmlLib.Core.Auth.MSession;
using Essy.Tools.InputBox;
using CmlLib.Core.Version;
using CmlLib.Core.Installers;
using CmlLib.Core.Installer.Forge;
using System.IO.Compression;
using System.Text.Json.Nodes;
using Guna.UI2.WinForms;
using System.Net;

namespace Inferz_Launcher
{
    public partial class Form1 : Form
    {
        string selectedversion = "1.21.1";
        string username;
        Process minecraftProcess;
        int ram;
        string selectedjava = "java.exe";
        int offsetX = 8;
        int offsetY = 15;
        Dictionary<string, string>pathsxd = new Dictionary<string, string>();
        public Form1()
        {
            InitializeComponent();
        }

        private class ModInfo
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string IconBase64 { get; set; }
        }

        public void LoadBase64Image(string base64String, Guna2PictureBox pictureBox)
        {
            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64String);
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    Image img = Image.FromStream(ms);
                    pictureBox.Image = img;
                }
            }
            catch (Exception ex)
            {
                
            }
        }

        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            modsmanager.BringToFront();
            VERSIONS.BringToFront();
            string minecraftPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            string minecraftPath2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions");
            string pathxd = Path.Combine(minecraftPath, "InferzLauncher");
            if (!Directory.Exists(pathxd))
            {
                Directory.CreateDirectory(pathxd);
            }
            string pathxdx = Path.Combine(pathxd, "Usernames");
            if (!Directory.Exists(pathxdx))
            {
                Directory.CreateDirectory(pathxdx);
            }
            foreach (string file in Directory.GetFiles(pathxdx))
            {
                guna2ComboBox1.Items.Add(File.ReadAllText(file));
            }

            if (Directory.Exists(minecraftPath2))
            {
                foreach (string dir in Directory.GetDirectories(minecraftPath2))
                {
                    guna2ComboBox3.Items.Add(Path.GetFileName(dir));
                }
            }
            Dictionary<string, int> ramOptions = new Dictionary<string, int>()
{
    { "512 MB (minimal)", 512 },
    { "2 GB (very low)", 2048 },
    { "4 GB (low)", 4096 },
    { "6 GB (normal)", 6128 },
    { "8 GB (high)", 8192 },
    { "10 GB (super high)", 10240 }
};
            guna2ComboBox2.DataSource = new BindingSource(ramOptions, null);
            guna2ComboBox2.DisplayMember = "Key"; 
            guna2ComboBox2.ValueMember = "Value";
            guna2ComboBox2.SelectedIndex = 2;
            string pathxdx2 = Path.Combine(pathxd, "Javas");
            if (!Directory.Exists(pathxdx2))
            {
                Directory.CreateDirectory(pathxdx2);
            }
            foreach(string file in  Directory.GetFiles(pathxdx2))
            {
                guna2ComboBox4.Items.Add($"{Path.GetFileName(File.ReadAllText(file))} : {Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(File.ReadAllText(file)))))}");
                pathsxd.Add($"{Path.GetFileName(File.ReadAllText(file))} : {Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(File.ReadAllText(file)))))}", File.ReadAllText(file));
            }
        }
        private string GenerateOfflineUUID(string username)
        {
            // Genera un UUID Offline basado en el username (UUID v3)
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes("OfflinePlayer:" + username));
                return new Guid(hash).ToString();
            }
        }
        
        private void FormatLogMessage(string text, bool isError)
        {
            // Si es un error general, pintarlo en rojo y salir
            if (isError)
            {
                AppendTextToRichTextBox(text + "\n", Color.FromArgb(209, 44, 44));
                return;
            }

            // Expresión regular para detectar la hora y el tipo de log
            Match match = Regex.Match(text, @"^\[(\d{2}:\d{2}:\d{2})\] \[(.*?)\]: (.*)$");

            if (match.Success)
            {
                string time = match.Groups[1].Value;
                string logCategory = match.Groups[2].Value;
                string message = match.Groups[3].Value;

                // Agregar la hora con color verde claro
                AppendTextToRichTextBox($"[{time}] ", Color.FromArgb(3, 252, 157));

                // Agregar la categoría del log con color amarillo dorado
                AppendTextToRichTextBox($"[{logCategory}] ", Color.FromArgb(252, 227, 3));

                // Si es un error, pintarlo en rojo oscuro
                if (logCategory.Contains("ERROR"))
                {
                    AppendTextToRichTextBox($"{message}\n", Color.FromArgb(209, 44, 44));
                }
                else
                {
                    AppendTextToRichTextBox($"{message}\n", richTextBox1.ForeColor);
                }
            }
            else
            {
                // Si no encaja en el patrón, se muestra en color normal
                AppendTextToRichTextBox(text + "\n", richTextBox1.ForeColor);
            }
        }
        private void AppendColoredTextToRichTextBox(string text, bool isError = false)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(() => FormatLogMessage(text, isError)));
            }
            else
            {
                FormatLogMessage(text, isError);
            }
        }
        private void AppendTextToRichTextBox(string text, Color color)
        {
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.SelectionLength = 0;
            richTextBox1.SelectionColor = color;
            richTextBox1.AppendText(text);
            richTextBox1.SelectionColor = richTextBox1.ForeColor; // Restaurar color normal
        }

        private void guna2GradientButton1_Click(object sender, EventArgs e)
        {
            selectedversion = "1.21.1";
            label4.Text = $"VERSION {selectedversion}";
            guna2GradientButton16.Text = "SELECTED 1.21.1";
            guna2PictureBox17.Image = guna2PictureBox2.Image;
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton2_Click(object sender, EventArgs e)
        {
            selectedversion = "1.20.1";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox3.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton3_Click(object sender, EventArgs e)
        {
            selectedversion = "1.19";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox4.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton6_Click(object sender, EventArgs e)
        {
            selectedversion = "1.17";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox7.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton5_Click(object sender, EventArgs e)
        {
            selectedversion = "1.16.5";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox6.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton4_Click(object sender, EventArgs e)
        {
            selectedversion = "1.15";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox5.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton12_Click(object sender, EventArgs e)
        {
            selectedversion = "1.14.4";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox13.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton11_Click(object sender, EventArgs e)
        {
            selectedversion = "1.13.1";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox12.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton10_Click(object sender, EventArgs e)
        {
            selectedversion = "1.12.2";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox11.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton9_Click(object sender, EventArgs e)
        {
            selectedversion = "1.11.2";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox10.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton8_Click(object sender, EventArgs e)
        {
            selectedversion = "1.10.1";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox9.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton7_Click(object sender, EventArgs e)
        {
            selectedversion = "1.9.9";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox8.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton15_Click(object sender, EventArgs e)
        {
            selectedversion = "1.8.9";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox16.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2GradientButton14_Click(object sender, EventArgs e)
        {
            selectedversion = "1.7.9";
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox15.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            string minecraftPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            string pathxd = Path.Combine(minecraftPath, "InferzLauncher");
            if (!Directory.Exists(pathxd))
            {
                Directory.CreateDirectory(pathxd);
            }
            string pathxdx = Path.Combine(pathxd, "Usernames");
            if (!Directory.Exists(pathxdx))
            {
                Directory.CreateDirectory(pathxdx);
            }
            foreach (string file in Directory.GetFiles(pathxdx))
            {
                File.Delete(file);
            }
            int index = 0;
            foreach (var item in guna2ComboBox1.Items)
            {
                File.WriteAllText(Path.Combine(pathxdx, $"username-{index}"), item.ToString());
                index++;
            }
            Environment.Exit(0);
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void guna2GradientButton16_Click(object sender, EventArgs e)
        {
            VERSIONS.Visible = true;
        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            VERSIONS.Visible = false;
        }

        private void guna2GradientButton13_Click(object sender, EventArgs e)
        {
            string xdd = InputBox.ShowInputBox("Enter a version"); 
            if(!string.IsNullOrEmpty(xdd) && xdd.Contains("."))
            {
                selectedversion = xdd;
                guna2GradientButton16.Text = $"SELECTED {selectedversion}";
                guna2PictureBox17.Image = guna2PictureBox14.Image;
                label4.Text = $"VERSION {selectedversion}";
                guna2PictureBox18.Image = guna2PictureBox17.Image;
            }
            else
            {
                MessageBox.Show("Please enter a valid version.");
            }
        }

        private async void guna2Button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            if (string.IsNullOrEmpty(username))
            {
                AppendColoredTextToRichTextBox($"[ERROR] Please select a username first!", true);
                return;
            }
            string minecraftPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            var launcherPath = new MinecraftPath(minecraftPath);
            var launcher = new MinecraftLauncher(launcherPath);
            guna2Button1.Text = "Starting";
            string version = selectedversion;
            var byteProgress = new SyncProgress<ByteProgress>(ex =>
            {
                try
                {
                    AppendColoredTextToRichTextBox($"[{DateTime.Now.ToString("HH:mm:ss")}] [INFO]: {ex.ProgressedBytes / 1024 / 1024}mb/{ex.TotalBytes / 1024 / 1024}mb - {ex.ProgressedBytes / ex.TotalBytes * 100}% downloaded", false);
                }
                catch 
                {

                }
            });
                
            var installerOutput = new SyncProgress<string>(ex =>
                AppendColoredTextToRichTextBox($"[{DateTime.Now.ToString("HH:mm:ss")}] [INFO]: {ex}", false));
            Minecraft.Initialize(Environment.GetEnvironmentVariable("APPDATA") + "\\.minecraft"); // Initialize
            var versions = launcher.GetAllVersionsAsync();
            if (guna2CheckBox2.Checked)
            {
                var forge = new ForgeInstaller(launcher);
                var version_name = await forge.Install(version, new ForgeInstallOptions
                {
                    ByteProgress = byteProgress,
                    InstallerOutput = installerOutput,
                });
                version = version_name;
            }
            string jarPath = Path.Combine(minecraftPath, "versions", version, $"{version}.jar");
            

            // Suscribirse al progreso de instalación
            if (!File.Exists(jarPath))
            {
                launcher.ByteProgressChanged += (sende, ex) =>
                {
                    var percentage = (int)((double)ex.ProgressedBytes / ex.TotalBytes * 100);
                    var current = (ex.ProgressedBytes / 1024 / 1024);
                    var total = (ex.TotalBytes / 1024 / 1024);
                    var timeElapsed = DateTime.Now.ToString("HH:mm:ss");
                    AppendColoredTextToRichTextBox($"[{timeElapsed}] [INFO]: {current}mb/{total}mb - {percentage}% downloaded", false);
                };
                try
                {
                    foreach (var process in Process.GetProcesses())
                    {
                        try
                        {
                            if (process.MainModule.FileName.Contains("minecraft") || process.MainModule.FileName.Contains("java"))
                            {
                                process.Kill();
                                Console.WriteLine($"Proceso {process.ProcessName} terminado.");
                            }
                        }
                        catch { /* Ignorar procesos protegidos */ }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
                await launcher.InstallAsync(version);
                AppendColoredTextToRichTextBox($"[{DateTime.Now.ToString("HH:mm:ss")}] [INFO]: Downloaded correctly.", false);
            }


            string uuid = GenerateOfflineUUID(username);

            var session = new MSession(username, "fake", uuid);


            CmlLib.Core.ProcessBuilder.MLaunchOption option = new CmlLib.Core.ProcessBuilder.MLaunchOption() // set options
            {

                JavaPath = selectedjava, // javapath
                MaximumRamMb = ram, // maxram
                IsDemo = false,
                ExtraJvmArguments = new List<CmlLib.Core.ProcessBuilder.MArgument>
    {
        new CmlLib.Core.ProcessBuilder.MArgument("-Djava.class.loader=com.mojang.launcher.legacy.CompatClassLoader") // Agregar el argumento
    },
                Session = session
            };
            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        if (process.MainModule.FileName.Contains("minecraft") || process.MainModule.FileName.Contains("java"))
                        {
                            process.Kill();
                            Console.WriteLine($"Proceso {process.ProcessName} terminado.");
                        }
                    }
                    catch { /* Ignorar procesos protegidos */ }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            
            var profile = launcher.GetVersionAsync(version);
            minecraftProcess = launcher.BuildProcess(profile.Result, option);
            var processUtil = new ProcessWrapper(minecraftProcess);
            // Configuración de la ventana oculta y redirección de salida
            minecraftProcess.StartInfo.RedirectStandardOutput = true;
            minecraftProcess.StartInfo.RedirectStandardError = true;
            minecraftProcess.StartInfo.UseShellExecute = false;
            minecraftProcess.StartInfo.CreateNoWindow = true;
            processUtil.Process.StartInfo.CreateNoWindow = true;
            processUtil.Process.StartInfo.RedirectStandardOutput = true;
            processUtil.Process.StartInfo.RedirectStandardError = true;
            processUtil.Process.StartInfo.UseShellExecute = false;
            // Capturar salida estándar y errores
            processUtil.OutputReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args))
                    AppendColoredTextToRichTextBox(args);
            };

            minecraftProcess.ErrorDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    AppendColoredTextToRichTextBox("[ERROR] " + args.Data, isError: true);
            };

            processUtil.StartWithEvents();
            
            AppendColoredTextToRichTextBox($"[{DateTime.Now.ToString("HH:mm:ss")}] [INFO]: Starting...", false);
            guna2Button1.Text = "Running";
            // Esperar a que el proceso finalice y detectar crasheo
            new System.Threading.Thread(() =>
            {
                minecraftProcess.WaitForExit();

                if (minecraftProcess.ExitCode != 0)
                {
                    AppendColoredTextToRichTextBox("[ERROR] " + "Minecraft crashed!", isError: true);
                }
                else
                {
                    richTextBox1.Invoke(new Action(() => richTextBox1.Clear()));
                    AppendColoredTextToRichTextBox($"[{DateTime.Now.ToString("HH:mm:ss")}] [INFO]: Minecraft closed.", false);
                }
                guna2Button1.Invoke(new Action(() => guna2Button1.Text = "Play"));
            }).Start();
        }

        private void guna2Button6_Click(object sender, EventArgs e)
        {
            string xdd = InputBox.ShowInputBox("Enter a username");
            if (!string.IsNullOrEmpty(xdd))
            {
                guna2ComboBox1.Items.Add(xdd);
            }
            else
            {
                MessageBox.Show("Please enter a valid username.");
            }
        }

        private void guna2ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            username = guna2ComboBox1.SelectedItem.ToString();
        }

        private void guna2CheckBox3_CheckedChanged(object sender, EventArgs e)
        {
            guna2ComboBox2.Enabled = !guna2CheckBox3.Checked;
            if(guna2CheckBox3.Checked == true)
            {
                ram = 4096;
            }
        }

        private void guna2ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            ram = (int)((KeyValuePair<string, int>)guna2ComboBox2.SelectedItem).Value;
        }

        private void guna2Button5_Click(object sender, EventArgs e)
        {
            minecraftProcess.Kill();
        }

        private void guna2Panel3_Paint(object sender, PaintEventArgs e)
        {

        }
        public string GetModVersionFromJar(string modPath)
        {
            if (!File.Exists(modPath) || Path.GetExtension(modPath).ToLower() != ".jar")
                return null;

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(modPath))
                {
                    // Buscar mods.toml (Forge)
                    var modsToml = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("mods.toml", StringComparison.OrdinalIgnoreCase));
                    if (modsToml != null)
                    {
                        using (StreamReader reader = new StreamReader(modsToml.Open()))
                        {
                            while (!reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                // Se acepta version= o version =, y se capturan caracteres alfanuméricos, punto y guion
                                Match match = Regex.Match(line, @"version\s*=\s*[""'](?<modVer>[\w\.\-]+)[""']");
                                if (match.Success)
                                    return match.Groups["modVer"].Value; // Ejemplo: "1.6.15a"
                            }
                        }
                    }

                    // Buscar fabric.mod.json (Fabric)
                    var fabricJson = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("fabric.mod.json", StringComparison.OrdinalIgnoreCase));
                    if (fabricJson != null)
                    {
                        using (StreamReader reader = new StreamReader(fabricJson.Open()))
                        {
                            string jsonContent = reader.ReadToEnd();
                            var jsonDoc = JsonNode.Parse(jsonContent);
                            if (jsonDoc != null && jsonDoc["version"] != null)
                            {
                                return jsonDoc["version"].ToString(); // Ejemplo: "2.2.1-a"
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer el mod: {ex.Message}");
            }

            return null;
        }

        public string GetMinecraftVersionFromMod(string modPath)
        {
            if (!File.Exists(modPath) || Path.GetExtension(modPath).ToLower() != ".jar")
                return null;
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(modPath))
                {
                    // Buscar mods.toml (Forge)
                    var modsToml = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("mods.toml", StringComparison.OrdinalIgnoreCase));
                    if (modsToml != null)
                    {
                        using (StreamReader reader = new StreamReader(modsToml.Open()))
                        {
                            string content = reader.ReadToEnd();
                            // Expresión regular para capturar versionRange de la dependencia minecraft.
                            // Captura "min" y opcionalmente "max" (por ejemplo: [1.20,1.21) o [1.20.1])
                            Regex regex = new Regex(
                        @"modId\s*=\s*[""']minecraft[""'][\s\S]*?versionRange\s*=\s*[""']\[(?<min>[0-9.]+)(?:\s*,\s*(?<max>[0-9.]*))?(?:\)|\])[""']",
                        RegexOptions.IgnoreCase);
                            Match match = regex.Match(content);
                            if (match.Success)
                            {
                                string minVer = match.Groups["min"].Value.Trim();
                                if (match.Groups["max"].Success)
                                {
                                    string maxVer = match.Groups["max"].Value.Trim();
                                    if(string.IsNullOrEmpty(maxVer))
                                    {
                                        return minVer;
                                    }
                                    return $"{minVer} - {maxVer}";
                                }
                                else
                                {
                                    return minVer;
                                }
                            }
                        }
                    }

                    // Buscar fabric.mod.json (Fabric)
                    var fabricJson = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("fabric.mod.json", StringComparison.OrdinalIgnoreCase));
                    if (fabricJson != null)
                    {
                        using (StreamReader reader = new StreamReader(fabricJson.Open()))
                        {
                            string jsonContent = reader.ReadToEnd();
                            var jsonDoc = JsonNode.Parse(jsonContent);
                            if (jsonDoc != null && jsonDoc["depends"]?["minecraft"] != null)
                            {
                                var mcDepends = jsonDoc["depends"]["minecraft"];
                                if (mcDepends is JsonArray arr)
                                {
                                    if (arr.Count >= 2)
                                    {
                                        string first = arr[0]?.ToString()?.Trim();
                                        string second = arr[1]?.ToString()?.Trim();
                                        return $"{first} - {second}";
                                    }
                                    else if (arr.Count == 1)
                                    {
                                        return arr[0]?.ToString()?.Trim();
                                    }
                                }
                                else
                                {
                                    return mcDepends.ToString().Trim();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer el mod: {ex.Message}");
            }
            return null;
        }

        public async Task<(string minver, string maxver)> GetTwoVersions(string modPath)
        {
            if (!File.Exists(modPath) || Path.GetExtension(modPath).ToLower() != ".jar")
                return (null, null);
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(modPath))
                {
                    // Buscar mods.toml (Forge)
                    var modsToml = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("mods.toml", StringComparison.OrdinalIgnoreCase));
                    if (modsToml != null)
                    {
                        using (StreamReader reader = new StreamReader(modsToml.Open()))
                        {
                            string content = reader.ReadToEnd();
                            // Expresión regular para capturar versionRange de la dependencia minecraft.
                            // Captura "min" y opcionalmente "max" (por ejemplo: [1.20,1.21) o [1.20.1])
                            Regex regex = new Regex(
                        @"modId\s*=\s*[""']minecraft[""'][\s\S]*?versionRange\s*=\s*[""']\[(?<min>[0-9.]+)(?:\s*,\s*(?<max>[0-9.]*))?(?:\)|\])[""']",
                        RegexOptions.IgnoreCase);
                            Match match = regex.Match(content);
                            if (match.Success)
                            {
                                string minVer = match.Groups["min"].Value.Trim();
                                if (match.Groups["max"].Success)
                                {
                                    string maxVer = match.Groups["max"].Value.Trim();
                                    if (string.IsNullOrEmpty(maxVer))
                                    {
                                        return (minVer, null);
                                    }
                                    return (minVer, maxVer);
                                }
                                else
                                {
                                    return (minVer, null);
                                }
                            }
                        }
                    }

                    // Buscar fabric.mod.json (Fabric)
                    var fabricJson = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("fabric.mod.json", StringComparison.OrdinalIgnoreCase));
                    if (fabricJson != null)
                    {
                        using (StreamReader reader = new StreamReader(fabricJson.Open()))
                        {
                            string jsonContent = reader.ReadToEnd();
                            var jsonDoc = JsonNode.Parse(jsonContent);
                            if (jsonDoc != null && jsonDoc["depends"]?["minecraft"] != null)
                            {
                                var mcDepends = jsonDoc["depends"]["minecraft"];
                                if (mcDepends is JsonArray arr)
                                {
                                    if (arr.Count >= 2)
                                    {
                                        string first = arr[0]?.ToString()?.Trim();
                                        string second = arr[1]?.ToString()?.Trim();
                                        return (first, second);
                                    }
                                    else if (arr.Count == 1)
                                    {
                                        return (arr[0]?.ToString()?.Trim(), null);
                                    }
                                }
                                else
                                {
                                    return (mcDepends.ToString().Trim(), null);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer el mod: {ex.Message}");
            }
            return (null, null);
        }

        private string GetModLoaderType(string modPath)
        {
            if (!File.Exists(modPath) || Path.GetExtension(modPath).ToLower() != ".jar")
                return null;

            bool isForge = false;
            bool isFabric = false;

            try
            {
                using (var archive = ZipFile.OpenRead(modPath))
                {
                    isForge = archive.Entries.Any(e => e.FullName.EndsWith("mods.toml", StringComparison.OrdinalIgnoreCase));
                    isFabric = archive.Entries.Any(e => e.FullName.EndsWith("fabric.mod.json", StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }

            if (isForge && isFabric)
                return "forge/fabric";
            else if (isForge)
                return "forge";
            else if (isFabric)
                return "fabric";
            else
                return "unknown";
        }


        public async Task<bool> IsVersionCompatible(string modPath, string userVersion)
        {
            var(modVersion1, moversion2) = await GetTwoVersions(modPath);
            if (string.IsNullOrEmpty(modVersion1) || string.IsNullOrEmpty(userVersion))
                return false;
            try
            {
                if(!string.IsNullOrEmpty(moversion2))
                {
                    Version mod1 = new Version(modVersion1);
                    Version mod2 = new Version(moversion2);
                    Version user = new Version(userVersion);
                    Console.WriteLine($"{mod1} {mod2} {user}");
                    return user >= mod1 && user <= mod2;

                }
                else
                {
                    Version mod1 = new Version(modVersion1);
                    Version user = new Version(userVersion);
                    return mod1 == user;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }


        private ModInfo GetInfo(string modPath)
        {
            if (!File.Exists(modPath) || Path.GetExtension(modPath).ToLower() != ".jar")
                return null;
            ModInfo info = new ModInfo();
            using (ZipArchive archive = ZipFile.OpenRead(modPath))
            {
                var modsToml = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("mods.toml", StringComparison.OrdinalIgnoreCase));
                if (modsToml != null)
                {
                    string tomlContent;
                    using (var reader = new StreamReader(modsToml.Open()))
                    {
                        tomlContent = reader.ReadToEnd();
                    }
                    var titleMatch = Regex.Match(tomlContent, @"displayName\s*=\s*[""'](?<title>.*?)[""']");
                    info.Title = titleMatch.Success ? titleMatch.Groups["title"].Value.Trim() : null;
                    var descMatch = Regex.Match(tomlContent, @"description\s*=\s*'''(?<desc>[\s\S]*?)'''");
                    if (!descMatch.Success)
                        descMatch = Regex.Match(tomlContent, @"description\s*=\s*[""'](?<desc>.*?)[""']");
                    info.Description = descMatch.Success ? descMatch.Groups["desc"].Value.Trim() : null;
                    var logoMatch = Regex.Match(tomlContent, @"logoFile\s*=\s*[""'](?<logo>[^""']+)[""']");
                    string iconPath = logoMatch.Success ? logoMatch.Groups["logo"].Value.Trim() : null;
                    info.IconBase64 = ExtractIconBase64(archive, iconPath);
                }
                else
                {
                    var fabricJson = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("fabric.mod.json", StringComparison.OrdinalIgnoreCase));
                    if (fabricJson != null)
                    {
                        string jsonContent;
                        using (var reader = new StreamReader(fabricJson.Open()))
                        {
                            jsonContent = reader.ReadToEnd();
                        }
                        var jsonDoc = JsonNode.Parse(jsonContent);
                        info.Title = jsonDoc?["name"]?.ToString();
                        info.Description = jsonDoc?["description"]?.ToString();
                        string iconPath = jsonDoc?["icon"]?.ToString();
                        info.IconBase64 = ExtractIconBase64(archive, iconPath);
                    }
                }
            }
            return info;
        }

        private string ExtractIconBase64(ZipArchive archive, string iconPath)
        {
            ZipArchiveEntry entry = null;
            if (!string.IsNullOrEmpty(iconPath))
                entry = archive.Entries.FirstOrDefault(e => e.FullName.Equals(iconPath, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
                entry = archive.Entries.FirstOrDefault(e => !e.FullName.Contains("/") && e.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
            if (entry == null)
                entry = archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("assets/") && e.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (var stream = entry.Open())
                    {
                        stream.CopyTo(ms);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
            return null;
        }

        private void guna2ComboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedversion = guna2ComboBox3.SelectedItem.ToString();
            guna2GradientButton16.Text = $"SELECTED {selectedversion}";
            guna2PictureBox17.Image = guna2PictureBox15.Image;
            label4.Text = $"VERSION {selectedversion}";
            guna2PictureBox18.Image = guna2PictureBox17.Image;
        }

        private void guna2Button7_Click(object sender, EventArgs e)
        {
            guna2ComboBox3.Items.Clear();
            string minecraftPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions");

            if (Directory.Exists(minecraftPath))
            {
                foreach (string dir in Directory.GetDirectories(minecraftPath))
                {
                    guna2ComboBox3.Items.Add(Path.GetFileName(dir));
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            string minecraftPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            string pathxd = Path.Combine(minecraftPath, "InferzLauncher");
            if(!Directory.Exists(pathxd))
            {
                Directory.CreateDirectory(pathxd);
            }
            string pathxdx = Path.Combine(pathxd, "Usernames");
            if (!Directory.Exists(pathxdx))
            {
                Directory.CreateDirectory(pathxdx);
            }
            foreach(string file in Directory.GetFiles(pathxdx))
            {
                File.Delete(file);
            }
            int index = 0;
            foreach (var item in guna2ComboBox1.Items)
            {
                File.WriteAllText(Path.Combine(pathxdx, $"username-{index}"), item.ToString());
                index++;
            }
        }

        private void guna2Button9_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executables (*.exe)|*.exe";
            openFileDialog.Title = "Select an executable file";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                string minecraftPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
                string pathxd = Path.Combine(minecraftPath, "InferzLauncher");
                if (!Directory.Exists(pathxd))
                {
                    Directory.CreateDirectory(pathxd);
                }
                string pathxdx = Path.Combine(pathxd, "Javas");
                if (!Directory.Exists(pathxdx))
                {
                    Directory.CreateDirectory(pathxdx);
                }
                int index = 1;
                while(File.Exists(Path.Combine(pathxdx, index.ToString())))
                {
                    index++;
                }
                File.WriteAllText(Path.Combine(pathxdx, index.ToString()), filePath);
            }
        }

        private void guna2ComboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedjava = pathsxd[guna2ComboBox4.SelectedItem.ToString()];
        }

        private void guna2Button8_Click(object sender, EventArgs e)
        {
            guna2ComboBox4.Items.Clear();
            string minecraftPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            string pathxd = Path.Combine(minecraftPath, "InferzLauncher");
            if (!Directory.Exists(pathxd))
            {
                Directory.CreateDirectory(pathxd);
            }
            string pathxdx2 = Path.Combine(pathxd, "Javas");

            if (!Directory.Exists(pathxdx2))
            {
                Directory.CreateDirectory(pathxdx2);
            }
            pathsxd.Clear();
            foreach (string file in Directory.GetFiles(pathxdx2))
            {
                guna2ComboBox4.Items.Add($"{Path.GetFileName(File.ReadAllText(file))} : {Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(File.ReadAllText(file)))))}");
                pathsxd.Add($"{Path.GetFileName(File.ReadAllText(file))} : {Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(File.ReadAllText(file)))))}", File.ReadAllText(file));
            }
        }

        private void guna2CheckBox4_CheckedChanged(object sender, EventArgs e)
        {
            guna2ComboBox4.Enabled = !guna2CheckBox4.Checked;
            if(guna2CheckBox4.Checked)
            {
                selectedjava = "java.exe";
            }
        }

        private async void Loadmods()
        {
            string minecraftPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            string pathxd = Path.Combine(minecraftPath, "mods");
            foreach(string mod in  Directory.GetFiles(pathxd))
            {
                    string modversion = GetModVersionFromJar(mod);
                    string modloader = GetModLoaderType(mod);
                    string minecraftversion = GetMinecraftVersionFromMod(mod);
                    bool iscompatible = await IsVersionCompatible(mod, selectedversion);
                ModInfo info = GetInfo(mod);
                    Guna2Panel panel = new Guna2Panel();
                    panel.Size = guna2Panel7.Size;
                    panel.Left = offsetX - guna2Panel11.HorizontalScroll.Value;
                    panel.Top = offsetY - guna2Panel11.VerticalScroll.Value;
                    panel.BorderRadius = guna2Panel7.BorderRadius;
                    panel.BorderColor = guna2Panel7.BorderColor;
                panel.FillColor = guna2Panel7.FillColor;
                    panel.BorderThickness = guna2Panel7.BorderThickness;
                    guna2Panel11.Controls.Add(panel);
                    Guna2PictureBox picture = new Guna2PictureBox();
                    picture.Size = guna2PictureBox19.Size;
                    picture.Location = guna2PictureBox19.Location;
                    picture.SizeMode = guna2PictureBox19.SizeMode;
                    picture.BackColor = guna2PictureBox19.BackColor;
                    LoadBase64Image(info.IconBase64, picture);
                    panel.Controls.Add(picture);
                    Label label1xd = new Label();
                    label1xd.Size = label9.Size;
                    label1xd.AutoSize = label9.AutoSize;
                    label1xd.BackColor = label9.BackColor;
                    label1xd.ForeColor = label9.ForeColor;
                    label1xd.Font = label9.Font;
                    label1xd.BackColor = label9.BackColor;
                    label1xd.Location = label9.Location;
                    label1xd.Text = info.Title;
                    panel.Controls.Add(label1xd);
                    Label label1xd2 = new Label();
                    label1xd2.Size = label18.Size;
                    label1xd2.AutoSize = label18.AutoSize;
                    label1xd2.BackColor = label18.BackColor;
                    label1xd2.ForeColor = label18.ForeColor;
                    label1xd2.Font = label18.Font;
                    label1xd2.BackColor = label18.BackColor;
                    label1xd2.Location = label18.Location;
                    label1xd2.Text = info.Description;
                    panel.Controls.Add(label1xd2);
                    Label label1xd3 = new Label();
                    label1xd3.Size = label10.Size;
                    label1xd3.AutoSize = label10.AutoSize;
                    label1xd3.BackColor = label10.BackColor;
                    label1xd3.ForeColor = label10.ForeColor;
                    label1xd3.Font = label10.Font;
                    label1xd3.BackColor = label10.BackColor;
                    label1xd3.Location = label10.Location;
                    label1xd3.Text = label10.Text;
                    panel.Controls.Add(label1xd3);
                    Label label1xd4 = new Label();
                    label1xd4.Size = label11.Size;
                    label1xd4.AutoSize = label11.AutoSize;
                    label1xd4.BackColor = label11.BackColor;
                    label1xd4.ForeColor = label11.ForeColor;
                    label1xd4.Font = label11.Font;
                    label1xd4.BackColor = label11.BackColor;
                    label1xd4.Location = label11.Location;
                    label1xd4.Text = label11.Text;
                    panel.Controls.Add(label1xd4);
                    Label label1xd5 = new Label();
                    label1xd5.Size = label17.Size;
                    label1xd5.AutoSize = label17.AutoSize;
                    label1xd5.BackColor = label17.BackColor;
                    label1xd5.ForeColor = label17.ForeColor;
                    label1xd5.Font = label17.Font;
                    label1xd5.BackColor = label17.BackColor;
                    label1xd5.Location = label17.Location;
                    label1xd5.Text = label17.Text;
                    panel.Controls.Add(label1xd5);
                    Label label1xd6 = new Label();
                    label1xd6.Size = label16.Size;
                    label1xd6.AutoSize = label16.AutoSize;
                    label1xd6.BackColor = label16.BackColor;
                    label1xd6.ForeColor = label16.ForeColor;
                    label1xd6.Font = label16.Font;
                    label1xd6.BackColor = label16.BackColor;
                    label1xd6.Location = label16.Location;
                    label1xd6.Text = label16.Text;
                    panel.Controls.Add(label1xd6);
                    Label label1xd7 = new Label();
                    label1xd7.Size = label12.Size;
                    label1xd7.AutoSize = label12.AutoSize;
                    label1xd7.BackColor = label12.BackColor;
                    label1xd7.ForeColor = label12.ForeColor;
                    label1xd7.Font = label12.Font;
                    label1xd7.BackColor = label12.BackColor;
                    label1xd7.Location = label12.Location;
                label1xd7.Text = modversion;
                    label1xd7.TextAlign = label12.TextAlign;
                    panel.Controls.Add(label1xd7);
                    Label label1xd8 = new Label();
                    label1xd8.Size = label13.Size;
                    label1xd8.AutoSize = label13.AutoSize;
                    label1xd8.BackColor = label13.BackColor;
                    label1xd8.ForeColor = label13.ForeColor;
                    label1xd8.Font = label13.Font;
                    label1xd8.BackColor = label13.BackColor;
                    label1xd8.Location = label13.Location;
                label1xd8.Text = minecraftversion;
                    label1xd8.TextAlign = label13.TextAlign;
                    panel.Controls.Add(label1xd8);
                    Label label1xd9 = new Label();
                    label1xd9.Size = label15.Size;
                    label1xd9.AutoSize = label15.AutoSize;
                    label1xd9.BackColor = label15.BackColor;
                    label1xd9.ForeColor = label15.ForeColor;
                    label1xd9.Font = label15.Font;
                    label1xd9.BackColor = label15.BackColor;
                    label1xd9.Location = label15.Location;
                label1xd9.Text = modloader;
                    label1xd9.TextAlign = label15.TextAlign;
                    panel.Controls.Add(label1xd9);
                    Label label1xd10 = new Label();
                    label1xd10.Size = label14.Size;
                    label1xd10.AutoSize = label14.AutoSize;
                    label1xd10.BackColor = label14.BackColor;
                    label1xd10.Font = label14.Font;
                    label1xd10.BackColor = label14.BackColor;
                    label1xd10.Location = label14.Location;
                    if (!iscompatible)
                    {
                        label1xd10.Text = label14.Text;
                        label1xd10.ForeColor = label14.ForeColor;
                    }
                    else
                    {

                        label1xd10.Text = "Compatible";
                        label1xd10.ForeColor = Color.FromArgb(78, 237, 99);
                    }
                    label1xd10.TextAlign = label14.TextAlign;
                    panel.Controls.Add(label1xd10);
                Guna2Button button = new Guna2Button();
                button.Size = guna2Button13.Size;
                button.Location = guna2Button13.Location;
                button.BorderRadius = guna2Button13.BorderRadius;
                button.BackColor = guna2Button13.BackColor;
                button.FillColor = guna2Button13.FillColor;
                button.ImageSize = guna2Button13.ImageSize;
                button.Text = guna2Button13.Text;
                button.Font = guna2Button13.Font;
                button.Click += (sender, e) =>
                {
                    File.Delete(mod);
                    Reloadmods();
                };
                panel.Controls.Add(button);
                Guna2Button button2 = new Guna2Button();
                button2.Size = guna2Button14.Size;
                button2.Location = guna2Button14.Location;
                button2.BorderRadius = guna2Button14.BorderRadius;
                button2.BackColor = guna2Button14.BackColor;
                button2.FillColor = guna2Button14.FillColor;
                button2.ImageSize = guna2Button14.ImageSize;
                button2.Text = guna2Button14.Text;
                button2.Font = guna2Button14.Font;
                button2.Click += async (sender, e) =>
                {
                    bool jiscompatible = await IsVersionCompatible(mod, selectedversion);
                    if (!jiscompatible)
                    {
                        label1xd10.Text = label14.Text;
                        label1xd10.ForeColor = label14.ForeColor;
                    }
                    else
                    {

                        label1xd10.Text = "Compatible";
                        label1xd10.ForeColor = Color.FromArgb(78, 237, 99);
                    }
                };
                panel.Controls.Add(button2);
                if (offsetX == 672)
                    {
                        offsetX = 8;
                        offsetY = offsetY + 251;
                    }
                    else
                    {
                        offsetX = offsetX + 332;
                    }
                
            }
            offsetX = 8;
            offsetY = 15;

        }
        private async void Reloadmods()
        {
            for (int i = guna2Panel11.Controls.Count - 1; i >= 0; i--)
            {
                Control control = guna2Panel11.Controls[i];
                if (control != guna2Panel7)
                {
                    guna2Panel11.Controls.RemoveAt(i);
                    control.Dispose();
                }
            }

            Loadmods();
        }

        private void guna2Button11_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Jar mods (*.jar)|*.jar"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string minecraftPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
                string modsPath = Path.Combine(minecraftPath, "mods");
                if (!Directory.Exists(modsPath))
                {
                    Directory.CreateDirectory(modsPath);
                }

                foreach (string file in ofd.FileNames)
                {
                    string destFile = Path.Combine(modsPath, Path.GetFileName(file));
                    try
                    {
                        File.Move(file, destFile);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error moving file {file}: {ex.Message}");
                    }
                }
            }
        }

        private void guna2Button10_Click(object sender, EventArgs e)
        {
            Reloadmods();
        }

        private void guna2GradientButton17_Click(object sender, EventArgs e)
        {
            modsmanager.Visible = true;
        }

        private void guna2Button12_Click(object sender, EventArgs e)
        {
            modsmanager.Visible =false;
        }
    }
}

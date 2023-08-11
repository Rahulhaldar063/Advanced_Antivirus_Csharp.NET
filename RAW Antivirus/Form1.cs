using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.Devices;
using System.Security.Cryptography;
using Microsoft.VisualBasic.CompilerServices;
using static System.Windows.Forms.DataFormats;
using Guna.UI2.WinForms;

namespace RAW_Antivirus
{
    public partial class Form1 : Form
    {
        public int _totalCount;
        public int _maliciousCount;
        bool isSituation = false;

        public Form1()
        {
            InitializeComponent();
        }
        private void hidePanel()
        {
            pnlTools.Visible = false;
            pnlHome.Visible = false;
            pnlSecurity.Visible = false;
            pnlHelp.Visible = false;
        }

        private void btnTools_Click(object sender, EventArgs e)
        {
            hidePanel();
            pnlTools.Visible = true;
        }

        private void btnCmd_Click(object sender, EventArgs e)
        {
            Process.Start("cmd.exe", "/k cd C:\\Windows\\System32");
        }

        private void btnTM_Click(object sender, EventArgs e)
        {
            try
            {
                Process taskManager = new Process();
                taskManager.StartInfo.FileName = "taskmgr.exe";
                taskManager.Start();

            }
            catch (Win32Exception)
            {
                MessageBox.Show("The application must be run as an administrator to open Task Manager Processes.\nPlease run the application as an administrator and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnNP_Click(object sender, EventArgs e)
        {
            Process.Start("notepad.exe");
        }

        private void btnBP_Click(object sender, EventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd", "/c tree C:\\");
            Process? treeProcess = Process.Start(psi);
            treeProcess.WaitForExit();

            // Disable visual effects for better performance
            DisableVisualEffects();
            // Empty the system's working set to free up memory
            EmptyWorkingSet();

            // Update drivers and software
            try
            {
                Process.Start("ms-settings:windowsupdate");
            }
            catch (Win32Exception)
            {
                MessageBox.Show("Access is denied!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            // Restart computer
            DialogResult dialogResult = MessageBox.Show("Your computer will now restart. Do you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialogResult == DialogResult.Yes)
            {
                Process.Start("shutdown.exe", "-r -t 0");
            }
        }
        private void DisableVisualEffects()
        {
            const int SPI_SETANIMATION = 0x0049;
            const int SPIF_SENDCHANGE = 0x02;
            SystemParametersInfo(SPI_SETANIMATION, 0, IntPtr.Zero, SPIF_SENDCHANGE);
        }
        private void EmptyWorkingSet()
        {
            Process currentProcess = Process.GetCurrentProcess();
            SetProcessWorkingSetSize(currentProcess.Handle, -1, -1);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, IntPtr lpvParam, int fuWinIni);

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize);

        private void btnCleaner_Click(object sender, EventArgs e)
        {
            // Clean up disk space
            Process.Start("cleanmgr.exe");
            // Defragment hard drive
            Process.Start("dfrgui.exe");
        }

        // GUIDs for the power plans
        private static readonly Guid BatterySaverModePlanGuid = new Guid("a1841308-3541-4fab-bc81-f71556f20b4a");
        private static readonly Guid HighPerformancePlanGuid = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

        [DllImport("powrprof.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint PowerSetActiveScheme(IntPtr UserPowerKey, ref Guid ActivePolicyGuid);
        private bool isBatterySaverEnabled = false;
        private void btnBS_Click(object sender, EventArgs e)
        {
            isBatterySaverEnabled = !isBatterySaverEnabled;
            SetBatterySaverMode(isBatterySaverEnabled);
        }
        private void SetBatterySaverMode(bool enable)
        {
            try
            {
                Guid activePlanGuid = enable ? BatterySaverModePlanGuid : HighPerformancePlanGuid;
                PowerSetActiveScheme(IntPtr.Zero, ref activePlanGuid);
                if (enable)
                {
                    MessageBox.Show("Battery saver has been turned on successfully.", "Battery Saver", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Battery saver has been turned off successfully.", "Battery Saver", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("An error occurred while changing the power plan.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private long _totalSize;
        private long _freeSpace;
        private long _totalMemory;

        private PerformanceCounter? _cpuCounter;
        private PowerStatus? _powerStatus;

        private void UpdateBatteryLevel()
        {
            try
            {
                int batteryLevel = (int)(_powerStatus.BatteryLifePercent * 100);
                guna2CircleProgressBar4.Value = batteryLevel;
            }
            catch { }
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            hidePanel();
            pnlHome.Visible = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                //CPU Usage
                int cpuUsage = (int)_cpuCounter.NextValue();
                guna2CircleProgressBar1.Value = cpuUsage;
                //RAM Usage
                long usedMemory = _totalMemory - new PerformanceCounter("Memory", "Available Bytes").RawValue;
                int memoryUsage = (int)(usedMemory * 100 / _totalMemory);
                guna2CircleProgressBar2.Value = memoryUsage;

                //DISK Usage
                long usedSpace = _totalSize - _freeSpace;
                int diskUsage = (int)(usedSpace * 100 / _totalSize);
                guna2CircleProgressBar3.Value = diskUsage;

                //BATTERY Usage
                _powerStatus = SystemInformation.PowerStatus;
                UpdateBatteryLevel();
            }
            catch { }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            pnlHome.Visible = true;
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue();

            _totalMemory = (long)(new ComputerInfo().TotalPhysicalMemory);

            string? driveName = Path.GetPathRoot(Application.StartupPath);
            DriveInfo driveInfo = new DriveInfo(driveName);
            _totalSize = driveInfo.TotalSize;
            _freeSpace = driveInfo.AvailableFreeSpace;

            _powerStatus = SystemInformation.PowerStatus;
            UpdateBatteryLevel();
        }

        public void scanningTask()
        {
            _totalCount = 0;
            _maliciousCount = 0;
            listBox2.Items.Clear();
            scanningPB.Value = 0;
        }

        public void ShowPanel()
        {
            hidePanel();
            pnlSecurity.Visible = true;
        }

        private void btnScanFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string? selectedFolder = folderBrowserDialog.SelectedPath;
                    scanningTask();
                    if (!Directory.Exists(selectedFolder))
                    {
                        MessageBox.Show("Invalid path!");
                        return;
                    }

                    string[] files = Directory.GetFiles(selectedFolder, "*", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        listBox1.Items.Add(file);
                    }
                    ShowPanel();
                    timer2.Start();
                }
                catch (NullReferenceException)
                {
                    MessageBox.Show("Select a Folder.");
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Access to some files/directories is denied.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
        }
        private void btnSecurity_Click(object sender, EventArgs e)
        {
            hidePanel();
            pnlSecurity.Visible = true;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            scanningPB.Maximum = Conversions.ToInteger(listBox1.Items.Count);

            if (scanningPB.Value < scanningPB.Maximum)
            {
                btnAbort.Visible = true;
                try
                {
                    listBox1.SelectedIndex = listBox1.SelectedIndex + 1;
                    label12.Text = listBox1.SelectedItem.ToString();
                    label13.Text = listBox1.SelectedIndex.ToString();
                }
                catch (Exception)
                {
                }

                try
                {
                    TextBox scanbox = new TextBox();
                    string read = File.ReadAllText("main.db").ToString();
                    scanningPB.Value += 1;
                    label14.Text = Conversions.ToString(listBox2.Items.Count);
                    label13.Text = Conversions.ToString(scanningPB.Value);
                    scanbox.Text = read.ToString();
                    MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

                    string selectedFilePath = listBox1.SelectedItem?.ToString();
                    if (selectedFilePath != null && File.Exists(selectedFilePath))
                    {
                        using (FileStream fileStream = new FileStream(selectedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192))
                        {
                            md5.ComputeHash(fileStream);
                            byte[] hash = md5.Hash;
                            StringBuilder buff = new StringBuilder();
                            foreach (byte hashByte in hash)
                            {
                                buff.Append(string.Format("{0:X2}", hashByte));
                            }

                            if (scanbox.Text.Contains(buff.ToString()))
                            {
                                listBox2.Items.Add(listBox1.SelectedItem);
                                isSituation = true;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("The selected item is not a valid file path or the file does not exist.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        timer2.Stop();
                    }

                }
                catch (Exception)
                {
                    MessageBox.Show("Unable to access folder.");
                    timer2.Stop();
                }
            }
            else
            {
                timer2.Stop();

                if (listBox2.Items.Count > 0)
                {
                    MessageBox.Show("Scanning has been completed. There was " + Environment.NewLine + listBox2.Items.Count + " viruses detected. Please review the list and choose an action.", "RAW Antivirus", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    btnAbort.Visible = false;
                    btnDelete.Visible = true;
                    btnIgnore.Visible = true;
                    listBox1.Items.Clear();
                    return;
                }
                else
                {
                    MessageBox.Show("Scanning has been completed." + Environment.NewLine + "No viruses found.", "RAW Antivirus", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    listBox1.Items.Clear();
                }
            }
            if (isSituation)
            {
                pictureBox1.Image = Image.FromFile("cyber-crime.png");
                label5.Text = "Infected!";
                label5.ForeColor = Color.Red;
                label6.Text = "Low";
                label6.ForeColor = Color.Red;
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            foreach (string file in listBox2.SelectedItems)
            {
                try
                {
                    File.Delete(file);

                    // Update count
                    _maliciousCount--;
                    if (_maliciousCount == 0)
                    {
                        MessageBox.Show("Selected viruses have been removed. Your computer is now secured.", "Report", MessageBoxButtons.OK);
                        btnDelete.Visible = false;
                        btnIgnore.Visible = false;
                    }
                }
                catch (Exception)
                {
                    // Log any errors and continue
                    MessageBox.Show("Error deleting file.");
                }
            }

            // Refresh list box and text boxes
            label14.Text = _maliciousCount.ToString();
        }

        private void btnIgnore_Click(object sender, EventArgs e)
        {
            foreach (string selectedItem in listBox2.SelectedItems)
            {
                listBox2.Items.Remove(selectedItem);
                _maliciousCount--;
                if (_maliciousCount == 0)
                {
                    MessageBox.Show("All viruses have been removed. Your computer is now secured.", "Report", MessageBoxButtons.OK);
                    btnDelete.Visible = false;
                    btnIgnore.Visible = false;
                }
            }
            label14.Text = _maliciousCount.ToString();
        }
        private void btnAbort_Click(object sender, EventArgs e)
        {
            timer2.Stop();
            scanningPB.Value = 0;
            label13.Text = "0";
            label14.Text = "0";
            label12.Text = "";
            listBox1.Items.Clear();
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            hidePanel();
            pnlHelp.Visible = true;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = "https://www.facebook.com/rahul.haldar.940641?mibextid=ZbWKwL";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception)
            {
                MessageBox.Show("There is an Error to open the link.");
            }
        }
    }
}
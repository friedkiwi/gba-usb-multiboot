using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GbaUploadGUI
{
    public partial class GbaUploadGuiForm : Form
    {
        private byte[] _selectedRom;
        private string _selectedRomPath;
        private bool _isUploading;

        public GbaUploadGuiForm()
        {
            InitializeComponent();
            serialPortSelection.SelectedIndexChanged += SerialPortSelection_SelectedIndexChanged;
            PopulateSerialPorts();
            ResetLoadedRom();
            ResetProgressBar();
            UpdateControlState();
        }

        private void PopulateSerialPorts()
        {
            string selectedPort = serialPortSelection.SelectedItem as string;
            string[] availablePorts = SerialPort.GetPortNames()
                .OrderBy(portName => portName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            serialPortSelection.BeginUpdate();

            try
            {
                serialPortSelection.Items.Clear();
                serialPortSelection.Items.AddRange(availablePorts);

                if (!string.IsNullOrEmpty(selectedPort) &&
                    availablePorts.Any(portName => string.Equals(portName, selectedPort, StringComparison.OrdinalIgnoreCase)))
                {
                    serialPortSelection.SelectedItem = availablePorts.First(
                        portName => string.Equals(portName, selectedPort, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    serialPortSelection.SelectedIndex = -1;
                    serialPortSelection.Text = string.Empty;
                }
            }
            finally
            {
                serialPortSelection.EndUpdate();
            }
        }

        private void UpdateControlState()
        {
            bool hasSelectedPort = serialPortSelection.SelectedItem is string;
            bool hasValidRom = _selectedRom != null;

            serialPortSelection.Enabled = !_isUploading;
            BrowseROMFileButton.Enabled = !_isUploading && hasSelectedPort;
            UploadButton.Enabled = !_isUploading && hasSelectedPort && hasValidRom;
        }

        private void ResetLoadedRom()
        {
            _selectedRom = null;
            _selectedRomPath = null;
            SelectedFileLabel.Text = "No file selected";
            RomTitleLabel.Text = "Title:";
            label3.Text = "Game code:";
            RomCRC32.Text = "CRC32:";
        }

        private void LoadRomFile(string filePath)
        {
            byte[] romBytes = File.ReadAllBytes(filePath);
            GbaRomHeader header;

            try
            {
                header = GbaRomHeaderParser.Parse(romBytes);
            }
            catch (GbaRomHeaderValidationException exception)
            {
                ResetLoadedRom();
                ResetProgressBar();
                UpdateControlState();

                MessageBox.Show(
                    this,
                    exception.Message,
                    "Invalid ROM file",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            _selectedRom = romBytes;
            _selectedRomPath = filePath;

            SelectedFileLabel.Text = Path.GetFileName(filePath);
            RomTitleLabel.Text = "Title: " + header.Title;
            label3.Text = "Game code: " + header.GameCode;
            RomCRC32.Text = "CRC32: " + ComputeCrc32(romBytes).ToString("X8");

            ResetProgressBar();
            UpdateControlState();
        }

        private void ResetProgressBar()
        {
            progressBar1.Style = ProgressBarStyle.Blocks;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
        }

        private void ApplyUploadProgress(MultibootUploadProgress progress)
        {
            if (progress == null)
            {
                return;
            }

            if (progress.IsIndeterminate)
            {
                if (progressBar1.Style != ProgressBarStyle.Marquee)
                {
                    progressBar1.Style = ProgressBarStyle.Marquee;
                    progressBar1.MarqueeAnimationSpeed = 25;
                }
            }
            else
            {
                if (progressBar1.Style != ProgressBarStyle.Blocks)
                {
                    progressBar1.Style = ProgressBarStyle.Blocks;
                    progressBar1.MarqueeAnimationSpeed = 0;
                }

                int value = Math.Max(progressBar1.Minimum, Math.Min(progressBar1.Maximum, progress.PercentComplete));
                progressBar1.Value = value;
            }

            groupBox3.Text = progress.Message;
        }

        private static uint ComputeCrc32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;

            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];

                for (int bit = 0; bit < 8; bit++)
                {
                    if ((crc & 1U) != 0)
                    {
                        crc = (crc >> 1) ^ 0xEDB88320U;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return ~crc;
        }

        private void SerialPortSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControlState();
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private async void UploadButton_Click(object sender, EventArgs e)
        {
            string selectedPort = serialPortSelection.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(selectedPort))
            {
                MessageBox.Show(
                    this,
                    "Select a serial port before starting the upload.",
                    "Serial port required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_selectedRom == null)
            {
                MessageBox.Show(
                    this,
                    "Load a valid multiboot ROM before starting the upload.",
                    "ROM required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!SerialPort.GetPortNames().Any(portName => string.Equals(portName, selectedPort, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(
                    this,
                    string.Format("The selected serial port '{0}' is no longer available.", selectedPort),
                    "Serial port unavailable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                PopulateSerialPorts();
                UpdateControlState();
                return;
            }

            _isUploading = true;
            ResetProgressBar();
            groupBox3.Text = "Uploading...";
            UploadButton.Text = "Uploading...";
            UpdateControlState();

            Progress<MultibootUploadProgress> progress = new Progress<MultibootUploadProgress>(ApplyUploadProgress);

            try
            {
                using (MultibootComms uploader = new MultibootComms(
                    selectedPort,
                    new MultibootOptions
                    {
                        BaudRate = MultibootComms.DefaultBaudRate
                    }))
                {
                    await uploader.UploadRomAsync(_selectedRom, progress);
                }

                progressBar1.Style = ProgressBarStyle.Blocks;
                progressBar1.Value = progressBar1.Maximum;
                groupBox3.Text = "Upload complete";

                MessageBox.Show(
                    this,
                    string.Format("Uploaded '{0}' successfully over {1}.", Path.GetFileName(_selectedRomPath), selectedPort),
                    "Upload complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (UnauthorizedAccessException)
            {
                ShowUploadError(
                    "The selected serial port is already in use or access was denied. Close any terminal or uploader using that port and try again.");
            }
            catch (IOException exception)
            {
                ShowUploadError(
                    "The upload failed because the serial connection was interrupted or the device stopped responding." +
                    Environment.NewLine + Environment.NewLine +
                    exception.Message);
            }
            catch (TimeoutException exception)
            {
                ShowUploadError(
                    "The upload timed out while waiting for the GBA adapter or console. Make sure the cable is connected, the RP2040 is running the firmware, and the GBA is in multiboot receive mode." +
                    Environment.NewLine + Environment.NewLine +
                    exception.Message);
            }
            catch (MultibootProtocolException exception)
            {
                ShowUploadError(
                    "The adapter responded, but the multiboot protocol exchange failed. Check the link cable wiring and confirm the selected file is a valid multiboot image." +
                    Environment.NewLine + Environment.NewLine +
                    exception.Message);
            }
            catch (ArgumentException exception)
            {
                ShowUploadError(
                    "The selected ROM could not be uploaded." +
                    Environment.NewLine + Environment.NewLine +
                    exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                ShowUploadError(
                    "The serial port could not be opened for upload." +
                    Environment.NewLine + Environment.NewLine +
                    exception.Message);
            }
            catch (Exception exception)
            {
                ShowUploadError(
                    "The upload failed unexpectedly." +
                    Environment.NewLine + Environment.NewLine +
                    exception.Message);
            }
            finally
            {
                _isUploading = false;
                UploadButton.Text = "Upload!";

                if (groupBox3.Text != "Upload complete")
                {
                    groupBox3.Text = "Upload";
                }

                if (progressBar1.Style == ProgressBarStyle.Marquee)
                {
                    ResetProgressBar();
                }

                PopulateSerialPorts();
                UpdateControlState();
            }
        }

        private void ShowUploadError(string message)
        {
            ResetProgressBar();
            groupBox3.Text = "Upload failed";

            MessageBox.Show(
                this,
                message,
                "Upload failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void BrowseROMFileButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Open multiboot image";
                dialog.Filter = "MultiBoot files (*.mb)|*.mb|GBA ROM files (*.gba)|*.gba|All files (*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.Multiselect = false;

                if (!string.IsNullOrEmpty(_selectedRomPath))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(_selectedRomPath);
                    dialog.FileName = Path.GetFileName(_selectedRomPath);
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadRomFile(dialog.FileName);
                }
            }
        }
    }
}

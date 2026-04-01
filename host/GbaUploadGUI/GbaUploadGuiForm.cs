using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GbaUploadGUI
{
    public partial class GbaUploadGuiForm : Form
    {
        private byte[] _selectedRom;
        private string _selectedRomPath;
        private bool _selectedRomWasPatched;
        private bool _isUploading;
        private CancellationTokenSource _uploadCancellationSource;

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
            UploadButton.Enabled = _isUploading || (hasSelectedPort && hasValidRom);
            UploadButton.Text = _isUploading
                ? (_uploadCancellationSource != null && _uploadCancellationSource.IsCancellationRequested ? "Cancelling..." : "Cancel")
                : "Upload!";
        }

        private void ResetLoadedRom()
        {
            _selectedRom = null;
            _selectedRomPath = null;
            _selectedRomWasPatched = false;
            SelectedFileLabel.Text = "No file selected";
            RomTitleLabel.Text = "Title:";
            label3.Text = "Game code:";
            RomCRC32.Text = "CRC32:";
        }

        private void LoadRomFile(string filePath)
        {
            byte[] romBytes = File.ReadAllBytes(filePath);
            GbaPreparedRom preparedRom;

            try
            {
                preparedRom = GbaRomHeaderParser.PrepareForMultiboot(romBytes, false);
            }
            catch (GbaRomHeaderValidationException exception)
            {
                if (exception.Error == GbaRomValidationError.PatchableMultibootEntryMissing)
                {
                    DialogResult patchResult = MessageBox.Show(
                        this,
                        "This ROM appears to be a valid GBA ROM and fits in multiboot RAM, but its dedicated multiboot entry point is empty." +
                        Environment.NewLine + Environment.NewLine +
                        "An experimental in-memory patch can copy the normal cartridge entry branch into the multiboot entry slot for this upload only." +
                        Environment.NewLine + Environment.NewLine +
                        "The file on disk will not be modified." +
                        Environment.NewLine + Environment.NewLine +
                        "Do you want to try this experimental patch?",
                        "Experimental multiboot patch",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (patchResult == DialogResult.Yes)
                    {
                        try
                        {
                            preparedRom = GbaRomHeaderParser.PrepareForMultiboot(romBytes, true);
                        }
                        catch (GbaRomHeaderValidationException patchException)
                        {
                            ResetLoadedRom();
                            ResetProgressBar();
                            UpdateControlState();

                            MessageBox.Show(
                                this,
                                patchException.Message,
                                "Invalid ROM file",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else
                    {
                        ResetLoadedRom();
                        ResetProgressBar();
                        UpdateControlState();
                        return;
                    }
                }
                else
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
            }

            _selectedRom = preparedRom.RomBytes;
            _selectedRomPath = filePath;
            _selectedRomWasPatched = preparedRom.WasPatchedInMemory;

            SelectedFileLabel.Text = GetRomDisplayName(filePath, _selectedRomWasPatched);
            RomTitleLabel.Text = "Title: " + preparedRom.Header.Title;
            label3.Text = "Game code: " + preparedRom.Header.GameCode;
            RomCRC32.Text = "CRC32: " + ComputeCrc32(_selectedRom).ToString("X8");

            if (_selectedRomWasPatched)
            {
                MessageBox.Show(
                    this,
                    "An experimental in-memory patch was applied to copy the normal cartridge entry branch into the multiboot entry point for this upload session." +
                    Environment.NewLine + Environment.NewLine +
                    "The file on disk was not modified.",
                    "Experimental patch applied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            ResetProgressBar();
            UpdateControlState();
        }

        private static string GetRomDisplayName(string romPath, bool wasPatchedInMemory)
        {
            string fileName = Path.GetFileName(romPath);
            return wasPatchedInMemory ? fileName + " (patched in RAM)" : fileName;
        }

        private void ResetProgressBar()
        {
            progressBar1.Style = ProgressBarStyle.Blocks;
            progressBar1.MarqueeAnimationSpeed = 0;
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
            if (_isUploading)
            {
                if (_uploadCancellationSource != null && !_uploadCancellationSource.IsCancellationRequested)
                {
                    _uploadCancellationSource.Cancel();
                    groupBox3.Text = "Cancelling upload...";
                    UpdateControlState();
                }

                return;
            }

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

            bool wasCancelled = false;
            bool uploadCompleted = false;

            _isUploading = true;
            _uploadCancellationSource = new CancellationTokenSource();
            ResetProgressBar();
            groupBox3.Text = "Uploading...";
            UpdateControlState();

            Progress<MultibootUploadProgress> progress = new Progress<MultibootUploadProgress>(ApplyUploadProgress);
            MultibootOptions options = new MultibootOptions
            {
                BaudRate = MultibootComms.DefaultBaudRate,
                ClientConnectTimeoutMs = 30000
            };

            try
            {
                using (MultibootComms uploader = new MultibootComms(selectedPort, options))
                {
                    await uploader.UploadRomAsync(_selectedRom, progress, _uploadCancellationSource.Token);
                }

                if (_uploadCancellationSource != null && _uploadCancellationSource.IsCancellationRequested)
                {
                    wasCancelled = true;
                    ResetProgressBar();
                    groupBox3.Text = "Upload cancelled";
                    return;
                }

                uploadCompleted = true;
                progressBar1.Style = ProgressBarStyle.Blocks;
                progressBar1.MarqueeAnimationSpeed = 0;
                progressBar1.Value = progressBar1.Maximum;
                groupBox3.Text = "Upload complete";

                MessageBox.Show(
                    this,
                    string.Format("Uploaded '{0}' successfully over {1}.", Path.GetFileName(_selectedRomPath), selectedPort),
                    "Upload complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
                ResetProgressBar();
                groupBox3.Text = "Upload cancelled";
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
                if (_uploadCancellationSource != null)
                {
                    _uploadCancellationSource.Dispose();
                    _uploadCancellationSource = null;
                }

                _isUploading = false;

                if (!uploadCompleted && !wasCancelled && groupBox3.Text != "Upload failed")
                {
                    groupBox3.Text = "Upload";
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

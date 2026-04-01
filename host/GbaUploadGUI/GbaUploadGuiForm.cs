using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;

namespace GbaUploadGUI
{
    public partial class GbaUploadGuiForm : Form
    {
        private byte[] _selectedRom;
        private string _selectedRomPath;

        public GbaUploadGuiForm()
        {
            InitializeComponent();
            serialPortSelection.SelectedIndexChanged += SerialPortSelection_SelectedIndexChanged;
            PopulateSerialPorts();
            ResetLoadedRom();
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

            BrowseROMFileButton.Enabled = hasSelectedPort;
            UploadButton.Enabled = hasSelectedPort && hasValidRom;
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

            UpdateControlState();
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

        private void OpenSerialPortButton_Click(object sender, EventArgs e)
        {
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

        private void UploadButton_Click(object sender, EventArgs e)
        {
        }
    }
}

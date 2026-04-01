using System;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;

namespace GbaUploadGUI
{
    public partial class GbaUploadGuiForm : Form
    {
        public GbaUploadGuiForm()
        {
            InitializeComponent();
            PopulateSerialPorts();
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
                else if (availablePorts.Length > 0)
                {
                    serialPortSelection.SelectedIndex = 0;
                }
            }
            finally
            {
                serialPortSelection.EndUpdate();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void OpenSerialPortButton_Click(object sender, EventArgs e)
        {
        }

        private void BrowseROMFileButton_Click(object sender, EventArgs e)
        {
        }

        private void UploadButton_Click(object sender, EventArgs e)
        {
        }
    }
}

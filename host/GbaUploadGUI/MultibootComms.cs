using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GbaUploadGUI
{
    public enum MultibootTransferStage
    {
        Preparing,
        WaitingForClient,
        SendingHeader,
        Negotiating,
        SendingPayload,
        Finalizing,
        Completed
    }

    public sealed class MultibootUploadProgress
    {
        internal MultibootUploadProgress(
            MultibootTransferStage stage,
            int completedUnits,
            int totalUnits,
            int bytesTransferred,
            int totalBytes,
            bool isIndeterminate,
            string message)
        {
            Stage = stage;
            CompletedUnits = completedUnits;
            TotalUnits = totalUnits;
            BytesTransferred = bytesTransferred;
            TotalBytes = totalBytes;
            IsIndeterminate = isIndeterminate;
            Message = message;
        }

        public MultibootTransferStage Stage { get; private set; }

        public int CompletedUnits { get; private set; }

        public int TotalUnits { get; private set; }

        public int BytesTransferred { get; private set; }

        public int TotalBytes { get; private set; }

        public bool IsIndeterminate { get; private set; }

        public string Message { get; private set; }

        public int PercentComplete
        {
            get
            {
                if (TotalUnits <= 0)
                {
                    return 0;
                }

                return (int)((CompletedUnits * 100L) / TotalUnits);
            }
        }
    }

    public sealed class MultibootProgressChangedEventArgs : EventArgs
    {
        public MultibootProgressChangedEventArgs(MultibootUploadProgress progress)
        {
            Progress = progress;
        }

        public MultibootUploadProgress Progress { get; private set; }
    }

    public sealed class MultibootOptions
    {
        public MultibootOptions()
        {
            BaudRate = MultibootComms.DefaultBaudRate;
            ReadTimeoutMs = 500;
            WriteTimeoutMs = 5000;
            StartupDrainMs = 2000;
            ClientConnectTimeoutMs = 30000;
            ReadyTimeoutMs = 5000;
            FinalizationTimeoutMs = 5000;
            RetryDelayMs = 63;
            PostHandshakeDelayMs = 63;
            ProgressUpdateStrideWords = 64;
            Palette = 0xD1;
        }

        public int BaudRate { get; set; }

        public int ReadTimeoutMs { get; set; }

        public int WriteTimeoutMs { get; set; }

        public int StartupDrainMs { get; set; }

        public int ClientConnectTimeoutMs { get; set; }

        public int ReadyTimeoutMs { get; set; }

        public int FinalizationTimeoutMs { get; set; }

        public int RetryDelayMs { get; set; }

        public int PostHandshakeDelayMs { get; set; }

        public int ProgressUpdateStrideWords { get; set; }

        public byte Palette { get; set; }
    }

    public sealed class MultibootProtocolException : InvalidOperationException
    {
        public MultibootProtocolException(string message)
            : base(message)
        {
        }
    }

    public sealed class MultibootComms : IDisposable
    {
        public const int DefaultBaudRate = 9600;

        private const int HeaderLengthBytes = 0xC0;
        private const int MaxRomSizeBytes = 0x40000;
        private const int ExpectedClientId = 0x2;

        private readonly SerialPort _port;
        private readonly MultibootOptions _options;
        private readonly bool _ownsPort;
        private CancellationToken _activeCancellationToken;
        private bool _disposed;

        public MultibootComms(string portName)
            : this(portName, null)
        {
        }

        public MultibootComms(string portName, MultibootOptions options)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentException("A serial port name is required.", "portName");
            }

            _options = options ?? new MultibootOptions();
            _port = CreateConfiguredPort(portName, _options);
            _ownsPort = true;
        }

        public MultibootComms(SerialPort port)
            : this(port, null)
        {
        }

        public MultibootComms(SerialPort port, MultibootOptions options)
        {
            if (port == null)
            {
                throw new ArgumentNullException("port");
            }

            _options = options ?? new MultibootOptions();
            _port = port;
            _ownsPort = false;
        }

        public event EventHandler<MultibootProgressChangedEventArgs> ProgressChanged;

        public void Open()
        {
            ThrowIfDisposed();

            if (_port.IsOpen)
            {
                return;
            }

            ApplyPortSettings(_port, _options);
            _port.Open();
            DrainStartupBanner();
        }

        public void Close()
        {
            if (_disposed)
            {
                return;
            }

            if (_port.IsOpen)
            {
                _port.Close();
            }
        }

        public void UploadRom(string romPath, IProgress<MultibootUploadProgress> progress = null)
        {
            UploadRom(romPath, progress, CancellationToken.None);
        }

        public void UploadRom(string romPath, IProgress<MultibootUploadProgress> progress, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(romPath))
            {
                throw new ArgumentException("A ROM path is required.", "romPath");
            }

            UploadRom(File.ReadAllBytes(romPath), progress, cancellationToken);
        }

        public void UploadRom(byte[] romBytes, IProgress<MultibootUploadProgress> progress = null)
        {
            UploadRom(romBytes, progress, CancellationToken.None);
        }

        public void UploadRom(byte[] romBytes, IProgress<MultibootUploadProgress> progress, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            byte[] preparedRom = PrepareRom(romBytes);
            bool closeWhenDone = !_port.IsOpen;

            if (closeWhenDone)
            {
                Open();
            }
            else
            {
                DrainStartupBanner();
            }

            try
            {
                _activeCancellationToken = cancellationToken;
                ExecuteUpload(preparedRom, progress, cancellationToken);
            }
            finally
            {
                _activeCancellationToken = CancellationToken.None;

                if (closeWhenDone)
                {
                    Close();
                }
            }
        }

        public Task UploadRomAsync(string romPath, IProgress<MultibootUploadProgress> progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(
                delegate
                {
                    UploadRom(romPath, progress, cancellationToken);
                },
                cancellationToken);
        }

        public Task UploadRomAsync(byte[] romBytes, IProgress<MultibootUploadProgress> progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(
                delegate
                {
                    UploadRom(romBytes, progress, cancellationToken);
                },
                cancellationToken);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_ownsPort)
            {
                _port.Dispose();
            }

            _disposed = true;
        }

        private static SerialPort CreateConfiguredPort(string portName, MultibootOptions options)
        {
            SerialPort port = new SerialPort(portName);
            ApplyPortSettings(port, options);
            return port;
        }

        private static void ApplyPortSettings(SerialPort port, MultibootOptions options)
        {
            if (port.IsOpen)
            {
                return;
            }

            port.BaudRate = options.BaudRate;
            port.DataBits = 8;
            port.Parity = Parity.None;
            port.StopBits = StopBits.One;
            port.Handshake = Handshake.None;
            port.DtrEnable = true;
            port.RtsEnable = true;
            port.ReadTimeout = options.ReadTimeoutMs;
            port.WriteTimeout = options.WriteTimeoutMs;
        }

        private static byte[] PrepareRom(byte[] romBytes)
        {
            if (romBytes == null)
            {
                throw new ArgumentNullException("romBytes");
            }

            if (romBytes.Length > MaxRomSizeBytes)
            {
                throw new ArgumentException("The ROM is larger than the GBA multiboot RAM limit (256 KiB).", "romBytes");
            }

            if (romBytes.Length < HeaderLengthBytes)
            {
                throw new ArgumentException("The ROM is too small to contain a valid multiboot header.", "romBytes");
            }

            int paddedLength = (romBytes.Length + 15) & ~15;
            byte[] preparedRom = new byte[paddedLength];
            Buffer.BlockCopy(romBytes, 0, preparedRom, 0, romBytes.Length);
            return preparedRom;
        }

        private bool ExecuteUpload(byte[] rom, IProgress<MultibootUploadProgress> progress, CancellationToken cancellationToken)
        {
            int headerWordCount = HeaderLengthBytes / 2;
            int payloadWordCount = (rom.Length - HeaderLengthBytes) / 4;
            int totalBytes = rom.Length;
            int totalUnits = headerWordCount + payloadWordCount + 4;
            int completedUnits = 0;

            ReportProgress(
                MultibootTransferStage.Preparing,
                completedUnits,
                totalUnits,
                0,
                totalBytes,
                true,
                "Preparing transfer.",
                progress);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (!WaitForClient(cancellationToken, progress, totalUnits, totalBytes, completedUnits))
            {
                return false;
            }

            ushort? masterInfoReply = Xfer16(0x6102);
            if (!masterInfoReply.HasValue)
            {
                return false;
            }

            ExpectEqual(masterInfoReply.Value, 0x7202, "master/slave info exchange");

            ReportProgress(
                MultibootTransferStage.SendingHeader,
                completedUnits,
                totalUnits,
                0,
                totalBytes,
                false,
                "Sending multiboot header.",
                progress);

            for (int offset = 0; offset < HeaderLengthBytes; offset += 2)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                ushort headerWord = (ushort)(rom[offset] | (rom[offset + 1] << 8));
                ushort? replyValue = Xfer16(headerWord);
                if (!replyValue.HasValue)
                {
                    return false;
                }

                ushort reply = replyValue.Value;
                int expectedIndex = (HeaderLengthBytes - offset) / 2;

                if (((reply >> 8) & 0xFF) != expectedIndex)
                {
                    throw new MultibootProtocolException(
                        string.Format("Header acknowledgement mismatch at offset 0x{0:X}: expected index {1}, received 0x{2:X4}.", offset, expectedIndex, reply));
                }

                if ((reply & 0xFF) != ExpectedClientId)
                {
                    throw new MultibootProtocolException(
                        string.Format("Header acknowledgement returned unexpected client id 0x{0:X2}.", reply & 0xFF));
                }

                completedUnits++;
                ReportProgress(
                    MultibootTransferStage.SendingHeader,
                    completedUnits,
                    totalUnits,
                    offset + 2,
                    totalBytes,
                    false,
                    "Sending multiboot header.",
                    progress);
            }

            ushort? headerCompleteReply = Xfer16(0x6200);
            if (!headerCompleteReply.HasValue)
            {
                return false;
            }

            ExpectEqual(headerCompleteReply.Value, ExpectedClientId, "header completion");

            ushort? postHeaderReply = Xfer16(0x6202);
            if (!postHeaderReply.HasValue)
            {
                return false;
            }

            ExpectEqual(postHeaderReply.Value, 0x7202, "post-header info exchange");

            ReportProgress(
                MultibootTransferStage.Negotiating,
                completedUnits,
                totalUnits,
                HeaderLengthBytes,
                totalBytes,
                true,
                "Negotiating encrypted payload transfer.",
                progress);

            ushort? readyReplyValue = WaitForReadyState(cancellationToken);
            if (!readyReplyValue.HasValue)
            {
                return false;
            }

            ushort readyReply = readyReplyValue.Value;
            byte clientData = (byte)(readyReply & 0xFF);
            byte handshake = (byte)((0x11 + clientData + 0xFF + 0xFF) & 0xFF);

            ushort? handshakeReplyValue = Xfer16((ushort)(0x6400 | handshake));
            if (!handshakeReplyValue.HasValue)
            {
                return false;
            }

            ushort handshakeReply = handshakeReplyValue.Value;
            ExpectHighByte(handshakeReply, 0x73, "handshake");

            if (_options.PostHandshakeDelayMs > 0)
            {
                Thread.Sleep(_options.PostHandshakeDelayMs);
            }

            ushort lengthInfo = unchecked((ushort)(((rom.Length - HeaderLengthBytes) / 4) - 0x34));
            ushort? lengthReplyValue = Xfer16(lengthInfo);
            if (!lengthReplyValue.HasValue)
            {
                return false;
            }

            ushort lengthReply = lengthReplyValue.Value;
            ExpectHighByte(lengthReply, 0x73, "length exchange");
            byte lengthToken = (byte)(lengthReply & 0xFF);

            completedUnits++;
            ReportProgress(
                MultibootTransferStage.SendingPayload,
                completedUnits,
                totalUnits,
                HeaderLengthBytes,
                totalBytes,
                false,
                "Sending encrypted payload.",
                progress);

            uint checksum = 0xC387;
            uint polynomial = 0xC37B;
            uint key = 0x43202F2F;
            uint seed = unchecked(0xFFFF0000U | ((uint)clientData << 8) | _options.Palette);
            uint footer = unchecked(0xFFFF0000U | ((uint)lengthToken << 8) | handshake);
            int payloadWordsSent = 0;
            int reportStride = Math.Max(1, _options.ProgressUpdateStrideWords);

            for (int offset = HeaderLengthBytes; offset < rom.Length; offset += 4)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                uint data =
                    (uint)rom[offset] |
                    ((uint)rom[offset + 1] << 8) |
                    ((uint)rom[offset + 2] << 16) |
                    ((uint)rom[offset + 3] << 24);

                checksum = UpdateChecksum(checksum ^ data, polynomial);
                seed = unchecked(0x6F646573U * seed + 1U);

                uint complement = unchecked((uint)(-0x02000000 - offset));
                uint encrypted = data ^ complement ^ seed ^ key;
                uint? replyValue = Xfer32(encrypted);
                if (!replyValue.HasValue)
                {
                    return false;
                }

                uint reply = replyValue.Value;

                if ((reply >> 16) != (uint)(offset & 0xFFFF))
                {
                    throw new MultibootProtocolException(
                        string.Format("Payload acknowledgement mismatch at offset 0x{0:X}: received 0x{1:X8}.", offset, reply));
                }

                payloadWordsSent++;

                if ((payloadWordsSent % reportStride) == 0 || payloadWordsSent == payloadWordCount)
                {
                    ReportProgress(
                        MultibootTransferStage.SendingPayload,
                        completedUnits + payloadWordsSent,
                        totalUnits,
                        offset + 4,
                        totalBytes,
                        false,
                        "Sending encrypted payload.",
                        progress);
                }
            }

            completedUnits += payloadWordsSent;
            checksum = UpdateChecksum(checksum ^ footer, polynomial);

            ReportProgress(
                MultibootTransferStage.Finalizing,
                completedUnits,
                totalUnits,
                totalBytes,
                totalBytes,
                true,
                "Finalizing transfer.",
                progress);

            ushort? payloadCompleteReply = Xfer16(0x0065);
            if (!payloadCompleteReply.HasValue)
            {
                return false;
            }

            ExpectEqual(payloadCompleteReply.Value, rom.Length & 0xFFFF, "payload completion acknowledgement");
            completedUnits++;

            if (!WaitForCrcWindow(cancellationToken))
            {
                return false;
            }

            completedUnits++;

            ushort? crcReadyReply = Xfer16(0x0066);
            if (!crcReadyReply.HasValue)
            {
                return false;
            }

            ExpectEqual(crcReadyReply.Value, 0x0075, "CRC ready signal");
            completedUnits++;

            ushort crc = unchecked((ushort)checksum);
            ushort? crcReply = Xfer16(crc);
            if (!crcReply.HasValue)
            {
                return false;
            }

            ExpectEqual(crcReply.Value, crc, "CRC exchange");
            completedUnits++;

            ReportProgress(
                MultibootTransferStage.Completed,
                completedUnits,
                totalUnits,
                totalBytes,
                totalBytes,
                false,
                "Upload complete.",
                progress);

            return true;
        }

        private bool WaitForClient(CancellationToken cancellationToken, IProgress<MultibootUploadProgress> progress, int totalUnits, int totalBytes, int completedUnits)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                ushort? replyValue;

                try
                {
                    replyValue = Xfer16(0x6202);
                }
                catch (TimeoutException)
                {
                    if (HasTimedOut(stopwatch, _options.ClientConnectTimeoutMs))
                    {
                        throw new TimeoutException(
                            string.Format(
                                "Timed out after {0} seconds while waiting for the GBA adapter to respond to the initial multiboot handshake.",
                                _options.ClientConnectTimeoutMs / 1000));
                    }

                    DrainTimedOutTransaction();
                    Thread.Sleep(_options.RetryDelayMs);
                    continue;
                }

                if (!replyValue.HasValue)
                {
                    return false;
                }

                ushort reply = replyValue.Value;
                ReportProgress(
                    MultibootTransferStage.WaitingForClient,
                    completedUnits,
                    totalUnits,
                    0,
                    totalBytes,
                    true,
                    string.Format("Waiting for GBA client. Last reply: 0x{0:X4}.", reply),
                    progress);

                if ((reply & 0xFFF0) == 0x7200)
                {
                    int clientId = reply & 0xF;

                    if (clientId != ExpectedClientId)
                    {
                        throw new MultibootProtocolException(
                            string.Format("Expected client id 0x{0:X}, received 0x{1:X}.", ExpectedClientId, clientId));
                    }

                    return true;
                }

                if (HasTimedOut(stopwatch, _options.ClientConnectTimeoutMs))
                {
                    throw new TimeoutException(
                        string.Format(
                            "Timed out after {0} seconds while waiting for the GBA to enter multiboot mode.",
                            _options.ClientConnectTimeoutMs / 1000));
                }

                Thread.Sleep(_options.RetryDelayMs);
            }
        }

        private ushort? WaitForReadyState(CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                ushort? replyValue;

                try
                {
                    replyValue = Xfer16((ushort)(0x6300 | _options.Palette));
                }
                catch (TimeoutException)
                {
                    if (HasTimedOut(stopwatch, _options.ReadyTimeoutMs))
                    {
                        throw new TimeoutException("Timed out waiting for the GBA to become ready for encrypted transfer.");
                    }

                    DrainTimedOutTransaction();
                    continue;
                }

                if (!replyValue.HasValue)
                {
                    return null;
                }

                ushort reply = replyValue.Value;
                if ((reply & 0xFF00) == 0x7300)
                {
                    return reply;
                }

                if (HasTimedOut(stopwatch, _options.ReadyTimeoutMs))
                {
                    throw new TimeoutException("Timed out waiting for the GBA to become ready for encrypted transfer.");
                }
            }
        }

        private bool WaitForCrcWindow(CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                ushort? replyValue;

                try
                {
                    replyValue = Xfer16(0x0065);
                }
                catch (TimeoutException)
                {
                    if (HasTimedOut(stopwatch, _options.FinalizationTimeoutMs))
                    {
                        throw new TimeoutException("Timed out waiting for the GBA to request the CRC.");
                    }

                    DrainTimedOutTransaction();
                    continue;
                }

                if (!replyValue.HasValue)
                {
                    return false;
                }

                ushort reply = replyValue.Value;
                if (reply == 0x0075)
                {
                    return true;
                }

                if (reply != 0x0074)
                {
                    throw new MultibootProtocolException(
                        string.Format("Unexpected CRC wait acknowledgement 0x{0:X4}.", reply));
                }

                if (HasTimedOut(stopwatch, _options.FinalizationTimeoutMs))
                {
                    throw new TimeoutException("Timed out waiting for the GBA to request the CRC.");
                }
            }
        }

        private static bool HasTimedOut(Stopwatch stopwatch, int timeoutMs)
        {
            return timeoutMs >= 0 && stopwatch.ElapsedMilliseconds > timeoutMs;
        }

        private void DrainStartupBanner()
        {
            if (!_port.IsOpen)
            {
                return;
            }

            Stopwatch overall = Stopwatch.StartNew();
            Stopwatch quiet = Stopwatch.StartNew();
            StringBuilder startupText = new StringBuilder();

            while (overall.ElapsedMilliseconds < _options.StartupDrainMs)
            {
                if (_port.BytesToRead > 0)
                {
                    startupText.Append(_port.ReadExisting());
                    quiet.Restart();

                    if (startupText.ToString().IndexOf("Ready", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        break;
                    }
                }
                else if (startupText.Length > 0 && quiet.ElapsedMilliseconds >= 100)
                {
                    break;
                }

                Thread.Sleep(15);
            }

            _port.DiscardInBuffer();
            _port.DiscardOutBuffer();
        }

        private void DrainTimedOutTransaction()
        {
            if (!_port.IsOpen)
            {
                return;
            }

            Thread.Sleep(_options.RetryDelayMs);

            if (_port.BytesToRead > 0)
            {
                _port.DiscardInBuffer();
            }
        }

        private uint? Xfer32(uint output)
        {
            if (_activeCancellationToken.IsCancellationRequested)
            {
                return null;
            }

            byte[] outputBytes = new byte[4];
            outputBytes[0] = (byte)(output & 0xFF);
            outputBytes[1] = (byte)((output >> 8) & 0xFF);
            outputBytes[2] = (byte)((output >> 16) & 0xFF);
            outputBytes[3] = (byte)((output >> 24) & 0xFF);

            _port.Write(outputBytes, 0, outputBytes.Length);
            _port.BaseStream.Flush();

            byte[] inputBytes = ReadExact(4);
            if (inputBytes == null)
            {
                return null;
            }

            return
                (uint)inputBytes[0] |
                ((uint)inputBytes[1] << 8) |
                ((uint)inputBytes[2] << 16) |
                ((uint)inputBytes[3] << 24);
        }

        private ushort? Xfer16(ushort output)
        {
            uint? reply = Xfer32(output);
            if (!reply.HasValue)
            {
                return null;
            }

            return (ushort)(reply.Value >> 16);
        }

        private byte[] ReadExact(int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;

            while (offset < length)
            {
                if (_activeCancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                int read;

                try
                {
                    read = _port.Read(buffer, offset, length - offset);
                }
                catch (TimeoutException)
                {
                    if (_activeCancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }

                    throw;
                }

                if (read <= 0)
                {
                    throw new TimeoutException(
                        string.Format("Timed out while reading {0} bytes from the serial port.", length));
                }

                offset += read;
            }

            return buffer;
        }

        private static uint UpdateChecksum(uint value, uint polynomial)
        {
            for (int bit = 0; bit < 32; bit++)
            {
                uint carry = value & 1U;
                value >>= 1;

                if (carry != 0)
                {
                    value ^= polynomial;
                }
            }

            return value;
        }

        private static void ExpectEqual(int actual, int expected, string operation)
        {
            if (actual != expected)
            {
                throw new MultibootProtocolException(
                    string.Format("Unexpected response during {0}: expected 0x{1:X4}, received 0x{2:X4}.", operation, expected, actual));
            }
        }

        private static void ExpectHighByte(ushort reply, int expectedHighByte, string operation)
        {
            if ((reply & 0xFF00) != (expectedHighByte << 8))
            {
                throw new MultibootProtocolException(
                    string.Format("Unexpected response during {0}: received 0x{1:X4}.", operation, reply));
            }
        }

        private void ReportProgress(
            MultibootTransferStage stage,
            int completedUnits,
            int totalUnits,
            int bytesTransferred,
            int totalBytes,
            bool isIndeterminate,
            string message,
            IProgress<MultibootUploadProgress> progress)
        {
            MultibootUploadProgress snapshot = new MultibootUploadProgress(
                stage,
                completedUnits,
                totalUnits,
                bytesTransferred,
                totalBytes,
                isIndeterminate,
                message);

            if (progress != null)
            {
                progress.Report(snapshot);
            }

            EventHandler<MultibootProgressChangedEventArgs> handler = ProgressChanged;
            if (handler != null)
            {
                handler(this, new MultibootProgressChangedEventArgs(snapshot));
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}

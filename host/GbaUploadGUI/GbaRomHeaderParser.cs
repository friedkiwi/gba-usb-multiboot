using System;
using System.Linq;
using System.Text;

namespace GbaUploadGUI
{
    public enum GbaRomValidationError
    {
        InvalidFile,
        InvalidContents,
        NotMultibootRom,
        PatchableMultibootEntryMissing
    }

    public sealed class GbaRomHeaderValidationException : InvalidOperationException
    {
        public GbaRomHeaderValidationException(GbaRomValidationError error, string message, GbaRomHeader header = null)
            : base(message)
        {
            Error = error;
            Header = header;
        }

        public GbaRomValidationError Error { get; private set; }

        public GbaRomHeader Header { get; private set; }
    }

    public sealed class GbaRomHeader
    {
        public GbaRomHeader(
            string title,
            string gameCode,
            byte storedComplementCheck,
            byte calculatedComplementCheck,
            uint cartEntryInstruction,
            uint normalMultiplayEntryInstruction,
            bool hasCartEntryPoint,
            bool hasNormalMultiplayEntryPoint,
            bool isNormalMultiplayEntryEmpty)
        {
            Title = title;
            GameCode = gameCode;
            StoredComplementCheck = storedComplementCheck;
            CalculatedComplementCheck = calculatedComplementCheck;
            CartEntryInstruction = cartEntryInstruction;
            NormalMultiplayEntryInstruction = normalMultiplayEntryInstruction;
            HasCartEntryPoint = hasCartEntryPoint;
            HasNormalMultiplayEntryPoint = hasNormalMultiplayEntryPoint;
            IsNormalMultiplayEntryEmpty = isNormalMultiplayEntryEmpty;
        }

        public string Title { get; private set; }

        public string GameCode { get; private set; }

        public byte StoredComplementCheck { get; private set; }

        public byte CalculatedComplementCheck { get; private set; }

        public uint CartEntryInstruction { get; private set; }

        public uint NormalMultiplayEntryInstruction { get; private set; }

        public bool HasCartEntryPoint { get; private set; }

        public bool HasNormalMultiplayEntryPoint { get; private set; }

        public bool IsNormalMultiplayEntryEmpty { get; private set; }

        public bool CanPatchMultibootEntryFromCart
        {
            get { return IsNormalMultiplayEntryEmpty && HasCartEntryPoint; }
        }
    }

    public sealed class GbaPreparedRom
    {
        public GbaPreparedRom(byte[] romBytes, GbaRomHeader header, bool wasPatchedInMemory)
        {
            RomBytes = romBytes;
            Header = header;
            WasPatchedInMemory = wasPatchedInMemory;
        }

        public byte[] RomBytes { get; private set; }

        public GbaRomHeader Header { get; private set; }

        public bool WasPatchedInMemory { get; private set; }
    }

    public static class GbaRomHeaderParser
    {
        private const int GbaHeaderLength = 0xC0;
        private const int MaxMultibootRomSize = 0x40000;
        private const int GameTitleOffset = 0xA0;
        private const int GameTitleLength = 12;
        private const int GameCodeOffset = 0xAC;
        private const int GameCodeLength = 4;
        private const int ComplementCheckOffset = 0xBD;
        private const int CartEntryPointOffset = 0x00;
        private const int NormalMultiplayEntryPointOffset = 0xC0;

        public static GbaRomHeader Parse(byte[] romBytes)
        {
            ValidateFileShape(romBytes);

            string title = ReadAsciiField(romBytes, GameTitleOffset, GameTitleLength, "(untitled)");
            string gameCode = ReadAsciiField(romBytes, GameCodeOffset, GameCodeLength, "N/A");
            byte storedComplementCheck = romBytes[ComplementCheckOffset];
            byte calculatedComplementCheck = CalculateComplementCheck(romBytes);

            if (storedComplementCheck != calculatedComplementCheck)
            {
                throw new GbaRomHeaderValidationException(
                    GbaRomValidationError.InvalidContents,
                    string.Format(
                        "Invalid ROM file contents: the header complement check failed (expected 0x{0:X2}, found 0x{1:X2}).",
                        calculatedComplementCheck,
                        storedComplementCheck));
            }

            uint cartEntryInstruction = BitConverter.ToUInt32(romBytes, CartEntryPointOffset);
            uint normalMultiplayEntryInstruction = BitConverter.ToUInt32(romBytes, NormalMultiplayEntryPointOffset);

            return new GbaRomHeader(
                title,
                gameCode,
                storedComplementCheck,
                calculatedComplementCheck,
                cartEntryInstruction,
                normalMultiplayEntryInstruction,
                IsArmBranchInstruction(cartEntryInstruction),
                IsArmBranchInstruction(normalMultiplayEntryInstruction),
                IsEmptyInstruction(normalMultiplayEntryInstruction));
        }

        public static GbaPreparedRom PrepareForMultiboot(byte[] romBytes, bool patchMissingMultibootEntry)
        {
            GbaRomHeader header = Parse(romBytes);

            if (header.HasNormalMultiplayEntryPoint)
            {
                return new GbaPreparedRom((byte[])romBytes.Clone(), header, false);
            }

            if (patchMissingMultibootEntry && header.CanPatchMultibootEntryFromCart)
            {
                byte[] patchedRom = (byte[])romBytes.Clone();
                byte[] instructionBytes = BitConverter.GetBytes(header.CartEntryInstruction);
                Buffer.BlockCopy(instructionBytes, 0, patchedRom, NormalMultiplayEntryPointOffset, instructionBytes.Length);
                WriteComplementCheck(patchedRom);

                GbaRomHeader patchedHeader = Parse(patchedRom);
                if (!patchedHeader.HasNormalMultiplayEntryPoint)
                {
                    throw new GbaRomHeaderValidationException(
                        GbaRomValidationError.NotMultibootRom,
                        "The experimental in-memory patch did not produce a valid multiboot entry point.",
                        patchedHeader);
                }

                return new GbaPreparedRom(patchedRom, patchedHeader, true);
            }

            if (header.CanPatchMultibootEntryFromCart)
            {
                throw new GbaRomHeaderValidationException(
                    GbaRomValidationError.PatchableMultibootEntryMissing,
                    "This ROM looks like a valid GBA ROM and fits in multiboot RAM, but the dedicated multiboot entry point is empty. " +
                    "Its normal cartridge entry point looks patchable, so an experimental in-memory patch may work.",
                    header);
            }

            throw new GbaRomHeaderValidationException(
                GbaRomValidationError.NotMultibootRom,
                string.Format(
                    "This is a valid GBA ROM, but not a valid multiboot ROM.{0}Multiboot entry: 0x{1:X8}{0}Classic entry: 0x{2:X8}",
                    Environment.NewLine,
                    header.NormalMultiplayEntryInstruction,
                    header.CartEntryInstruction),
                header);
        }

        private static void ValidateFileShape(byte[] romBytes)
        {
            if (romBytes == null || romBytes.Length == 0)
            {
                throw new GbaRomHeaderValidationException(
                    GbaRomValidationError.InvalidFile,
                    "The selected file is empty.");
            }

            if (romBytes.Length < GbaHeaderLength)
            {
                throw new GbaRomHeaderValidationException(
                    GbaRomValidationError.InvalidFile,
                    "The selected file is too small to contain a valid GBA header.");
            }

            if (romBytes.Length > MaxMultibootRomSize)
            {
                throw new GbaRomHeaderValidationException(
                    GbaRomValidationError.InvalidFile,
                    "The selected file exceeds the 256 KiB GBA multiboot limit.");
            }
        }

        private static byte CalculateComplementCheck(byte[] romBytes)
        {
            int checksum = 0;

            for (int offset = GameTitleOffset; offset <= 0xBC; offset++)
            {
                checksum -= romBytes[offset];
            }

            checksum -= 0x19;
            return (byte)(checksum & 0xFF);
        }

        private static void WriteComplementCheck(byte[] romBytes)
        {
            romBytes[ComplementCheckOffset] = CalculateComplementCheck(romBytes);
        }

        private static bool IsArmBranchInstruction(uint instruction)
        {
            return (instruction & 0x0E000000U) == 0x0A000000U;
        }

        private static bool IsEmptyInstruction(uint instruction)
        {
            return instruction == 0x00000000U || instruction == 0xFFFFFFFFU;
        }

        private static string ReadAsciiField(byte[] romBytes, int offset, int length, string fallback)
        {
            string value = Encoding.ASCII.GetString(romBytes, offset, length)
                .TrimEnd('\0', ' ')
                .Trim();

            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            return new string(value.Where(ch => !char.IsControl(ch)).ToArray());
        }
    }
}

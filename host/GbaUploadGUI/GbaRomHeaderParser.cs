using System;
using System.Linq;
using System.Text;

namespace GbaUploadGUI
{
    public enum GbaRomValidationError
    {
        InvalidFile,
        InvalidContents,
        NotMultibootRom
    }

    public sealed class GbaRomHeaderValidationException : InvalidOperationException
    {
        public GbaRomHeaderValidationException(GbaRomValidationError error, string message)
            : base(message)
        {
            Error = error;
        }

        public GbaRomValidationError Error { get; private set; }
    }

    public sealed class GbaRomHeader
    {
        public GbaRomHeader(
            string title,
            string gameCode,
            byte storedComplementCheck,
            byte calculatedComplementCheck,
            uint normalMultiplayEntryInstruction,
            bool hasNormalMultiplayEntryPoint)
        {
            Title = title;
            GameCode = gameCode;
            StoredComplementCheck = storedComplementCheck;
            CalculatedComplementCheck = calculatedComplementCheck;
            NormalMultiplayEntryInstruction = normalMultiplayEntryInstruction;
            HasNormalMultiplayEntryPoint = hasNormalMultiplayEntryPoint;
        }

        public string Title { get; private set; }

        public string GameCode { get; private set; }

        public byte StoredComplementCheck { get; private set; }

        public byte CalculatedComplementCheck { get; private set; }

        public uint NormalMultiplayEntryInstruction { get; private set; }

        public bool HasNormalMultiplayEntryPoint { get; private set; }
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
        private const int NormalMultiplayEntryPointOffset = 0xC0;

        public static GbaRomHeader Parse(byte[] romBytes)
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

            uint normalMultiplayEntryInstruction = BitConverter.ToUInt32(romBytes, NormalMultiplayEntryPointOffset);
            bool hasNormalMultiplayEntryPoint = IsArmBranchInstruction(normalMultiplayEntryInstruction);

            if (!hasNormalMultiplayEntryPoint)
            {
                throw new GbaRomHeaderValidationException(
                    GbaRomValidationError.NotMultibootRom,
                    "This is a valid GBA ROM, but not a valid multiboot ROM.");
            }

            return new GbaRomHeader(
                title,
                gameCode,
                storedComplementCheck,
                calculatedComplementCheck,
                normalMultiplayEntryInstruction,
                hasNormalMultiplayEntryPoint);
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

        private static bool IsArmBranchInstruction(uint instruction)
        {
            return (instruction & 0x0E000000U) == 0x0A000000U;
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

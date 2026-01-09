using PeNet.Header.Pe;
using PeNet;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace FrostyStringGeneration
{
    internal class FrostbiteStringScannerParams
    {
        /// <summary>
        /// If true, escape control characters (\t, \n, \r) if a string includes them.
        /// </summary>
        public bool EscapeSpecialCharacters { get; set; } = true;

        /// <summary>
        /// If true, allow scanning for UTF16LE strings.
        /// </summary>
        public bool ScanForUtf16Le { get; set; } = true;

        /// <summary>
        /// If non-null and non-empty, the scanner will only start recording strings after encountering a string that matches this value.
        /// </summary>
        public string StartOnString { get; set; } = null;

        /// <summary>
        /// If non-null and non-empty, the scanning process will stop immediately after encountering a string that matches this value.
        /// </summary>
        public string StopOnString { get; set; } = null;
    }

    internal class FrostbiteStringScanner
    {
        private readonly PeFile m_peFile;
        private readonly string m_filePath;
        private readonly byte[] m_fileBytes;
        private readonly ulong m_fileLen;

        private readonly ulong m_imageBase;
        private readonly ulong m_pointerRangeStart;
        private readonly ulong m_pointerRangeEnd;

        private readonly FrostbiteStringScannerParams m_scanParams;

        private bool m_hasEncounteredStartOnString = false;

        private enum StringType
        {
            Ascii,
            Utf16,
            Unknown
        }

        public FrostbiteStringScanner(string executableFilePath)
            : this(executableFilePath, null)
        {
        }

        public FrostbiteStringScanner(string executableFilePath, FrostbiteStringScannerParams scanParams)
        {
            m_scanParams = scanParams ?? new FrostbiteStringScannerParams();

            m_peFile = new PeFile(executableFilePath);
            if (m_peFile.ImageNtHeaders == null)
                throw new InvalidOperationException("Failed to parse PE file. 'ImageNtHeaders' is null, so this is likely not a valid PE file.");

            m_filePath = executableFilePath;
            m_fileBytes = File.ReadAllBytes(executableFilePath);
            m_fileLen = (ulong)m_fileBytes.LongLength;

            m_imageBase = m_peFile.ImageNtHeaders.OptionalHeader.ImageBase;
            ulong sizeOfImage = m_peFile.ImageNtHeaders.OptionalHeader.SizeOfImage;
            m_pointerRangeStart = m_imageBase;
            m_pointerRangeEnd = m_imageBase + sizeOfImage;
        }

        private ImageSectionHeader FindSection(string sectionName)
        {
            if (m_peFile.ImageSectionHeaders != null)
            {
                foreach (var s in m_peFile.ImageSectionHeaders)
                {
                    if (s.Name.TrimEnd('\0') == sectionName)
                        return s;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if a ulong value is pointer-like, (for instance a value between 0x140000000 and 0x147000000).
        /// </summary>
        private bool IsPointerLike(ulong candidate) => candidate >= m_pointerRangeStart && candidate <= m_pointerRangeEnd;

        public void ScanSection(string sectionName, HashSet<string> foundStrings)
        {
            var targetSection = FindSection(sectionName);
            if (targetSection == null)
            {
                Console.WriteLine($"Could not find section: {sectionName}");
                return;
            }

            ulong sectionStart = targetSection.PointerToRawData;
            ulong sectionSize = targetSection.VirtualSize;
            ulong sectionEnd = sectionStart + sectionSize;
            if (sectionEnd > m_fileLen)
                sectionEnd = m_fileLen;

            ulong offset = sectionStart;
            while (offset < sectionEnd)
            {
                if (offset + 7 >= sectionEnd)
                    break;

                // 8-byte aligned => do additional pointer check
                if (offset % 8 == 0)
                {
                    // The 8-byte value must not be a pointer
                    ulong candidate = BitConverter.ToUInt64(m_fileBytes, (int)offset);
                    if (IsPointerLike(candidate))
                    {
                        offset += 8;
                        continue;
                    }

                    // Quickly check if the first byte is printable and non-zero
                    byte firstByte = m_fileBytes[offset];
                    if (!IsPrintableAscii(firstByte, true) || firstByte == 0x00)
                    {
                        offset += 4;
                        continue;
                    }

                    // Quickly check if there's an upcoming single byte-value null terminator before encountering a non-printable character
                    (bool hasForwardAsciiNullTerminator, ulong scannedBytes) = LookAheadForNullTerminator(offset, sectionEnd - 1, StringType.Ascii);
                    if (!hasForwardAsciiNullTerminator)
                    {
                        offset += scannedBytes;
                        continue;
                    }

                    if (!IsReadableStringHeuristic(offset, sectionEnd - 1, out StringType stringType, out ulong bytesToSkip))
                    {
                        offset += bytesToSkip;
                        continue;
                    }

                    // Attempt to read a string
                    (string readStr, ulong bytesConsumed) = ReadString(offset, sectionEnd - 1, stringType);

                    // Heuristic: if the previous byte is a lowercase alpha char, we skip the string entirely. lowercase only because including uppercase leads to too many false positives
                    // while the string is readable, this heuristic implies it's likely unaligned to 4, meaning its a useless string
                    byte previousByte = m_fileBytes[offset - 1];
                    if (IsLowerAlphaAscii(previousByte))
                    {
                        offset += bytesConsumed;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(readStr))
                    {
                        offset += bytesConsumed;

                        if (!string.IsNullOrWhiteSpace(readStr))
                        {
                            if (m_scanParams.EscapeSpecialCharacters)
                            {
                                readStr = EscapeSpecialCharacters(readStr);
                            }

                            AddString(readStr, foundStrings);

                            if (!string.IsNullOrEmpty(m_scanParams.StopOnString) &&
                                readStr == m_scanParams.StopOnString)
                            {
                                break; // immediately stop scanning this section
                            }
                        }
                        continue;
                    }
                    else
                    {
                        offset += 4;
                    }
                }
                // 4-byte aligned but not 8
                else if (offset % 4 == 0)
                {
                    // Quickly check if the first byte is printable and non-zero
                    byte firstByte = m_fileBytes[offset];
                    if (!IsPrintableAscii(firstByte, true) || firstByte == 0x00)
                    {
                        offset = AlignTo8(offset);
                        continue;
                    }

                    // Quickly check if there's an upcoming single-byte value null terminator before encountering a non-printable character
                    (bool hasForwardAsciiNullTerminator, ulong scannedBytes) = LookAheadForNullTerminator(offset, sectionEnd - 1, StringType.Ascii);
                    if (!hasForwardAsciiNullTerminator)
                    {
                        offset += scannedBytes;
                        continue;
                    }

                    if (!IsReadableStringHeuristic(offset, sectionEnd - 1, out StringType stringType, out ulong bytesToSkip))
                    {
                        offset += bytesToSkip;
                        continue;
                    }

                    // Attempt to read a string
                    (string readStr, ulong bytesConsumed) = ReadString(offset, sectionEnd - 1, stringType);

                    // Heuristic: if the previous byte is a lowercase alpha char, we skip the string entirely. lowercase only because including uppercase leads to too many false positives
                    // while the string is readable, this heuristic implies it's likely unaligned to 4, meaning its a useless string
                    byte previousByte = m_fileBytes[offset - 1];
                    if (IsLowerAlphaAscii(previousByte))
                    {
                        offset += bytesConsumed;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(readStr))
                    {
                        offset += bytesConsumed;

                        if (!string.IsNullOrWhiteSpace(readStr))
                        {
                            if (m_scanParams.EscapeSpecialCharacters)
                            {
                                readStr = EscapeSpecialCharacters(readStr);
                            }

                            AddString(readStr, foundStrings);

                            if (!string.IsNullOrEmpty(m_scanParams.StopOnString) &&
                                readStr == m_scanParams.StopOnString)
                            {
                                break; // immediately stop scanning this section
                            }
                        }
                        continue;
                    }
                    else
                    {
                        offset = AlignTo8(offset);
                    }
                }
                else
                {
                    // Align to 4
                    ulong nextOffset = AlignTo4(offset);
                    if (nextOffset < sectionEnd && nextOffset != offset)
                    {
                        offset = nextOffset;
                    }
                }
            }
        }

        private void AddString(string readStr, HashSet<string> foundStrings)
        {
            // If there's a StartOnString, skip adding until we encounter it.
            if (!string.IsNullOrEmpty(m_scanParams.StartOnString))
            {
                if (!m_hasEncounteredStartOnString)
                {
                    if (readStr == m_scanParams.StartOnString)
                    {
                        m_hasEncounteredStartOnString = true;
                    }
                    else
                    {
                        return;
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(readStr) && !readStr.All(char.IsWhiteSpace))
            {
                foundStrings.Add(readStr);
            }
        }

        private bool IsReadableStringHeuristic(ulong offset, ulong maxOffset, out StringType stringType, out ulong bytesToSkip)
        {
            bool result;
            stringType = StringType.Unknown;
            bytesToSkip = 0;

            (bool hasForwardAsciiNullTerminator, ulong bytesBeforeSingleByteNullTerminator) = LookAheadForNullTerminator(offset, maxOffset, StringType.Ascii);

            if (bytesBeforeSingleByteNullTerminator == 0x02)
            {
                ushort currentCh = BitConverter.ToUInt16(m_fileBytes, (int)offset);
                ushort nextCh = BitConverter.ToUInt16(m_fileBytes, (int)(offset + 2));

                if (IsPrintableAscii(m_fileBytes[offset]) && nextCh == 0x0000)
                {
                    // This is a heuristic to skip some strings by checking the next 4 bytes
                    int nextInt = BitConverter.ToInt32(m_fileBytes, (int)(offset + 4));
                    if (nextInt == Int32.MinValue // 00 00 00 80
                        || nextInt == 1065353216) // 00 00 80 3F
                    {
                        stringType = StringType.Unknown;
                        bytesToSkip = 8;
                        result = false;
                    }
                    else
                    {
                        stringType = StringType.Ascii;
                        result = true;
                    }
                }
                else if (m_scanParams.ScanForUtf16Le && IsPrintableAscii(m_fileBytes[offset]) && IsPrintableUtf16(currentCh, false) && IsPrintableUtf16(nextCh))
                {
                    (bool hasForwardUtf16NullTerminator, ulong bytesBeforeTwoByteNullTerminator) = LookAheadForNullTerminator(offset, maxOffset, StringType.Utf16);
                    if (!hasForwardUtf16NullTerminator)
                    {
                        stringType = StringType.Unknown;
                        bytesToSkip = 4;
                        result = false;
                    }
                    else
                    {
                        stringType = StringType.Utf16;
                        result = true;
                    }
                }
                else
                {
                    stringType = StringType.Ascii;
                    result = true;
                }
            }
            else
            {
                stringType = StringType.Ascii;
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Look ahead for a null terminator within [offset..offset+maxScan]. 
        /// Returns (true, scanned) if it finds 0x00 with no invalid chars, else (false, scanned).
        /// </summary>
        private (bool, ulong) LookAheadForNullTerminator(ulong offset, ulong maxOffset, StringType stringType)
        {
            ulong scannedBytes = 0;
            if (stringType == StringType.Ascii)
            {
                for (ulong i = offset; i <= maxOffset; i++)
                {
                    scannedBytes++;
                    byte b = m_fileBytes[i];

                    if (b == 0x00)
                    {
                        return (true, scannedBytes);
                    }

                    if (!IsPrintableAscii(b))
                    {
                        // Invalid character encountered
                        return (false, scannedBytes);
                    }
                }
            }
            else if (stringType == StringType.Utf16)
            {
                for (ulong i = offset; i <= maxOffset; i += 2)
                {
                    scannedBytes += 2;
                    ushort shortValue = BitConverter.ToUInt16(m_fileBytes, (int)i);

                    if (shortValue == 0x0000)
                    {
                        return (true, scannedBytes);
                    }

                    if (!IsPrintableUtf16(shortValue))
                    {
                        // Invalid character encountered
                        return (false, scannedBytes);
                    }
                }
            }
            return (false, scannedBytes);
        }

        /// <summary>
        /// Tries to read a ASCII or UTF-16 string at the offset.
        /// </summary>
        private (string, ulong) ReadString(ulong offset, ulong maxOffset, StringType type)
        {
            if (offset + 1 < m_fileLen)
            {
                if (type == StringType.Ascii)
                {
                    string asciiCandidate = ReadNullTerminatedAscii(offset, maxOffset);
                    if (!string.IsNullOrEmpty(asciiCandidate))
                    {
                        ulong consumed = (ulong)(asciiCandidate.Length + 1);
                        return (asciiCandidate, consumed);
                    }
                }
                else if (type == StringType.Utf16)
                {
                    string utf16Candidate = ReadNullTerminatedUtf16Le(offset, maxOffset);
                    if (!string.IsNullOrEmpty(utf16Candidate))
                    {
                        ulong consumed = (ulong)(utf16Candidate.Length * 2 + 2);
                        return (utf16Candidate, consumed);
                    }
                }
            }
            return ("", 0);
        }

        /// <summary>
        /// Reads a null-terminated ASCII string.
        /// Stops at 0x00 or a non-printable character.
        /// </summary>
        private string ReadNullTerminatedAscii(ulong offset, ulong maxOffset)
        {
            if (offset >= m_fileLen)
                return "";

            using (MemoryStream ms = new MemoryStream())
            {
                for (ulong i = offset; i <= maxOffset && i < m_fileLen; i++)
                {
                    byte b = m_fileBytes[i];
                    // Stop if null terminator or out of ASCII printable range
                    if (b == 0x00 || !IsPrintableAscii(b))
                        break;

                    ms.WriteByte(b);
                }
                return Encoding.ASCII.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// Reads a null-terminated UTF-16-LE string.
        /// Each character is 2 bytes. Stops at 0x0000 or a non-printable character.
        /// </summary>
        private string ReadNullTerminatedUtf16Le(ulong offset, ulong maxOffset)
        {
            if (offset >= m_fileLen)
                return "";

            using (MemoryStream ms = new MemoryStream())
            {
                for (ulong i = offset; i + 1 <= maxOffset && i + 1 < m_fileLen; i += 2)
                {
                    ushort ch = BitConverter.ToUInt16(m_fileBytes, (int)i);
                    // Stop if null terminator or out of Unicode's ascii printable range
                    if (ch == 0x0000 || !IsPrintableUtf16(ch))
                        break;

                    ms.WriteByte(m_fileBytes[i]);     // low byte
                    ms.WriteByte(m_fileBytes[i + 1]); // high byte
                }

                byte[] rawUtf16 = ms.ToArray();
                if (rawUtf16.Length == 0)
                    return "";

                string readStr = Encoding.Unicode.GetString(rawUtf16);
                return readStr;
            }
        }

        private bool IsPrintableAscii(byte b, bool includeControlChars = true)
        {
            // 0x09 = \t
            // 0x0A = \n
            // 0x0D = \r
            if (includeControlChars)
            {
                return (b >= 0x20 && b <= 0x7E) || b == 0x09 || b == 0x0A || b == 0x0D;
            }
            else
            {
                return (b >= 0x20 && b <= 0x7E);
            }
        }

        private bool IsPrintableUtf16(ushort value, bool includeControlChars = true)
        {
            // 0x09 = \t
            // 0x0A = \n
            // 0x0D = \r
            // We only care about ASCII range unicode values + basic control chars
            if (includeControlChars)
            {
                return (value >= 0x20 && value <= 0x7E) || value == 0x09 || value == 0x0A || value == 0x0D;
            }
            else
            {
                return (value >= 0x20 && value <= 0x7E);
            }
        }

        private bool IsLowerAlphaAscii(byte b)
        {
            return (b >= 0x61 && b <= 0x7A); // a-z
        }

        private bool IsLowerAlphaUtf16(ushort value)
        {
            return (value >= 0x61 && value <= 0x7A); // a-z
        }

        private ulong AlignTo4(ulong val)
        {
            ulong remainder = val % 4;
            return remainder == 0 ? val : val + (4 - remainder);
        }

        private ulong AlignTo8(ulong val)
        {
            ulong remainder = val % 8;
            return remainder == 0 ? val : val + (8 - remainder);
        }

        public static string EscapeSpecialCharacters(string input)
        {
            return input
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}

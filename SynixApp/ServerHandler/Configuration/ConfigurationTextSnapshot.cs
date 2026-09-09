// ============================================================================
// PROJECT: Synix Game Server Control Panel
// AUTHOR: Jason Turner (ubidzz)
// COPYRIGHT: © 2026 All Rights Reserved.
//
// LEGAL NOTICE:
// This source code is proprietary and confidential.
// 1. Permission is granted for PERSONAL, NON-COMMERCIAL use only.
// 2. You may modify this code for your own use, but you may NOT redistribute,
//    rebrand, or sell this code or derivative works without written consent.
// 3. The "Synix" brand and logic remain the property of Jason Turner.
// ============================================================================
using System.Text;

namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	internal sealed class ConfigurationTextSnapshot
	{
		public string Text { get; private init; } = string.Empty;
		public Encoding TextEncoding { get; private init; } = new UTF8Encoding(false);
		public bool HasByteOrderMark { get; private init; }

		public static ConfigurationTextSnapshot Read(string path)
		{
			byte[] bytes;
			using (FileStream stream = new(
				path,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite))
			{
				if (stream.Length > int.MaxValue)
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.FileTooLarge"));
				}

				bytes = new byte[(int)stream.Length];
				stream.ReadExactly(bytes);
			}

			(Encoding encoding, int preambleLength, bool hasBom) =
				DetectEncoding(bytes);
			return new ConfigurationTextSnapshot
			{
				Text = encoding.GetString(
					bytes,
					preambleLength,
					bytes.Length - preambleLength),
				TextEncoding = encoding,
				HasByteOrderMark = hasBom
			};
		}

		public byte[] Encode(string content)
		{
			byte[] contentBytes = TextEncoding.GetBytes(content);
			if (!HasByteOrderMark)
				return contentBytes;

			byte[] preamble = TextEncoding.GetPreamble();
			byte[] output = new byte[preamble.Length + contentBytes.Length];
			Buffer.BlockCopy(preamble, 0, output, 0, preamble.Length);
			Buffer.BlockCopy(contentBytes, 0, output, preamble.Length, contentBytes.Length);
			return output;
		}

		private static (Encoding Encoding, int PreambleLength, bool HasBom)
			DetectEncoding(byte[] bytes)
		{
			if (StartsWith(bytes, [0x00, 0x00, 0xFE, 0xFF]))
				return (new UTF32Encoding(true, true, true), 4, true);
			if (StartsWith(bytes, [0xFF, 0xFE, 0x00, 0x00]))
				return (new UTF32Encoding(false, true, true), 4, true);
			if (StartsWith(bytes, [0xEF, 0xBB, 0xBF]))
				return (new UTF8Encoding(true, true), 3, true);
			if (StartsWith(bytes, [0xFE, 0xFF]))
				return (new UnicodeEncoding(true, true, true), 2, true);
			if (StartsWith(bytes, [0xFF, 0xFE]))
				return (new UnicodeEncoding(false, true, true), 2, true);

			return IsValidUtf8(bytes)
				? (new UTF8Encoding(false, true), 0, false)
				: (CreateStrictLatin1Encoding(), 0, false);
		}

		private static bool StartsWith(byte[] source, byte[] prefix) =>
			source.AsSpan().StartsWith(prefix);

		private static bool IsValidUtf8(byte[] bytes)
		{
			try
			{
				_ = new UTF8Encoding(false, true).GetString(bytes);
				return true;
			}
			catch (DecoderFallbackException)
			{
				return false;
			}
		}

		private static Encoding CreateStrictLatin1Encoding()
		{
			Encoding encoding = (Encoding)Encoding.Latin1.Clone();
			encoding.EncoderFallback = EncoderFallback.ExceptionFallback;
			encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
			return encoding;
		}
	}
}

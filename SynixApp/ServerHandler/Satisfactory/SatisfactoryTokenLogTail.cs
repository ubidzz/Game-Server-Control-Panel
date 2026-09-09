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
using System.Security.Cryptography;
using System.Text;

namespace Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;

/// <summary>
/// A short-lived cursor into this server's own console-output log. Only bytes
/// appended after the command boundary are eligible; existing tokens are ignored.
/// Never copies output to Synix logs, the clipboard, reports or another file.
/// </summary>
internal sealed class SatisfactoryTokenLogTail : IDisposable
{
	private const int MaximumNewBytes = 2 * 1024 * 1024;
	private readonly FileStream _stream;
	private readonly byte[] _buffer = new byte[4096];
	private readonly char[] _characters = new char[Encoding.UTF8.GetMaxCharCount(4096)];
	private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
	private readonly StringBuilder _line = new();
	private long _boundary, _position;
	private byte[] _boundaryHash = [];
	private int _boundaryBytes, _bytesRead;
	private bool _discardLine, _captured, _invalid;

	private SatisfactoryTokenLogTail(FileStream stream) => _stream = stream;

	internal static SatisfactoryTokenLogTail? TryOpen(string installPath)
	{
		try
		{
			if (!Path.IsPathFullyQualified(installPath)) return null;
			string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(installPath));
			if (root == Path.TrimEndingDirectorySeparator(Path.GetPathRoot(root) ?? "")) return null;
			string path = Path.Combine(root, "FactoryGame", "Saved", "Logs", "FactoryGame.log");
			// Do not follow a link into another server or an unrelated private file.
			for (string? current = path; current != null; current = Path.GetDirectoryName(current))
			{
				if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return null;
			}
			return new(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
		{ return null; }
	}

	internal bool CaptureStart()
	{
		try
		{
			_boundary = _position = _stream.Length;
			_boundaryBytes = (int)Math.Min(_boundary, 128);
			_stream.Position = _boundary - _boundaryBytes;
			_stream.ReadExactly(_buffer.AsSpan(0, _boundaryBytes));
			_boundaryHash = SHA256.HashData(_buffer.AsSpan(0, _boundaryBytes));
			_discardLine = _boundaryBytes > 0 && _buffer[_boundaryBytes - 1] != (byte)'\n';
			_line.Clear();
			_decoder.Reset();
			_bytesRead = 0;
			_invalid = false;
			_captured = true;
			return true;
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{ _invalid = true; return false; }
	}

	internal string? ReadFreshToken(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (!_captured || _invalid) return null;
		try
		{
			long length = _stream.Length;
			if (length < _position || !BoundaryUnchanged()) { _invalid = true; return null; }
			_stream.Position = _position;
			// A per-read and total limit keep noisy servers from consuming unbounded
			// time or memory. A partial token is held until the complete line arrives.
			int available = (int)Math.Min(length - _position, 65536);
			while (available > 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
				int read = _stream.Read(_buffer, 0, Math.Min(available, _buffer.Length));
				if (read == 0) break;
				_position += read;
				_bytesRead += read;
				available -= read;
				if (_bytesRead > MaximumNewBytes) { _invalid = true; return null; }
				int count = _decoder.GetChars(_buffer, 0, read, _characters, 0, false);
				for (int index = 0; index < count; index++)
				{
					char character = _characters[index];
					if (character == '\n')
					{
						string? token = null;
						if (!_discardLine && _line.ToString().Contains(SatisfactoryTokenParser.ConsoleLabel, StringComparison.Ordinal))
							token = SatisfactoryTokenParser.Extract(_line.ToString());
						_line.Clear();
						_discardLine = false;
						if (token != null)
						{
							if (!BoundaryUnchanged()) { _invalid = true; return null; }
							return token;
						}
					}
					else if (!_discardLine)
					{
						if (_line.Length >= SatisfactoryTokenParser.MaximumInputLength) { _line.Clear(); _discardLine = true; }
						else _line.Append(character);
					}
				}
			}
			return null;
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{ _invalid = true; return null; }
	}

	private bool BoundaryUnchanged()
	{
		if (_stream.Length < _boundary) return false;
		Span<byte> previous = stackalloc byte[128];
		_stream.Position = _boundary - _boundaryBytes;
		_stream.ReadExactly(previous[.._boundaryBytes]);
		Span<byte> hash = stackalloc byte[32];
		SHA256.HashData(previous[.._boundaryBytes], hash);
		return CryptographicOperations.FixedTimeEquals(_boundaryHash, hash);
	}

	public void Dispose()
	{
		_stream.Dispose();
		CryptographicOperations.ZeroMemory(_buffer);
		Array.Clear(_characters);
		_line.Clear();
	}
}

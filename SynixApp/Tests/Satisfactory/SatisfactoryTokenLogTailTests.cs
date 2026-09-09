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
using Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class SatisfactoryTokenLogTailTests
{
	internal static string Token(char signature = 'A') => Convert.ToBase64String(Encoding.UTF8.GetBytes("{\r\n\t\"pl\": \"APIToken\"\r\n}")) + "." + new string(signature, 128);
	internal static string Line(char signature = 'A') => "[2026.09.05-15.54.59:642][803]" + SatisfactoryTokenParser.ConsoleLabel + " " + Token(signature) + "\r\n";

	[Theory]
	[InlineData('A')]
	[InlineData('B')]
	public void IgnoresExistingTokensButAcceptsANewCompleteResponseEvenWhenIdentical(char signature)
	{
		using SatisfactoryTokenLogFixture log = new(Line());
		using SatisfactoryTokenLogTail tail = Assert.IsType<SatisfactoryTokenLogTail>(SatisfactoryTokenLogTail.TryOpen(log.Root));
		Assert.True(tail.CaptureStart());
		Assert.Null(tail.ReadFreshToken(default));
		log.Append(Line(signature));
		Assert.Equal(Token(signature), tail.ReadFreshToken(default));
	}

	[Fact]
	public void WaitsForTheCompleteLineAndHandlesSplitWrites()
	{
		using SatisfactoryTokenLogFixture log = new("Existing output\n");
		using SatisfactoryTokenLogTail tail = Assert.IsType<SatisfactoryTokenLogTail>(SatisfactoryTokenLogTail.TryOpen(log.Root));
		Assert.True(tail.CaptureStart());
		string line = Line();
		log.Append(line[..70]);
		Assert.Null(tail.ReadFreshToken(default));
		log.Append(line[70..^2]);
		Assert.Null(tail.ReadFreshToken(default)); // Never accept a partial hex signature.
		log.Append("\r\n");
		Assert.Equal(Token(), tail.ReadFreshToken(default));
	}

	[Fact]
	public void DoesNotFinishAPreexistingPartialTokenLine()
	{
		string line = Line();
		using SatisfactoryTokenLogFixture log = new(line[..40]);
		using SatisfactoryTokenLogTail tail = Assert.IsType<SatisfactoryTokenLogTail>(SatisfactoryTokenLogTail.TryOpen(log.Root));
		Assert.True(tail.CaptureStart());
		log.Append(line[40..]);
		Assert.Null(tail.ReadFreshToken(default));
		log.Append(Line('B'));
		Assert.Equal(Token('B'), tail.ReadFreshToken(default));
	}

	[Fact]
	public void RejectsTruncatedOrRewrittenLogsWithoutReturningAnOldToken()
	{
		using SatisfactoryTokenLogFixture log = new("Original boundary\n");
		using SatisfactoryTokenLogTail tail = Assert.IsType<SatisfactoryTokenLogTail>(SatisfactoryTokenLogTail.TryOpen(log.Root));
		Assert.True(tail.CaptureStart());
		File.WriteAllText(log.Path, Line('B'), new UTF8Encoding(false));
		Assert.Null(tail.ReadFreshToken(default));
		log.Append(Line());
		Assert.Null(tail.ReadFreshToken(default));
	}

	[Fact]
	public void CanFollowANewResponseAfterManyOrdinaryLinesWithoutUnboundedBuffering()
	{
		using SatisfactoryTokenLogFixture log = new("Boundary\n");
		using SatisfactoryTokenLogTail tail = Assert.IsType<SatisfactoryTokenLogTail>(SatisfactoryTokenLogTail.TryOpen(log.Root));
		Assert.True(tail.CaptureStart());
		log.Append(new string('x', 70000) + "\n" + Line());
		Assert.Null(tail.ReadFreshToken(default));
		Assert.Equal(Token(), tail.ReadFreshToken(default));
	}

	[Fact]
	public void MissingOrNonAbsoluteInstallCannotReadAnotherFile()
	{
		Assert.Null(SatisfactoryTokenLogTail.TryOpen("relative-server"));
		Assert.Null(SatisfactoryTokenLogTail.TryOpen(System.IO.Path.GetPathRoot(Environment.CurrentDirectory)!));
		using SatisfactoryTokenLogFixture log = new("");
		File.Delete(log.Path);
		Assert.Null(SatisfactoryTokenLogTail.TryOpen(log.Root));
	}

	[Fact]
	public void CanceledReadDoesNotConsumeThePendingToken()
	{
		using SatisfactoryTokenLogFixture log = new("");
		using SatisfactoryTokenLogTail tail = Assert.IsType<SatisfactoryTokenLogTail>(SatisfactoryTokenLogTail.TryOpen(log.Root));
		Assert.True(tail.CaptureStart());
		log.Append(Line());
		using CancellationTokenSource canceled = new();
		canceled.Cancel();
		Assert.Throws<OperationCanceledException>(() => tail.ReadFreshToken(canceled.Token));
		Assert.Equal(Token(), tail.ReadFreshToken(default));
	}
}

internal sealed class SatisfactoryTokenLogFixture : IDisposable
{
	internal string Root { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SynixTokenLog_" + Guid.NewGuid().ToString("N"));
	internal string Path => System.IO.Path.Combine(Root, "FactoryGame", "Saved", "Logs", "FactoryGame.log");
	internal SatisfactoryTokenLogFixture(string initial)
	{
		Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
		File.WriteAllText(Path, initial, new UTF8Encoding(false));
	}
	internal void Append(string text) => File.AppendAllText(Path, text, new UTF8Encoding(false));
	public void Dispose()
	{
		File.Delete(Path);
		// Only delete the empty directories created by this fixture, never recurse.
		Directory.Delete(System.IO.Path.GetDirectoryName(Path)!);
		Directory.Delete(System.IO.Path.Combine(Root, "FactoryGame", "Saved"));
		Directory.Delete(System.IO.Path.Combine(Root, "FactoryGame"));
		Directory.Delete(Root);
	}
}

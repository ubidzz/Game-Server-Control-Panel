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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;

/// <summary>
/// Minimal Windows UI Automation reader. Uses the OS COM API rather than adding WPF
/// to the app. Legacy MSAA descriptions truncate long list-view columns, including tokens.
/// </summary>
internal static class SatisfactoryConsoleAccessibility
{
	internal static string? CaptureTail(IntPtr list, CancellationToken cancellationToken) =>
		ReadAppendedTokenLine(list, 0, null, cancellationToken, captureOnly: true);

	// Return a token only from rows appended after the captured boundary. An old
	// token is never a fallback, even if generation returns the same token again.
	internal static string? ReadAppendedTokenLine(IntPtr list, int appendedRows, string? boundary,
		CancellationToken cancellationToken, bool captureOnly = false)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (appendedRows < 0 || appendedRows > 2048)
			throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleTokenMissing);
		IUiAutomation? automation = null;
		IElement? root = null, row = null;
		ITreeWalker? walker = null;
		try
		{
			automation = (IUiAutomation)new CUiAutomation();
			Check(automation.ElementFromHandle(list, out root));
			Check(automation.GetRawViewWalker(out walker));
			if (root == null || walker == null) throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleUnavailable);
			Check(walker.GetLastChildElement(root, out row));
			if (captureOnly) return row == null ? null : RowSignature(walker, row, cancellationToken);
			string? tokenLine = null;
			for (int index = 0; index < appendedRows; index++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (row == null) throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleTokenMissing);
				tokenLine ??= ReadRow(walker, row, cancellationToken);
				Check(walker.GetPreviousSiblingElement(row, out IElement? previous));
				Release(row);
				row = previous;
			}
			if (boundary != null && (row == null || RowSignature(walker, row, cancellationToken) != boundary))
				throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleTokenMissing);
			return tokenLine;
		}
		finally { Release(row); Release(root); Release(walker); Release(automation); }
	}

	private static string RowSignature(ITreeWalker walker, IElement row, CancellationToken cancellationToken)
	{
		StringBuilder contents = new();
		IElement? cell = null;
		try
		{
			Check(row.GetRuntimeId(out int[] runtimeId));
			contents.AppendJoin(',', runtimeId).Append('|');
			// ListBox entries have their text on the row itself, without cells.
			// Include it so a cleared/replaced row cannot masquerade as the boundary.
			AppendName(contents, row);
			Check(walker.GetFirstChildElement(row, out cell));
			for (int columns = 0; cell != null && columns < 8; columns++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				AppendName(contents, cell);
				Check(walker.GetNextSiblingElement(cell, out IElement? next));
				Release(cell);
				cell = next;
			}
			return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contents.ToString())));
		}
		finally { Release(cell); }
	}

	private static void AppendName(StringBuilder contents, IElement element)
	{
		Check(element.GetCurrentPropertyValue(30005, out object value));
		try
		{
			if (value is string text && text.Length <= SatisfactoryTokenParser.MaximumInputLength)
				contents.Append(text.Length).Append(':').Append(text);
		}
		finally { Release(value); }
	}

	internal static string ReadLatestTokenLine(IntPtr list, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		IUiAutomation? automation = null;
		IElement? root = null, row = null;
		ITreeWalker? walker = null;
		try
		{
			automation = (IUiAutomation)new CUiAutomation();
			Check(automation.ElementFromHandle(list, out root));
			Check(automation.GetRawViewWalker(out walker));
			if (root == null || walker == null) throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleUnavailable);
			Check(walker.GetLastChildElement(root, out row));
			for (int rows = 0; row != null && rows < 2048; rows++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				string? output = ReadRow(walker, row, cancellationToken);
				if (output != null) return output;
				Check(walker.GetPreviousSiblingElement(row, out IElement? previous));
				Release(row);
				row = previous;
			}
			throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleTokenMissing);
		}
		finally { Release(row); Release(root); Release(walker); Release(automation); }
	}

	private static string? ReadRow(ITreeWalker walker, IElement row, CancellationToken cancellationToken)
	{
		IElement? cell = null;
		try
		{
			Check(walker.GetFirstChildElement(row, out cell));
			for (int columns = 0; cell != null && columns < 8; columns++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				string? output = TokenLine(cell);
				if (output != null) return output;
				Check(walker.GetNextSiblingElement(cell, out IElement? next));
				Release(cell);
				cell = next;
			}
			return TokenLine(row);
		}
		finally { Release(cell); }
	}

	private static string? TokenLine(IElement element)
	{
		const int nameProperty = 30005; // UIA_NamePropertyId in UIAutomationClient.h.
		Check(element.GetCurrentPropertyValue(nameProperty, out object value));
		try
		{
			return value is string text && text.Length <= SatisfactoryTokenParser.MaximumInputLength &&
				text.Contains(SatisfactoryTokenParser.ConsoleLabel, StringComparison.Ordinal) ? text : null;
		}
		finally { Release(value); }
	}

	private static void Check(int result)
	{
		if (result < 0) throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleUnavailable);
	}
	private static void Release(object? value)
	{
		if (value != null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);
	}

	// ABI prefixes copied from Windows SDK UIAutomationClient.h. Unused entries are
	// retained in their original order so the called methods have the correct slots.
	[ComImport, Guid("ff48dba4-60ef-4201-aa87-54103eef594e")]
	private class CUiAutomation { }
	[ComImport, Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IUiAutomation
	{
		[PreserveSig] int CompareElements(IntPtr first, IntPtr second, out int same);
		[PreserveSig] int CompareRuntimeIds(IntPtr first, IntPtr second, out int same);
		[PreserveSig] int GetRootElement(out IntPtr element);
		[PreserveSig] int ElementFromHandle(IntPtr window, out IElement? element);
		[PreserveSig] int ElementFromPoint(NativePoint point, out IntPtr element);
		[PreserveSig] int GetFocusedElement(out IntPtr element);
		[PreserveSig] int GetRootElementBuildCache(IntPtr cache, out IntPtr element);
		[PreserveSig] int ElementFromHandleBuildCache(IntPtr window, IntPtr cache, out IntPtr element);
		[PreserveSig] int ElementFromPointBuildCache(NativePoint point, IntPtr cache, out IntPtr element);
		[PreserveSig] int GetFocusedElementBuildCache(IntPtr cache, out IntPtr element);
		[PreserveSig] int CreateTreeWalker(IntPtr condition, out IntPtr walker);
		[PreserveSig] int GetControlViewWalker(out IntPtr walker);
		[PreserveSig] int GetContentViewWalker(out IntPtr walker);
		[PreserveSig] int GetRawViewWalker(out ITreeWalker? walker);
	}
	[StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
	[ComImport, Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IElement
	{
		[PreserveSig] int SetFocus();
		[PreserveSig] int GetRuntimeId([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] out int[] runtimeId);
		[PreserveSig] int FindFirst(int scope, IntPtr condition, out IntPtr element);
		[PreserveSig] int FindAll(int scope, IntPtr condition, out IntPtr elements);
		[PreserveSig] int FindFirstBuildCache(int scope, IntPtr condition, IntPtr cache, out IntPtr element);
		[PreserveSig] int FindAllBuildCache(int scope, IntPtr condition, IntPtr cache, out IntPtr elements);
		[PreserveSig] int BuildUpdatedCache(IntPtr cache, out IntPtr element);
		[PreserveSig] int GetCurrentPropertyValue(int propertyId, [MarshalAs(UnmanagedType.Struct)] out object value);
	}
	[ComImport, Guid("4042c624-389c-4afc-a630-9df854a541fc"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface ITreeWalker
	{
		[PreserveSig] int GetParentElement(IElement element, out IElement? parent);
		[PreserveSig] int GetFirstChildElement(IElement element, out IElement? first);
		[PreserveSig] int GetLastChildElement(IElement element, out IElement? last);
		[PreserveSig] int GetNextSiblingElement(IElement element, out IElement? next);
		[PreserveSig] int GetPreviousSiblingElement(IElement element, out IElement? previous);
	}
}

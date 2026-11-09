using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static class FileNameUtility
{
	private static readonly char[] InvalidChars = Enumerable.Concat("\"<>|:*?\\/", from i in Enumerable.Range(0, 32)
		select (char)i).ToArray();

	private static readonly HashSet<string> Reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6",
		"COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7",
		"LPT8", "LPT9"
	};

	public static bool IsValidFilename(string name)
	{
		Regex regex = new Regex("[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]");
		if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(name))
		{
			return false;
		}
		if (name.Length > 64)
		{
			return false;
		}
		if (name.IndexOfAny(InvalidChars) >= 0)
		{
			return false;
		}
		if (name.EndsWith(".") || name.EndsWith(" "))
		{
			return false;
		}
		if (name.Trim('.').Length == 0)
		{
			return false;
		}
		if (Reserved.Contains(name.Split('.')[0].TrimEnd()))
		{
			return false;
		}
		if (regex.IsMatch(name))
		{
			return false;
		}
		return true;
	}

	public static string Sanitize(string input)
	{
		return Regex.Replace((input ?? "").Trim(), "\\s+", " ").TrimEnd('.', ' ');
	}
}

using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class InputUtility : MonoBehaviour
{
	private static readonly Regex Separator = new Regex("(?<=\\w)/(?=\\w)");

	private static bool lastFromComposite;

	public static string DisplayName(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return " ";
		}
		InputControl control = InputSystem.FindControl(path);
		string text = InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice, control);
		if (!string.IsNullOrEmpty(text))
		{
			return Separator.Replace(text, " ");
		}
		return path;
	}

	public static string[] GetGroups(InputBinding binding)
	{
		if (string.IsNullOrEmpty(binding.groups))
		{
			return Array.Empty<string>();
		}
		return (from g in binding.groups.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries)
			select g.Trim() into g
			where g.Length > 0
			select g).ToArray();
	}

	public static bool IsFromComposite(InputAction action)
	{
		InputControl activeControl = action.activeControl;
		if (activeControl == null)
		{
			return lastFromComposite;
		}
		int bindingIndexForControl = action.GetBindingIndexForControl(activeControl);
		lastFromComposite = bindingIndexForControl >= 0 && action.bindings[bindingIndexForControl].isPartOfComposite;
		return lastFromComposite;
	}

	public static int FindBindingIndex(InputAction action, string bindingId)
	{
		if (action == null || !Guid.TryParse(bindingId, out var result))
		{
			return -1;
		}
		ReadOnlyArray<InputBinding> bindings = action.bindings;
		for (int i = 0; i < bindings.Count; i++)
		{
			if (bindings[i].id == result)
			{
				return i;
			}
		}
		return -1;
	}
}

using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public static class InputActionCopy
{
	public static InputAction FromAsset(InputAction source, string name = null)
	{
		InputAction inputAction = new InputAction(name ?? source.name, source.type, null, source.interactions, source.processors, source.expectedControlType);
		foreach (InputBinding binding in source.bindings)
		{
			inputAction.AddBinding(new InputBinding
			{
				id = binding.id,
				name = binding.name,
				path = binding.effectivePath,
				interactions = binding.effectiveInteractions,
				processors = binding.effectiveProcessors,
				groups = binding.groups,
				isComposite = binding.isComposite,
				isPartOfComposite = binding.isPartOfComposite
			});
		}
		return inputAction;
	}

	public static void RefreshDefaults(InputAction copy, InputAction source)
	{
		bool enabled = copy.enabled;
		copy.Disable();
		foreach (InputBinding binding2 in source.bindings)
		{
			ReadOnlyArray<InputBinding> bindings = copy.bindings;
			for (int i = 0; i < bindings.Count; i++)
			{
				if (!(bindings[i].id != binding2.id))
				{
					InputBinding binding = bindings[i];
					binding.path = binding2.effectivePath;
					binding.interactions = binding2.effectiveInteractions;
					binding.processors = binding2.effectiveProcessors;
					copy.ChangeBinding(i).To(binding);
					break;
				}
			}
		}
		if (enabled)
		{
			copy.Enable();
		}
	}

	public static int FindBindingIndex(InputAction action, string group, string part = null)
	{
		ReadOnlyArray<InputBinding> bindings = action.bindings;
		for (int i = 0; i < bindings.Count; i++)
		{
			InputBinding binding = bindings[i];
			if (binding.isComposite || !InputBinding.MaskByGroup(group).Matches(binding))
			{
				continue;
			}
			bool num;
			if (part != null)
			{
				if (!binding.isPartOfComposite)
				{
					continue;
				}
				num = string.Equals(binding.name, part, StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				num = !binding.isPartOfComposite;
			}
			if (num)
			{
				return i;
			}
		}
		return -1;
	}
}

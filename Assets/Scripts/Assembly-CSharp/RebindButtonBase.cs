using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

public abstract class RebindButtonBase : MonoBehaviour
{
	public InputActionReference actionReference;

	public float navigationUnlockDelay = 0.2f;

	protected static readonly Dictionary<string, string[]> groupToDevicePath = new Dictionary<string, string[]>
	{
		{
			"keyboard",
			new string[2] { "<Keyboard>", "<Mouse>" }
		},
		{
			"gamepad",
			new string[1] { "<Gamepad>" }
		},
		{
			"joystick",
			new string[1] { "<Joystick>" }
		}
	};

	private static readonly string[] excludedControls = new string[2] { "<Mouse>", "<VirtualMouse>" };

	private const string CancelPath = "<Keyboard>/escape";

	private const float MagnitudeThreshold = 0.125f;

	private const float WaitForAnotherMatch = 0.1f;

	private static RebindButtonBase activeRebind;

	private static RebindButtonBase navigationLockOwner;

	private static bool navigationWasEnabled = true;

	private InputActionRebindingExtensions.RebindingOperation operation;

	private bool actionWasEnabled;

	private int listeningIndex = -1;

	private Coroutine unlockRoutine;

	private InputAction runtimeAction;

	protected InputAction Action
	{
		get
		{
			InputAction action = runtimeAction;
			if (action == null)
			{
				if (!(actionReference != null))
				{
					return null;
				}
				action = actionReference.action;
			}
			return action;
		}
	}

	protected bool IsRebinding => operation != null;

	protected abstract void UpdateLabel();

	protected virtual void OnRebindEnded(int bindingIndex, bool completed)
	{
	}

	protected virtual void OnEnable()
	{
		UpdateLabel();
		Singleton<InputManager>.Instance.KeybindsChanged += UpdateLabel;
	}

	protected virtual void OnDisable()
	{
		if (Singleton<InputManager>.Instance != null)
		{
			Singleton<InputManager>.Instance.KeybindsChanged -= UpdateLabel;
		}
		CancelRebind();
		if (navigationLockOwner == this)
		{
			ReleaseNavigation();
		}
	}

	protected virtual void OnDestroy()
	{
		operation?.Dispose();
		operation = null;
		if (activeRebind == this)
		{
			activeRebind = null;
		}
	}

	public void CancelRebind()
	{
		operation?.Cancel();
	}

	public void SetAction(InputAction action, Action<InputAction> save = null)
	{
		CancelRebind();
		runtimeAction = action;
		UpdateLabel();
	}

	public abstract bool IsBound();

	protected int FindBindingIndex(string bindingId)
	{
		InputAction action = Action;
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

	protected bool BeginRebind(int bindingIndex, string expectedControlType = null, Action<InputActionRebindingExtensions.RebindingOperation> configure = null)
	{
		if (operation != null)
		{
			return false;
		}
		InputAction action = Action;
		if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
		{
			return false;
		}
		InputBinding binding = action.bindings[bindingIndex];
		if (binding.isComposite)
		{
			Debug.LogError($"{base.name}: binding {bindingIndex} of '{action.name}' is a composite. Listen on one of its parts instead.", this);
			return false;
		}
		if (activeRebind != null && activeRebind != this)
		{
			activeRebind.CancelRebind();
		}
		actionWasEnabled = action.enabled;
		action.Disable();
		operation = action.PerformInteractiveRebinding(bindingIndex).WithCancelingThrough("<Keyboard>/escape").WithMagnitudeHavingToBeGreaterThan(0.125f)
			.OnMatchWaitForAnother(0.1f);
		operation.OnPotentialMatch((InputActionRebindingExtensions.RebindingOperation op) =>
		{
			if (expectedControlType == "Axis")
			{
				for (int i = 0; i < op.candidates.Count; i++)
				{
					if (op.candidates[i] is ButtonControl { parent: DpadControl parent } buttonControl)
					{
						bool flag = buttonControl == parent.up || buttonControl == parent.down;
						op.RemoveCandidate(buttonControl);
						op.AddCandidate(flag ? parent.y : parent.x, 1000f);
						break;
					}
				}
			}
			op.Complete();
		});
		for (int num = 0; num < excludedControls.Length; num++)
		{
			operation.WithControlsExcluding(excludedControls[num]);
		}
		if (!string.IsNullOrEmpty(expectedControlType))
		{
			operation.WithExpectedControlType(expectedControlType);
		}
		string[] groups = InputUtility.GetGroups(binding);
		for (int num2 = 0; num2 < groups.Length; num2++)
		{
			if (groupToDevicePath.TryGetValue(groups[num2], out var value))
			{
				for (int num3 = 0; num3 < value.Length; num3++)
				{
					operation.WithControlsHavingToMatchPath(value[num3]);
				}
			}
		}
		configure?.Invoke(operation);
		operation.OnComplete((InputActionRebindingExtensions.RebindingOperation _) =>
		{
			Finish(completed: true);
		});
		operation.OnCancel((InputActionRebindingExtensions.RebindingOperation _) =>
		{
			Finish(completed: false);
		});
		activeRebind = this;
		listeningIndex = bindingIndex;
		LockNavigation();
		UpdateLabel();
		operation.Start();
		return true;
	}

	protected void ResetToDefault(params int[] bindingIndices)
	{
		CancelRebind();
		InputAction action = Action;
		if (action == null)
		{
			return;
		}
		foreach (int num in bindingIndices)
		{
			if (num >= 0 && num < action.bindings.Count)
			{
				action.RemoveBindingOverride(num);
			}
		}
		SaveBindings();
		UpdateLabel();
	}

	protected static void SaveBindings()
	{
		Singleton<InputManager>.Instance.SaveKeybinds();
	}

	protected static void SetOverridePath(InputAction action, int index, string path)
	{
		InputBinding inputBinding = action.bindings[index];
		action.ApplyBindingOverride(index, new InputBinding
		{
			overridePath = path,
			overrideInteractions = inputBinding.overrideInteractions,
			overrideProcessors = inputBinding.overrideProcessors
		});
	}

	protected static int GetCompositeOrSelf(InputAction action, int index)
	{
		if (action == null || index < 0)
		{
			return -1;
		}
		ReadOnlyArray<InputBinding> bindings = action.bindings;
		while (index > 0 && bindings[index].isPartOfComposite)
		{
			index--;
		}
		return index;
	}

	protected static bool HasProcessor(InputAction action, int index, string processorName)
	{
		foreach (string item in SplitProcessors(action.bindings[index].effectiveProcessors))
		{
			if (IsProcessor(item, processorName))
			{
				return true;
			}
		}
		return false;
	}

	protected static void SetProcessor(InputAction action, int index, string processorName, bool enabled)
	{
		InputBinding inputBinding = action.bindings[index];
		List<string> list = SplitProcessors(inputBinding.effectiveProcessors);
		list.RemoveAll((string entry) => IsProcessor(entry, processorName));
		if (enabled)
		{
			list.Add(processorName);
		}
		string text = string.Join(",", list);
		string overrideProcessors = ((text == (inputBinding.processors ?? string.Empty)) ? null : text);
		action.ApplyBindingOverride(index, new InputBinding
		{
			overridePath = inputBinding.overridePath,
			overrideInteractions = inputBinding.overrideInteractions,
			overrideProcessors = overrideProcessors
		});
	}

	private static List<string> SplitProcessors(string processors)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrEmpty(processors))
		{
			return list;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i <= processors.Length; i++)
		{
			if (i == processors.Length || (processors[i] == ',' && num == 0))
			{
				string text = processors.Substring(num2, i - num2).Trim();
				if (text.Length > 0)
				{
					list.Add(text);
				}
				num2 = i + 1;
			}
			else if (processors[i] == '(')
			{
				num++;
			}
			else if (processors[i] == ')')
			{
				num--;
			}
		}
		return list;
	}

	private static bool IsProcessor(string entry, string processorName)
	{
		int num = entry.IndexOf('(');
		return string.Equals(((num < 0) ? entry : entry.Substring(0, num)).Trim(), processorName, StringComparison.OrdinalIgnoreCase);
	}

	private void Finish(bool completed)
	{
		int bindingIndex = listeningIndex;
		operation?.Dispose();
		operation = null;
		listeningIndex = -1;
		if (activeRebind == this)
		{
			activeRebind = null;
		}
		InputAction action = Action;
		if (actionWasEnabled)
		{
			action?.Enable();
		}
		UnlockNavigation();
		OnRebindEnded(bindingIndex, completed);
		SaveBindings();
		UpdateLabel();
		Singleton<AudioManager>.Instance.PlaySound(Singleton<InputManager>.Instance.rebindSound);
	}

	private void LockNavigation()
	{
		if (unlockRoutine != null)
		{
			StopCoroutine(unlockRoutine);
			unlockRoutine = null;
		}
		EventSystem current = EventSystem.current;
		if (!(current == null))
		{
			if (navigationLockOwner == null)
			{
				navigationWasEnabled = current.sendNavigationEvents;
			}
			navigationLockOwner = this;
			current.sendNavigationEvents = false;
		}
	}

	private void UnlockNavigation()
	{
		if (!(navigationLockOwner != this))
		{
			if (base.isActiveAndEnabled && navigationUnlockDelay > 0f)
			{
				unlockRoutine = StartCoroutine(ReleaseNavigationAfterDelay());
			}
			else
			{
				ReleaseNavigation();
			}
		}
	}

	private IEnumerator ReleaseNavigationAfterDelay()
	{
		yield return new WaitForSecondsRealtime(navigationUnlockDelay);
		unlockRoutine = null;
		if (navigationLockOwner == this)
		{
			ReleaseNavigation();
		}
	}

	private static void ReleaseNavigation()
	{
		navigationLockOwner = null;
		if (EventSystem.current != null)
		{
			EventSystem.current.sendNavigationEvents = navigationWasEnabled;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStatics()
	{
		activeRebind = null;
		navigationLockOwner = null;
		navigationWasEnabled = true;
	}
}

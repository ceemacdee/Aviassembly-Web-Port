using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class RebindAxisButton : RebindButtonBase
{
	private enum Slot
	{
		None = 0,
		Full = 1,
		Negative = 2,
		Positive = 3
	}

	[Header("Bindings")]
	[Tooltip("Optional. Leave empty for schemes without real axes, like keyboard.")]
	public string bindingIdFullAxis;

	public string bindingIdCompositePositive;

	public string bindingIdCompositeNegative;

	[Header("Buttons (each needs a Button and a RebindLabel)")]
	[Tooltip("The long button. Shown when an axis is bound, or when nothing is.")]
	public RebindLabel fullAxisLabel;

	[Tooltip("The halves. Shown when buttons are bound. Put a direction marker next to each label in the prefab.")]
	public RebindLabel compositeNegativeLabel;

	public RebindLabel compositePositiveLabel;

	[Header("Invert")]
	[Tooltip("Wired up in code. Don't also add SetInvert to its On Value Changed in the inspector.")]
	public Toggle invertToggle;

	[Tooltip("Optional. Hides the toggle in button mode without collapsing its space, so rows stay aligned.")]
	public CanvasGroup invertGroup;

	[Tooltip("Set invert from the direction the player moves the axis when asked for the positive direction.")]
	public bool detectInvert = true;

	[Header("Prompts")]
	[Tooltip("Shown on the long button while listening. Name the positive direction, e.g. \"Nose up\".")]
	public string fullListeningText = "Move or press";

	public string halfListeningText = "...";

	private const float InvertDetectThreshold = 0.1f;

	private Slot listeningSlot;

	private bool needsSecondHalf;

	private string firstHalfPath;

	private int FullAxisIndex => FindBindingIndex(bindingIdFullAxis);

	private int PositiveIndex => FindBindingIndex(bindingIdCompositePositive);

	private int NegativeIndex => FindBindingIndex(bindingIdCompositeNegative);

	protected virtual void Awake()
	{
		Hook(fullAxisLabel, Slot.Full);
		Hook(compositeNegativeLabel, Slot.Negative);
		Hook(compositePositiveLabel, Slot.Positive);
		if (invertToggle != null)
		{
			invertToggle.onValueChanged.AddListener(SetInvert);
		}
	}

	private void Hook(RebindLabel label, Slot slot)
	{
		if (label == null || label.Button == null)
		{
			Debug.LogWarning($"{base.name}: the {slot} label is missing or has no Button on the same object.", this);
			return;
		}
		if (label.transform == base.transform || base.transform.IsChildOf(label.transform))
		{
			Debug.LogWarning($"{base.name}: RebindAxisButton sits on or under the {slot} button. Move it to a parent object, or hiding that button will cancel listens.", this);
		}
		label.Button.onClick.AddListener(() =>
		{
			StartRebind(slot);
		});
	}

	public void SetInvert(bool inverted)
	{
		int fullAxisIndex = FullAxisIndex;
		if (fullAxisIndex >= 0)
		{
			RebindButtonBase.SetProcessor(base.Action, fullAxisIndex, "invert", inverted);
			RebindButtonBase.SaveBindings();
		}
	}

	public bool IsInverted()
	{
		int fullAxisIndex = FullAxisIndex;
		if (fullAxisIndex >= 0)
		{
			return RebindButtonBase.HasProcessor(base.Action, fullAxisIndex, "invert");
		}
		return false;
	}

	public void ResetToDefaults()
	{
		ResetToDefault(FullAxisIndex, NegativeIndex, PositiveIndex);
	}

	private void StartRebind(Slot slot)
	{
		if (!base.IsRebinding)
		{
			needsSecondHalf = false;
			firstHalfPath = null;
			Listen(slot, null);
		}
	}

	private void Listen(Slot slot, string excludePath)
	{
		int fullAxisIndex = FullAxisIndex;
		bool askPositive = slot != Slot.Negative;
		int bindingIndex = ((slot != Slot.Full) ? ((slot == Slot.Positive) ? PositiveIndex : NegativeIndex) : ((fullAxisIndex >= 0) ? fullAxisIndex : PositiveIndex));
		string expectedControlType = ((fullAxisIndex >= 0) ? "Axis" : "Button");
		listeningSlot = slot;
		if (!BeginRebind(bindingIndex, expectedControlType, (InputActionRebindingExtensions.RebindingOperation rebind) =>
		{
			if (!string.IsNullOrEmpty(excludePath))
			{
				rebind.WithControlsExcluding(excludePath);
			}
			if (detectInvert)
			{
				rebind.WithMatchingEventsBeingSuppressed(value: false);
			}
			rebind.OnApplyBinding((InputActionRebindingExtensions.RebindingOperation op, string path) =>
			{
				Apply(op, path, slot, askPositive);
			});
		}))
		{
			listeningSlot = Slot.None;
			UpdateLabel();
		}
	}

	private void Apply(InputActionRebindingExtensions.RebindingOperation op, string path, Slot slot, bool askedPositive)
	{
		InputAction action = base.Action;
		int fullAxisIndex = FullAxisIndex;
		int negativeIndex = NegativeIndex;
		int positiveIndex = PositiveIndex;
		if (op.selectedControl is ButtonControl || fullAxisIndex < 0)
		{
			int num = (askedPositive ? positiveIndex : negativeIndex);
			if (num >= 0)
			{
				RebindButtonBase.SetOverridePath(action, num, path);
				if (fullAxisIndex >= 0)
				{
					RebindButtonBase.SetOverridePath(action, fullAxisIndex, string.Empty);
				}
				int num2 = (askedPositive ? negativeIndex : positiveIndex);
				needsSecondHalf = slot == Slot.Full && num2 >= 0;
				firstHalfPath = path;
			}
			return;
		}
		RebindButtonBase.SetOverridePath(action, fullAxisIndex, path);
		if (negativeIndex >= 0)
		{
			RebindButtonBase.SetOverridePath(action, negativeIndex, string.Empty);
		}
		if (positiveIndex >= 0)
		{
			RebindButtonBase.SetOverridePath(action, positiveIndex, string.Empty);
		}
		needsSecondHalf = false;
		if (detectInvert && op.selectedControl is AxisControl axisControl)
		{
			float num3 = axisControl.ReadValue();
			if (Mathf.Abs(num3) >= 0.1f)
			{
				RebindButtonBase.SetProcessor(action, fullAxisIndex, "invert", askedPositive ? (num3 < 0f) : (num3 > 0f));
			}
		}
	}

	protected override void OnRebindEnded(int bindingIndex, bool completed)
	{
		listeningSlot = Slot.None;
		bool num = completed && needsSecondHalf;
		needsSecondHalf = false;
		if (num)
		{
			Listen(Slot.Negative, firstHalfPath);
		}
	}

	protected override void UpdateLabel()
	{
		InputAction action = base.Action;
		if (action == null || fullAxisLabel == null || compositeNegativeLabel == null || compositePositiveLabel == null)
		{
			return;
		}
		string text = PathOf(action, FullAxisIndex);
		string text2 = PathOf(action, NegativeIndex);
		string text3 = PathOf(action, PositiveIndex);
		bool flag = !string.IsNullOrEmpty(text);
		bool flag2 = !flag && (!string.IsNullOrEmpty(text2) || !string.IsNullOrEmpty(text3));
		Slot slot = (base.IsRebinding ? listeningSlot : Slot.None);
		EventSystem current = EventSystem.current;
		GameObject gameObject = ((current != null) ? current.currentSelectedGameObject : null);
		bool num = gameObject != null && (flag2 ? (gameObject == fullAxisLabel.gameObject) : (gameObject == compositeNegativeLabel.gameObject || gameObject == compositePositiveLabel.gameObject));
		fullAxisLabel.SetVisible(!flag2);
		compositeNegativeLabel.SetVisible(flag2);
		compositePositiveLabel.SetVisible(flag2);
		if (num)
		{
			RebindLabel rebindLabel = ((!flag2) ? fullAxisLabel : ((slot == Slot.Positive) ? compositePositiveLabel : compositeNegativeLabel));
			current.SetSelectedGameObject(rebindLabel.gameObject);
		}
		if (flag2)
		{
			ShowSlot(compositeNegativeLabel, text2, slot == Slot.Negative, halfListeningText);
			ShowSlot(compositePositiveLabel, text3, slot == Slot.Positive, halfListeningText);
		}
		else
		{
			ShowSlot(fullAxisLabel, text, slot == Slot.Full, fullListeningText);
		}
		if (invertToggle != null)
		{
			invertToggle.SetIsOnWithoutNotify(IsInverted());
			invertToggle.interactable = flag;
			if (invertGroup != null)
			{
				invertGroup.alpha = (flag ? 1f : 0f);
				invertGroup.blocksRaycasts = flag;
			}
		}
	}

	public override bool IsBound()
	{
		if (string.IsNullOrWhiteSpace(base.Action.bindings[FullAxisIndex].effectivePath))
		{
			if (!string.IsNullOrWhiteSpace(base.Action.bindings[PositiveIndex].effectivePath))
			{
				return !string.IsNullOrWhiteSpace(base.Action.bindings[NegativeIndex].effectivePath);
			}
			return false;
		}
		return true;
	}

	private static void ShowSlot(RebindLabel label, string path, bool listening, string prompt)
	{
		if (listening)
		{
			label.SetText(prompt);
		}
		else
		{
			label.SetLabelValue(path);
		}
	}

	private static string PathOf(InputAction action, int index)
	{
		if (index < 0)
		{
			return null;
		}
		return action.bindings[index].effectivePath;
	}
}

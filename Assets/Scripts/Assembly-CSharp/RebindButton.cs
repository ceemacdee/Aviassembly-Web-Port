using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RebindButton : RebindButtonBase
{
	public string bindingId;

	public string expectedControlType = "Button";

	[Space(10f)]
	public TMP_Text text;

	public RawImage icon;

	public Toggle invertToggle;

	protected virtual void Awake()
	{
		Button component = GetComponent<Button>();
		if (component != null)
		{
			component.onClick.AddListener(StartRebind);
		}
	}

	public void StartRebind()
	{
		BeginRebind(FindBindingIndex(bindingId), expectedControlType);
	}

	public void SetInvert(bool inverted)
	{
		int compositeOrSelf = RebindButtonBase.GetCompositeOrSelf(base.Action, FindBindingIndex(bindingId));
		if (compositeOrSelf >= 0)
		{
			RebindButtonBase.SetProcessor(base.Action, compositeOrSelf, "invert", inverted);
			RebindButtonBase.SaveBindings();
		}
	}

	public bool IsInverted()
	{
		int compositeOrSelf = RebindButtonBase.GetCompositeOrSelf(base.Action, FindBindingIndex(bindingId));
		if (compositeOrSelf >= 0)
		{
			return RebindButtonBase.HasProcessor(base.Action, compositeOrSelf, "invert");
		}
		return false;
	}

	protected override void UpdateLabel()
	{
		int num = FindBindingIndex(bindingId);
		if (base.IsRebinding || num < 0)
		{
			SetPlaceholderLabel();
		}
		else
		{
			SetLabelValue(base.Action.bindings[num].effectivePath);
		}
		if (invertToggle != null)
		{
			invertToggle.SetIsOnWithoutNotify(IsInverted());
		}
	}

	public override bool IsBound()
	{
		int index = FindBindingIndex(bindingId);
		return !string.IsNullOrWhiteSpace(base.Action.bindings[index].effectivePath);
	}

	public void SetLabelValue(string keyPath)
	{
		RebindLabel.Show(text, icon, keyPath);
	}

	public void SetPlaceholderLabel()
	{
		RebindLabel.ShowText(text, icon, "-");
	}
}

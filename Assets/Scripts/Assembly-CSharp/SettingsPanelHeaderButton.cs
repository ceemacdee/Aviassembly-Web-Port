using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelHeaderButton : MonoBehaviour
{
	public Image shadow;

	public RawImage icon;

	[HideInInspector]
	public SettingsPanel settingsPanel;

	private Button button;

	private ColorBlock colorBlock;

	public void Init()
	{
		button = GetComponent<Button>();
		colorBlock = button.colors;
		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(OpenPanel);
	}

	public void OpenPanel()
	{
		settingsPanel.OpenPanel(base.transform.GetSiblingIndex());
	}

	public void SetColor(Color color, Color iconColor, Color shadowColors)
	{
		colorBlock.normalColor = color;
		colorBlock.highlightedColor = shadowColors;
		shadow.color = shadowColors;
		button.colors = colorBlock;
		icon.color = iconColor;
	}
}

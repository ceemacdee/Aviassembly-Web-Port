public class TooltipSystem : Singleton<TooltipSystem>
{
	public Tooltip tooltip;

	private TooltipSettings currentSettings;

	private int showFlag;

	protected override void Awake()
	{
		base.Awake();
		Hide();
	}

	public void Show(TooltipSettings settings)
	{
		showFlag = -1;
		tooltip.gameObject.SetActive(value: true);
		tooltip.Init(settings);
		tooltip.Show();
	}

	public void ShowInUpdate(TooltipSettings settings)
	{
		if (settings == currentSettings)
		{
			showFlag = 1;
			return;
		}
		currentSettings = settings;
		Show(settings);
	}

	public void Hide()
	{
		tooltip.gameObject.SetActive(value: false);
		currentSettings = null;
	}

	private void LateUpdate()
	{
		if (showFlag == 0)
		{
			Hide();
		}
		if (showFlag == 1)
		{
			showFlag = 0;
		}
	}
}

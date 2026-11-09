using UnityEngine;

public class FlightWarningManager : Singleton<FlightWarningManager>
{
	public FlightWarning flightWarning;

	public AudioDef warningSound;

	public Texture defaultIcon;

	private int currentPriority;

	private float timer;

	public void ShowWarning(string title, string subtitle, Texture icon, int priority, float displayTime, bool warning = true, AudioDef sound = null)
	{
		if (priority >= currentPriority)
		{
			if (timer < 0f || priority != currentPriority)
			{
				Singleton<AudioManager>.Instance.PlaySound((sound == null) ? warningSound : sound);
			}
			if (icon == null)
			{
				icon = defaultIcon;
			}
			timer = displayTime;
			currentPriority = priority;
			flightWarning.gameObject.SetActive(value: true);
			flightWarning.SetContent(title, subtitle, icon, warning);
		}
	}

	private void Update()
	{
		timer -= Time.deltaTime;
		if (timer < 0f)
		{
			currentPriority = int.MinValue;
			flightWarning.gameObject.SetActive(value: false);
		}
	}
}

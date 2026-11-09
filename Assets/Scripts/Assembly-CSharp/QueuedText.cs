using System;

public class QueuedText
{
	public string text;

	public Action additionUICallback;

	public Action<bool> callback;

	public QueuedText(string text, Action callback)
	{
		this.text = text;
		additionUICallback = callback;
	}
}

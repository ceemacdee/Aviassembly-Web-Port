using System;

public class ProgressClass : IProgress<float>
{
	public float progressValue;

	public void Report(float value)
	{
		if (!(progressValue >= value))
		{
			progressValue = value;
		}
	}
}

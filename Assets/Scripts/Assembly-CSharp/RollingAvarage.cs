using System.Collections.Generic;
using UnityEngine;

public class RollingAvarage
{
	private struct RollingAvarageEntry
	{
		public float time;

		public float value;

		public RollingAvarageEntry(float time, float value)
		{
			this.value = value;
			this.time = time;
		}
	}

	private float defaultValue;

	private float maxSampleTime;

	private List<RollingAvarageEntry> entries = new List<RollingAvarageEntry>();

	public RollingAvarage(float sampleTime, float defaultValue)
	{
		maxSampleTime = sampleTime;
		this.defaultValue = defaultValue;
	}

	public void Add(float value)
	{
		entries.Add(new RollingAvarageEntry(Time.unscaledTime, value));
		for (int num = entries.Count - 1; num >= 0; num--)
		{
			if (Time.unscaledTime - entries[num].time > maxSampleTime * 1.5f)
			{
				entries.RemoveAt(num);
			}
		}
	}

	public void Reset()
	{
		entries.Clear();
	}

	public float GetAvarage(float sampleTime)
	{
		sampleTime = Mathf.Min(maxSampleTime, sampleTime);
		if (entries.Count == 0)
		{
			return defaultValue;
		}
		float time = entries[0].time;
		if (Mathf.Abs(entries[entries.Count - 1].time) - Mathf.Abs(time) < sampleTime)
		{
			return defaultValue;
		}
		float num = 0f;
		for (int i = 0; i < entries.Count; i++)
		{
			num += entries[i].value;
		}
		return num / (float)entries.Count;
	}

	public float GetMinValue(float sampleTime)
	{
		sampleTime = Mathf.Min(maxSampleTime, sampleTime);
		if (entries.Count == 0)
		{
			return defaultValue;
		}
		float time = entries[0].time;
		if (Mathf.Abs(entries[entries.Count - 1].time) - Mathf.Abs(time) < sampleTime)
		{
			return defaultValue;
		}
		float num = float.MaxValue;
		for (int i = 0; i < entries.Count; i++)
		{
			num = Mathf.Min(entries[i].value, num);
		}
		return num;
	}

	public float GetMaxValue(float sampleTime)
	{
		sampleTime = Mathf.Min(maxSampleTime, sampleTime);
		if (entries.Count == 0)
		{
			return defaultValue;
		}
		float time = entries[0].time;
		if (Mathf.Abs(entries[entries.Count - 1].time) - Mathf.Abs(time) < sampleTime)
		{
			return defaultValue;
		}
		float num = float.MinValue;
		for (int i = 0; i < entries.Count; i++)
		{
			num = Mathf.Max(entries[i].value, num);
		}
		return num;
	}

	public float GetAvarage()
	{
		if (entries.Count == 0)
		{
			return defaultValue;
		}
		float time = entries[0].time;
		if (Mathf.Abs(entries[entries.Count - 1].time) - Mathf.Abs(time) < maxSampleTime)
		{
			return defaultValue;
		}
		float num = 0f;
		for (int i = 0; i < entries.Count; i++)
		{
			num += entries[i].value;
		}
		return num / (float)entries.Count;
	}
}

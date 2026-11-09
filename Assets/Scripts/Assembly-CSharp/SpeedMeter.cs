using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpeedMeter : MonoBehaviour
{
	public TMP_Text text1;

	public TMP_Text text2;

	public TMP_Text text3;

	public TMP_Text text4;

	public TMP_Text text5;

	public bool altitude;

	private void Update()
	{
		float num = Singleton<PlaneContainer>.Instance.GetVelocityMagintude();
		if (altitude)
		{
			num = Singleton<PlaneContainer>.Instance.transform.position.y;
		}
		int[] digits = GetDigits((int)num);
		text1.text = Mathf.Clamp(digits[0], 0, 9).ToString();
		text2.text = Mathf.Clamp(digits[1], 0, 9).ToString();
		text3.text = Mathf.Clamp(digits[2], 0, 9).ToString();
		text4.text = Mathf.Clamp(digits[3], 0, 9).ToString();
		Color color = text1.color;
		color.a = ((digits[0] != -1) ? 1f : 0.5f);
		text1.color = color;
		color.a = ((digits[1] != -1) ? 1f : 0.5f);
		text2.color = color;
		color.a = ((digits[2] != -1) ? 1f : 0.5f);
		text3.color = color;
		color.a = ((digits[3] != -1) ? 1f : 0.5f);
		text4.color = color;
	}

	private int[] GetDigits(int value)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < 4; i++)
		{
			if (value <= 0)
			{
				list.Add(-1);
				continue;
			}
			list.Add(value % 10);
			value /= 10;
		}
		return list.ToArray();
	}
}

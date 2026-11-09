using TMPro;
using UnityEngine;

public class HeightDial : MonoBehaviour
{
	public float currentHeight;

	public int increments;

	public float wrapHeight;

	public Transform textParent;

	public TMP_Text[] texts;

	private void Update()
	{
		currentHeight = Singleton<PlaneContainer>.Instance.transform.position.y + 9f;
		textParent.transform.localPosition = Vector3.up * ((0f - currentHeight) % wrapHeight + wrapHeight * 1.5f);
		int num = -increments * 2 + Mathf.FloorToInt(currentHeight / wrapHeight) * increments;
		for (int i = 0; i < texts.Length; i++)
		{
			texts[i].text = (num + increments * i).ToString();
		}
	}
}

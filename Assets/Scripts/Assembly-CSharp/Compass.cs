using System;
using UnityEngine;

public class Compass : MonoBehaviour
{
	public bool questSelected;

	public Vector2 questMarkerPosition;

	public RectTransform questMarker;

	public Transform turnTable;

	public Transform[] labels;

	private void Awake()
	{
		Singleton<QuestFeedbackManager>.Instance.compass = this;
	}

	private void Update()
	{
		questMarker.gameObject.SetActive(questSelected);
		turnTable.eulerAngles = new Vector3(0f, 0f, Singleton<PlaneContainer>.Instance.transform.eulerAngles.y);
		Vector2 vector = new Vector2(Singleton<PlaneContainer>.Instance.transform.position.x, Singleton<PlaneContainer>.Instance.transform.position.z);
		Vector2 normalized = (questMarkerPosition - vector).normalized;
		float num = (Mathf.Atan2(normalized.y, normalized.x) - MathF.PI / 2f) * 57.29578f;
		questMarker.anchoredPosition = Quaternion.Euler(0f, 0f, num + Singleton<PlaneContainer>.Instance.transform.eulerAngles.y) * Vector3.up * 72.5f;
		for (int i = 0; i < labels.Length; i++)
		{
			labels[i].transform.rotation = Quaternion.identity;
		}
	}
}

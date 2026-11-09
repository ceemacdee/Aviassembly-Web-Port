using UnityEngine;

public class QuestFeedbackManager : Singleton<QuestFeedbackManager>
{
	public Quest currentQuest;

	public GameObject questMarkerPrefab;

	public GameObject questMarkerMapPrefab;

	public bool questsAvailable;

	public Compass compass;

	private HighlightInstance questMarker;

	private void Start()
	{
		questMarker = new HighlightInstance(questMarkerPrefab, Vector3.zero, float.MaxValue, 100f, 50f, enabled: false);
		questMarker.priority = 1;
		questMarker.mapPrefab = questMarkerMapPrefab;
		questMarker.alwaysVisable = true;
		questMarker.mapOffset = new Vector2(225f, 225f);
		AiportHighlighterManager.AddHighlightInstance(questMarker);
	}

	public void SelectQuest(Quest quest)
	{
		if (quest == currentQuest)
		{
			currentQuest = null;
		}
		else
		{
			currentQuest = quest;
		}
	}

	private void LateUpdate()
	{
		if (currentQuest != null && currentQuest.completed)
		{
			currentQuest = null;
		}
		questMarker.enabled = currentQuest != null;
		if (compass != null)
		{
			compass.questSelected = currentQuest != null;
		}
		if (currentQuest != null)
		{
			Vector3 currentMarkerPosition = currentQuest.GetCurrentMarkerPosition();
			questMarker.position = currentMarkerPosition;
			if (compass != null)
			{
				compass.questMarkerPosition = new Vector2(currentMarkerPosition.x, currentMarkerPosition.z);
			}
		}
	}
}

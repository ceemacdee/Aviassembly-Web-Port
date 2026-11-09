using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AiportHighlighterManager : Singleton<AiportHighlighterManager>
{
	public Vector3 wayPointPosition;

	public bool waypointEnabled;

	public GameObject waypointPrefab;

	[Space(20f)]
	public Canvas highlighterParent;

	public float yOffset;

	public Camera cam;

	private RectTransform highlighterRectTransform;

	private PlaneContainer plane;

	private TerrainGenerationManager terrainGenerationManager;

	public List<HighlightInstance> highlightInstances = new List<HighlightInstance>();

	private HighlightInstance waypointInstance;

	private Dictionary<HighlightInstance, AirportHighlighter> activeHighlighters = new Dictionary<HighlightInstance, AirportHighlighter>();

	private void Start()
	{
		plane = Singleton<PlaneContainer>.Instance;
		terrainGenerationManager = Singleton<TerrainGenerationManager>.Instance;
		waypointInstance = new HighlightInstance(waypointPrefab, wayPointPosition, float.MaxValue, 10f, 0f, waypointEnabled);
		AddHighlightInstance(waypointInstance);
	}

	public void InitFlyMode(Canvas parent, Camera cam)
	{
		highlighterParent = parent;
		highlighterRectTransform = parent.GetComponent<RectTransform>();
		this.cam = cam;
		for (int i = 0; i < highlightInstances.Count; i++)
		{
			if (highlightInstances[i].mapPrefab != null)
			{
				Singleton<Map>.Instance.AddMapObject(highlightInstances[i]);
			}
		}
		activeHighlighters = new Dictionary<HighlightInstance, AirportHighlighter>();
	}

	private void LateUpdate()
	{
		waypointInstance.position = wayPointPosition;
		waypointInstance.enabled = waypointEnabled;
		if (highlighterParent != null)
		{
			UpdateHighlightInstances();
		}
	}

	private bool PositionInScreen(Vector3 position)
	{
		float num = Vector3.Dot(cam.transform.forward, (position - plane.transform.position).normalized);
		Vector2 point = WorldToCanvasPosition(position);
		bool num2 = num > 0f;
		bool flag = ((RectTransform)highlighterParent.transform).rect.Contains(point);
		return num2 & flag;
	}

	private Vector2 WorldToCanvasPosition(Vector3 worldPosition)
	{
		Vector3 vector = cam.WorldToViewportPoint(worldPosition);
		RectTransform rectTransform = highlighterRectTransform;
		Vector2 sizeDelta = rectTransform.sizeDelta;
		return new Vector2(vector.x * sizeDelta.x, vector.y * sizeDelta.y) - sizeDelta * rectTransform.pivot;
	}

	public static void AddHighlightInstance(HighlightInstance instance)
	{
		if (!Singleton<AiportHighlighterManager>.Instance.highlightInstances.Contains(instance))
		{
			Singleton<AiportHighlighterManager>.Instance.highlightInstances.Add(instance);
		}
	}

	public static void RemoveHighlightInstance(HighlightInstance instance)
	{
		if (Singleton<AiportHighlighterManager>.Instance.highlightInstances.Contains(instance))
		{
			Singleton<AiportHighlighterManager>.Instance.highlightInstances.Remove(instance);
		}
		Singleton<AiportHighlighterManager>.Instance.UpdateHighlightInstances();
	}

	private void UpdateHighlightInstances()
	{
		List<HighlightInstance> list = activeHighlighters.Keys.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			if (!highlightInstances.Contains(list[i]))
			{
				Object.Destroy(activeHighlighters[list[i]].gameObject);
				activeHighlighters.Remove(list[i]);
			}
		}
		for (int j = 0; j < highlightInstances.Count; j++)
		{
			HighlightInstance highlightInstance = highlightInstances[j];
			float num = Vector3.Distance(plane.transform.position, highlightInstance.position);
			bool flag = false;
			for (int k = 0; k < highlightInstances.Count; k++)
			{
				if (k != j && Vector3.Distance(highlightInstances[k].position, highlightInstances[j].position) < 10f && highlightInstances[k].priority > highlightInstances[j].priority)
				{
					flag = true;
				}
			}
			if (!(num >= highlightInstance.minDistance) || !(num <= highlightInstance.maxDistance) || !PositionInScreen(highlightInstance.position) || !highlightInstance.enabled || flag)
			{
				if (activeHighlighters.ContainsKey(highlightInstance))
				{
					Object.Destroy(activeHighlighters[highlightInstance].gameObject);
					activeHighlighters.Remove(highlightInstance);
				}
				continue;
			}
			AirportHighlighter airportHighlighter = null;
			if (!activeHighlighters.ContainsKey(highlightInstance))
			{
				airportHighlighter = Object.Instantiate(highlightInstance.prefab).GetComponent<AirportHighlighter>();
				airportHighlighter.transform.SetParent(highlighterParent.transform, worldPositionStays: true);
				airportHighlighter.transform.localScale = Vector3.one;
				activeHighlighters.Add(highlightInstance, airportHighlighter);
			}
			airportHighlighter = activeHighlighters[highlightInstance];
			Vector3 worldPosition = highlightInstance.position + terrainGenerationManager.GetAvarageTerrainHeight(highlightInstance.position) * Vector3.up;
			Vector2 vector = WorldToCanvasPosition(worldPosition);
			vector.y += yOffset;
			airportHighlighter.transform.localPosition = vector;
			airportHighlighter.transform.localRotation = Quaternion.identity;
			airportHighlighter.airportPostition = highlightInstance.position;
			float b = Mathf.InverseLerp(highlightInstance.minDistance, highlightInstance.minDistance + highlightInstance.fadeDistance, num);
			float alpha = Mathf.Min(Mathf.InverseLerp(highlightInstance.maxDistance, highlightInstance.maxDistance - highlightInstance.fadeDistance, num), b);
			if (Mathf.Approximately(highlightInstance.maxDistance, float.MaxValue))
			{
				alpha = 1f;
			}
			airportHighlighter.SetAlpha(alpha);
		}
	}
}

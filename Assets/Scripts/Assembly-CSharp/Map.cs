using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Map : Singleton<Map>
{
	private bool wayPointEnabled;

	public RectTransform mapTransform;

	public Transform iconParent;

	public Transform topParent;

	public RawImage background;

	public RawImage fogOfWar;

	public MapInspector inspector;

	public TMP_Text hoverHint;

	public GameObject mapBackgroundPrefab;

	public Transform mapBackgroundParent;

	[Space(15f)]
	public float zoom;

	public float iconSize;

	public float playerIconPadding;

	public bool useFogOfWar;

	public float maxZoom;

	public float backgroundScale;

	public float backgroundOffset;

	[Header("Icons")]
	public GameObject mapItemPrefab;

	public GameObject airportItemPrefab;

	public Transform playerIcon;

	public Transform playerEdgePointer;

	public Transform waypoint;

	public Vector2 offset;

	private Vector2 prevMousePosition;

	private Vector2 mouseDownPosition;

	private List<HighlightInstance> mapObjects = new List<HighlightInstance>();

	private List<GameObject> mapIconInstances = new List<GameObject>();

	private List<AirportMapIcon> airportIconInstances = new List<AirportMapIcon>();

	private float startZoom;

	private float targetZoom;

	private float zoomVelocity;

	private Vector2 prevSnappedOffset;

	private float prevZoom;

	public void AddMapObject(HighlightInstance mapObject)
	{
		if (!mapObjects.Contains(mapObject))
		{
			mapObjects.Add(mapObject);
			Update();
		}
	}

	public void RemoveMapObject(HighlightInstance mapObject)
	{
		mapObjects.Remove(mapObject);
	}

	public void OpenMap()
	{
		offset = -(new Vector2(Singleton<PlaneContainer>.Instance.transform.position.x, Singleton<PlaneContainer>.Instance.transform.position.z) / zoom);
		offset.x = (float)Mathf.RoundToInt(offset.x * 1024f) / 1024f;
		offset.y = (float)Mathf.RoundToInt(offset.y * 1024f) / 1024f;
		fogOfWar.texture = Singleton<FogOfWar>.Instance.GetFogOfWarTexture();
	}

	private void Start()
	{
		targetZoom = zoom;
		startZoom = zoom;
		for (int i = 0; i < Singleton<ContinentManager>.Instance.continents.Count; i++)
		{
			GameObject obj = Object.Instantiate(mapBackgroundPrefab);
			MapBackground component = obj.GetComponent<MapBackground>();
			component.continent = i;
			component.surfaceColor = Singleton<ContinentManager>.Instance.continents[i].continentType.surfaceColor;
			component.map = this;
			obj.transform.parent = mapBackgroundParent;
			obj.transform.SetAsFirstSibling();
		}
	}

	private void Update()
	{
		hoverHint.gameObject.SetActive(inspector.hidden && inspector.showCount < 5);
		hoverHint.color = new Color(1f, 1f, 1f, 1f - (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f);
		Vector2 vector = ScreenToInterpolatorSpace(MouseInput.GetMousePosition());
		targetZoom -= MouseInput.GetDeltaScroll() * (2000f * (targetZoom / 8000f));
		targetZoom = Mathf.Clamp(targetZoom, 1000f, maxZoom);
		Vector2 vector2 = vector + new Vector2(0.5f, 0.5f);
		Vector3 vector3 = InterpolatorToWorldPosition(vector2);
		zoom = Mathf.SmoothDamp(zoom, targetZoom, ref zoomVelocity, 0.2f);
		offset = GetZoomCorrectedOffset(vector3, vector2);
		if (MouseInput.GetMouseButtonDown(0))
		{
			prevMousePosition = vector;
			mouseDownPosition = vector;
		}
		bool flag = RectTransformUtility.RectangleContainsScreenPoint(mapTransform, MouseInput.GetMousePosition());
		if ((MouseInput.GetMouseButtonUp(0) & flag) && !AirportManager.airportHover)
		{
			Vector3 wayPointPosition = Singleton<AiportHighlighterManager>.Instance.wayPointPosition;
			if (Vector2.Distance(mouseDownPosition, vector) < 0.05f)
			{
				Singleton<AiportHighlighterManager>.Instance.wayPointPosition = vector3;
				wayPointEnabled = Vector3.Distance(wayPointPosition, Singleton<AiportHighlighterManager>.Instance.wayPointPosition) > 300f || !wayPointEnabled;
			}
		}
		Singleton<AiportHighlighterManager>.Instance.waypointEnabled = wayPointEnabled;
		Vector2 zero = Vector2.zero;
		if (flag && MouseInput.GetMouseButton(0))
		{
			zero = new Vector2(vector.x - prevMousePosition.x, vector.y - prevMousePosition.y);
			offset.x += zero.x;
			offset.y += zero.y;
			prevMousePosition = vector;
		}
		float num = 10f * (zoom / 8000f) * backgroundScale;
		Rect uvRect = new Rect((0f - offset.x) * num - num * 0.5f + backgroundOffset, (0f - offset.y) * num - num * 0.5f + backgroundOffset, num, num);
		background.uvRect = uvRect;
		waypoint.gameObject.SetActive(WorldPositionIsInsideMap(Singleton<AiportHighlighterManager>.Instance.wayPointPosition) && wayPointEnabled);
		waypoint.transform.position = WorldToMapPosition(Singleton<AiportHighlighterManager>.Instance.wayPointPosition);
		waypoint.transform.localScale = Vector3.one * (4000f / zoom) * 1.75f;
		for (int num2 = mapIconInstances.Count - 1; num2 >= 0; num2--)
		{
			Object.Destroy(mapIconInstances[num2].gameObject);
		}
		mapIconInstances.Clear();
		for (int i = 0; i < mapObjects.Count; i++)
		{
			if (!mapObjects[i].enabled)
			{
				continue;
			}
			Vector3 vector4 = new Vector3(mapObjects[i].mapOffset.x, 0f, mapObjects[i].mapOffset.y);
			if (WorldPositionIsInsideMap(mapObjects[i].position + vector4) || mapObjects[i].alwaysVisable)
			{
				GameObject gameObject = Object.Instantiate(mapObjects[i].mapPrefab);
				gameObject.transform.rotation = Quaternion.identity;
				gameObject.transform.SetParent(topParent, worldPositionStays: true);
				gameObject.transform.localScale = Vector3.one * (4000f / zoom) * 1.33f;
				Vector2 interpolator = GetInterpolator(mapObjects[i].position + vector4);
				Vector2 vector5 = interpolator;
				if (mapObjects[i].alwaysVisable)
				{
					float num3 = playerIconPadding * (4000f / zoom) * 1.25f;
					vector5.x = Mathf.Clamp(vector5.x, num3, 1f - num3);
					vector5.y = Mathf.Clamp(vector5.y, num3, 1f - num3);
					Transform outOfMapIndicator = gameObject.GetComponent<MapIcon>().outOfMapIndicator;
					outOfMapIndicator.rotation = Quaternion.LookRotation(Vector3.forward, InterpolatorToMapPosition(vector5) - base.transform.position);
					outOfMapIndicator.gameObject.SetActive(vector5 != interpolator);
				}
				gameObject.transform.position = InterpolatorToMapPosition(vector5);
				mapIconInstances.Add(gameObject);
			}
		}
		List<Airport> airports = Singleton<AirportManager>.Instance.airports;
		UpdateAirportIconCount(airports);
		int num4 = 0;
		for (int j = 0; j < airports.Count; j++)
		{
			if (!WorldPositionIsInsideMap(airports[j].position))
			{
				continue;
			}
			if (useFogOfWar)
			{
				airportIconInstances[num4].transform.gameObject.SetActive(value: true);
				if (!Singleton<FogOfWar>.Instance.LocationDiscovered(airports[j].position))
				{
					airportIconInstances[num4].transform.gameObject.SetActive(value: false);
				}
			}
			airportIconInstances[num4].transform.transform.localScale = Vector3.one * (4000f / zoom) * 1.33f;
			airportIconInstances[num4].transform.position = WorldToMapPosition(airports[j].position);
			airportIconInstances[num4].transform.transform.localScale *= 1.2f;
			airportIconInstances[num4].airport = Singleton<AirportManager>.Instance.GetClosestAirport(airports[j].position);
			airportIconInstances[num4].Update();
			num4++;
		}
		Vector2 interpolator2 = GetInterpolator(Singleton<PlaneContainer>.Instance.transform.position);
		Vector2 vector6 = interpolator2;
		interpolator2.x = Mathf.Clamp(interpolator2.x, playerIconPadding, 1f - playerIconPadding);
		interpolator2.y = Mathf.Clamp(interpolator2.y, playerIconPadding, 1f - playerIconPadding);
		playerIcon.transform.position = InterpolatorToMapPosition(interpolator2);
		Vector3 forward = Singleton<PlaneContainer>.Instance.Forward;
		forward.y = 0f;
		playerIcon.transform.eulerAngles = new Vector3(0f, 0f, 0f - Vector3.SignedAngle(Vector3.forward, forward, Vector3.up));
		playerIcon.transform.SetAsLastSibling();
		playerEdgePointer.gameObject.SetActive(interpolator2 != vector6);
		playerEdgePointer.transform.rotation = Quaternion.LookRotation(Vector3.forward, playerIcon.transform.position - base.transform.position);
		if (prevSnappedOffset != offset || prevZoom != zoom)
		{
			fogOfWar.texture = Singleton<FogOfWar>.Instance.GetFogOfWarTexture();
		}
		prevSnappedOffset = offset;
		prevZoom = zoom;
		fogOfWar.gameObject.SetActive(useFogOfWar);
	}

	private void UpdateAirportIconCount(List<Airport> airports)
	{
		int num = 0;
		for (int i = 0; i < airports.Count; i++)
		{
			if (WorldPositionIsInsideMap(airports[i].position))
			{
				num++;
			}
		}
		int num2 = num - airportIconInstances.Count;
		if (num2 > 0)
		{
			for (int j = 0; j < num2; j++)
			{
				GameObject obj = Object.Instantiate(airportItemPrefab);
				obj.transform.rotation = Quaternion.identity;
				obj.transform.localScale = Vector3.one;
				obj.transform.SetParent(iconParent, worldPositionStays: true);
				obj.transform.SetAsLastSibling();
				AirportMapIcon component = obj.GetComponent<AirportMapIcon>();
				component.panel = inspector;
				airportIconInstances.Add(component);
			}
		}
		if (num2 < 0)
		{
			for (int num3 = Mathf.Abs(num2) - 1; num3 >= 0; num3--)
			{
				Object.Destroy(airportIconInstances[num3].gameObject);
				airportIconInstances.RemoveAt(num3);
			}
		}
	}

	public Vector3 WorldToMapPosition(Vector3 worldPosition)
	{
		Vector2 interpolator = GetInterpolator(worldPosition);
		return InterpolatorToMapPosition(interpolator);
	}

	public bool WorldPositionIsInsideMap(Vector3 worldPosition)
	{
		Vector2 interpolator = GetInterpolator(worldPosition);
		if (interpolator.x < 1f && interpolator.y < 1f && interpolator.x > 0f)
		{
			return interpolator.y > 0f;
		}
		return false;
	}

	private Vector3 InterpolatorToMapPosition(Vector2 interpolator)
	{
		float x = Mathf.LerpUnclamped(mapTransform.rect.xMin, mapTransform.rect.xMax, interpolator.x);
		float y = Mathf.LerpUnclamped(mapTransform.rect.yMin, mapTransform.rect.yMax, interpolator.y);
		return mapTransform.TransformPoint(new Vector3(x, y, 0f));
	}

	private Vector2 GetZoomCorrectedOffset(Vector3 worldposition, Vector2 targetInterpolator)
	{
		targetInterpolator.x -= 0.5f;
		targetInterpolator.y -= 0.5f;
		float x = (targetInterpolator.x * zoom - worldposition.x) / zoom;
		float y = (targetInterpolator.y * zoom - worldposition.z) / zoom;
		return new Vector2(x, y);
	}

	public Vector2 GetInterpolator(Vector3 worldPosition)
	{
		float num = (worldPosition.x + offset.x * zoom) / zoom;
		float num2 = (worldPosition.z + offset.y * zoom) / zoom;
		float x = num + 0.5f;
		num2 += 0.5f;
		return new Vector2(x, num2);
	}

	public Vector3 InterpolatorToWorldPosition(Vector2 interpolator)
	{
		interpolator.x -= 0.5f;
		interpolator.y -= 0.5f;
		float x = (interpolator.x - offset.x) * zoom;
		float z = (interpolator.y - offset.y) * zoom;
		return new Vector3(x, 0f, z);
	}

	private Vector2 ScreenToInterpolatorSpace(Vector3 screenSpace)
	{
		Vector2 localPoint = Vector2.zero;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(mapTransform, screenSpace, null, out localPoint);
		localPoint.x /= mapTransform.rect.width;
		localPoint.y /= mapTransform.rect.height;
		return localPoint;
	}
}

using System.Collections.Generic;
using UnityEngine;

public class RunwayGenerator : MonoBehaviour
{
	public Transform plane;

	public GameObject baseAiportDecorations;

	public GameObject cubePrefab;

	public Material runwayMaterial;

	public Material markingMaterial;

	public Material outlineMaterial;

	[Space(10f)]
	public Vector2 runwaySize;

	public float height;

	public float sideMarkingThickness;

	public float midMarkingThickness;

	public float stripeOffset;

	public float markinglength;

	public float markingInterval;

	public float outlineSize;

	private GameObject outline;

	private GameObject runway;

	private GameObject leftMarking;

	private GameObject rightMarking;

	private List<GameObject> markings = new List<GameObject>();

	private void Awake()
	{
		if (Singleton<PlaneContainer>.Instance != null)
		{
			plane = Singleton<PlaneContainer>.Instance.transform;
		}
		base.transform.localScale = Vector3.one;
		outline = CreateShape(outlineMaterial);
		runway = CreateShape(runwayMaterial);
		leftMarking = CreateShape(markingMaterial);
		rightMarking = CreateShape(markingMaterial);
		if (markinglength + markingInterval != 0f)
		{
			int num = (int)(runwaySize.y / (markinglength + markingInterval));
			for (int i = 0; i < num; i++)
			{
				GameObject item = CreateShape(markingMaterial);
				markings.Add(item);
			}
			UpdateRunway();
		}
	}

	private GameObject CreateShape(Material material)
	{
		GameObject obj = Object.Instantiate(cubePrefab);
		obj.transform.SetParent(base.transform);
		obj.transform.localRotation = Quaternion.identity;
		Material[] materials = new Material[1] { material };
		obj.GetComponent<Renderer>().materials = materials;
		return obj;
	}

	private void UpdateRunway()
	{
		float num = ((plane != null) ? Mathf.Lerp(0.01f, 0.2f, Vector3.Distance(plane.transform.position, base.transform.position) / 1500f) : 0.01f);
		outline.transform.localScale = new Vector3(runwaySize.x + outlineSize, 1f, runwaySize.y + outlineSize);
		outline.transform.position = base.transform.position - Vector3.up * (0.5f - num * 0.5f);
		runway.transform.localScale = new Vector3(runwaySize.x, 1f, runwaySize.y);
		runway.transform.position = base.transform.position - Vector3.up * (0.5f - num);
		leftMarking.transform.localScale = new Vector3(sideMarkingThickness, 1f, runwaySize.y);
		leftMarking.transform.position = base.transform.position + base.transform.right * (0f - (runwaySize.x / 2f + (0f - (sideMarkingThickness * 0.5f + stripeOffset)))) + base.transform.up * (0f - (0.5f - num * 2f));
		rightMarking.transform.localScale = new Vector3(sideMarkingThickness, 1f, runwaySize.y);
		rightMarking.transform.position = base.transform.position + base.transform.right * (runwaySize.x / 2f + (0f - (sideMarkingThickness * 0.5f + stripeOffset))) + base.transform.up * (0f - (0.5f - num * 2f));
		float num2 = markinglength + markingInterval;
		float num3 = (float)markings.Count * num2;
		Vector3 vector = base.transform.position - base.transform.forward * ((num3 - markingInterval) / 2f - markinglength * 0.5f);
		for (int i = 0; i < markings.Count; i++)
		{
			GameObject obj = markings[i];
			obj.transform.transform.position = vector + base.transform.forward * ((float)i * num2) + Vector3.up * (0f - (0.5f - num * 2f));
			obj.transform.localScale = new Vector3(midMarkingThickness, 1f, markinglength);
		}
	}

	private void Update()
	{
		UpdateRunway();
	}
}

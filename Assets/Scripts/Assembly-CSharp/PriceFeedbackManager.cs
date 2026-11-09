using System.Collections.Generic;
using UnityEngine;

public class PriceFeedbackManager : Singleton<PriceFeedbackManager>
{
	private class PersistentParticleInfo
	{
		public int lastUpdateFrame;

		public PlacementParticle particle;

		public PersistentParticleInfo(PlacementParticle particle, int lastUpdateFrame)
		{
			this.lastUpdateFrame = lastUpdateFrame;
			this.particle = particle;
		}
	}

	public GameObject placementParticlePrefab;

	public GameObject notEnoughMoneyTooltipPrefab;

	public Canvas canvas;

	public Color red;

	public float riseSpeed;

	private Dictionary<GameObject, PersistentParticleInfo> persistentParticles = new Dictionary<GameObject, PersistentParticleInfo>();

	private HighlightRenderer highlightRenderer;

	private TooltipSettings settings;

	private void Start()
	{
		highlightRenderer = Singleton<HighlightRenderer>.Instance;
		settings = new TooltipSettings();
		settings.prefab = notEnoughMoneyTooltipPrefab;
	}

	public PlacementParticle InstantiateParticle(float price, float cargo, GameObject placedObject)
	{
		if (Singleton<GameManager>.Instance.gameModeData.creativeMode)
		{
			return null;
		}
		GameObject obj = Object.Instantiate(placementParticlePrefab);
		obj.transform.SetParent(canvas.transform);
		obj.transform.localScale = Vector3.one;
		PlacementParticle component = obj.GetComponent<PlacementParticle>();
		component.Init(price, cargo, placedObject);
		riseSpeed = component.riseSpeed;
		return component;
	}

	public void UpdatePersistentParticle(float price, float cargo, GameObject targetObject, Vector3 position)
	{
		if (!Singleton<GameManager>.Instance.gameModeData.creativeMode)
		{
			if (!persistentParticles.ContainsKey(targetObject))
			{
				PlacementParticle placementParticle = InstantiateParticle(price, cargo, targetObject);
				placementParticle.riseSpeed = 0f;
				PersistentParticleInfo value = new PersistentParticleInfo(placementParticle, Time.frameCount);
				persistentParticles.Add(targetObject, value);
			}
			persistentParticles[targetObject].particle.Init(price, cargo, targetObject);
			persistentParticles[targetObject].lastUpdateFrame = Time.frameCount;
			persistentParticles[targetObject].particle.riseSpeed = 0f;
			persistentParticles[targetObject].particle.SetPosition(position);
		}
	}

	public void DestroyPersistentParticle(GameObject targetObject)
	{
		if (persistentParticles.ContainsKey(targetObject))
		{
			persistentParticles[targetObject].particle.DestroyParticle();
		}
	}

	public void HighlightNotEnoughMoneyPart(BuildingPart part)
	{
		highlightRenderer.HighlightBuildingPart(part, outline: true, highlight: true, red, 0.4f);
	}

	public void ShowNotEnoughMoneyTooltip()
	{
		Singleton<TooltipSystem>.Instance.ShowInUpdate(settings);
	}

	private void LateUpdate()
	{
		List<GameObject> list = new List<GameObject>();
		foreach (KeyValuePair<GameObject, PersistentParticleInfo> persistentParticle in persistentParticles)
		{
			if (persistentParticle.Value.lastUpdateFrame < Time.frameCount - 1)
			{
				persistentParticle.Value.particle.riseSpeed = riseSpeed;
				list.Add(persistentParticle.Key);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			persistentParticles.Remove(list[i]);
		}
	}
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class PartExploder : Singleton<PartExploder>
{
	private PlaneContainer planeContainer;

	private List<PlanePart> parts = new List<PlanePart>();

	public AudioDef explosionSound;

	public GameObject explosionParticlePrefab;

	private List<GameObject> debris = new List<GameObject>();

	public void ExplodePlane()
	{
		parts = new List<PlanePart>(planeContainer.GetComponentsInChildren<PlanePart>());
		for (int i = 0; i < parts.Count; i++)
		{
			ExplodePart(parts[i], rootExplosion: true, disableExplosion: true);
		}
	}

	public void ExplodePart(PlanePart part, bool rootExplosion = true, bool disableExplosion = false)
	{
		if (parts.Contains(part))
		{
			BuildingPart component = GetRootObject(part).GetComponent<BuildingPart>();
			if (!disableExplosion)
			{
				SpawnEffects(part.transform.position, rootExplosion);
			}
			if (GetRootObject(part).activeSelf)
			{
				CreateVisual(part, !disableExplosion);
			}
			if (component.isBasePart)
			{
				planeContainer.transform.gameObject.SetActive(value: false);
			}
			else
			{
				component.SetActive(value: false);
				parts.Remove(part);
				HandlePartRemoval(part);
			}
			for (int i = 0; i < component.children.Count; i++)
			{
				ExplodePart(component.children[i].GetComponentInChildren<PlanePart>(), rootExplosion: false, disableExplosion);
			}
			if (rootExplosion)
			{
				planeContainer.ReInitializePlane();
			}
		}
	}

	private void HandlePartRemoval(PlanePart part)
	{
		Type type = part.GetType();
		if (type == typeof(FuelTank))
		{
			planeContainer.fuelCapacity -= ((FuelTank)part).volume;
			planeContainer.fuel = Mathf.Min(planeContainer.fuelCapacity, planeContainer.fuel);
		}
		if (type == typeof(Wing))
		{
			planeContainer.fuelCapacity -= ((Wing)part).fuel;
			planeContainer.fuel = Mathf.Min(planeContainer.fuelCapacity, planeContainer.fuel);
		}
		if (type == typeof(Battery))
		{
			planeContainer.electricityStorageCapacity -= ((Battery)part).capacity;
			planeContainer.electricity = Mathf.Min(planeContainer.electricityStorageCapacity, planeContainer.electricity);
		}
	}

	public void ResetParts()
	{
		parts = new List<PlanePart>(planeContainer.GetComponentsInChildren<PlanePart>(includeInactive: true));
		BuildingPart[] componentsInChildren = planeContainer.GetComponentsInChildren<BuildingPart>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].SetActive(value: true);
		}
		for (int num = debris.Count - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(debris[num]);
			debris.RemoveAt(num);
		}
	}

	public void Init()
	{
		planeContainer = GetComponent<PlaneContainer>();
		parts = new List<PlanePart>(planeContainer.GetComponentsInChildren<PlanePart>(includeInactive: true));
	}

	private GameObject GetRootObject(PlanePart part)
	{
		if (part.transform.parent != null && part.transform.parent.gameObject != planeContainer.gameObject)
		{
			return part.transform.parent.gameObject;
		}
		return part.gameObject;
	}

	public void SpawnEffects(Vector3 position, bool root)
	{
		if (root)
		{
			Singleton<AudioManager>.Instance.PlaySound(explosionSound);
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(explosionParticlePrefab);
		gameObject.transform.position = position;
		gameObject.transform.localScale = Vector3.one * 0.1f;
		UnityEngine.Object.Destroy(gameObject, gameObject.GetComponent<ParticleSystem>().main.duration - 0.1f);
	}

	private void CreateVisual(PlanePart part, bool explode)
	{
		GameObject rootObject = GetRootObject(part);
		GameObject gameObject = ((part.GetType() != typeof(Fuselage)) ? UnityEngine.Object.Instantiate(rootObject) : CopyFuselage(part.gameObject));
		debris.Add(gameObject);
		RemoveBehaviour(gameObject, planeContainer.GetVelocityMagintude() > 50f);
		Vector3 normalized = (rootObject.transform.position - planeContainer.transform.position).normalized;
		gameObject.SetActive(value: true);
		gameObject.transform.position = rootObject.transform.position;
		if (explode)
		{
			gameObject.transform.position += normalized;
		}
		gameObject.transform.rotation = rootObject.transform.rotation;
		Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
		rigidbody.mass = part.weight / 15f;
		if (explode)
		{
			rigidbody.AddForce(normalized, ForceMode.Impulse);
		}
	}

	private GameObject CopyFuselage(GameObject fuselage)
	{
		GameObject obj = new GameObject();
		MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
		obj.AddComponent<MeshDestroyer>();
		MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
		meshCollider.convex = true;
		Mesh sharedMesh = fuselage.GetComponent<MeshFilter>().sharedMesh;
		meshFilter.sharedMesh = new Mesh();
		meshFilter.sharedMesh.vertices = sharedMesh.vertices;
		meshFilter.sharedMesh.triangles = sharedMesh.triangles;
		meshFilter.sharedMesh.uv = sharedMesh.uv;
		meshCollider.sharedMesh = meshFilter.sharedMesh;
		meshRenderer.materials = fuselage.gameObject.GetComponent<MeshRenderer>().materials;
		return obj;
	}

	private void RemoveBehaviour(GameObject obj, bool removeColliders)
	{
		MonoBehaviour[] componentsInChildren = obj.GetComponentsInChildren<MonoBehaviour>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
			if (!(componentsInChildren[i].GetType() == typeof(RetractableLandingGear)) && !(componentsInChildren[i].GetType() == typeof(PartDrag)) && !(componentsInChildren[i].GetType() == typeof(PaintableRenderer)) && !(componentsInChildren[i].GetType() == typeof(MeshDestroyer)))
			{
				UnityEngine.Object.Destroy(componentsInChildren[i]);
			}
		}
		if (removeColliders)
		{
			Collider[] componentsInChildren2 = obj.GetComponentsInChildren<Collider>();
			for (int j = 0; j < componentsInChildren2.Length; j++)
			{
				componentsInChildren2[j].enabled = false;
				UnityEngine.Object.Destroy(componentsInChildren2[j]);
			}
		}
		for (int k = 0; k < obj.transform.childCount; k++)
		{
			RemoveBehaviour(obj.transform.GetChild(k).gameObject, removeColliders);
		}
	}
}

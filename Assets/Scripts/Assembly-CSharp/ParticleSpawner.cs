using System.Collections.Generic;
using UnityEngine;

public class ParticleSpawner : Singleton<ParticleSpawner>
{
	public GameObject[] particlePrefabs;

	private List<PlaneParticle> currentParticles = new List<PlaneParticle>();

	public PlaneParticle SpawnParticle(PlaneParticleTypes particleType, Transform trackTransform, Vector3 offset, Quaternion rotationOffset)
	{
		GameObject obj = Object.Instantiate(particlePrefabs[(int)particleType]);
		PlaneParticle component = obj.GetComponent<PlaneParticle>();
		component.trackTransform = trackTransform;
		component.offset = offset;
		component.particleRotation = rotationOffset;
		currentParticles.Add(component);
		obj.transform.localScale = Vector3.one;
		obj.transform.rotation = Quaternion.identity;
		return component;
	}

	public void DestroyParticle(PlaneParticle planeParticle)
	{
		if (!(planeParticle == null))
		{
			currentParticles.Remove(planeParticle);
			Object.Destroy(planeParticle.gameObject);
		}
	}

	public void DestoyAll()
	{
		for (int i = 0; i < currentParticles.Count; i++)
		{
			Object.Destroy(currentParticles[i].gameObject);
		}
		currentParticles.Clear();
	}

	private void Update()
	{
		for (int i = 0; i < currentParticles.Count; i++)
		{
			currentParticles[i].transform.position = currentParticles[i].trackTransform.position + currentParticles[i].offset;
			currentParticles[i].transform.rotation = currentParticles[i].particleRotation;
		}
	}
}

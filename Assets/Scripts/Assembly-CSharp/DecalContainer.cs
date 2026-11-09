using System.Collections.Generic;
using UnityEngine;

public class DecalContainer : Singleton<DecalContainer>
{
	public Transform planeContainer;

	public List<Decal> decals = new List<Decal>();

	public List<Collider> colliders = new List<Collider>();

	public bool decalsHidden;

	private void FixedUpdate()
	{
		UpdateTransform();
	}

	private void Update()
	{
		UpdateTransform();
	}

	public void SetCollidersEnabled(bool value)
	{
		for (int i = 0; i < colliders.Count; i++)
		{
			colliders[i].enabled = value;
		}
	}

	public void SetDecalsHidden(bool value)
	{
		decalsHidden = value;
		bool activeInHierarchy = planeContainer.gameObject.activeInHierarchy;
		for (int i = 0; i < decals.Count; i++)
		{
			decals[i].gameObject.SetActive(activeInHierarchy && !decalsHidden);
		}
	}

	public void ResetDecals()
	{
		for (int num = decals.Count - 1; num >= 0; num--)
		{
			decals[num].DestroyDecal();
		}
		decals.Clear();
		colliders.Clear();
	}

	public void AddDecal(Decal decal)
	{
		decals.Add(decal);
		colliders.Add(decal.GetComponent<Collider>());
	}

	public void RemoveDecal(Decal decal)
	{
		decals.Remove(decal);
		colliders.Remove(decal.GetComponent<Collider>());
	}

	public void ResetSorting()
	{
		for (int i = 0; i < decals.Count; i++)
		{
			decals[i].sorted = false;
		}
	}

	public Decal GetMirrorDecal(Decal decal, PartPlacer partPlacer)
	{
		Vector3 mirroredPosition = partPlacer.symmetryPlane.GetMirroredPosition(decal.transform.position);
		Decal result = null;
		float num = float.MaxValue;
		for (int i = 0; i < decals.Count; i++)
		{
			if (!(decals[i] == decal))
			{
				float num2 = Vector3.Distance(mirroredPosition, decals[i].transform.position);
				float num3 = Vector3.Distance(decal.transform.localScale, decals[i].transform.localScale);
				float num4 = (float)Mathf.Abs(decals[i].meshDecal.layer - decal.meshDecal.layer) * 0.01f;
				if (num2 < 0.05f && num3 < 0.12f && num2 + num3 + num4 < num && decals[i].currentColor == decal.currentColor && decals[i].GetComponent<MeshRenderer>().materials[0].GetTexture("_MainTex") == decal.GetComponent<MeshRenderer>().materials[0].GetTexture("_MainTex"))
				{
					num = num2 + num3 + num4;
					result = decals[i];
				}
			}
		}
		return result;
	}

	public void Recenter(Vector3 offset)
	{
		UpdateTransform();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			base.transform.GetChild(i).transform.localPosition -= offset;
		}
	}

	public void UpdateTransform()
	{
		base.transform.position = planeContainer.transform.position;
		base.transform.rotation = planeContainer.transform.rotation;
		base.transform.localScale = planeContainer.transform.localScale;
		bool activeInHierarchy = planeContainer.gameObject.activeInHierarchy;
		if (GameManager.gameMode == GameMode.Building)
		{
			for (int i = 0; i < base.transform.childCount; i++)
			{
				base.transform.GetChild(i).gameObject.SetActive(activeInHierarchy && !decalsHidden);
			}
		}
		if (!activeInHierarchy)
		{
			for (int j = 0; j < base.transform.childCount; j++)
			{
				base.transform.GetChild(j).gameObject.SetActive(value: false);
			}
		}
	}

	public void ResetContainer()
	{
		for (int i = 0; i < base.transform.childCount; i++)
		{
			base.transform.GetChild(i).gameObject.GetComponent<Collider>().enabled = false;
		}
	}
}

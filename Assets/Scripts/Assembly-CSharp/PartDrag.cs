using UnityEngine;

public class PartDrag : MonoBehaviour
{
	[Range(0f, 1f)]
	public float dragFactor;

	private Renderer[] renderers;

	private void Awake()
	{
		renderers = GetComponentsInChildren<Renderer>();
		for (int i = 0; i < renderers.Length; i++)
		{
			for (int j = 0; j < renderers[i].materials.Length; j++)
			{
				renderers[i].materials[j].SetFloat("_DragFactor", dragFactor);
			}
		}
	}

	private void OnDestroy()
	{
		if (renderers == null)
		{
			return;
		}
		for (int i = 0; i < renderers.Length; i++)
		{
			for (int num = renderers[i].materials.Length - 1; num >= 0; num--)
			{
				if (renderers[i].materials[num] != null)
				{
					Object.Destroy(renderers[i].materials[num]);
				}
			}
		}
	}
}

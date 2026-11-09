using UnityEngine;

public class Tree : MonoBehaviour
{
	public MeshFilter LOD0;

	public MeshFilter LOD1;

	public MeshRenderer LOD0Renderer;

	public MeshRenderer LOD1Renderer;

	private CapsuleCollider capsuleCollider;

	private MeshRenderer meshRenderer;

	private Transform trans;

	private GameObject obj;

	public SurfaceDecoration surfaceDecorationType { get; private set; }

	private void Awake()
	{
		capsuleCollider = GetComponent<CapsuleCollider>();
		trans = base.transform;
		obj = base.gameObject;
	}

	public void SetPosition(Vector3 position)
	{
		trans.position = position;
	}

	public void SetRotation(Quaternion rotation)
	{
		trans.rotation = rotation;
	}

	public void SetScale(Vector3 scale)
	{
		trans.localScale = scale;
	}

	public void SetEnabled(bool enabled)
	{
		obj.SetActive(enabled);
	}

	public void Init(SurfaceDecoration surfaceDecoration)
	{
		if (surfaceDecorationType == surfaceDecoration)
		{
			return;
		}
		surfaceDecorationType = surfaceDecoration;
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat("_Shade", surfaceDecoration.shade);
		LOD0Renderer.SetPropertyBlock(materialPropertyBlock);
		LOD1Renderer.SetPropertyBlock(materialPropertyBlock);
		int num = 0;
		bool flag = false;
		if (surfaceDecoration.meshVariants != null && surfaceDecoration.meshVariants.Length != 0)
		{
			num = Random.Range(0, surfaceDecoration.meshVariants.Length);
			LOD0.sharedMesh = surfaceDecoration.meshVariants[num];
			if (surfaceDecoration.LODs != null && surfaceDecoration.LODs.Length == surfaceDecoration.meshVariants.Length)
			{
				LOD1.sharedMesh = surfaceDecoration.LODs[num];
				flag = true;
			}
		}
		else
		{
			LOD0.sharedMesh = surfaceDecoration.mesh;
		}
		if (!flag)
		{
			LOD1.sharedMesh = LOD0.sharedMesh;
		}
		capsuleCollider.height = surfaceDecoration.height;
		capsuleCollider.radius = surfaceDecoration.radius;
		if (!surfaceDecoration.center)
		{
			capsuleCollider.center = new Vector3(capsuleCollider.center.x, capsuleCollider.height * 0.5f, capsuleCollider.center.z);
		}
		else
		{
			capsuleCollider.center = new Vector3(0f, 0f, 0f);
		}
	}
}

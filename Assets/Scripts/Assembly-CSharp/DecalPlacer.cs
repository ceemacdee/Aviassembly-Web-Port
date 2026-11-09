using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DecalPlacer : MonoBehaviour
{
	[Header("Decal placement")]
	public GameObject decalPrefab;

	public Texture2D defaultTexture;

	public LayerMask layerMask;

	public LayerMask planePartMask;

	public AudioDef placementSound;

	public AudioDef selectionSound;

	public Camera overlapCamTest;

	[Header("Refrences")]
	public PartPlacer partPlacer;

	public PlacementToolbar toolbar;

	[Header("Gizmos")]
	public RotationGizmo rotationGizmo;

	public MoveGizmo moveGizmo;

	public ScaleGizmo scaleGizmo;

	private bool isMovingDecal;

	private bool placedThisFrame;

	public Decal selectedDecal;

	private Decal mirrorSelectedDecal;

	private DecalContainer decalContainer;

	private List<Bounds> previousBounds = new List<Bounds>();

	private Dictionary<DecalPair, bool> overlapChache = new Dictionary<DecalPair, bool>();

	private List<Tuple<int, List<GameObject>>> recalculationQueue = new List<Tuple<int, List<GameObject>>>();

	public Texture2D currentDecalTexture { get; private set; }

	public bool IsPlacingDecal
	{
		get
		{
			if (!isMovingDecal || !(selectedDecal != null))
			{
				return placedThisFrame;
			}
			return true;
		}
		private set
		{
		}
	}

	private void Start()
	{
		currentDecalTexture = defaultTexture;
		decalContainer = Singleton<DecalContainer>.Instance;
		toolbar.rotationClicked += Rotate;
		toolbar.moveClicked += Move;
		toolbar.deleteClicked += DestroyDecal;
		toolbar.scaleClicked += Scale;
	}

	public Decal MakeMirrorCopy(Decal decal, BuildingPart parentPart)
	{
		Decal component = UnityEngine.Object.Instantiate(decalPrefab).GetComponent<Decal>();
		component.transform.localScale = decal.transform.localScale;
		component.transform.position = partPlacer.symmetryPlane.GetMirroredPosition(decal.transform.position);
		component.transform.rotation = partPlacer.symmetryPlane.GetMirroredRotation(decal.transform);
		component.transform.rotation = Quaternion.LookRotation(component.transform.forward, -component.transform.up);
		component.transform.SetParent(decalContainer.transform, worldPositionStays: true);
		component.SetParent(parentPart);
		component.hasBeenPlaced = true;
		component.GetComponent<Collider>().enabled = false;
		component.gameObject.SetActive(parentPart.gameObject.activeInHierarchy);
		component.currentTexture = (Texture2D)decal.GetComponent<MeshRenderer>().materials[0].GetTexture("_MainTex");
		component.currentColor = decal.currentColor;
		component.GetComponent<MeshRenderer>().materials[0].SetTexture("_MainTex", component.currentTexture);
		component.SetColor(component.currentColor, apply: true);
		component.meshDecal.layer = GetHighestOverlap(component, simpleTest: true);
		component.meshDecal.Recalculate();
		decalContainer.decals.Add(component);
		decalContainer.colliders.Add(component.GetComponent<Collider>());
		return decal;
	}

	private void OrientProjectionVolume(Decal decal, Ray baseRay)
	{
		if (Physics.Raycast(baseRay, out var hitInfo, float.MaxValue, planePartMask))
		{
			Vector3 vector = Vector3.Cross(-hitInfo.normal, Singleton<PlaneContainer>.Instance.UpdateForwardDirection()).normalized;
			if (Vector3.Dot(vector, Vector3.up) < 0f)
			{
				vector = -vector;
			}
			if (decal.hasBeenPlaced)
			{
				vector = decal.transform.up;
			}
			decal.transform.position = hitInfo.point;
			decal.transform.rotation = Quaternion.LookRotation(-hitInfo.normal, vector);
			Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.red);
		}
	}

	private void CreateDecal()
	{
		if (selectedDecal != null)
		{
			DestroyDecal();
		}
		selectedDecal = UnityEngine.Object.Instantiate(decalPrefab).GetComponent<Decal>();
		selectedDecal.transform.localScale = Vector3.one * 0.3f;
		selectedDecal.GetComponent<Collider>().enabled = false;
		selectedDecal.GetComponent<MeshRenderer>().materials[0].SetTexture("_MainTex", currentDecalTexture);
		decalContainer.decals.Add(selectedDecal);
		decalContainer.colliders.Add(selectedDecal.GetComponent<Collider>());
		mirrorSelectedDecal = UnityEngine.Object.Instantiate(decalPrefab).GetComponent<Decal>();
		mirrorSelectedDecal.transform.localScale = Vector3.one * 0.3f;
		mirrorSelectedDecal.GetComponent<Collider>().enabled = false;
		mirrorSelectedDecal.GetComponent<MeshRenderer>().materials[0].SetTexture("_MainTex", currentDecalTexture);
		decalContainer.decals.Add(mirrorSelectedDecal);
		decalContainer.colliders.Add(mirrorSelectedDecal.GetComponent<Collider>());
		isMovingDecal = true;
	}

	public void PlaceDecal(Decal decal, GameObject parent)
	{
		decal.transform.SetParent(decalContainer.transform, worldPositionStays: true);
		decal.meshDecal.layer = GetHighestOverlap(decal);
		decal.meshDecal.Recalculate();
		decal.SetParent(PartPlacer.GetBuildingPartComponent(parent));
		decal.currentTexture = currentDecalTexture;
		placedThisFrame = true;
		decal.hasBeenPlaced = true;
	}

	private void DestroyDecal()
	{
		isMovingDecal = false;
		if (selectedDecal != null)
		{
			Decal mirrorDecal = decalContainer.GetMirrorDecal(selectedDecal, partPlacer);
			selectedDecal.DestroyDecal();
			selectedDecal = null;
			if (mirrorDecal != null && PartPlacer.mirrorMode)
			{
				mirrorDecal.DestroyDecal();
			}
		}
		if (mirrorSelectedDecal != null)
		{
			mirrorSelectedDecal.DestroyDecal();
			mirrorSelectedDecal = null;
		}
	}

	public void RecalculateDecals(GameObject additionalCheckPart = null, GameObject mirrorAdditionalCheckPart = null)
	{
		List<GameObject> list = new List<GameObject>();
		if (additionalCheckPart != null)
		{
			list.Add(additionalCheckPart);
		}
		if (mirrorAdditionalCheckPart != null)
		{
			list.Add(mirrorAdditionalCheckPart);
		}
		recalculationQueue.Add(new Tuple<int, List<GameObject>>(Time.frameCount, list));
	}

	private void ExecuteRecalculation(List<GameObject> additionalCheckParts)
	{
		List<Bounds> list = new List<Bounds>();
		MeshRenderer[] componentsInChildren = Singleton<PlaneContainer>.Instance.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			list.Add(componentsInChildren[i].bounds);
		}
		List<Bounds> list2 = list.Except(previousBounds).Union(previousBounds.Except(list)).ToList();
		for (int j = 0; j < additionalCheckParts.Count; j++)
		{
			list2.Add(additionalCheckParts[j].gameObject.GetComponentInChildren<MeshRenderer>().bounds);
		}
		List<Decal> list3 = new List<Decal>();
		for (int k = 0; k < list2.Count; k++)
		{
			for (int l = 0; l < decalContainer.decals.Count; l++)
			{
				if (list2[k].Intersects(decalContainer.decals[l].meshDecal.GetMeshAABB()) && !list3.Contains(decalContainer.decals[l]))
				{
					decalContainer.decals[l].meshDecal.Recalculate();
					list3.Add(decalContainer.decals[l]);
				}
			}
		}
		previousBounds = list;
	}

	private void UpdatePlacement()
	{
		Ray ray = BuildingCamera.cam.ScreenPointToRay(MouseInput.GetMousePosition());
		bool active = Physics.Raycast(ray, out var hitInfo);
		OrientProjectionVolume(selectedDecal, ray);
		Vector3 mirroredPosition = partPlacer.symmetryPlane.GetMirroredPosition(ray.origin);
		Vector3 normalized = (partPlacer.symmetryPlane.GetMirroredPosition(ray.origin + ray.direction) - mirroredPosition).normalized;
		bool flag = Physics.Raycast(new Ray(mirroredPosition, normalized), out var hitInfo2);
		if (flag && mirrorSelectedDecal != null)
		{
			mirrorSelectedDecal.transform.position = hitInfo2.point;
			mirrorSelectedDecal.transform.rotation = partPlacer.symmetryPlane.GetMirroredRotation(selectedDecal.transform);
			mirrorSelectedDecal.transform.rotation = Quaternion.LookRotation(mirrorSelectedDecal.transform.forward, -mirrorSelectedDecal.transform.up);
		}
		selectedDecal.gameObject.SetActive(active);
		if (mirrorSelectedDecal != null)
		{
			mirrorSelectedDecal.gameObject.SetActive(flag && PartPlacer.mirrorMode);
		}
		selectedDecal.meshDecal.layer = GetHighestOverlap(selectedDecal);
		selectedDecal.meshDecal.Recalculate();
		if (mirrorSelectedDecal != null)
		{
			mirrorSelectedDecal.meshDecal.layer = GetHighestOverlap(mirrorSelectedDecal);
			mirrorSelectedDecal.meshDecal.Recalculate();
		}
		if (MouseInput.GetMouseButtonDown(0))
		{
			bool hasBeenPlaced = selectedDecal.hasBeenPlaced;
			if (selectedDecal != null && selectedDecal.gameObject.activeInHierarchy)
			{
				PlaceDecal(selectedDecal, hitInfo.collider.gameObject);
				selectedDecal = null;
				Singleton<AudioManager>.Instance.PlaySound(placementSound);
				Singleton<PlaneStorage>.Instance.UpdateHistory();
			}
			if (mirrorSelectedDecal != null && mirrorSelectedDecal.gameObject.activeInHierarchy)
			{
				PlaceDecal(mirrorSelectedDecal, hitInfo2.collider.gameObject);
				mirrorSelectedDecal = null;
			}
			DestroyDecal();
			if (!hasBeenPlaced)
			{
				CreateDecal();
			}
			else
			{
				isMovingDecal = false;
			}
			placedThisFrame = true;
		}
	}

	public bool HoveringDecal()
	{
		return GetHoverDecal() != null;
	}

	public Decal GetHoverDecal()
	{
		RaycastHit[] hits = Physics.RaycastAll(BuildingCamera.cam.ScreenPointToRay(MouseInput.GetMousePosition()), 300f, layerMask);
		Decal decal = SelectBestDecal(hits, textureTest: true);
		if (decal != null)
		{
			return decal;
		}
		return SelectBestDecal(hits, textureTest: false);
	}

	private Decal SelectBestDecal(RaycastHit[] hits, bool textureTest)
	{
		Decal result = null;
		int num = 0;
		for (int i = 0; i < hits.Length; i++)
		{
			Decal component = hits[i].collider.GetComponent<Decal>();
			int layer = component.meshDecal.layer;
			if (textureTest)
			{
				Texture2D texture2D = (Texture2D)component.GetComponent<MeshRenderer>().materials[0].GetTexture("_MainTex");
				if (texture2D.GetPixel(Mathf.RoundToInt(hits[i].textureCoord.x * (float)texture2D.width), Mathf.RoundToInt(hits[i].textureCoord.y * (float)texture2D.height)).a < 0.1f)
				{
					continue;
				}
			}
			if (layer > num)
			{
				result = component;
				num = layer;
			}
		}
		return result;
	}

	public bool UpdateSelection()
	{
		if (isMovingDecal || partPlacer.procduralPartController.gizmoManager.GizmoHover())
		{
			return false;
		}
		Decal decal = selectedDecal;
		Decal hoverDecal = GetHoverDecal();
		if (hoverDecal != null)
		{
			Singleton<HighlightRenderer>.Instance.AddHighlightObject(hoverDecal.gameObject, new Color(0f, 0f, 0f, 0.2f));
		}
		if (MouseInput.GetMouseButtonDown(0) && !Singleton<MouseInput>.Instance.PointerIsOverUI && !rotationGizmo.MouseInGizmo() && !scaleGizmo.MouseInGizmo() && !moveGizmo.MouseInGizmo())
		{
			if (selectedDecal != hoverDecal)
			{
				scaleGizmo.gameObject.SetActive(value: false);
				rotationGizmo.gameObject.SetActive(value: false);
			}
			selectedDecal = hoverDecal;
			mirrorSelectedDecal = null;
			if (selectedDecal != null && PartPlacer.mirrorMode)
			{
				mirrorSelectedDecal = decalContainer.GetMirrorDecal(selectedDecal, partPlacer);
				if (mirrorSelectedDecal == null)
				{
					BuildingPart buildingPart = partPlacer.GetMirrorPart(selectedDecal.parentBuildingPart);
					if ((double)partPlacer.symmetryPlane.DistanceFromPlane(selectedDecal.parentBuildingPart.transform.position) < 0.05)
					{
						buildingPart = selectedDecal.parentBuildingPart;
					}
					if (buildingPart != null)
					{
						MakeMirrorCopy(selectedDecal, buildingPart);
						mirrorSelectedDecal = decalContainer.GetMirrorDecal(selectedDecal, partPlacer);
					}
				}
			}
		}
		if (MouseInput.GetMouseButtonDown(1))
		{
			selectedDecal = null;
		}
		if (decal != null && selectedDecal == null)
		{
			Singleton<PlaneStorage>.Instance.UpdateHistory();
		}
		if (selectedDecal != decal && selectedDecal != null)
		{
			Singleton<AudioManager>.Instance.PlaySound(selectionSound);
		}
		return selectedDecal != null;
	}

	private void Update()
	{
		for (int num = recalculationQueue.Count - 1; num >= 0; num--)
		{
			if (Time.frameCount > recalculationQueue[num].Item1)
			{
				ExecuteRecalculation(recalculationQueue[num].Item2);
				recalculationQueue.RemoveAt(num);
			}
		}
		placedThisFrame = false;
		decalContainer.SetCollidersEnabled(!isMovingDecal && !moveGizmo.gameObject.activeInHierarchy && partPlacer.currentMovingPart == null && !Singleton<GameManager>.Instance.Loading && GameManager.gameMode == GameMode.Building);
		if (selectedDecal != null)
		{
			decalContainer.SetDecalsHidden(value: false);
		}
		if (partPlacer.currentMovingPart != null)
		{
			selectedDecal = null;
		}
		if (selectedDecal == null)
		{
			scaleGizmo.gameObject.SetActive(value: false);
		}
		if (isMovingDecal && selectedDecal != null)
		{
			UpdatePlacement();
		}
		if (selectedDecal != null)
		{
			if (!IsPlacingDecal)
			{
				Singleton<HighlightRenderer>.Instance.AddOutlineObject(selectedDecal.gameObject, Color.black);
			}
			if (!isMovingDecal && !moveGizmo.gameObject.activeInHierarchy && !rotationGizmo.gameObject.activeInHierarchy && !scaleGizmo.gameObject.activeInHierarchy)
			{
				toolbar.EnableToolbar(selectedDecal.gameObject);
				toolbar.UpdateButtons(0, hasSettings: false, hasKeybinds: false, rotateable: true, moveable: true, isDecal: true);
			}
			if (rotationGizmo.gameObject.activeInHierarchy)
			{
				selectedDecal.transform.rotation = rotationGizmo.transform.rotation;
				selectedDecal.meshDecal.Recalculate();
				SolveCollisions(selectedDecal, increment: true, 100);
			}
			if (moveGizmo.gameObject.activeInHierarchy)
			{
				moveGizmo.UpdateGizmo();
				selectedDecal.transform.position = moveGizmo.transform.position;
				Ray baseRay = BuildingCamera.cam.ScreenPointToRay(BuildingCamera.cam.WorldToScreenPoint(moveGizmo.transform.position));
				OrientProjectionVolume(selectedDecal, baseRay);
				selectedDecal.meshDecal.Recalculate();
				SolveCollisions(selectedDecal, increment: true, 100);
			}
			if (scaleGizmo.gameObject.activeInHierarchy)
			{
				scaleGizmo.UpdateGizmo();
				Vector3 currentScale = scaleGizmo.currentScale;
				currentScale.x = Mathf.Clamp(currentScale.x, 0.05f, 1.5f);
				currentScale.y = Mathf.Clamp(currentScale.y, 0.05f, 1.5f);
				currentScale.z = Mathf.Clamp(currentScale.z, 0.05f, 1.5f);
				selectedDecal.transform.localScale = currentScale;
				selectedDecal.meshDecal.Recalculate();
				SolveCollisions(selectedDecal, increment: true, 100);
			}
			if (mirrorSelectedDecal != null)
			{
				mirrorSelectedDecal.transform.position = partPlacer.symmetryPlane.GetMirroredPosition(selectedDecal.transform.position);
				mirrorSelectedDecal.transform.rotation = partPlacer.symmetryPlane.GetMirroredRotation(selectedDecal.transform);
				mirrorSelectedDecal.transform.rotation = Quaternion.LookRotation(mirrorSelectedDecal.transform.forward, -mirrorSelectedDecal.transform.up);
				mirrorSelectedDecal.transform.localScale = selectedDecal.transform.localScale;
				mirrorSelectedDecal.meshDecal.Recalculate();
			}
		}
		if (IsPlacingDecal && MouseInput.GetMouseButtonDown(1))
		{
			ToggleDecalMode();
		}
	}

	private int GetHighestOverlap(Decal current, bool simpleTest = false)
	{
		overlapChache.Clear();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < decalContainer.decals.Count; i++)
		{
			if (!(decalContainer.decals[i].meshDecal == current.meshDecal) && !(decalContainer.decals[i] == mirrorSelectedDecal))
			{
				num2++;
				if (CheckOverlap(current, decalContainer.decals[i], simpleTest))
				{
					num = Mathf.Max(decalContainer.decals[i].meshDecal.layer, num);
					num3++;
				}
			}
		}
		return num + 1;
	}

	private bool CheckOverlap(Decal decal1, Decal decal2, bool simpleTest = false)
	{
		DecalPair key = new DecalPair(decal1, decal2);
		if (overlapChache.ContainsKey(key))
		{
			return overlapChache[key];
		}
		bool flag = decal1.meshDecal.GetProjectionVolumeAABB().Intersects(decal2.meshDecal.GetProjectionVolumeAABB());
		if (simpleTest)
		{
			return flag;
		}
		if (!simpleTest && !flag)
		{
			overlapChache.Add(key, value: false);
			return false;
		}
		RenderTexture renderTexture = new RenderTexture(64, Mathf.RoundToInt(64f * (decal1.transform.localScale.y / decal1.transform.localScale.x)), 24);
		renderTexture.Create();
		overlapCamTest.targetTexture = renderTexture;
		RenderTexture.active = renderTexture;
		Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, mipChain: false);
		Texture2D texture2D2 = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, mipChain: false);
		overlapCamTest.transform.position = decal1.transform.position - decal1.transform.forward;
		overlapCamTest.transform.rotation = decal1.transform.rotation;
		overlapCamTest.orthographicSize = decal1.transform.localScale.y;
		decal1.gameObject.layer = LayerMask.NameToLayer("DecalOverlapTest");
		overlapCamTest.Render();
		texture2D.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
		texture2D.Apply();
		decal1.gameObject.layer = LayerMask.NameToLayer("Decal");
		decal2.gameObject.layer = LayerMask.NameToLayer("DecalOverlapTest");
		overlapCamTest.Render();
		texture2D2.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
		texture2D2.Apply();
		decal2.gameObject.layer = LayerMask.NameToLayer("Decal");
		Color32[] pixels = texture2D.GetPixels32();
		Color32[] pixels2 = texture2D2.GetPixels32();
		RenderTexture.active = null;
		renderTexture.Release();
		UnityEngine.Object.Destroy(renderTexture);
		UnityEngine.Object.Destroy(texture2D);
		UnityEngine.Object.Destroy(texture2D2);
		int num = renderTexture.width * renderTexture.height;
		for (int i = 0; i < num; i++)
		{
			if (pixels[i].r > 2 && pixels2[i].r > 2)
			{
				overlapChache.Add(key, value: true);
				return true;
			}
		}
		overlapChache.Add(key, value: false);
		return false;
	}

	private bool SolveCollisions(Decal origin, bool increment, int itterations, bool recusiveCall = false)
	{
		if (!recusiveCall)
		{
			Singleton<DecalContainer>.Instance.ResetSorting();
			overlapChache.Clear();
			origin.sorted = true;
			Decal mirrorDecal = Singleton<DecalContainer>.Instance.GetMirrorDecal(origin, partPlacer);
			if (mirrorDecal != null)
			{
				mirrorDecal.sorted = true;
			}
		}
		for (int i = 0; i < itterations; i++)
		{
			List<Decal> list = new List<Decal>();
			for (int j = 0; j < decalContainer.decals.Count; j++)
			{
				for (int k = 0; k < decalContainer.decals.Count; k++)
				{
					if (!(decalContainer.decals[j] == decalContainer.decals[k]) && decalContainer.decals[j].meshDecal.layer == decalContainer.decals[k].meshDecal.layer && !decalContainer.decals[k].sorted && CheckOverlap(decalContainer.decals[j], decalContainer.decals[k]) && !list.Contains(decalContainer.decals[k]) && !list.Contains(decalContainer.decals[j]))
					{
						list.Add(decalContainer.decals[k]);
					}
				}
			}
			if (list.Count == 0)
			{
				return false;
			}
			List<Decal> list2 = new List<Decal>();
			for (int l = 0; l < list.Count; l++)
			{
				if (list[l].meshDecal.layer == 1 && !increment)
				{
					list2.Add(list[l]);
					list[l].meshDecal.layer = 1;
				}
				if (list2.Count <= 0)
				{
					list[l].meshDecal.layer += (increment ? 1 : (-1));
					list[l].meshDecal.Recalculate();
					list[l].sorted = true;
				}
			}
			if (list2.Count > 0)
			{
				decalContainer.ResetSorting();
				for (int m = 0; m < list2.Count; m++)
				{
					list2[m].sorted = true;
				}
				SolveCollisions(list2[0], increment: true, 100, recusiveCall: true);
				return false;
			}
		}
		return true;
	}

	public void ToggleDecalMode()
	{
		if (!IsPlacingDecal)
		{
			PartPainter.PaintModeEnabled = false;
			CreateDecal();
		}
		else
		{
			DestroyDecal();
		}
	}

	public void SelectTexture(Texture2D texture)
	{
		currentDecalTexture = texture;
		if (selectedDecal != null)
		{
			selectedDecal.GetComponent<MeshRenderer>().materials[0].SetTexture("_MainTex", currentDecalTexture);
			selectedDecal.currentTexture = texture;
		}
		if (mirrorSelectedDecal != null)
		{
			mirrorSelectedDecal.GetComponent<MeshRenderer>().materials[0].SetTexture("_MainTex", currentDecalTexture);
			mirrorSelectedDecal.currentTexture = texture;
		}
	}

	private void Rotate()
	{
		if (selectedDecal != null)
		{
			rotationGizmo.transform.rotation = selectedDecal.transform.rotation;
			rotationGizmo.gameObject.SetActive(value: true);
			rotationGizmo.transform.position = CustomMath.GetObjectCenter(selectedDecal.gameObject);
			rotationGizmo.SetElementEnabled(x: false, y: false, z: true);
			scaleGizmo.gameObject.SetActive(value: false);
		}
	}

	private void Move()
	{
		if (selectedDecal != null)
		{
			moveGizmo.transform.position = selectedDecal.transform.position;
			moveGizmo.transform.rotation = selectedDecal.transform.rotation;
			moveGizmo.gameObject.SetActive(value: true);
			moveGizmo.SetElementEnabled(x: true, y: true, z: false);
		}
	}

	private void Scale()
	{
		if (selectedDecal != null)
		{
			scaleGizmo.ResetScaleOffset(selectedDecal.transform.localScale);
			scaleGizmo.gameObject.SetActive(value: true);
			scaleGizmo.transform.position = selectedDecal.transform.position;
			scaleGizmo.transform.rotation = selectedDecal.transform.rotation;
			scaleGizmo.SetElementEnabled(x: true, y: true, z: false);
			rotationGizmo.gameObject.SetActive(value: false);
		}
	}

	public void IncreaseLayer()
	{
		IncreaseLayer(selectedDecal);
		if (mirrorSelectedDecal != null)
		{
			IncreaseLayer(mirrorSelectedDecal);
		}
	}

	public void DecreaseLayer()
	{
		DecreaseLayer(selectedDecal);
		if (mirrorSelectedDecal != null)
		{
			DecreaseLayer(mirrorSelectedDecal);
		}
	}

	private void IncreaseLayer(Decal decal)
	{
		if (decal == null)
		{
			return;
		}
		overlapChache.Clear();
		int num = int.MaxValue;
		Decal decal2 = null;
		for (int i = 0; i < decalContainer.decals.Count; i++)
		{
			if (decalContainer.decals[i].meshDecal.layer > decal.meshDecal.layer && decalContainer.decals[i].meshDecal.layer < num && CheckOverlap(decal, decalContainer.decals[i]))
			{
				decal2 = decalContainer.decals[i];
				num = decalContainer.decals[i].meshDecal.layer;
			}
		}
		if (decal2 != null)
		{
			decal.meshDecal.layer = Mathf.Max(decal2.meshDecal.layer, 1);
			decal.meshDecal.Recalculate();
			decal.sorted = true;
		}
		SolveCollisions(decal, increment: false, 100);
	}

	public void DecreaseLayer(Decal decal)
	{
		if (decal == null)
		{
			return;
		}
		overlapChache.Clear();
		int num = 0;
		Decal decal2 = null;
		for (int i = 0; i < decalContainer.decals.Count; i++)
		{
			if (decalContainer.decals[i].meshDecal.layer < decal.meshDecal.layer && decalContainer.decals[i].meshDecal.layer > num && CheckOverlap(decal, decalContainer.decals[i]))
			{
				decal2 = decalContainer.decals[i];
				num = decalContainer.decals[i].meshDecal.layer;
			}
		}
		if (decal2 != null)
		{
			decal.meshDecal.layer = Mathf.Max(decal2.meshDecal.layer, 1);
			decal.meshDecal.Recalculate();
			decal.sorted = true;
		}
		SolveCollisions(decal, increment: true, 100);
	}
}

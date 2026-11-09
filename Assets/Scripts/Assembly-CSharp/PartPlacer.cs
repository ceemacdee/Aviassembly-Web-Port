using UnityEngine;

public class PartPlacer : MonoBehaviour
{
	[Header("Audio")]
	public AudioDef selectionSound;

	public AudioDef placementSound;

	public AudioDef removalSound;

	[Space(20f)]
	[Header("Misc")]
	public float snapDistance;

	public BuildingCamera buildingCamera;

	public HighlightRenderer highlightRenderer;

	public RotationGizmo rotationGizmo;

	public MoveGizmo moveGizmo;

	public ProceduralPartManager procduralPartController;

	public DecalPlacer decalPlacer;

	public LayerMask selectionMask;

	[Space(20f)]
	[Header("Particle")]
	public PriceFeedbackManager priceFeedbackManager;

	public Color red;

	public Color green;

	public PartContainer partContainer = new PartContainer();

	public SymmetryPlane symmetryPlane;

	public static bool mirrorMode = false;

	public static bool snapMode = true;

	private float rotationOffset;

	private Vector3 prefMousePos;

	private AudioManager audioManager;

	private PlaneContainer planeContainer;

	private float currentDistance;

	private Quaternion currentRotation;

	private Vector3 positionOffset;

	private const float centerSnapDistance = 0.075f;

	private const float maxSelectionDistance = 1000f;

	public static bool PlaneReady { get; private set; }

	public BuildingPart currentMovingPart { get; private set; }

	public BuildingPart currentSelectedPart { get; private set; }

	public BuildingPart currentMirrorPart { get; private set; }

	public float movementSinceClick { get; private set; }

	private void Start()
	{
		priceFeedbackManager = GetComponent<PriceFeedbackManager>();
		partContainer.red = red;
		audioManager = Singleton<AudioManager>.Instance;
		symmetryPlane = new SymmetryPlane(this);
		Singleton<PlaneContainer>.Instance.gameObject.GetComponent<PlaneStorage>().ResetPlaneEvent += ResetPlaneBuilding;
		rotationGizmo.gameObject.SetActive(value: false);
		moveGizmo.gameObject.SetActive(value: false);
	}

	private void ResetPlaneBuilding()
	{
		partContainer.ResetContainer();
	}

	private void Update()
	{
		procduralPartController.UpdateProcduralPartController();
		symmetryPlane.UpdatePlanePosition(Singleton<PlaneContainer>.Instance);
		planeContainer = Singleton<PlaneContainer>.Instance;
		PlaneReady = !partContainer.UpdateAttachmentFeedback();
		partContainer.UpdateEngineBacksides(currentMovingPart);
		UpdateMeshReadablityWarning(planeContainer);
		bool partWasPlacedThisFrame = UpdatePlacement();
		UpdateSelection(partWasPlacedThisFrame);
		if (currentSelectedPart != null && (currentMovingPart == null || !currentMovingPart.IsAttatched()))
		{
			Color color = (currentSelectedPart.IsAttatched() ? green : red);
			highlightRenderer.HighlightBuildingPart(currentSelectedPart, outline: true, color == red, color, (color == red) ? 0.4f : 0.2f);
		}
		if (currentMovingPart == null)
		{
			buildingCamera.UpdateOrigin();
		}
		if (GameManager.gameMode == GameMode.Building && !Singleton<GameManager>.Instance.Loading)
		{
			UpdateRotation();
			UpdateMove();
		}
		if ((Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) && Singleton<InputManager>.Instance.currentInputBlocker == null)
		{
			DestroySelectedPart();
		}
	}

	private void UpdateRotation()
	{
		if (currentSelectedPart != null && rotationGizmo.gameObject.activeInHierarchy)
		{
			BuildingPart mirrorPart = GetMirrorPart(currentSelectedPart);
			currentSelectedPart.SetRotation(rotationGizmo.transform.rotation);
			Quaternion quaternion = Quaternion.Inverse(currentSelectedPart.baseRotation) * rotationGizmo.transform.rotation;
			currentSelectedPart.rotationOffset = quaternion;
			if (mirrorPart != null)
			{
				Quaternion mirroredRotation = symmetryPlane.GetMirroredRotation(currentSelectedPart.transform);
				Quaternion quaternion2 = Quaternion.Inverse(mirrorPart.baseRotation) * mirroredRotation;
				mirrorPart.rotationOffset = quaternion2;
				mirrorPart.SetRotation(mirroredRotation);
			}
		}
	}

	private void UpdateMove()
	{
		if (currentSelectedPart != null && moveGizmo.gameObject.activeInHierarchy)
		{
			moveGizmo.UpdateGizmo();
			BuildingPart mirrorPart = GetMirrorPart(currentSelectedPart);
			currentSelectedPart.SetPosition(moveGizmo.transform.position);
			if (mirrorPart != null)
			{
				Vector3 mirroredPosition = symmetryPlane.GetMirroredPosition(moveGizmo.transform.position);
				mirrorPart.SetPosition(mirroredPosition);
			}
			partContainer.UpdateAttachment(currentSelectedPart);
			if (mirrorPart != null)
			{
				partContainer.UpdateAttachment(mirrorPart);
			}
			partContainer.UpdateUsedSnapPoints();
		}
	}

	public void InstantiatePart(GameObject partPrefab)
	{
		if (partContainer != null)
		{
			if (currentMovingPart != null && !currentMovingPart.hasBeenPlaced)
			{
				DestroySelectedPart();
				currentMovingPart = null;
			}
			GameObject gameObject = Object.Instantiate(partPrefab);
			gameObject.transform.SetParent(planeContainer.transform, worldPositionStays: true);
			SelectPart(GetBuildingPartComponent(gameObject));
			Update();
		}
	}

	public void SelectPart(BuildingPart buildingPart, bool child = false)
	{
		if (currentMovingPart != null && !child)
		{
			PlaceSelectedPart();
		}
		partContainer.RemovePart(buildingPart);
		if (!child)
		{
			currentMovingPart = buildingPart;
			currentSelectedPart = buildingPart;
			UpdateGizmos(currentSelectedPart);
			currentMirrorPart = GetMirrorPart(buildingPart);
			if (currentMirrorPart == null)
			{
				currentMirrorPart = CreateMirrorPart(buildingPart);
			}
			if (currentMirrorPart != null)
			{
				SelectPart(currentMirrorPart, child: true);
			}
		}
		buildingPart.EnableColliders(value: false);
		for (int i = 0; i < buildingPart.children.Count; i++)
		{
			SelectPart(buildingPart.children[i], child: true);
		}
	}

	public void PlaceSelectedPart()
	{
		BuildingPart buildingPart = currentSelectedPart;
		BuildingPart buildingPart2 = currentMirrorPart;
		currentMovingPart = null;
		currentMirrorPart = null;
		if (!currentSelectedPart.hasBeenPlaced)
		{
			currentSelectedPart = null;
		}
		PlacePart(buildingPart);
		if (buildingPart2 != null)
		{
			if (buildingPart2.gameObject.activeInHierarchy)
			{
				PlacePart(buildingPart2);
			}
			else
			{
				DestroyPart(buildingPart2);
			}
		}
	}

	public void DestroySelectedPart()
	{
		if (!(currentSelectedPart == null))
		{
			BuildingPart mirrorPart = currentMirrorPart;
			if (mirrorPart == null)
			{
				mirrorPart = GetMirrorPart(currentSelectedPart);
			}
			DestroyPart(mirrorPart);
			DestroyPart(currentSelectedPart);
			if (currentMovingPart == currentSelectedPart)
			{
				currentMovingPart = null;
			}
			currentSelectedPart = null;
			currentMirrorPart = null;
			PlaySound(removalSound);
		}
	}

	private void DestroyPart(BuildingPart part)
	{
		if (!(part == null))
		{
			procduralPartController.ResetTransformation();
			float priceValue = 0f;
			float cargoVolume = 0f;
			part.DestoryPart(partContainer, ref priceValue, ref cargoVolume);
			Singleton<PlaneStorage>.Instance.UpdateHistory();
			decalPlacer.RecalculateDecals();
			Singleton<MoneyManager>.Instance.ChangeMoneyAmount(priceValue);
			if (part.hasBeenPlaced)
			{
				priceFeedbackManager.InstantiateParticle(priceValue, 0f - cargoVolume, part.transform.gameObject);
			}
		}
	}

	public void PlacePart(BuildingPart buildingPart)
	{
		partContainer.AddPart(buildingPart);
		buildingPart.EnableColliders(value: true);
		for (int i = 0; i < buildingPart.children.Count; i++)
		{
			PlacePart(buildingPart.children[i]);
		}
		buildingPart.hasBeenPlaced = true;
		Singleton<PlaneStorage>.Instance.UpdateHistory();
		decalPlacer.RecalculateDecals();
	}

	private void UpdateSelection(bool partWasPlacedThisFrame)
	{
		if (currentSelectedPart != null && currentMovingPart == null)
		{
			if (MouseInput.currentMouse.leftButton.wasPressedThisFrame && !Singleton<MouseInput>.Instance.PointerIsOverUI && !rotationGizmo.gameObject.activeInHierarchy && !moveGizmo.gameObject.activeInHierarchy && !procduralPartController.gizmoManager.GizmoHover())
			{
				currentSelectedPart = null;
			}
			if (MouseInput.currentMouse.leftButton.wasPressedThisFrame && !Singleton<MouseInput>.Instance.PointerIsOverUI && ((rotationGizmo.gameObject.activeInHierarchy && !rotationGizmo.MouseInGizmo()) || (moveGizmo.gameObject.activeInHierarchy && !moveGizmo.MouseInGizmo())))
			{
				rotationGizmo.gameObject.SetActive(value: false);
				moveGizmo.gameObject.SetActive(value: false);
			}
		}
		if (MouseInput.currentMouse.rightButton.wasReleasedThisFrame && !buildingCamera.Rotated && currentSelectedPart != null)
		{
			if (currentMovingPart != null)
			{
				if (currentMovingPart.hasBeenPlaced)
				{
					if (currentMirrorPart != null && !currentMirrorPart.hasBeenPlaced)
					{
						DestroyPart(currentMirrorPart);
						currentMirrorPart = null;
					}
					PlaceSelectedPart();
				}
				else
				{
					DestroySelectedPart();
				}
			}
			currentSelectedPart = null;
		}
		if (currentMovingPart == null)
		{
			positionOffset = Vector3.zero;
			currentRotation = Quaternion.identity;
			currentDistance = buildingCamera.currentDistance;
		}
		if (PartPainter.PaintModeEnabled || decalPlacer.IsPlacingDecal)
		{
			return;
		}
		if (decalPlacer.UpdateSelection())
		{
			if (currentMovingPart != null)
			{
				PlaceSelectedPart();
			}
			currentMovingPart = null;
			currentSelectedPart = null;
		}
		else
		{
			if (rotationGizmo.gameObject.activeInHierarchy || procduralPartController.gizmoManager.GizmoHover() || moveGizmo.gameObject.activeInHierarchy || !(currentMovingPart == null) || partWasPlacedThisFrame)
			{
				return;
			}
			Ray ray = BuildingCamera.cam.ScreenPointToRay(MouseInput.GetMousePosition());
			if (Singleton<MouseInput>.Instance.PointerIsOverUI || !Physics.Raycast(ray, out var hitInfo, 1000f, selectionMask))
			{
				return;
			}
			BuildingPart buildingPartComponent = GetBuildingPartComponent(hitInfo.transform.gameObject);
			if (!(buildingPartComponent == null))
			{
				if (buildingPartComponent.IsAttatched() && !decalPlacer.HoveringDecal())
				{
					highlightRenderer.HighlightBuildingPart(buildingPartComponent, outline: false, highlight: true, Color.black, 0.3f);
					highlightRenderer.HighlightBuildingPart(GetMirrorPart(buildingPartComponent), outline: false, highlight: true, Color.black, 0.3f);
				}
				if (buildingPartComponent != null && MouseInput.currentMouse.leftButton.wasPressedThisFrame)
				{
					Vector3 lhs = hitInfo.point - buildingCamera.transform.position;
					currentDistance = Mathf.Abs(Vector3.Dot(lhs, buildingCamera.transform.forward));
					currentRotation = buildingPartComponent.transform.rotation;
					positionOffset = buildingPartComponent.transform.position - GetMouseWorldPosition();
					SelectPart(buildingPartComponent);
					PlaySound(selectionSound);
					movementSinceClick = 0f;
					prefMousePos = MouseInput.GetMousePosition();
				}
			}
		}
	}

	private bool UpdatePlacement()
	{
		if (currentMovingPart == null)
		{
			return false;
		}
		rotationGizmo.gameObject.SetActive(value: false);
		moveGizmo.gameObject.SetActive(value: false);
		if (currentMirrorPart != null)
		{
			Vector3 position = ((currentMovingPart.collisionPoint != null) ? currentMovingPart.collisionPoint.transform.position : currentMovingPart.transform.position);
			float a = Mathf.Abs(symmetryPlane.DistanceFromPlane(position));
			float b = Mathf.Abs(symmetryPlane.DistanceFromPlane(currentMovingPart.transform.position));
			bool flag = Mathf.Min(a, b) > 0.025f;
			bool flag2 = currentSelectedPart.IsAttatched() && currentMirrorPart.IsAttatched();
			Vector3 objectCenter = CustomMath.GetObjectCenter(currentMovingPart.gameObject);
			Vector3 objectCenter2 = CustomMath.GetObjectCenter(currentMirrorPart.gameObject);
			bool flag3 = Vector3.Distance(objectCenter, objectCenter2) > 0.075f;
			currentMirrorPart.SetActive(flag & flag2 & flag3);
			if (currentMirrorPart != null)
			{
				currentMirrorPart.SetParent(MoveMirrorPart(currentMirrorPart, currentMovingPart));
			}
		}
		float num = (currentSelectedPart.hasBeenPlaced ? 0f : currentSelectedPart.GetPrice());
		float num2 = ((currentMirrorPart == null || currentMirrorPart.hasBeenPlaced || !currentMirrorPart.gameObject.activeInHierarchy) ? 0f : currentMirrorPart.GetPrice());
		float num3 = num + num2;
		if (!Singleton<MoneyManager>.Instance.HasEnoughMoney(num3))
		{
			highlightRenderer.HighlightBuildingPart(currentMirrorPart, outline: true, highlight: true, red, 0.4f);
			priceFeedbackManager.ShowNotEnoughMoneyTooltip();
		}
		if (MouseInput.currentMouse.leftButton.isPressed)
		{
			movementSinceClick += (prefMousePos - MouseInput.GetMousePosition()).magnitude;
			prefMousePos = MouseInput.GetMousePosition();
			if (movementSinceClick > 5f)
			{
				currentMovingPart.SetParent(MovePart(currentMovingPart));
				UpdateGizmos(currentMovingPart);
			}
		}
		if (MouseInput.currentMouse.leftButton.wasReleasedThisFrame)
		{
			if (num3 > 0f)
			{
				if (!currentMovingPart.hasBeenPlaced && !currentMovingPart.IsAttatched())
				{
					DestroySelectedPart();
					return false;
				}
				if (!Singleton<MoneyManager>.Instance.HasEnoughMoney(num3))
				{
					DestroyPart(currentMirrorPart);
					num3 -= num2;
					num2 = 0f;
					currentMirrorPart = null;
				}
				if (num > 0f)
				{
					float cargoVolume = currentMovingPart.GetCargoVolume();
					priceFeedbackManager.InstantiateParticle(0f - num, cargoVolume, currentMovingPart.transform.gameObject);
				}
				if (num2 > 0f)
				{
					float cargoVolume2 = currentMirrorPart.GetCargoVolume();
					priceFeedbackManager.InstantiateParticle(0f - num2, cargoVolume2, currentMirrorPart.transform.gameObject);
				}
				Singleton<MoneyManager>.Instance.ChangeMoneyAmount(0f - num3);
			}
			PlaceSelectedPart();
			if (movementSinceClick > 10f)
			{
				PlaySound(placementSound);
			}
			movementSinceClick = 0f;
			return true;
		}
		return false;
	}

	private GameObject MovePart(BuildingPart buildingPart)
	{
		if (buildingPart == null)
		{
			return null;
		}
		if (Input.GetKeyDown(KeyCode.R) && Singleton<InputManager>.Instance.currentInputBlocker == null)
		{
			RotatePart();
		}
		float z = (buildingPart.lockRotation ? 0f : rotationOffset);
		Vector3 position = GetMouseWorldPosition() + positionOffset;
		buildingPart.SetPositionAndRotation(position, currentRotation);
		GameObject gameObject = SolveSnapping(buildingPart, mouseSnapping: true, snapDistance);
		if (gameObject != null)
		{
			return gameObject;
		}
		bool flag = Physics.Raycast(BuildingCamera.cam.ScreenPointToRay(MouseInput.GetMousePosition()), out var hitInfo, 1000f, selectionMask);
		if (!flag && buildingPart.collisionPoint != null)
		{
			float num = 0.75f;
			Vector3 forward = buildingPart.collisionPoint.forward;
			Vector3 origin = buildingPart.collisionPoint.transform.position - forward * num;
			num += 0.25f;
			flag = Physics.Raycast(new Ray(origin, forward), out hitInfo, num, selectionMask);
		}
		if (flag)
		{
			BuildingPart buildingPartComponent = GetBuildingPartComponent(hitInfo.collider.gameObject);
			if (buildingPart.collisionPoint != null && buildingPartComponent.IsAttatched())
			{
				Vector3 lhs = buildingPartComponent.GetForwardOrientation();
				Vector3 planeForward = GetPlaneForward();
				if (Vector3.Dot(lhs, planeForward) < -0.99f && buildingPartComponent.symetrical)
				{
					lhs = planeForward;
				}
				Vector3 vector = Vector3.Cross(lhs, hitInfo.normal);
				float increment = 1f;
				Vector3 vector2 = new Vector3(CustomMath.SnapToIncrement(hitInfo.normal.x, increment), CustomMath.SnapToIncrement(hitInfo.normal.y, increment), CustomMath.SnapToIncrement(hitInfo.normal.z, increment));
				vector2.Normalize();
				if (buildingPart.ignoreSnappingOnTangentPlane)
				{
					Vector3 vector3 = Vector3.ProjectOnPlane(hitInfo.normal, vector);
					Vector3 to = Vector3.ProjectOnPlane(vector2, vector);
					vector2 = Quaternion.AngleAxis(0f - Vector3.SignedAngle(vector3, to, vector), vector) * vector2;
					vector = Vector3.Cross(lhs, vector2);
				}
				buildingPart.baseRotation = Quaternion.LookRotation(-vector2, vector) * Quaternion.Inverse(buildingPart.collisionPoint.localRotation);
				buildingPart.SetRotation(buildingPart.baseRotation * buildingPart.rotationOffset);
				buildingPart.SetPosition(hitInfo.point);
				Vector3 vector4 = Matrix4x4.Scale(buildingPart.transform.localScale) * buildingPart.collisionPoint.localPosition;
				buildingPart.SetPosition(buildingPart.transform.position - buildingPart.transform.rotation * vector4);
				float num2 = Mathf.Abs(symmetryPlane.DistanceFromPlane(buildingPart.transform.position));
				if (num2 < 0.075f)
				{
					Vector3 normalized = (symmetryPlane.GetMirroredPosition(buildingPart.transform.position) - buildingPart.transform.position).normalized;
					buildingPart.SetPosition(buildingPart.transform.position + normalized * num2);
				}
				if (buildingPart.flipSymmetry)
				{
					Vector3 lhs2 = Vector3.Cross(lhs, base.transform.up);
					Vector3 normalized2 = (buildingPartComponent.transform.position - buildingPart.transform.position).normalized;
					Vector3 localScale = buildingPart.transform.localScale;
					buildingPart.SetScale(new Vector3(localScale.x, Mathf.Abs(localScale.y) * Mathf.Sign(Vector3.Dot(lhs2, normalized2)), localScale.z));
				}
				return GetBuildingPartComponent(hitInfo.collider.gameObject).gameObject;
			}
		}
		buildingPart.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, z) * currentRotation);
		buildingPart.isBasePart = partContainer.GetBasePartCount() == 0 && buildingPart.basePart;
		return null;
	}

	private GameObject MoveMirrorPart(BuildingPart mirrorPart, BuildingPart originalPart)
	{
		mirrorPart.SetPosition(symmetryPlane.GetMirroredPosition(originalPart.transform.position));
		mirrorPart.SetRotation(symmetryPlane.GetMirroredRotation(originalPart.transform));
		mirrorPart.SetScale(symmetryPlane.GetMirroredScale(originalPart.transform.localScale));
		GameObject gameObject = SolveSnapping(mirrorPart, mouseSnapping: false, snapDistance * 0.33f);
		if (gameObject != null)
		{
			return gameObject;
		}
		if (mirrorPart.collisionPoint != null)
		{
			float num = 0.75f;
			Vector3 forward = mirrorPart.collisionPoint.forward;
			Vector3 origin = mirrorPart.collisionPoint.transform.position - forward * num;
			num += 0.25f;
			if (Physics.Raycast(new Ray(origin, forward), out var hitInfo, num, selectionMask))
			{
				return GetBuildingPartComponent(hitInfo.collider.gameObject).gameObject;
			}
		}
		Ray ray = BuildingCamera.cam.ScreenPointToRay(MouseInput.GetMousePosition());
		Vector3 mirroredPosition = symmetryPlane.GetMirroredPosition(ray.origin);
		Vector3 normalized = (symmetryPlane.GetMirroredPosition(ray.origin + ray.direction) - mirroredPosition).normalized;
		Ray ray2 = new Ray(mirroredPosition, normalized);
		Debug.DrawRay(ray2.origin, ray2.direction * 10f, Color.red);
		if (Physics.Raycast(ray2, out var hitInfo2, 1000f, selectionMask))
		{
			return GetBuildingPartComponent(hitInfo2.collider.gameObject).gameObject;
		}
		return null;
	}

	private GameObject SolveSnapping(BuildingPart part, bool mouseSnapping, float snapDistance)
	{
		if (!snapMode)
		{
			return null;
		}
		float num = float.MaxValue;
		Vector3 position = Vector3.zero;
		Transform snappingPoint = null;
		Quaternion quaternion = Quaternion.identity;
		for (int i = 0; i < partContainer.SnapPointCount(); i++)
		{
			for (int j = 0; j < part.snapPoints.Length; j++)
			{
				Transform snapPoint = partContainer.GetSnapPoint(i);
				bool flag = false;
				for (int k = 0; k < part.children.Count; k++)
				{
					for (int l = 0; l < part.children[k].snapPoints.Length; l++)
					{
						if (Vector3.Distance(part.snapPoints[j].position, part.children[k].snapPoints[l].position) < 0.05f)
						{
							flag = true;
						}
					}
				}
				if (!((partContainer.SnapPointAttatched(snapPoint) || !partContainer.SnappingPointToPart(snapPoint).IsAttatched()) | flag))
				{
					float a = CustomMath.DistanceToLine(BuildingCamera.cam.ScreenPointToRay(MouseInput.GetMousePosition()), snapPoint.position);
					float num2 = Vector3.Distance(snapPoint.transform.position, part.snapPoints[j].transform.position);
					float num3 = num2;
					if (mouseSnapping)
					{
						num3 = Mathf.Min(a, num2);
					}
					if (num3 < num)
					{
						Vector3 toDirection = -snapPoint.forward;
						quaternion = Quaternion.FromToRotation(part.snapPoints[j].forward, toDirection);
						position = snapPoint.position;
						Vector3 vector = part.snapPoints[j].position - part.transform.position;
						Vector3 vector2 = quaternion * vector;
						position -= vector2;
						snappingPoint = snapPoint;
						num = num3;
					}
				}
			}
		}
		if (num < snapDistance)
		{
			part.SetRotation(quaternion * part.transform.rotation);
			BuildingPart buildingPart = partContainer.SnappingPointToPart(snappingPoint);
			if (part.symetrical && buildingPart.symetrical && Vector3.Dot(buildingPart.GetForwardOrientation(), part.GetForwardOrientation()) < -0.99f)
			{
				part.FlipOrientation();
			}
			part.SetPosition(position);
			if (part.tryOrientUpwards)
			{
				part.SetRotation(Quaternion.LookRotation(part.transform.forward, Vector3.up));
			}
			if (part.tryMatchParentRotation)
			{
				part.SetRotation(Quaternion.LookRotation(part.transform.forward, buildingPart.transform.up));
			}
			return buildingPart.gameObject;
		}
		return null;
	}

	public BuildingPart GetMirrorPart(BuildingPart part)
	{
		if (!mirrorMode || part == null)
		{
			return null;
		}
		return partContainer.GetMirrorPart(part, this);
	}

	private BuildingPart CreateMirrorPart(BuildingPart part)
	{
		if (!mirrorMode)
		{
			return null;
		}
		BuildingPart basePart = partContainer.GetBasePart();
		if (basePart != null && basePart != part)
		{
			BuildingPart buildingPart = part.MakeMirrorCopy(this);
			SelectPart(buildingPart, child: true);
			buildingPart.SetActive(value: false);
			return buildingPart;
		}
		return null;
	}

	public static BuildingPart GetBuildingPartComponent(GameObject obj)
	{
		if (obj == null)
		{
			return null;
		}
		BuildingPart buildingPart = obj.GetComponent<BuildingPart>();
		Transform parent = obj.transform;
		while (buildingPart == null && parent.parent != null)
		{
			parent = parent.parent;
			buildingPart = parent.gameObject.GetComponent<BuildingPart>();
		}
		if (buildingPart == null)
		{
			buildingPart = obj.GetComponentInChildren<BuildingPart>();
		}
		return buildingPart;
	}

	private void UpdateMeshReadablityWarning(PlaneContainer container)
	{
		MeshCollider[] componentsInChildren = container.GetComponentsInChildren<MeshCollider>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			BuildingPart buildingPartComponent = GetBuildingPartComponent(componentsInChildren[i].gameObject);
			if (componentsInChildren[i].sharedMesh != null && !componentsInChildren[i].sharedMesh.isReadable && buildingPartComponent != null)
			{
				Debug.LogError("Mesh read/write option not set correctly. Mesh name: " + componentsInChildren[i].gameObject.name);
			}
		}
	}

	private Vector3 GetMouseWorldPosition()
	{
		Camera cam = BuildingCamera.cam;
		Vector3 mousePosition = MouseInput.GetMousePosition();
		return cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, currentDistance));
	}

	private void UpdateGizmos(BuildingPart part)
	{
		rotationGizmo.transform.position = part.transform.position;
		rotationGizmo.transform.rotation = part.transform.rotation;
		moveGizmo.transform.position = part.transform.position;
		moveGizmo.transform.rotation = part.transform.rotation;
	}

	private void PlaySound(AudioDef audioDef)
	{
		if (audioManager != null && audioDef != null)
		{
			audioManager.PlaySound(audioDef);
		}
	}

	public void RotatePart()
	{
		if (currentSelectedPart != null)
		{
			rotationGizmo.gameObject.SetActive(value: true);
			rotationGizmo.SetElementEnabled(x: true, y: true, z: true);
			rotationGizmo.transform.position = currentSelectedPart.transform.position;
		}
	}

	public void MovePart()
	{
		if (currentSelectedPart != null)
		{
			moveGizmo.gameObject.SetActive(value: true);
			moveGizmo.SetElementEnabled(x: true, y: true, z: true);
			moveGizmo.transform.position = currentSelectedPart.transform.position;
		}
	}

	public bool GizmoActive()
	{
		if (!rotationGizmo.gameObject.activeInHierarchy)
		{
			return moveGizmo.gameObject.activeInHierarchy;
		}
		return true;
	}

	private Vector3 GetPlaneForward()
	{
		return partContainer.GetBasePart().transform.right;
	}
}

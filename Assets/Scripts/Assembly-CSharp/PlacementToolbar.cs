using System;
using UnityEngine;

public class PlacementToolbar : MonoBehaviour
{
	public GameObject[] proceduralFuselageButtons;

	public GameObject settingsButton;

	public GameObject fuselageMove;

	public GameObject rotateButton;

	public GameObject moveButton;

	public GameObject scaleButton;

	public GameObject keybindButton;

	public GameObject[] decalButtons;

	public AnimationCurve openCurve;

	public PartSettingsPanel partSettings;

	public GameObject settingDevider;

	public GameObject miscDevider;

	private int lastEnableCallFrame;

	private GameObject targetObject;

	private RectTransform[] rectTransforms;

	private float currentScale;

	private bool isEnabled;

	public event Action rotationClicked;

	public event Action moveClicked;

	public event Action deleteClicked;

	public event Action scaleClicked;

	private void Awake()
	{
		rectTransforms = new RectTransform[base.transform.childCount];
		for (int i = 0; i < base.transform.childCount; i++)
		{
			rectTransforms[i] = (RectTransform)base.transform.GetChild(i).transform;
		}
	}

	public void EnableToolbar(GameObject targetObject)
	{
		lastEnableCallFrame = Time.frameCount;
		base.gameObject.SetActive(value: true);
		this.targetObject = targetObject;
	}

	public void UpdateButtons(int fuselage, bool hasSettings, bool hasKeybinds, bool rotateable, bool moveable, bool isDecal)
	{
		fuselageMove.gameObject.SetActive(fuselage > 1);
		for (int i = 0; i < proceduralFuselageButtons.Length; i++)
		{
			proceduralFuselageButtons[i].gameObject.SetActive(fuselage > 0);
		}
		for (int j = 0; j < decalButtons.Length; j++)
		{
			decalButtons[j].gameObject.SetActive(isDecal);
		}
		rotateButton.gameObject.SetActive(rotateable);
		moveButton.gameObject.SetActive(moveable);
		settingsButton.gameObject.SetActive(hasSettings);
		keybindButton.gameObject.SetActive(hasKeybinds);
		settingDevider.gameObject.SetActive(hasSettings | hasKeybinds);
		scaleButton.gameObject.SetActive(isDecal);
		miscDevider.gameObject.SetActive(isDecal || fuselage > 0);
		float num = 0f;
		int num2 = 0;
		for (int k = 0; k < rectTransforms.Length; k++)
		{
			if (rectTransforms[k].gameObject.activeInHierarchy)
			{
				num2++;
				num += rectTransforms[k].sizeDelta.x;
			}
		}
		((RectTransform)base.transform).sizeDelta = new Vector2(num + (float)(3 * num2), ((RectTransform)base.transform).sizeDelta.y);
		Update();
	}

	public void ClickRotation()
	{
		if (rotationClicked != null)
		{
			rotationClicked();
		}
	}

	public void ClickScale()
	{
		if (scaleClicked != null)
		{
			scaleClicked();
		}
	}

	public void ClickMove()
	{
		if (moveClicked != null)
		{
			moveClicked();
		}
	}

	public void ClickDelete()
	{
		if (deleteClicked != null)
		{
			deleteClicked();
		}
	}

	private void Update()
	{
		isEnabled = lastEnableCallFrame > Time.frameCount - 1;
		if (targetObject != null)
		{
			Vector3 position = CustomMath.GetObjectCenter(targetObject) + Vector3.up;
			base.transform.position = BuildingCamera.cam.WorldToScreenPoint(position);
		}
	}

	private void LateUpdate()
	{
		if (isEnabled && !partSettings.gameObject.activeInHierarchy)
		{
			currentScale += Time.deltaTime * 15f;
		}
		else
		{
			currentScale -= Time.deltaTime * 15f;
		}
		currentScale = Mathf.Clamp01(currentScale);
		float num = openCurve.Evaluate(currentScale);
		base.transform.localScale = Vector3.one * num;
		if (base.transform.localScale.x < 0.05f && !isEnabled)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}

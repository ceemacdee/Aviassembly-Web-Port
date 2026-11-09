using UnityEngine;

public class MenuModalManager : MonoBehaviour
{
	public CanvasGroup title;

	public CanvasGroup background;

	private float targetTitleAlpha;

	private float titleAlphaVelocity;

	private MenuDialog menuDialog;

	private GameObject currentOpenModal;

	private void Start()
	{
		targetTitleAlpha = 1f;
		menuDialog = Singleton<MenuDialog>.Instance;
	}

	public void OpenMenu(GameObject menu)
	{
		menu.SetActive(value: true);
	}

	public void CloseMenu(GameObject menu)
	{
		menu.SetActive(value: false);
	}

	public void OpenModal(GameObject modal)
	{
		currentOpenModal = modal;
		targetTitleAlpha = 0f;
		modal.SetActive(value: true);
	}

	public void CloseModal(GameObject modal)
	{
		currentOpenModal = null;
		targetTitleAlpha = 1f;
		modal.SetActive(value: false);
	}

	public void OpenSettings()
	{
		OpenModal(menuDialog.settingsPanel);
	}

	private void Update()
	{
		if (currentOpenModal != null && !currentOpenModal.activeInHierarchy)
		{
			CloseModal(currentOpenModal);
		}
		float smoothTime = ((targetTitleAlpha > title.alpha) ? 0.05f : 0.1f);
		title.alpha = Mathf.SmoothDamp(title.alpha, targetTitleAlpha, ref titleAlphaVelocity, smoothTime);
		background.alpha = 1f - title.alpha;
	}
}

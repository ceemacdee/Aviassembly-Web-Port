using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonAudio : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public AudioDef clickSound;

	public AudioDef hoverSound;

	public bool onRelease;

	private bool pointerInButton;

	private bool hoverSoundPlayed;

	private float t;

	private void Start()
	{
		GetComponent<Button>().onClick.AddListener(() =>
		{
			PlaySound();
		});
	}

	private void PlaySound()
	{
		Singleton<AudioManager>.Instance.PlaySound(clickSound);
	}

	private void Update()
	{
		if (!pointerInButton || hoverSoundPlayed)
		{
			return;
		}
		t += Time.deltaTime;
		if (t > 0.02f)
		{
			if (hoverSound != null)
			{
				Singleton<AudioManager>.Instance.PlaySound(hoverSound);
			}
			hoverSoundPlayed = true;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		pointerInButton = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		pointerInButton = false;
		hoverSoundPlayed = false;
		t = 0f;
	}
}

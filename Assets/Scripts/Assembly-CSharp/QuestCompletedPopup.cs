using System.Collections;
using TMPro;
using UnityEngine;

public class QuestCompletedPopup : MonoBehaviour
{
	public GameObject completedObject;

	public GameObject rewardCounterObject;

	public RectTransform rewardPopup;

	public AudioClip clickSound;

	public AudioDef rewaredSound;

	public AudioSource source;

	public TMP_Text moneyText;

	public TMP_Text rewardText;

	public TMP_Text advancedScrapText;

	private int currentMoney;

	private int currentScrap;

	private int currentAdvancedScrap;

	private int targetMoney;

	private int targetScrap;

	private int targetAdvancedScrap;

	private bool questCompleted;

	private float waitTime;

	private float t;

	private void Start()
	{
		base.gameObject.SetActive(value: false);
		source.volume = 0.3f;
	}

	private void Update()
	{
		RectTransform rectTransform = (RectTransform)base.transform;
		rectTransform.sizeDelta = new Vector2(Mathf.Max(rewardPopup.sizeDelta.x + 40f, 380f), rectTransform.sizeDelta.y);
	}

	public void Trigger(int prevMoney, int prevScrap, int prevAdvancedScrap, int targetMoney, int targetScrap, int targetAdvancedScrap, bool questCompleted)
	{
		currentMoney = prevMoney;
		currentScrap = prevScrap;
		currentAdvancedScrap = prevAdvancedScrap;
		this.targetMoney = targetMoney;
		this.targetScrap = targetScrap;
		this.targetAdvancedScrap = targetAdvancedScrap;
		base.gameObject.SetActive(value: true);
		completedObject.gameObject.SetActive(value: true);
		rewardCounterObject.gameObject.SetActive(value: false);
		rewardText.text = currentScrap.ToString();
		moneyText.text = currentMoney.ToString();
		advancedScrapText.text = currentAdvancedScrap.ToString();
		float num = targetMoney - prevMoney;
		float num2 = targetScrap - prevScrap;
		float num3 = targetAdvancedScrap - prevAdvancedScrap;
		rewardText.transform.parent.gameObject.SetActive(num2 > 0f);
		moneyText.transform.parent.gameObject.SetActive(num > 0f);
		advancedScrapText.transform.parent.gameObject.SetActive(num3 > 0f);
		waitTime = Mathf.Min(0.05f, 4f / num);
		StartCoroutine(Sequence());
	}

	private IEnumerator Sequence()
	{
		if (questCompleted)
		{
			yield return new WaitForSeconds(0.6f);
		}
		completedObject.gameObject.SetActive(value: false);
		rewardCounterObject.gameObject.SetActive(value: true);
		t = 0f;
		int delta = targetMoney - currentMoney;
		int itteration = 0;
		yield return new WaitForSeconds(0.2f);
		while (currentMoney < targetMoney)
		{
			t += Time.deltaTime;
			if (t > waitTime)
			{
				source.pitch = Mathf.Lerp(0.75f, 1.25f, (float)itteration / (float)delta);
				source.pitch += Random.Range(0.05f, -0.05f);
				source.volume = Random.Range(0.2f, 0.25f);
				source.PlayOneShot(clickSound);
			}
			while (t > waitTime)
			{
				currentMoney++;
				t -= waitTime;
				itteration++;
			}
			moneyText.text = currentMoney.ToString();
			yield return null;
		}
		while (currentScrap < targetScrap)
		{
			currentScrap++;
			rewardText.text = currentScrap.ToString();
			source.pitch = Random.Range(0.9f, 1.1f);
			source.volume = Random.Range(0.2f, 0.25f);
			source.PlayOneShot(clickSound);
			yield return new WaitForSeconds(0.1f);
		}
		while (currentAdvancedScrap < targetAdvancedScrap)
		{
			currentAdvancedScrap++;
			advancedScrapText.text = currentAdvancedScrap.ToString();
			source.pitch = Random.Range(0.9f, 1.1f);
			source.volume = Random.Range(0.2f, 0.25f);
			source.PlayOneShot(clickSound);
			yield return new WaitForSeconds(0.1f);
		}
		Singleton<AudioManager>.Instance.PlaySound(rewaredSound);
		while (rewardCounterObject.transform.localScale.x < 1.1f)
		{
			rewardCounterObject.transform.localScale += Vector3.one * Time.deltaTime * 2f;
			yield return null;
		}
		while (rewardCounterObject.transform.localScale.x > 1f)
		{
			rewardCounterObject.transform.localScale -= Vector3.one * Time.deltaTime * 2f;
			yield return null;
		}
		rewardCounterObject.transform.localScale = Vector3.one;
		yield return new WaitForSeconds(1.5f);
		base.gameObject.SetActive(value: false);
	}
}

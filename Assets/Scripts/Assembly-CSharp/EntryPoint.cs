using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EntryPoint : MonoBehaviour
{
	private void Start()
	{
		StartCoroutine(StartGame());
	}

	private IEnumerator StartGame()
	{
		yield return null;
		SceneManager.LoadScene("Persistent", LoadSceneMode.Additive);
	}
}

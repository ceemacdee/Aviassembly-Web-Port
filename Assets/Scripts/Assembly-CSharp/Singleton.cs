using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static bool m_ShuttingDown = false;

	private static object m_Lock = new object();

	private static T m_Instance;

	public static T Instance
	{
		get
		{
			if (m_ShuttingDown)
			{
				return null;
			}
			lock (m_Lock)
			{
				if (m_Instance == null)
				{
					m_Instance = (T)Object.FindFirstObjectByType(typeof(T));
				}
				return m_Instance;
			}
		}
	}

	protected virtual void Awake()
	{
		if (m_Instance == null)
		{
			m_Instance = this as T;
		}
	}

	public static void InitSingleton()
	{
		if (m_Instance == null)
		{
			m_Instance = (T)Object.FindFirstObjectByType(typeof(T));
		}
	}

	private void OnApplicationQuit()
	{
		m_ShuttingDown = true;
	}
}

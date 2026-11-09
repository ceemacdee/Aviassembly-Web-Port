using UnityEngine;

public class MovingObject : MonoBehaviour
{
	private void Awake()
	{
		Singleton<CustomFixedUpdates>.Instance.movingObjects.Add(this);
	}

	private void OnDestroy()
	{
		if (Singleton<CustomFixedUpdates>.Instance != null)
		{
			Singleton<CustomFixedUpdates>.Instance.movingObjects.Remove(this);
		}
	}

	public virtual void PhysicsSyncUpdate(float deltaTime)
	{
	}
}

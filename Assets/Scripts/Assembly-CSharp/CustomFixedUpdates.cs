using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomFixedUpdates : Singleton<CustomFixedUpdates>
{
	private int fixedUpdates;

	public float physicsSyncedDeltaTime;

	public List<MovingObject> movingObjects = new List<MovingObject>();

	protected override void Awake()
	{
		base.Awake();
		StartCoroutine(LateFixedUpdateManager());
	}

	private void LateUpdate()
	{
		physicsSyncedDeltaTime = (float)fixedUpdates * Time.fixedDeltaTime;
		fixedUpdates = 0;
		for (int i = 0; i < movingObjects.Count; i++)
		{
			if (movingObjects[i].gameObject.activeInHierarchy)
			{
				movingObjects[i].PhysicsSyncUpdate(physicsSyncedDeltaTime);
			}
		}
	}

	private IEnumerator LateFixedUpdateManager()
	{
		while (true)
		{
			fixedUpdates++;
			yield return new WaitForFixedUpdate();
		}
	}
}

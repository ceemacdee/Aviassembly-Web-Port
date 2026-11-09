using UnityEngine;

public class CameraModeToggle : MonoBehaviour
{
	public CameraController cameraController;

	private CameraPart[] cameraParts;

	private int cameraIndex;

	private void Start()
	{
		cameraParts = Singleton<PlaneContainer>.Instance.GetComponentsInChildren<CameraPart>();
	}

	public void Click()
	{
		int cameraMode = (int)cameraController.cameraMode;
		if (cameraMode == 2 && IncrementCamera())
		{
			return;
		}
		cameraMode--;
		if (cameraMode < 0)
		{
			cameraMode = 2;
			cameraIndex = 0;
		}
		if (cameraMode == 2)
		{
			if (IncrementCamera())
			{
				return;
			}
			cameraMode = 1;
		}
		cameraController.SetCameraMode(cameraMode);
	}

	private bool IncrementCamera()
	{
		if (cameraIndex < cameraParts.Length)
		{
			cameraController.SetCameraMode(2, cameraParts[cameraIndex].transform);
			cameraIndex++;
			return true;
		}
		return false;
	}
}

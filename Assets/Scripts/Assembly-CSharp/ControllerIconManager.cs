using UnityEngine;

public class ControllerIconManager : Singleton<ControllerIconManager>
{
	public ControllerIcon[] controllerIcons;

	public Texture GetIcon(string keyPath)
	{
		for (int i = 0; i < controllerIcons.Length; i++)
		{
			if (controllerIcons[i].keyPath == keyPath)
			{
				return controllerIcons[i].icon;
			}
		}
		return null;
	}
}

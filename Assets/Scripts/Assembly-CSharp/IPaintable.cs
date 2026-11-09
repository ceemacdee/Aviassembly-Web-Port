using UnityEngine;

public interface IPaintable
{
	void SetColor(Color color, bool apply);

	void ResetColor();

	Color GetCurrentColor();
}

using UnityEngine;
using UnityEngine.UI;

public class DecalButton : MonoBehaviour
{
	public Image background;

	public Color normalColor;

	public Color selectedColor;

	public RawImage shadow;

	public RawImage image;

	public DecalPlacer decalPlacer;

	private Texture2D tex;

	public void Init(Texture2D texture, DecalPlacer placer)
	{
		tex = texture;
		shadow.texture = texture;
		image.texture = texture;
		decalPlacer = placer;
	}

	private void Update()
	{
		if (tex == decalPlacer.currentDecalTexture)
		{
			background.color = selectedColor;
		}
		else
		{
			background.color = normalColor;
		}
	}

	public void PressButton()
	{
		decalPlacer.SelectTexture(tex);
	}
}

using UnityEngine;
using UnityEngine.UI;

public class ColorPickerUI : MonoBehaviour
{
	public RawImage saturationValuePicker;

	public RawImage huePicker;

	public Image colorPreview;

	public RawImage pipetIcon;

	public Image customColorBackground;

	public RectTransform pickIconHue;

	public RectTransform pickIconSV;

	private float hue;

	private float saturation;

	private float value;

	private Texture2D huePickerTexture;

	private Texture2D saturationValuePickerTexture;

	private Color[] saturationValueColors;

	private const int textureSize = 64;

	private bool hueSelectorActive;

	private bool svSelectorActive;

	private void Start()
	{
		huePickerTexture = new Texture2D(64, 64);
		for (int i = 0; i < huePickerTexture.width; i++)
		{
			for (int j = 0; j < huePickerTexture.height; j++)
			{
				huePickerTexture.SetPixel(i, j, Color.HSVToRGB((float)j / (float)huePickerTexture.height, 1f, 1f));
			}
		}
		huePickerTexture.Apply();
		huePicker.texture = huePickerTexture;
		saturationValueColors = new Color[4096];
		Color.RGBToHSV(new Color(0.75f, 0.5f, 0.2f), out hue, out saturation, out value);
		pickIconHue.position = GetWorldPosition(new Vector2(0.5f, hue), huePicker.rectTransform);
		pickIconSV.position = GetWorldPosition(new Vector2(saturation, value), saturationValuePicker.rectTransform);
	}

	public Color GetColor()
	{
		Color result = Color.HSVToRGB(hue, saturation, value);
		result.r = Mathf.Clamp(result.r, 0.15f, 0.95f);
		result.g = Mathf.Clamp(result.g, 0.15f, 0.95f);
		result.b = Mathf.Clamp(result.b, 0.15f, 0.95f);
		return result;
	}

	public void SetColor(Color color)
	{
		Color.RGBToHSV(color, out hue, out saturation, out value);
		pickIconHue.position = GetWorldPosition(new Vector2(0.5f, hue), huePicker.rectTransform);
		pickIconSV.position = GetWorldPosition(new Vector2(saturation, value), saturationValuePicker.rectTransform);
	}

	private void Update()
	{
		if (MouseInRect(huePicker.rectTransform) && Input.GetMouseButtonDown(0))
		{
			hueSelectorActive = true;
		}
		if (MouseInRect(saturationValuePicker.rectTransform) && Input.GetMouseButtonDown(0))
		{
			svSelectorActive = true;
		}
		if (Input.GetMouseButtonUp(0))
		{
			hueSelectorActive = false;
			svSelectorActive = false;
		}
		if (hueSelectorActive)
		{
			Vector2 positionInRect = GetPositionInRect(huePicker.rectTransform);
			hue = positionInRect.y;
			positionInRect.x = 0.5f;
			pickIconHue.position = GetWorldPosition(positionInRect, huePicker.rectTransform);
		}
		if (svSelectorActive)
		{
			Vector2 positionInRect2 = GetPositionInRect(saturationValuePicker.rectTransform);
			saturation = positionInRect2.x;
			value = positionInRect2.y;
			pickIconSV.position = GetWorldPosition(positionInRect2, saturationValuePicker.rectTransform);
		}
		Object.Destroy(saturationValuePickerTexture);
		saturationValuePickerTexture = new Texture2D(64, 64);
		saturationValuePickerTexture.SetPixels(GenerateSVMap(64, 64, hue));
		saturationValuePickerTexture.Apply();
		saturationValuePicker.texture = saturationValuePickerTexture;
		colorPreview.color = GetColor();
		customColorBackground.color = colorPreview.color;
		float num = colorPreview.color.r + colorPreview.color.g + colorPreview.color.b;
		pipetIcon.color = ((num > 1.5f) ? Color.black : Color.white);
	}

	private Vector2 GetPositionInRect(RectTransform rectTransform)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out var localPoint);
		Rect rect = rectTransform.rect;
		float num = (localPoint.x - rect.x) / rect.width;
		return new Vector2(y: Mathf.Clamp01((localPoint.y - rect.y) / rect.height), x: Mathf.Clamp01(num));
	}

	private bool MouseInRect(RectTransform rectTransform)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out var localPoint);
		return rectTransform.rect.Contains(localPoint);
	}

	private Vector3 GetWorldPosition(Vector3 normalizedPosition, RectTransform rectTransform)
	{
		Rect rect = rectTransform.rect;
		Vector2 vector = new Vector2(rect.x + normalizedPosition.x * rect.width, rect.y + normalizedPosition.y * rect.height);
		Vector3 worldPoint = rectTransform.TransformPoint(vector);
		return RectTransformUtility.WorldToScreenPoint(null, worldPoint);
	}

	private Color[] GenerateSVMap(int width, int height, float hue)
	{
		hue = Mathf.Repeat(hue, 1f);
		float num = hue * 6f;
		int num2 = (int)num;
		float num3 = num - (float)num2;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		switch (num2)
		{
		case 0:
			num4 = 1f;
			num5 = num3;
			num6 = 0f;
			break;
		case 1:
			num4 = 1f - num3;
			num5 = 1f;
			num6 = 0f;
			break;
		case 2:
			num4 = 0f;
			num5 = 1f;
			num6 = num3;
			break;
		case 3:
			num4 = 0f;
			num5 = 1f - num3;
			num6 = 1f;
			break;
		case 4:
			num4 = num3;
			num5 = 0f;
			num6 = 1f;
			break;
		case 5:
			num4 = 1f;
			num5 = 0f;
			num6 = 1f - num3;
			break;
		}
		for (int i = 0; i < height; i++)
		{
			float num7 = (float)i / (float)(height - 1);
			for (int j = 0; j < width; j++)
			{
				float num8 = (float)j / (float)(width - 1);
				float num9 = num7 * num8;
				float num10 = num7 - num9;
				float r = num4 * num9 + num10;
				float g = num5 * num9 + num10;
				float b = num6 * num9 + num10;
				saturationValueColors[i * width + j] = new Color(r, g, b, 1f);
			}
		}
		return saturationValueColors;
	}
}

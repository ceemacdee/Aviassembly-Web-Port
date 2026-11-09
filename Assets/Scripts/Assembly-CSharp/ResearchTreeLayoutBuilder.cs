using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResearchTreeLayoutBuilder : MonoBehaviour
{
	public ResearchTree researchTree;

	public GameObject nodePrefab;

	public GameObject linePrefab;

	public float nodeSize;

	public float spacing;

	public float lineThickness;

	private List<GameObject> nodes = new List<GameObject>();

	private Dictionary<ResearchTreeItem, ResearchButton> buttons = new Dictionary<ResearchTreeItem, ResearchButton>();

	private void Start()
	{
		for (int i = 0; i < nodes.Count; i++)
		{
			Object.Destroy(nodes[i]);
		}
		nodes.Clear();
		BuildLayout();
	}

	public void BuildLayout()
	{
		((RectTransform)base.transform).sizeDelta = GetSize();
		for (int i = 0; i < researchTree.items.Count; i++)
		{
			GameObject gameObject = Object.Instantiate(nodePrefab);
			ResearchButton component = gameObject.GetComponent<ResearchButton>();
			component.prefab = researchTree.items[i].Prefab;
			buttons.Add(researchTree.items[i], component);
			gameObject.transform.parent = base.transform;
			gameObject.transform.localScale = Vector3.one;
			gameObject.transform.rotation = Quaternion.identity;
			((RectTransform)gameObject.transform).anchoredPosition = GetNodePosition(researchTree.items[i].window.position);
			nodes.Add(gameObject);
			List<ResearchTreeItem> parents = GetParents(researchTree.items[i]);
			for (int j = 0; j < parents.Count; j++)
			{
				component.connectionLines.Add(ConnectNodes(researchTree.items[i], parents[j]));
			}
		}
		for (int k = 0; k < researchTree.items.Count; k++)
		{
			ResearchButton researchButton = buttons[researchTree.items[k]];
			List<ResearchTreeItem> parents2 = GetParents(researchTree.items[k]);
			if (parents2.Count > 0)
			{
				for (int l = 0; l < parents2.Count; l++)
				{
					researchButton.parents.Add(buttons[parents2[l]]);
				}
			}
		}
	}

	private List<ResearchTreeItem> GetParents(ResearchTreeItem item)
	{
		List<ResearchTreeItem> list = new List<ResearchTreeItem>();
		for (int i = 0; i < researchTree.items.Count; i++)
		{
			if (researchTree.items[i].children.Contains(item))
			{
				list.Add(researchTree.items[i]);
			}
		}
		return list;
	}

	private RawImage ConnectNodes(ResearchTreeItem item1, ResearchTreeItem item2)
	{
		GameObject gameObject = Object.Instantiate(linePrefab);
		gameObject.transform.parent = base.transform;
		gameObject.transform.SetAsFirstSibling();
		gameObject.transform.localScale = Vector3.one;
		nodes.Add(gameObject);
		Vector3 nodePosition = GetNodePosition(item1.window.position);
		Vector3 nodePosition2 = GetNodePosition(item2.window.position);
		Vector3 vector = Vector3.Lerp(nodePosition, nodePosition2, 0.5f);
		((RectTransform)gameObject.transform).anchoredPosition = vector;
		gameObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, (vector - nodePosition).normalized);
		((RectTransform)gameObject.transform).sizeDelta = new Vector2(lineThickness, Vector3.Distance(nodePosition, nodePosition2));
		return gameObject.GetComponent<RawImage>();
	}

	private Vector3 GetNodePosition(Vector2 graphPosition)
	{
		Vector2Int vector2Int = new Vector2Int(Mathf.FloorToInt(graphPosition.x / 200f), Mathf.FloorToInt(graphPosition.y / 200f));
		return new Vector3((float)vector2Int.x * nodeSize + (float)vector2Int.x * spacing, (float)(-vector2Int.y) * nodeSize - (float)vector2Int.y * spacing, 0f) + new Vector3((0f - ((RectTransform)base.transform).rect.width) * 0.5f, ((RectTransform)base.transform).rect.height * 0.5f) + new Vector3(nodeSize * 0.5f, (0f - nodeSize) * 0.5f, 0f) + new Vector3(spacing * 0.5f, (0f - spacing) * 0.5f, 0f);
	}

	private Vector2 GetSize()
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < researchTree.items.Count; i++)
		{
			Vector2Int vector2Int = new Vector2Int(Mathf.FloorToInt(researchTree.items[i].window.position.x / 200f), Mathf.FloorToInt(researchTree.items[i].window.position.y / 200f));
			if ((float)vector2Int.x > num)
			{
				num = vector2Int.x;
			}
			if ((float)vector2Int.y > num2)
			{
				num2 = vector2Int.y;
			}
		}
		return new Vector2(num * nodeSize + num * spacing + nodeSize, num2 * nodeSize + num2 * spacing + nodeSize) + new Vector2(spacing, spacing);
	}
}

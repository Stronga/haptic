using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GridCellSize : MonoBehaviour {
	public GridLayoutGroup grid;
	public RectTransform targetSize, scrollViewRect, verticalLayoutRect;
	public void UpdateGrid(bool noCategories = true)
	{
		int _projectCount = transform.childCount-1; // don't count prefab
		Vector2 rectSize = new Vector2(targetSize.rect.width, targetSize.rect.height);
		Debug.Log("RECT SIZE " + rectSize);
		float cellWidth, cellHeight;
		if (noCategories)
		{
			Debug.Log("PROJECTS IN GRID: " + _projectCount);
			switch (_projectCount)
			{
				case 2:
					cellWidth = (rectSize.x - (grid.spacing.x * 1)) / 2f * 0.8f;
					cellHeight = cellWidth / 68.0f * 47.0f;
					grid.cellSize = new Vector2(cellWidth, cellHeight);
					grid.constraintCount = 2;
					grid.childAlignment = TextAnchor.MiddleCenter;
					break;
				case 3:
					cellWidth = (rectSize.x - (grid.spacing.x * 2)) / 3f;
					cellHeight = (rectSize.y - (grid.spacing.y)) / 2f;
					grid.cellSize = new Vector2(cellWidth, cellHeight);
					grid.constraintCount = 3;
					grid.childAlignment = TextAnchor.MiddleCenter;
					break;
				case 4:
					cellWidth = (rectSize.x - (grid.spacing.x * 2)) / 3f;
					cellHeight = (rectSize.y - (grid.spacing.y)) / 2f;
					grid.cellSize = new Vector2(cellWidth, cellHeight);
					grid.constraintCount = 2;
					grid.childAlignment = TextAnchor.MiddleCenter;
					break;
				case 5:
					cellWidth = (rectSize.x - (grid.spacing.x * 2)) / 3f;
					cellHeight = (rectSize.y - (grid.spacing.y)) / 2f;
					grid.cellSize = new Vector2(cellWidth, cellHeight);
					grid.constraintCount = 3;
					grid.childAlignment = TextAnchor.MiddleCenter;
					break;
				default:
					Debug.Log("One project");
					cellWidth = (rectSize.x - (grid.spacing.x * 2)) / 3f;
					cellHeight = (rectSize.y - (grid.spacing.y)) / 2f;
					grid.cellSize = new Vector2(cellWidth, cellHeight);
					grid.constraintCount = 3;
					grid.childAlignment = TextAnchor.UpperCenter;
					break;
			}
		}
		else
		{
			//cellWidth = (rectSize.x - (grid.spacing.x * 2)) / 3f;
			//cellHeight = (rectSize.y - (grid.spacing.y)) / 2f;
			cellWidth = (scrollViewRect.rect.width - (0.02f * 2f)) / 3f;
			cellHeight = scrollViewRect.rect.height;
			grid.cellSize = new Vector2(cellWidth, cellHeight);
			grid.constraintCount = 3;
			grid.childAlignment = TextAnchor.UpperLeft;
			verticalLayoutRect.anchoredPosition += new Vector2(0.17f, 0);
		}
	}
}

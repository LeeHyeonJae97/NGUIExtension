using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All children added to the game object with this script will be repositioned to be on a grid of specified dimensions.
/// And the child game object (cell) is reused to show vast amount of data.
/// If you want the cells to automatically set their scale based on the dimensions of their content, take a look at UITable.
/// </summary>

public class UIReuseGrid : MonoBehaviour
{
	/// <summary>
	/// Which way the new lines will be added horizontally.
	/// </summary>

	public enum DirectionHorizontal
	{
		Left,
		Right,
	}

	/// <summary>
	/// Which way the new lines will be added vertically.
	/// </summary>

	public enum DirectionVertical
	{
		Up,
		Down,
	}

	/// <summary>
	/// How many columns there will be before a new line is started. (When scroll moves horizontally, it means the count of row.)
	/// </summary>

	[Min(1)]
	public int columns = 0;

	/// <summary>
	/// Which way the new lines will be added horizontally.
	/// </summary>

	public DirectionHorizontal directionHorizontal = DirectionHorizontal.Right;

	/// <summary>
	/// Which way the new lines will be added vertically.
	/// </summary>

	public DirectionVertical directionVertical = DirectionVertical.Down;

	/// <summary>
	/// Size of cell, in pixels.
	/// </summary>

	public Vector2 size = Vector2.zero;

	/// <summary>
	/// Space between cells, in pixels.
	/// </summary>

	public Vector2 spacing = Vector2.zero;

	UIPanel panel;
	UIScrollView scrollView;
	Vector2 initialScroll;
	Vector2 scroll;
	int firstIndex = -1;
	int lastIndex = -1;

	/// <summary>
	/// Data that this grid contains.
	/// </summary>

	public List<IReuseGridCellData> Data { get; private set; } = new List<IReuseGridCellData>();

	/// <summary>
	/// Prefab to instantiate when cell need to be instantiated additionally.
	/// </summary>

	public UIReuseGridCell CellPrefab { get; set; }

	/// <summary>
	/// Bounds of this grid.
	/// </summary>

	public Bounds Bounds { get; private set; }

	void Awake()
	{
		panel = NGUITools.FindInParents<UIPanel>(gameObject, true);
		initialScroll = panel.transform.localPosition;
		scrollView = panel.GetComponent<UIScrollView>();
	}

	void OnEnable()
	{
		UpdateChildren(true);
	}

	void Update()
	{
		UpdateChildren(false);
	}

	/// <summary>
	/// Update children's position to show and bounds of contents.
	/// </summary>
	/// <param name="forcibly">If true, update forcibly though there's no change of scroll position</param>

	public void UpdateChildren(bool forcibly)
	{
		switch (scrollView.movement)
		{
			case UIScrollView.Movement.Horizontal:
				UpdateChildrenHorizontally(forcibly);
				break;

			case UIScrollView.Movement.Vertical:
				UpdateChildrenVertically(forcibly);
				break;

			// TODO :
			//
			case UIScrollView.Movement.Unrestricted:
			case UIScrollView.Movement.Custom:
				break;

			default:
				throw new System.NotImplementedException();
		}
	}

	void UpdateChildrenHorizontally(bool forcibly)
	{
		// check there is any scroll input
		if (!forcibly && !IsScrolled()) return;

		// inactivate all of the cells
		if (Data.Count == 0)
		{
			for (int i = 0; i < transform.childCount; i++)
			{
				transform.GetChild(i).gameObject.SetActive(false);
			}

			return;
		}

		int rows = this.columns;
		int columns = Data.Count / rows + (Data.Count % rows > 0 ? 1 : 0);
		int firstColumn = Mathf.Max(0, (int)(scroll.x / size.x));
		int lastColumn = Mathf.Min(columns - 1, Mathf.CeilToInt((scroll.x + panel.width) / size.x) - 1);

		// update bounds to use when check scrollable region
		var bounds = new Bounds();
		bounds.min = new Vector2(transform.localPosition.x, transform.localPosition.y - panel.height);
		bounds.max = new Vector3(transform.localPosition.x + columns * size.x, transform.localPosition.y);

		Bounds = bounds;

		// check there is any change of first column index to show or last column index to show
		if (!forcibly && firstColumn == firstIndex && lastColumn == lastIndex) return;

		firstIndex = firstColumn;
		lastIndex = lastColumn;

		// inactivate cells that is not used
		if (firstColumn == 0 && lastColumn - firstColumn + 1 < transform.childCount)
		{
			for (int i = lastColumn - firstColumn + 1; i < transform.childCount; i++)
			{
				transform.GetChild(i).gameObject.SetActive(false);
			}
		}

		// get count of cell should be instantiated additionally
		int needCount = (lastColumn - firstColumn) * rows + (lastColumn == columns - 1 && Data.Count % rows > 0 ? Data.Count % rows : rows) - transform.childCount;

		for (int i = 0; i < needCount; i++)
		{
			Instantiate(CellPrefab, transform);
		}

		// position cells
		for (int c = firstColumn; c <= lastColumn; c++)
		{
			for (int r = 0; r < rows; r++)
			{
				int index = c * rows + r;

				if (index >= Data.Count) break;

				// get child cell like circular buffer to reuse
				Transform child = transform.GetChild(index % transform.childCount);

				// consider the pivot of the cell itself
				Vector2 pivotValue = child.GetComponent<UIWidget>().pivotOffset;

				Vector2 localPosition;
				localPosition.x = Mathf.Lerp(c * size.x, (c + 1) * size.x, pivotValue.x) * (directionHorizontal == DirectionHorizontal.Right ? 1 : -1);
				localPosition.y = Mathf.Lerp(r * size.y, (r + 1) * size.y, 1 - pivotValue.y) * (directionVertical == DirectionVertical.Down ? -1 : 1);

				// update position and data of the cell
				child.gameObject.SetActive(true);
				child.localPosition = localPosition;
				child.GetComponent<UIReuseGridCell>().UpdateData(Data[index]);
			}
		}
	}

	void UpdateChildrenVertically(bool forcibly)
	{
		// check there is any scroll input
		if (!forcibly && !IsScrolled()) return;

		// inactivate all of the cells
		if (Data.Count == 0)
		{
			for (int i = 0; i < transform.childCount; i++)
			{
				transform.GetChild(i).gameObject.SetActive(false);
			}

			return;
		}

		int rows = Data.Count / columns + (Data.Count % columns > 0 ? 1 : 0);
		int firstRow = Mathf.Max(0, (int)(scroll.y / size.y));
		int lastRow = Mathf.Min(rows - 1, Mathf.CeilToInt((scroll.y + panel.height) / size.y) - 1);

		// update bounds to use when check scrollable region
		var bounds = new Bounds();
		bounds.min = new Vector2(transform.localPosition.x, transform.localPosition.y - rows * size.y);
		bounds.max = new Vector3(transform.localPosition.x + panel.width, transform.localPosition.y);

		Bounds = bounds;

		// check there is any change of first row index to show or last row index to show
		if (!forcibly && firstRow == firstIndex && lastRow == lastIndex) return;

		firstIndex = firstRow;
		lastIndex = lastRow;

		// inactivate cells that is not used
		if (firstRow == 0 && lastRow - firstRow + 1 < transform.childCount)
		{
			for (int i = lastRow - firstRow + 1; i < transform.childCount; i++)
			{
				transform.GetChild(i).gameObject.SetActive(false);
			}
		}

		// get count of cell should be instantiated additionally
		int needCount = (lastRow - firstRow) * columns + (lastRow == rows - 1 && Data.Count % columns > 0 ? Data.Count % columns : columns) - transform.childCount;

		for (int i = 0; i < needCount; i++)
		{
			Instantiate(CellPrefab, transform);
		}

		// position cells
		for (int r = firstRow; r <= lastRow; r++)
		{
			for (int c = 0; c < columns; c++)
			{
				int index = r * columns + c;

				if (index >= Data.Count) break;

				// get child cell like circular buffer to reuse
				Transform child = transform.GetChild(index % transform.childCount);

				// consider the pivot of the cell itself
				Vector2 pivotValue = child.GetComponent<UIWidget>().pivotOffset;

				Vector2 localPosition;
				localPosition.x = Mathf.Lerp(c * size.x, (c + 1) * size.x, pivotValue.x) * (directionHorizontal == DirectionHorizontal.Right ? 1 : -1);
				localPosition.y = Mathf.Lerp(r * size.y, (r + 1) * size.y, 1 - pivotValue.y) * (directionVertical == DirectionVertical.Down ? -1 : 1);

				// update position and data of the cell
				child.gameObject.SetActive(true);
				child.localPosition = localPosition;
				child.GetComponent<UIReuseGridCell>().UpdateData(Data[index]);
			}
		}
	}

	bool IsScrolled()
	{
		// do not take absolute value of scoll position to clamp after
		Vector2 scroll;
		scroll.x = (panel.transform.localPosition.x - initialScroll.x) * (directionHorizontal == DirectionHorizontal.Right ? -1 : 1);
		scroll.y = (panel.transform.localPosition.y - initialScroll.y) * (directionVertical == DirectionVertical.Down ? 1 : -1);

		Vector2 minScroll = new Vector2(0, 0);
		Vector2 maxScroll = new Vector2(Bounds.size.x - panel.width, Bounds.size.y - panel.height);

		scroll.x = Mathf.Clamp(scroll.x, minScroll.x, maxScroll.x);
		scroll.y = Mathf.Clamp(scroll.y, minScroll.y, maxScroll.y);

		// check there is valid change of scroll position
		if (Mathf.Approximately(scroll.x, this.scroll.x) && Mathf.Approximately(scroll.y, this.scroll.y))
		{
			return false;
		}
		else
		{
			// update scroll position
			this.scroll = scroll;

			return true;
		}
	}
}

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// All children added to the game object with this script will be arranged into a table
/// with rows and columns automatically adjusting their size to fit their content.
/// And the child game object (cell) is reused to show vast amount of data.
/// </summary>

public sealed class UIReuseTable : UIWidgetContainer
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
	/// Data that this table contains.
	/// </summary>

	public List<IReuseTableCellData> Data { get; private set; } = new List<IReuseTableCellData>();

	/// <summary>
	/// Prefab to instantiate when cell need to be instantiated additionally.
	/// </summary>

	public UIReuseTableCell CellPrefab { get; set; }

	/// <summary>
	/// Bounds of this table.
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
		int columns = Data.Count / rows;
		int firstColumn = 0;
		int lastColumn = 0;
		float width = 0;
		float columnPosX = 0;

		// get first column index to show
		for (int c = 0; c <= columns; c++)
		{
			float columnWidth = 0;

			for (int r = 0; r < rows; r++)
			{
				int index = c * rows + r;

				if (index >= Data.Count) break;

				// column's width should be max value
				columnWidth = Mathf.Max(columnWidth, Data[index].Size.x + spacing.x);
			}

			// do not add first column's width to calculate last column index together
			if (scroll.x - (width + columnWidth) <= 0 || c == columns)
			{
				firstColumn = c;

				// set first column's position of x
				columnPosX = width;

				break;
			}

			width += columnWidth;
		}

		// get last column index to show
		for (int c = firstColumn; c <= columns; c++)
		{
			float columnWidth = 0;

			for (int r = 0; r < rows; r++)
			{
				int index = c * rows + r;

				if (index >= Data.Count) break;

				// column's width should be max value
				columnWidth = Mathf.Max(columnWidth, Data[index].Size.x + spacing.x);
			}

			width += columnWidth;

			if ((scroll.x + panel.width) - width <= 0 || c == columns)
			{
				lastColumn = c;
				break;
			}
		}

		// get bounds of this table (the size of contents)
		for (int c = lastColumn + 1; c <= columns; c++)
		{
			float columnWidth = 0;

			for (int r = 0; r < rows; r++)
			{
				int index = c * rows + r;

				if (index >= Data.Count) break;

				columnWidth = Mathf.Max(columnWidth, Data[index].Size.x + spacing.x);
			}

			width += columnWidth;
		}

		// update bounds to use when check scrollable region
		var bounds = new Bounds();
		bounds.min = new Vector2(transform.localPosition.x, transform.localPosition.y - panel.height);
		bounds.max = new Vector3(transform.localPosition.x + width, transform.localPosition.y);

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
		int needCount = (lastColumn - firstColumn + 1) * rows - ((lastColumn == Data.Count / rows) ? (rows - Data.Count % rows) : 0) - transform.childCount;

		for (int i = 0; i < needCount; i++)
		{
			Instantiate(CellPrefab, transform);
		}

		// position cells
		for (int c = firstColumn; c <= lastColumn; c++)
		{
			float columnWidth = 0;
			float rowPosY = 0f;

			for (int r = 0; r < rows; r++)
			{
				int index = c * rows + r;

				if (index >= Data.Count) break;

				// get child cell like circular buffer to reuse
				Transform child = transform.GetChild(index % transform.childCount);

				// consider the pivot of the cell itself
				Vector2 pivotValue = child.GetComponent<UIWidget>().pivotOffset;

				Vector2 localPosition;
				localPosition.x = Mathf.Lerp(columnPosX, columnPosX + Data[index].Size.x, pivotValue.x) * (directionHorizontal == DirectionHorizontal.Right ? 1 : -1);
				localPosition.y = Mathf.Lerp(rowPosY, rowPosY + Data[index].Size.y, 1 - pivotValue.y) * (directionVertical == DirectionVertical.Down ? -1 : 1);

				// update position and data of the cell
				child.gameObject.SetActive(true);
				child.localPosition = localPosition;
				child.GetComponent<UIReuseTableCell>().UpdateData(Data[index]);

				columnWidth = Mathf.Max(columnWidth, Data[index].Size.x + spacing.x);

				// get next cell's position of y
				rowPosY += Data[index].Size.y + spacing.y;
			}

			// get next cell's position of x
			columnPosX += columnWidth;
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

		int rows = Data.Count / columns;
		int firstRow = 0;
		int lastRow = 0;
		float height = 0;
		float rowPosY = 0;

		// get first row index to show
		for (int r = 0; r <= rows; r++)
		{
			float rowHeight = 0;

			for (int c = 0; c < columns; c++)
			{
				int index = r * columns + c;

				if (index >= Data.Count) break;

				// row's height should be max value
				rowHeight = Mathf.Max(rowHeight, Data[index].Size.y + spacing.y);
			}

			// do not add first row's height to calculate last row index together
			if (scroll.y - (height + rowHeight) <= 0 || r == rows)
			{
				firstRow = r;

				// set first row's position of y
				rowPosY = height;

				break;
			}

			height += rowHeight;
		}

		// get last row index to show
		for (int r = firstRow; r <= rows; r++)
		{
			float rowHeight = 0;

			for (int c = 0; c < columns; c++)
			{
				int index = r * columns + c;

				if (index >= Data.Count) break;

				// row's height should be max value
				rowHeight = Mathf.Max(rowHeight, Data[index].Size.y + spacing.y);
			}

			height += rowHeight;

			if ((scroll.y + panel.height) - height <= 0 || r == rows)
			{
				lastRow = r;
				break;
			}
		}

		// get bounds of this table (the size of contents)
		for (int r = lastRow + 1; r <= rows; r++)
		{
			float rowHeight = 0;

			for (int c = 0; c < columns; c++)
			{
				int index = r * columns + c;

				if (index >= Data.Count) break;

				rowHeight = Mathf.Max(rowHeight, Data[index].Size.y + spacing.y);
			}

			height += rowHeight;
		}

		// update bounds to use when check scrollable region
		var bounds = new Bounds();
		bounds.min = new Vector2(transform.localPosition.x, transform.localPosition.y - height);
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
		int needCount = (lastRow - firstRow + 1) * columns - ((lastRow == Data.Count / columns) ? (columns - Data.Count % columns) : 0) - transform.childCount;

		for (int i = 0; i < needCount; i++)
		{
			Instantiate(CellPrefab, transform);
		}

		// position cells
		for (int r = firstRow; r <= lastRow; r++)
		{
			float rowHeight = 0;
			float columnPosX = 0f;

			for (int c = 0; c < columns; c++)
			{
				int index = r * columns + c;

				if (index >= Data.Count) break;

				// get child cell like circular buffer to reuse
				Transform child = transform.GetChild(index % transform.childCount);

				// consider the pivot of the cell itself
				Vector2 pivotValue = child.GetComponent<UIWidget>().pivotOffset;

				Vector2 localPosition;
				localPosition.x = Mathf.Lerp(columnPosX, columnPosX + Data[index].Size.x, pivotValue.x) * (directionHorizontal == DirectionHorizontal.Right ? 1 : -1);
				localPosition.y = Mathf.Lerp(rowPosY, rowPosY + Data[index].Size.y, 1 - pivotValue.y) * (directionVertical == DirectionVertical.Down ? -1 : 1);

				// update position and data of the cell
				child.gameObject.SetActive(true);
				child.localPosition = localPosition;
				child.GetComponent<UIReuseTableCell>().UpdateData(Data[index]);

				rowHeight = Mathf.Max(rowHeight, Data[index].Size.y + spacing.y);

				// get next cell's position of x
				columnPosX += Data[index].Size.x + spacing.x;
			}

			// get next cell's position of y
			rowPosY += rowHeight;
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

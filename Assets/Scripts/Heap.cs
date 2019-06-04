using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// most code from https://youtu.be/3Dw5d7PlcTM
public class Heap<T> where T : IHeapItem<T>{

	T[] items;
	int currentNumItems;

	public Heap(int maxItems)
	{
		items = new T[maxItems];
		currentNumItems = 0;
	}

	public void Add(T item)
	{
		item.heapIndex = currentNumItems;
		if (currentNumItems < items.Length) {
			items [currentNumItems] = item;
		} else {
			Debug.Log ("Array out of range. CurrentIndex: " + currentNumItems + " max: " + items.Length);
		}
		SortUp (item);
		currentNumItems++;
	}

	public T ReturnFirst()
	{
		// Get the first item (item with highest priority in the heap)
		T firstItem = items[0];
		currentNumItems--;
		items [0] = items [currentNumItems];
		items [0].heapIndex = 0;
		SortDown (items [0]);
		return firstItem;
	}

	public void UpdateItem(T item)
	{
		// update an item if it is already in the heap, but needs a new value
		SortUp(item);
	}

	public int Count
	{
		get {
			return currentNumItems;
		}
	}

	public bool Contains(T item)
	{
		// check if the item is already on the heap
		return Equals (item, items [item.heapIndex]);
	}

	void SortDown(T item)
	{
		while (true) {
			// sort down is for sorting the heap after an item is removed
			int leftChildIndex = (2 * item.heapIndex) + 1;
			int rightChildIndex = (2 * item.heapIndex) + 2;
			int swapIndex = 0;

			if (leftChildIndex < currentNumItems) {
				// has a left child
				swapIndex = leftChildIndex;
				if (rightChildIndex < currentNumItems) {
					// has a right child
					if (items [leftChildIndex].CompareTo (items [rightChildIndex]) < 0) {
						// right child is of higher priority than left child
						swapIndex = rightChildIndex;
					}
				}

				if (item.CompareTo (items [swapIndex]) < 0) {
					// one of the children has a higher priority than item
					// swap them
					Swap (item, items [swapIndex]);
				} else {
					// item already has higher priority than its children
					return;
				}
			} else {
				// item has no children
				// already in correct position
				return;
			}
		}
	}

	void SortUp(T item)
	{
		// Sort up is for adding a new item to the heap and putting it in the proper position
		int parentIndex = (item.heapIndex - 1) / 2;

		while (true) {
			T parentItem = items [parentIndex];
			if (item.CompareTo (parentItem) > 0) {
				// if item has higher priority than parent, swap them
				Swap (item, parentItem);
			} else {
				// if it doesn't, item is in the right place
				break;
			}
			parentIndex = (item.heapIndex - 1) / 2;
		}
	}

	void Swap(T itemA, T itemB)
	{
		items [itemA.heapIndex] = itemB;
		items [itemB.heapIndex] = itemA;

		int temp = itemA.heapIndex;
		itemA.heapIndex = itemB.heapIndex;
		itemB.heapIndex = temp;
	}


}

public interface IHeapItem<T> : IComparable<T>
{
	int heapIndex {
		get;
		set;
	}

}
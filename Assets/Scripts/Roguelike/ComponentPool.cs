using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ComponentPool<T> where T : Component
{
    private readonly Func<T> factory;
    private readonly Stack<T> inactiveItems = new Stack<T>();

    public ComponentPool(Func<T> factory)
    {
        this.factory = factory;
    }

    public void Warm(int count)
    {
        for (int index = inactiveItems.Count; index < count; index++)
        {
            T item = CreateNew();
            item.gameObject.SetActive(false);
            inactiveItems.Push(item);
        }
    }

    public T Get()
    {
        T item = inactiveItems.Count > 0 ? inactiveItems.Pop() : CreateNew();
        item.gameObject.SetActive(true);
        return item;
    }

    public void Release(T item)
    {
        if (item == null)
        {
            return;
        }

        item.gameObject.SetActive(false);
        inactiveItems.Push(item);
    }

    private T CreateNew()
    {
        T item = factory();
        item.gameObject.SetActive(false);
        return item;
    }
}

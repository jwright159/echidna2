﻿namespace Echidna2.Serialization;

public class PrefabRoot
{
	public List<PrefabRoot> ChildPrefabs = [];
	public List<object> Components = [];
	public object RootObject = null!;
	public string PrefabPath = "";
	
	public PrefabRoot? GetPrefabRoot(object component)
	{
		foreach (PrefabRoot childPrefab in ChildPrefabs)
		{
			if (childPrefab.RootObject == component)
				return childPrefab;
			if (childPrefab.GetPrefabRoot(component) is { } childPrefabRoot)
				return childPrefabRoot;
		}
		return null;
	}
	
	public PrefabRoot GetOwningPrefabRoot(object component)
	{
		if (Components.Contains(component))
			return this;
		foreach (PrefabRoot childPrefab in ChildPrefabs)
			if (childPrefab.GetOwningPrefabRoot(component) is { } childPrefabRoot)
				return childPrefabRoot;
		throw new Exception("Component not found in prefab root");
	}
}
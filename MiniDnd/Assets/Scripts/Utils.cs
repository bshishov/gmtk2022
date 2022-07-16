using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public static class Utils
{
    public static IEnumerable<T> ConstructAllObjectOfType<T>()
    {
        var type = typeof(T);
        var types = System.Reflection.Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && type.IsAssignableFrom(t));
        foreach (var foundType in types)
        {
            var instance = Activator.CreateInstance(foundType);
            if (instance is T o)
            {
                yield return o;
            }
            else
            {
                Debug.LogWarning($"Invalid instance: {instance}");
            }
        }
    }
    
    public static T Choice<T>(IList<T> items, Func<T, float> getWeight)
    {
        if (items == null)
            return default;

        var weights = new float[items.Count];
        var total = 0f;
        for (var i = 0; i < items.Count; i++)
        {
            var w = getWeight(items[i]);
            weights[i] = w;
            total += w;
        }

        var upTo = 0f;
        var r = Random.Range(0, total);

        for (var i = 0; i < items.Count; i++)
        {
            if (upTo + weights[i] >= r) 
                return items[i];

            upTo += weights[i];
        }

        return default;
    }

    public static T Choice<T>(IList<T> items)
    {
        var idx = Random.Range(0, items.Count);
        return items[idx];
    }

    public static List<T> NRandom<T>(IEnumerable<T> list, int elementsCount)
    {
        return list.OrderBy(arg => Random.value).Take(elementsCount).ToList();
    }
}

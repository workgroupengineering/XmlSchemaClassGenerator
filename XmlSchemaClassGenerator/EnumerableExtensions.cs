﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlSchemaClassGenerator;

public static class EnumerableExtensions
{
    public static NamespaceProvider ToNamespaceProvider(this IEnumerable<KeyValuePair<NamespaceKey, string>> items,
        GenerateNamespaceDelegate generate = null)
    {
        var result = new NamespaceProvider()
        {
            GenerateNamespace = generate,
        };
        foreach (var item in items)
            result.Add(item.Key, item.Value);
        return result;
    }

    public static IEnumerable<RestrictionModel> Sanitize(this IEnumerable<RestrictionModel> input)
    {
        var patterns = new List<PatternRestrictionModel>();
        foreach (var item in input)
        {
            if (item is PatternRestrictionModel pattern)
            {
                patterns.Add(pattern);
            }
            else
            {
                yield return item;
            }
        }
        if (patterns.Count == 1)
        {
            yield return patterns[0];
        }
        else if (patterns.Count > 1)
        {
            var config = patterns.Select(x => x.Configuration).First();
            var pattern = string.Join("|", patterns.Select(x => string.Format("({0})", x.Value)));
            yield return new PatternRestrictionModel(config)
            {
                Value = pattern,
            };
        }
    }

    public static IEnumerable<NamespaceHierarchyItem> Flatten(this IEnumerable<NamespaceHierarchyItem> hierarchyItems)
    {
        foreach (var hierarchyItem in hierarchyItems)
        {
            if (hierarchyItem.Models.Any())
            {
                yield return hierarchyItem;
            }
            foreach (var subNamespaceItem in hierarchyItem.SubNamespaces.Flatten())
                yield return subNamespaceItem;
        }
    }

    private static void MarkAmbiguousNamespaceTypes(NamespaceHierarchyItem item)
    {
        foreach (var nsModel in item.Models)
            nsModel.IsAmbiguous = true;
        foreach (var subItem in item.SubNamespaces)
            MarkAmbiguousNamespaceTypes(subItem);
    }

    private static void MarkAmbiguousNamespaceTypes(string rootName, NamespaceHierarchyItem item, List<TypeModel> parentTypes)
    {
        var visibleTypes = new List<TypeModel>(parentTypes);
        visibleTypes.AddRange(item.TypeModels);
        var visibleTypesLookup = visibleTypes.ToLookup(x => x.Name);
        var isAmbiguous = visibleTypesLookup.Contains(rootName);
        if (isAmbiguous)
        {
            MarkAmbiguousNamespaceTypes(item);
        }
        else
        {
            foreach (var subItem in item.SubNamespaces)
                MarkAmbiguousNamespaceTypes(rootName, subItem, visibleTypes);
        }
    }

    public static IEnumerable<NamespaceHierarchyItem> MarkAmbiguousNamespaceTypes(
        this IEnumerable<NamespaceHierarchyItem> hierarchyItems)
    {
        var emptyTypes = new List<TypeModel>();
        foreach (var hierarchyItem in hierarchyItems)
        {
            MarkAmbiguousNamespaceTypes(hierarchyItem.Name, hierarchyItem, emptyTypes);
            yield return hierarchyItem;
        }
    }

    /// <summary>
    /// Creates a <see cref="Dictionary{TKey,TValue}" /> from an <see cref="IEnumerable{T}" /> splitting the values into key and value pairs based on the <paramref name="delimiter"/>.
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable{T}" /> to create a <see cref="Dictionary{TKey,TValue}" /> from.</param>
    /// <param name="delimiter">An optional value that supplies the delimiter to use to create the key and value from the <paramref name="source"/>.</param>
    /// <returns>A <see cref="Dictionary{TKey,TValue}" /> that contains keys and values. The values within each group are in the same order as in <paramref name="source" />.</returns>
    public static Dictionary<string, string> ToDictionary(this IEnumerable<string> source, string delimiter = "=")
    {
        var result = new Dictionary<string, string>();
        foreach (string value in source ?? [])
        {
            var pair = value.Split([delimiter ?? "="], 2, StringSplitOptions.None);
            result.Add(pair[0], pair[1]);
        }

        return result;
    }
}

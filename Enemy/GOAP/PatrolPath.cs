using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class PatrolNode
{
    public Vector3 position;
    public Quaternion rotation;

    public float stopDuration;
    public float stopDurationVariance;

    public PatrolNode()
    {
        position = Vector3.zero;
        rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
    }

    public PatrolNode(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }

    /// <summary>
    /// From an array of PatrolNodes, return the index of the closest node to the provided position in Euclidean distance.
    /// </summary>
    /// <param name="nodes">List of nodes.</param>
    /// <param name="position">Position to compare with.</param>
    /// <returns>The index of the closest node.</returns>
    public static int GetClosestNodeIndex(PatrolNode[] nodes, Vector3 position)
    {
        if (nodes.Length <= 0)
        {
            return -1;
        }
        var best = 0;
        var bestDistance = Vector3.Distance(nodes[best].position, position);
        for (int i = 1; i < nodes.Length; i++)
        {
            var newDistance = Vector3.Distance(nodes[i].position, position);
            if (newDistance < bestDistance)
            {
                bestDistance = newDistance;
                best = i;
            }
        }

        return best;
    }

    /// <summary>
    /// From an array of PatrolNodes, return the closest node to the provided position in Euclidean distance.
    /// </summary>
    /// <param name="nodes">List of nodes.</param>
    /// <param name="position">Position to compare with.</param>
    /// <returns>The closest PatrolNode.</returns>
    public static PatrolNode GetClosestNode(PatrolNode[] nodes, Vector3 position)
    {
        var i = GetClosestNodeIndex(nodes, position);
        if (i < 0)
        {
            return null;
        }
        return nodes[i];
    }

    /// <summary>
    /// Returns a sorted array of PatrolNodes by distance to a specific position.
    /// </summary>
    /// <param name="nodes">List of nodes.</param>
    /// <param name="position">Position to compare with.</param>
    /// <returns>A new sorted array.</returns>
    public static PatrolNode[] SortPatrolNodesByDistance(PatrolNode[] nodes, Vector3 position)
    {
        Dictionary<PatrolNode, float> nodeDistance = new();
        List<PatrolNode> sortedNodes = new List<PatrolNode>(nodes);

        foreach (var node in nodes)
        {
            nodeDistance[node] = Vector3.Distance(node.position, position);
        }

        sortedNodes.Sort((node1, node2) =>
        {
            int v = nodeDistance[node1].CompareTo(nodeDistance[node2]);
            return v;
        });

        return sortedNodes.ToArray();
    }
}

[ExecuteInEditMode]
public class PatrolPath
{
    public List<PatrolNode> nodes;
    public PatrolPath()
    {
        nodes = new List<PatrolNode>();
    }
}
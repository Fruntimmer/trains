using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AStar
{
    Dictionary<IntersectionNode, AStarInfo> infoMap = new Dictionary<IntersectionNode, AStarInfo>();
    private List<RailPiece> result = new List<RailPiece>();
    private List<IntersectionNode> open = new List<IntersectionNode>();
    private List<IntersectionNode> closed = new List<IntersectionNode>();

    public AStar()
    {
        //_mm.Nodes.ForEach(node => infoMap[node] = new AStarInfo());
    }

    public class AStarInfo
    {
        public float g;
        public float h;
        public float f;
        public RailPiece parent;
    }

    private void CalculatePath(IntersectionNode currentNode, IntersectionNode start, IntersectionNode goal)
    {
        if (currentNode == goal)
        {
            IntersectionNode pathNode = currentNode;
            while (pathNode != start)
            {
                RailPiece parent = GetParent(pathNode);
                result.Add(parent);
                pathNode = pathNode == parent.start ? parent.end : parent.start;
            }
            return;
        }
        closed.Add(currentNode);
        open.Remove(currentNode);
        currentNode.neighbors.ForEach(neighbor => AddNeighbor(neighbor, neighbor.start == currentNode ? neighbor.end : neighbor.start, currentNode, goal));
        currentNode.neighbors.ForEach(neighbor => CalculateScores(neighbor, neighbor.start == currentNode ? neighbor.end : neighbor.start));
        if (open.Count == 0)
        {
            return;
        }
        open.Sort((left, right) => GetF(left) > GetF(right) ? -1 : 1);
        CalculatePath(open.Last(), start, goal);
    }

    public List<RailPiece> GetPath(IntersectionNode start, IntersectionNode goal)
    {
        result.Clear();
        open.Clear();
        closed.Clear();

        CalculatePath(start, start, goal);
        result.Reverse();
        return result;
    }

    public int GetDistance(IntersectionNode start, IntersectionNode goal)
    {
        return GetPath(start, goal).Count - 2;
    }

    private void AddNeighbor(RailPiece rail_piece, IntersectionNode neighbor, IntersectionNode currentNode, IntersectionNode goal)
    {
        if (!closed.Contains(neighbor))
        {
            if (open.Contains(neighbor))
            {
                if (GetG(currentNode) + rail_piece.getLength() < GetG(neighbor))
                {
                    SetParent(rail_piece, neighbor);
                    CalculateScores(rail_piece, neighbor);
                }
            }
            else
            {
                open.Add(neighbor);
                SetParent(rail_piece, neighbor);
                SetH(neighbor, Vector3.Distance(currentNode.position, neighbor.position));
            }
        }
    }

    private void CalculateScores(RailPiece rail_piece, IntersectionNode node)
    {
        RailPiece parent = GetParent(node);
        float parentG = 0;
        if (parent != null)
        {
            parentG = GetG(parent.start == node ? parent.end : parent.start);
        }
        SetG(node, parentG + rail_piece.getLength());
        SetF(node, GetG(node) + GetH(node));
    }

    private void SetParent(RailPiece parent, IntersectionNode child)
    {
        if (!infoMap.ContainsKey(child)) {
            infoMap[child] = new AStarInfo();
        }
        AStarInfo info = infoMap[child];
        info.parent = parent;
    }

    private void SetG(IntersectionNode node, float g)
    {
        if (!infoMap.ContainsKey(node))
        {
            infoMap[node] = new AStarInfo();
        }
        AStarInfo info = infoMap[node];
        info.g = g +node.signalCost;
    }

    private void SetH(IntersectionNode node, float h)
    {
        if (!infoMap.ContainsKey(node))
        {
            infoMap[node] = new AStarInfo();
        }
        AStarInfo info = infoMap[node];
        info.h = h;
    }

    private void SetF(IntersectionNode node, float f)
    {
        if (!infoMap.ContainsKey(node))
        {
            infoMap[node] = new AStarInfo();
        }
        AStarInfo info = infoMap[node];
        info.f = f;
    }

    private RailPiece GetParent(IntersectionNode child)
    {
        if (!infoMap.ContainsKey(child))
        {
            infoMap[child] = new AStarInfo();
        }
        return infoMap[child].parent;
    }

    private float GetG(IntersectionNode node)
    {
        if (!infoMap.ContainsKey(node))
        {
            infoMap[node] = new AStarInfo();
        }
        return infoMap[node].g;
    }

    private float GetH(IntersectionNode node)
    {
        if (!infoMap.ContainsKey(node))
        {
            infoMap[node] = new AStarInfo();
        }
        return infoMap[node].h;
    }

    private float GetF(IntersectionNode node)
    {
        if (!infoMap.ContainsKey(node))
        {
            infoMap[node] = new AStarInfo();
        }
        return infoMap[node].f;
    }

    public void DebugDraw()
    {
        DebugDraw(Color.green);
    }

    public void DebugDraw(Color color)
    {
        IntersectionNode lastNode = null;
        foreach (RailPiece node in result)
        {
            if (lastNode != null)
            {
                Debug.DrawLine(node.start.position, node.end.position);
            }
        }
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IntersectionNode
{
    public Vector3 position;
    public float signalCost = 0;
    public List<RailPiece> neighbors = new List<RailPiece>();
    public IntersectionNode(Vector3 position)
    {
        this.position = position;
    }

    public void AddNeighbor(RailPiece neighbor)
    {
        if (!neighbors.Contains(neighbor))
        {
            neighbors.Add(neighbor);
        }
    }

    public void TrainEnter(List<Signal> busy_signals)
    {
        TrainEnter(new List<IntersectionNode>(), busy_signals);
    }

    public void TrainEnter(List<IntersectionNode> visited, List<Signal> busy_signals)
    {
        visited.Add(this);
        foreach (RailPiece neighbor in neighbors)
        {
            bool keep_searching = true;
            if (neighbor.start == this)
            {
                if (visited.Contains(neighbor.end))
                {
                    keep_searching = false;
                }
            }
            else
            {
                if (visited.Contains(neighbor.start))
                {
                    keep_searching = false;
                }
            }
            neighbor.TrainEnter(this, visited, busy_signals, keep_searching);
        }

        
    }
}
public class Signal
{
    public bool busy = false;
    private Vector3 position;
    public Signal(Vector3 position)
    {
        this.position = position;
    }
    public Vector3 Position
    {
        get { return this.position;}
    }
    public void OnDrawGizmos()
    {
        if(busy)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.blue;
        }
        Gizmos.DrawWireCube(position, new Vector3(.5f,.5f,.5f));
    }
}
public class Train {
    float distance_from_source;
    RailPiece rail_piece;
    public List<RailPiece> path = new List<RailPiece>();
    int pathSegment = 0;
    float speed = 0.1f;
    float baseSpeed = 0.1f;
    IntersectionNode going_from;
    bool going_from_start = true;
    List<Signal> previous_signals = new List<Signal>();

    int current_signal_iterator = -1;

    public Train(RailPiece rail_piece, IntersectionNode going_from)
    {
        this.rail_piece = rail_piece;
        this.going_from = going_from;
        going_from_start = going_from == rail_piece.start ? true : false;
        if (going_from_start)
        {
            current_signal_iterator = 0;
        }
        else
        {
            current_signal_iterator = rail_piece.signals_list.Count - 1;
        }

        if (getNextSignal() != null)
        {
            getNextSignal().busy = true;
            previous_signals.Add(getNextSignal());
        }

        this.going_from.TrainEnter(previous_signals);
    }

    Signal getNextSignal()
    {
        if (rail_piece.signals_list.Count == 0 || current_signal_iterator < 0 || current_signal_iterator > rail_piece.signals_list.Count - 1) {
            return null;
        }
        return rail_piece.signals_list[current_signal_iterator];
    }

    void incrementSignalIterator()
    {
        if (going_from_start)
        {
            current_signal_iterator++;
        }
        else
        {
            current_signal_iterator--;
        }
    }

    Signal passedSignal()
    {
        if (getNextSignal() != null)
        {
            if (going_from_start)
            {
                if (Vector3.Distance(getPosition(), rail_piece.start.position) >= Vector3.Distance(getNextSignal().Position, rail_piece.start.position))
                {
                    return getNextSignal();
                }
            }
            else
            {
                if (Vector3.Distance(getPosition(), rail_piece.end.position) >= Vector3.Distance(getNextSignal().Position, rail_piece.end.position))
                {
                    return getNextSignal();
                }
            }
        }
        return null;
    }

    Vector3 getPosition()
    {
        if (going_from_start)
        {
            return rail_piece.start.position + rail_piece.getNormalizedVector() * distance_from_source;
        }
        else
        {
            return rail_piece.end.position - rail_piece.getNormalizedVector() * distance_from_source;
        }
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(getPosition(), new Vector3(0.5f, 0.5f, 0.5f));

        Gizmos.color = Color.yellow;
        foreach (Signal previous in previous_signals)
        {
            Gizmos.DrawWireCube(previous.Position, new Vector3(0.4f, 0.4f, 0.4f));
        }
    }

    public void DebugDraw()
    {
        DebugDraw(Color.green);
    }

    public void DebugDraw(Color color)
    {
        foreach (RailPiece rail_piece in path)
        {
            Debug.DrawLine(rail_piece.start.position, rail_piece.end.position, color);
        }
    }

    public void Update() {
        distance_from_source += speed;
        if (distance_from_source >= rail_piece.getLength())
        {
            AdvanceToNewRailPiece();
        }

        Signal passed_signal = passedSignal();
        if (passed_signal != null)
        {
            foreach(Signal previous_signal in previous_signals){
                previous_signal.busy = false;
            }
            previous_signals.Clear();
            previous_signals.Add(getNextSignal());
            getNextSignal().busy = true;

            incrementSignalIterator();
            if (getNextSignal() != null)
            {
                getNextSignal().busy = true;
                //previous_signals.Add(getNextSignal());
            }
            else
            {
                List<IntersectionNode> no_go = new List<IntersectionNode>();
                if (going_from_start)
                {
                    //no_go.Add(rail_piece.start);
                    rail_piece.end.TrainEnter(no_go, previous_signals);
                }
                else
                {
                    //no_go.Add(rail_piece.end);
                    rail_piece.start.TrainEnter(no_go, previous_signals);
                }
            }
        }
    }

    private void AdvanceToNewRailPiece()
    {
        // Check if the new path segment is still valid
        pathSegment++;
        if (pathSegment == path.Count)
        {
            path.Reverse();
            pathSegment = 0;
        }
        IntersectionNode nextIntersection = going_from_start ? rail_piece.end : rail_piece.start;
        going_from = nextIntersection;
        rail_piece = path[pathSegment];
        going_from_start = going_from == rail_piece.start ? true : false;

        distance_from_source = 0;

        if (going_from_start)
        {
            current_signal_iterator = 0;
        }
        else
        {
            current_signal_iterator = rail_piece.signals_list.Count - 1;
        }
    }
}

public class RailPiece
{
    public IntersectionNode start, end;
    public List<Signal> signals_list = new List<Signal>();
    public float signalCost = 0;
    public RailPiece(IntersectionNode start, IntersectionNode end)
    {
        this.start = start;
        this.end = end;
        start.AddNeighbor(this);
        end.AddNeighbor(this);
    }

    public void AddSignal(Signal signal)
    {
        signals_list.Add(signal);
        signals_list.Sort(
            (left, right) =>
            Vector3.Distance(left.Position, this.start.position) <
            Vector3.Distance(right.Position, this.start.position)
                ? -1
                : 1);
    }

    public void DebugDraw()
    {
        DebugDraw(Color.white);
    }

    public void DebugDraw(Color color)
    {
        Debug.DrawLine(this.start.position, this.end.position, color);
    }

    public Vector3 getNormalizedVector()
    {
        return (this.end.position - this.start.position).normalized;
    }

    public float getLength() {
        return Vector3.Distance(this.start.position, this.end.position);
    }

    public void Update()
    {
        if(signalCost > 0)
        {
            signalCost = Mathf.Clamp(signalCost - 1, 0, Mathf.Infinity);
        }
        start.signalCost = this.signalCost;
        end.signalCost = this.signalCost;
    }

    internal void TrainEnter(IntersectionNode from_node, List<IntersectionNode> visited, List<Signal> busy_signals, bool keep_searching)
    {
        if (signals_list.Count > 0)
        {
            if (start == from_node)
            {
                signals_list[0].busy = true;
                busy_signals.Add(signals_list[0]);
                return;
            }
            else
            {
                signals_list[signals_list.Count - 1].busy = true;
                busy_signals.Add(signals_list[signals_list.Count - 1]);
                return;
            }
        }
        else
        {
            if (keep_searching)
            {
                if (start == from_node)
                {
                    end.TrainEnter(visited, busy_signals);
                }
                else
                {
                    start.TrainEnter(visited, busy_signals);
                }
            }
        }
    }
}

public class RailPieceScript : MonoBehaviour {
}

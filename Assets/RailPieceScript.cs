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
}
public class Signal
{
    public bool busy = false;
    private Vector3 position;
    public Signal(Vector3 position)
    {
        this.position = position;
    }
    public Vector3 Position()
    {
        return this.position;
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

    private Signal previousSignal;
    private Signal nextSignal;
    private List<Signal> currentSignalList = new List<Signal>();

    public Train(RailPiece rail_piece, IntersectionNode going_from)
    {
        this.rail_piece = rail_piece;
        this.going_from = going_from;
        currentSignalList = GetSignalList();
    }

    List<Signal> GetSignalList()
    {
        //closestIntersections.Sort((left, right) => Vector3.Distance(left.position, hit.point) < Vector3.Distance(right.position, hit.point) ? -1 : 1);
        Debug.Log("Ordering signal list");
        List<Signal> signalsOrdered = new List<Signal>(rail_piece.signals_list);
        if (signalsOrdered.Count > 0)
        {
            currentSignalList.Sort(
                (left, right) =>
                Vector3.Distance(left.Position(), this.getPosition()) <
                Vector3.Distance(right.Position(), this.getPosition())
                    ? -1
                    : 1);
            return signalsOrdered;
        }
        return signalsOrdered;
    }
    Vector3 getPosition()
    {
        if (going_from == rail_piece.start)
        {
            return rail_piece.start.position + rail_piece.getNormalizedVector() * distance_from_source;
        }
        else
        {
            return rail_piece.end.position - rail_piece.getNormalizedVector() * distance_from_source;
        }
    }

    Signal GetClosestSignal()
    {      
        Signal closestSignal = new Signal(Vector3.zero);
        float closestDist = 1000;
        foreach (Signal signal in currentSignalList)
        {
            float dist = Vector3.Distance(this.getPosition(), signal.Position());
            if (dist < closestDist)
            {
                closestDist = dist;
                closestSignal = signal;
            }
        }
        //Signal closestSignal = currentSignalList[0]; <= This should be enough since the list is ordered. Doesn't work when train turns around
        return closestSignal;
    }
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(getPosition(), new Vector3(0.5f, 0.5f, 0.5f));
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
            // Check if the new path segment is still valid
            pathSegment++;
            if (pathSegment == path.Count)
            {
                path.Reverse();
                pathSegment = 0;
            }
            IntersectionNode nextIntersection = going_from == rail_piece.start ? rail_piece.end : rail_piece.start;
            going_from = nextIntersection;
            rail_piece = path[pathSegment];
            distance_from_source = 0;
            
            nextSignal = null;
            currentSignalList = GetSignalList();
        }
        if(currentSignalList.Count > 0)
        {
            if(nextSignal == null)
            {
                Debug.Log("Getting closest signal");
                nextSignal = GetClosestSignal();
                //nextSignal.busy = true;
            }
            //Makes trains stop if the next signal is busy. Only works on the same railpiece since that's what currentSignalsList contains.
            /*
            if(nextSignal.busy == true && nextSignal != previousSignal)
            {
                speed = 0;
            }
            else
            {
                speed = baseSpeed;
            }
            */
            if(Vector3.Distance(this.getPosition(), nextSignal.Position())<0.5f) //Very very poopy. Yes sir. If train goes to fast it will miss. Entirely scale dependant.
            {
                if(previousSignal != null)
                {
                    previousSignal.busy = false;
                }
                previousSignal = nextSignal;
                previousSignal.busy = true;
                rail_piece.signalCost += 10000;
                currentSignalList.Remove(nextSignal);
                nextSignal = null;
            }
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
}

public class RailPieceScript : MonoBehaviour {

    RailPiece rp,rp2;
    Train t;
	// Use this for initialization
    /*
	void Start () {
        IntersectionNode startNode = new IntersectionNode(new Vector3(0, 0, 0));
        IntersectionNode endNode = new IntersectionNode(new Vector3(10, 0, 2));
        IntersectionNode intersection = new IntersectionNode(new Vector3(10,0,0));
        rp = new RailPiece(startNode, intersection);
        rp2 = new RailPiece(intersection, endNode);
        t = new Train(rp, startNode);
	}

    void DebugDraw()
    {
        rp.DebugDraw();
        rp2.DebugDraw();
    }

    void OnDrawGizmos()
    {
        if (t != null)
        {
            t.OnDrawGizmos();
        }
    }

	// Update is called once per frame
	void Update () {
        t.Update();
        DebugDraw();
	}*/
}

using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class CreateRailScript : MonoBehaviour {
    public GameObject pRail;


    IntersectionNode current_node = null;
    IntersectionNode current_train_node = null;
    IntersectionNode snap_point = null;

    List<Train> t = new List<Train>();
    List<RailPiece> rail_pieces = new List<RailPiece>();
    List<IntersectionNode> intersections = new List<IntersectionNode>();
    
    
    string active_tool = "road";
    Vector3 previz_point = Vector3.zero;
    bool draw_previz_point = false;

    //GUI variables
    private float bHeight = 50;
    private float bWidth = 100;
    private float lHeight = 0;
    private Train selectedTrain;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        RaycastHit hit;
        Ray ray = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);
        int terrain_mask = 1 << 8 | 1 << 9;
        snap_point = null;

        if(Physics.Raycast(ray, out hit, Mathf.Infinity, terrain_mask))
        {
            IntersectionNode next_node = null;
            List<IntersectionNode> closestIntersections = null;
            if (intersections.Count > 0) {
                closestIntersections = new List<IntersectionNode>(intersections);
                closestIntersections.Sort((left, right) => Vector3.Distance(left.position, hit.point) < Vector3.Distance(right.position, hit.point) ? -1 : 1);

                if (Vector3.Distance(closestIntersections[0].position, hit.point) < 0.7f)
                {
                    next_node = closestIntersections[0];
                    snap_point = next_node;
                }
            }


            if (Input.GetMouseButtonDown(0))
            {
                if (active_tool == "road")
                {
                    RoadTool(hit, next_node, closestIntersections);
                }
                if (active_tool == "train" && snap_point != null)
                {
                    TrainTool();
                }
                //Reroute existing train
                if(active_tool == "route" && snap_point != null)
                {
                    TrainTool();
                }
            }
        }
        int rail_layerMask = 1 << 9;
        draw_previz_point = false;
        //Draws "cursor" for potential signal or railpiece to be placed on existing rail.
        if((active_tool == "signal" || active_tool == "road") && Physics.Raycast(ray, out hit, Mathf.Infinity, rail_layerMask))
        {
            DrawPreviz(hit);
            //Calls function to place signals
            if (active_tool == "signal")
            {
                SignalTool(hit);
            }
        }

        if (Input.GetKeyDown(KeyCode.T) && intersections.Count >= 2)
        {
            Debug.Log("Train Tool");
            active_tool = "train";
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            current_node = null;
            Debug.Log("Road Tool");
            active_tool = "road";
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Signal Tool");
            active_tool = "signal";
        }

        if (t.Count > 0)
        {
            foreach (Train train in t)
            {
                train.Update();
            }
        }

        if (rail_pieces.Count > 0)
        {
            foreach(RailPiece rail in rail_pieces)
            {
                rail.Update();
            }
        }
        DebugDraw();
	}

    private void DrawPreviz(RaycastHit hit)
    {
        RailPiece rail_piece = ((RailMeshScript)hit.collider.gameObject.GetComponent("RailMeshScript")).rail_piece;
        previz_point = ClosestPointOnLine(rail_piece.start.position, rail_piece.end.position, hit.point);
        draw_previz_point = true;
    }

    private void SignalTool(RaycastHit hit)
    {
        if (Input.GetMouseButtonDown(0))
        {
            RailPiece rail_piece = ((RailMeshScript)hit.collider.gameObject.GetComponent("RailMeshScript")).rail_piece;
            rail_piece.AddSignal(new Signal(ClosestPointOnLine(rail_piece.start.position, rail_piece.end.position, hit.point)));
            Debug.Log("Created Signal");
        }
    }

    private void TrainTool()
    {
        Debug.Log("Train Click!");
        if (current_train_node == null)
        {
            current_train_node = snap_point;
        }
        else
        {
            //Create train and assign path
            AStar astar = new AStar();
            List<RailPiece> shortestPath = astar.GetPath(current_train_node, snap_point);

            if (selectedTrain != null)
            {
                selectedTrain.path = shortestPath;
            }
            else
            {
                t.Add(new Train(shortestPath[0], current_train_node));
                t[t.Count - 1].path = shortestPath;
            }
            current_train_node = null;
        }
    }

    private void RoadTool(RaycastHit hit, IntersectionNode next_node, List<IntersectionNode> closestIntersections)
    {
        //If we don't have a start point
        if (current_node == null)
        {
            if (hit.collider.tag == "Rail")
            {
                if (closestIntersections != null)
                {
                    //If we click on an intersection
                    if (Vector3.Distance(closestIntersections[0].position, hit.point) < 1.0f)
                    {
                        Debug.Log("Clicked a previous node!");
                        current_node = closestIntersections[0];
                    }
                    //If we click on a rail but not near a intersection, we create an intersection.
                    else
                    {
                        RailPiece rail_piece = ((RailMeshScript)hit.collider.gameObject.GetComponent("RailMeshScript")).rail_piece;
                        current_node = CreateIntersection(rail_piece, ClosestPointOnLine(rail_piece.start.position, rail_piece.end.position, hit.point));
                        intersections.Add(current_node);
                    }
                }
            }
            //If we click on nothing at all
            else
            {
                current_node = new IntersectionNode(hit.point);
                intersections.Add(current_node);
            }
        }
        //If we already have a start point
        else
        {
            if (next_node == null)
            {
                //If we click on a rail
                if(hit.collider.tag == "Rail")
                {
                    RailPiece rail_piece = ((RailMeshScript)hit.collider.gameObject.GetComponent("RailMeshScript")).rail_piece;
                    next_node = CreateIntersection(rail_piece, ClosestPointOnLine(rail_piece.start.position, rail_piece.end.position, hit.point));
                }
                else
                {
                    next_node = new IntersectionNode(hit.point);
                }
            }

            intersections.Add(next_node);

            CreateRail(current_node, next_node);
            current_node = next_node;
        }
    }
    
    void CreateRail(IntersectionNode current_node, IntersectionNode next_node)
    {
        RailPiece rail = new RailPiece(current_node, next_node);
        rail_pieces.Add(rail);
        
        GameObject railObj = (GameObject)Instantiate(pRail, rail.start.position, Quaternion.identity);
        railObj.transform.LookAt(rail.end.position);
        railObj.transform.localScale = new Vector3(railObj.transform.localScale.x, .1f, rail.getLength());
        ((RailMeshScript)railObj.transform.Find("Rail_GEO").GetComponent("RailMeshScript")).rail_piece = rail;

        rail.collisionMesh = railObj;
    }

    IntersectionNode CreateIntersection(RailPiece rail, Vector3 point)
    {
        IntersectionNode split = new IntersectionNode(point);
        intersections.Add(split);

        if (rail.start.neighbors.Count <= 1)
            intersections.Remove(rail.start);

        if (rail.end.neighbors.Count <= 1)
            intersections.Remove(rail.end);

        CreateRail(rail.start, split);
        intersections.Add(rail.start);
        
        CreateRail(split, rail.end);
        intersections.Add(rail.end);

        rail.end.neighbors.Remove(rail);
        rail.start.neighbors.Remove(rail);
        
        Destroy(rail.collisionMesh);
        rail_pieces.Remove(rail);

        return split;
    }

    void OnDrawGizmos()
    {
        if (t.Count > 0)
        {
            foreach (Train train in t)
            {
                train.OnDrawGizmos();
            }
        }

        if (snap_point != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(snap_point.position, new Vector3(0.5f, 0.5f, 0.5f));
        }

        if (draw_previz_point)
        {
            if (active_tool == "signal")
                Gizmos.color = Color.blue;
            else if (active_tool == "road")
                Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(previz_point, new Vector3(0.5f, 0.5f, 0.5f));
        }
        foreach (RailPiece rail in rail_pieces)
        {
            //rail.DebugDraw();
            if (rail.signals_list.Count > 0)
            {
                foreach (Signal currSignal in rail.signals_list)
                {
                    currSignal.OnDrawGizmos();
                }
            }
        }
    }

    void DebugDraw()
    {
        rail_pieces.ForEach(rail_piece => rail_piece.DebugDraw());

        if (t.Count > 0)
        {
            foreach (Train train in t)
            {
                train.DebugDraw();
            }
        }

        if (snap_point != null)
        {
            snap_point.neighbors.ForEach(rail_piece => rail_piece.DebugDraw(Color.red));
        }
    }

    void OnGUI()
    {
        if (t.Count > 0)
        {
            foreach (Train train in t)
            {
                if (GUI.Button(new Rect(Screen.width-140, 250 + lHeight, bWidth, bHeight), "Train"))
                {
                    active_tool = "route";
                    selectedTrain = train;
                    Debug.Log("Selected train: " + selectedTrain);
                    current_train_node = null;
                }
                lHeight += bHeight;
            }
        }
        lHeight = 0;
    }

    Vector3 ClosestPointOnLine(Vector3 p1, Vector3 p2, Vector3 target_point) {
        Vector3 vVector1 = target_point - p1;
        Vector3 vVector2 = (p2 - p1).normalized;

        float d = Vector3.Distance(p1, p2);
        float t = Vector3.Dot(vVector2, vVector1);

        if (t <= 0)
        {
            return p1;
        }

        if (t >= d)
        {
            return p2;
        }

        Vector3 vVector3 = vVector2 * t;
        Vector3 vClosestPoint = p1 + vVector3;
        return vClosestPoint;
   }
}

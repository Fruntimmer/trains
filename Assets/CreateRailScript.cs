using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class CreateRailScript : MonoBehaviour {
    public GameObject pRail;


    IntersectionNode current_node = null;
    IntersectionNode current_train_node = null;
    IntersectionNode snapPoint = null;

    List<Train> t = new List<Train>();
    List<RailPiece> rail_pieces = new List<RailPiece>();
    List<IntersectionNode> intersections = new List<IntersectionNode>();
    
    
    string active_tool = "road";
    Vector3 signal_point = Vector3.zero;
    bool draw_signal_point = false;

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
        int terrain_mask = 1 << 8;
        snapPoint = null;

        if(Physics.Raycast(ray, out hit, Mathf.Infinity, terrain_mask))
        {
            IntersectionNode next_node = null;
            IntersectionNode next_train_node = null;
            List<IntersectionNode> closestIntersections = null;
            if (intersections.Count > 0) {
                closestIntersections = new List<IntersectionNode>(intersections);
                closestIntersections.Sort((left, right) => Vector3.Distance(left.position, hit.point) < Vector3.Distance(right.position, hit.point) ? -1 : 1);

                if (Vector3.Distance(closestIntersections[0].position, hit.point) < 0.7f)
                {

                    next_node = closestIntersections[0];
                    snapPoint = next_node;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (active_tool == "road")
                {
                    if (current_node == null)
                    {
                        if (closestIntersections != null)
                        {
                            if (Vector3.Distance(closestIntersections[0].position, hit.point) < 0.7f)
                            {
                                Debug.Log("Clicked a previous node!");
                                current_node = closestIntersections[0];
                            }
                            else
                            {
                                current_node = new IntersectionNode(hit.point);
                                intersections.Add(current_node);
                            }
                        }
                        else
                        {
                            current_node = new IntersectionNode(hit.point);
                            intersections.Add(current_node);
                        }
                    }
                    else
                    {
                        if (next_node == null)
                        {
                            next_node = new IntersectionNode(hit.point);
                        }

                        intersections.Add(next_node);
                        RailPiece rail = new RailPiece(current_node, next_node);
                        rail_pieces.Add(rail);
                        current_node = next_node;

                        GameObject railObj = (GameObject) Instantiate(pRail, rail.start.position, Quaternion.identity);
                        railObj.transform.LookAt(rail.end.position);
                        railObj.transform.localScale = new Vector3(railObj.transform.localScale.x, .1f, rail.getLength());
                        //Debug.Log((RailMeshScript)railObj.GetComponent("RailMeshScript"));
                        ((RailMeshScript) railObj.transform.Find("Rail_GEO").GetComponent("RailMeshScript")).rail_piece
                            = rail;
                    }                  
                }
                if (active_tool == "train" && closestIntersections != null)
                {
                    Debug.Log("Train Click!");
                    if (current_train_node == null)
                    {
                        current_train_node = closestIntersections[0];
                    }
                    else
                    {
                        next_train_node = closestIntersections[0];

                        //Create train and assign path
                        AStar astar = new AStar();
                        List<RailPiece> shortestPath = astar.GetPath(current_train_node, next_train_node);

                        t.Add(new Train(shortestPath[0], current_train_node));
                        t[t.Count - 1].path = shortestPath;

                        next_train_node = null;
                        current_train_node = null;
                    }
                }
                //Reroute existing train

                if(active_tool == "route")
                {
                    if(current_train_node == null)
                    {
                        current_train_node = closestIntersections[0];
                    }
                    else
                    {
                        next_train_node = closestIntersections[0];
                        
                        AStar astar = new AStar();
                        List<RailPiece> shortestPath = astar.GetPath(current_train_node, next_train_node);

                        selectedTrain.path = shortestPath;
                    }
                }
            }
        }
        int rail_layerMask = 1 << 9;
        draw_signal_point = false;
        if(active_tool == "signal" && Physics.Raycast(ray, out hit, Mathf.Infinity, rail_layerMask))
        {
            RailPiece rail_piece = ((RailMeshScript)hit.collider.gameObject.GetComponent("RailMeshScript")).rail_piece;
            signal_point = ClosestPointOnLine(rail_piece.start.position, rail_piece.end.position, hit.point);
            draw_signal_point = true;
            
            if(Input.GetMouseButtonDown(0))
            {
                rail_piece.signalsList.Add(new Signal(signal_point));
                Debug.Log("Created Signal");
            }
        }

        if (Input.GetKeyDown(KeyCode.T) && intersections.Count > 2)
        {
            Debug.Log("Train mode!");
            active_tool = "train";
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            current_node = null;
            active_tool = "road";
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
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

    void OnDrawGizmos()
    {
        if (t.Count > 0)
        {
            foreach (Train train in t)
            {
                train.OnDrawGizmos();
            }
        }

        if (snapPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(snapPoint.position, new Vector3(0.5f, 0.5f, 0.5f));
        }

        if (draw_signal_point)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(signal_point, new Vector3(0.5f, 0.5f, 0.5f));
        }
        foreach (RailPiece rail in rail_pieces)
        {
            //rail.DebugDraw();
            if (rail.signalsList.Count > 0)
            {
                foreach (Signal currSignal in rail.signalsList)
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

        if (snapPoint != null)
        {
            snapPoint.neighbors.ForEach(rail_piece => rail_piece.DebugDraw(Color.red));
        }
    }

    void OnGUI()
    {
        if (t.Count > 0)
        {
            foreach (Train train in t)
            {
                if (GUI.Button(new Rect(750, 250 + lHeight, bWidth, bHeight), "Train"))
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

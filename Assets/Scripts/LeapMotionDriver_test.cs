using UnityEngine;
using System.Collections;
using Leap;
using System;

public class LeapMotionDriver_test : MonoBehaviour {

    public GameObject Drone;
    public float PalmPadding = 30f;
    public float TurnPadding = 100f;
    public float scale = 0.01f;
    public float TurnScale = 0.005f;

    // init variables
    private Controller LeapController;
    private DroneMovement DroneScript;
    private Frame currFrame;
    private HandList Hands;
    private Hand hand;

    
    // used for boundaries
    private Vector3 PalmStartPosition;
    private float L_LIMIT; // left
    private float R_LIMIT; // right
    private float F_LIMIT; // front
    private float B_LIMIT; // back
    private float U_LIMIT; // up
    private float D_LIMIT; // down

    private float POS_ROTATION_LIMIT = .55f; //Clockwise 
    private float NEG_ROTATION_LIMIT = -.55f; //Counter-Clockwise 

    private float StartHoverHeight;

    private Vector3 PalmPosition;
    private Vector3 DirectionVector;
    private float TurnDegree;


    //used for stablization calculation
    public float time_start;

    public bool Stablization_start;

    // Use this for initialization
    void Start () {
        LeapController = new Controller();
        DroneScript = Drone.GetComponent<DroneMovement>();
        print(DroneScript);
        StartHoverHeight = DroneScript.HoverHeight;

        if (LeapController.IsConnected)
        {
            print("Leap Motion Detected!");
        } else
        {
            print("Leap Motion Not Detected!");
        }

    }
	
	// Update is called once per frame
	void Update () {
        currFrame = LeapController.Frame();
       
        // check that there is only one hand
        Hands = currFrame.Hands;

        //Stablize the Drone
        
        if (DroneScript.Deactivated)
        {
            return;
        }



        if (Hands.Count > 1)
        {



            if(Hands[0].GrabStrength > .9f && Hands[1].GrabStrength > .9f)
            {
                print("stablizing");

                if (!Stablization_start)
                {
                    time_start = Time.time;
                    Stablization_start = true;
                }

                if(Time.time - time_start > 5 && Stablization_start)
                {
                    print("deactivated");
                    DroneScript.Deactivated = true;
                    DroneScript.ZeroOut();
                }


            }


            DroneScript.ZeroOut();
            return;
        }

        Stablization_start = false; 

        print(Hands[0].GrabStrength);
        float[] pPos = Hands[0].PalmPosition.ToFloatArray();
        
        // current palm position
        PalmPosition = new Vector3(pPos[0], pPos[1], pPos[2]);

        // the save initial palm position, used for the base point for 
        // "comfortable" steering
        if (PalmStartPosition == Vector3.zero && PalmPosition != Vector3.zero)
        {
            // PalmStartPosition = PalmPosition;
            PalmStartPosition = Vector3.zero;
            // z compensate
            float zcomp = 40f;
            L_LIMIT = PalmStartPosition.x - PalmPadding;
            R_LIMIT = PalmStartPosition.x + PalmPadding;
            F_LIMIT = PalmStartPosition.z - PalmPadding;
            B_LIMIT = PalmStartPosition.z + PalmPadding;
            U_LIMIT = PalmStartPosition.y + PalmPadding;
            D_LIMIT = PalmStartPosition.y - PalmPadding;
        }
        DirectionVector = getDirectionFromPalm(PalmPosition) * scale;
        //print(DirectionVector);
       // print(Hands[0].PalmNormal);


    }

    void FixedUpdate()
    {
        if (DroneScript != null)
        {
            DroneScript.MoveDirection(DirectionVector);
            DroneScript.TurnDegree(TurnDegree);
            
        }
        
    }

    // check the current palm position vector, 
    // if out of bounds get the direction vector from difference
    private Vector3 getDirectionFromPalm(Vector3 position)
    {
        float px, py, pz;
        float dx = 0f, dy = 0f, dz = 0f;
        px = position.x;
        py = position.y;
        pz = position.z;



        // check all bounds
        if (px < L_LIMIT || px > R_LIMIT)
        {
            if (px < L_LIMIT)
            {
                dx = px - L_LIMIT;
            }
            else
            {
                dx = px - R_LIMIT;
            }

        }

        if (pz < F_LIMIT || pz > B_LIMIT)
        {
            if (pz < F_LIMIT)
            {

                dz = pz - F_LIMIT;
            } else
            {

                dz = pz - B_LIMIT;
            }
        }

        if (py < D_LIMIT || py > U_LIMIT)
        {
            if (py < D_LIMIT)
            {
                //print(D_LIMIT);
                float offset = py - D_LIMIT;
                if (DroneScript.HoverHeight + offset >= StartHoverHeight)
                {
                    DroneScript.SetHoverHeight(DroneScript.HoverHeight + offset);
                }
                
            }
            else
            {
                float offset = py - U_LIMIT;
                DroneScript.SetHoverHeight(StartHoverHeight + 2.0f*offset);
            }
        }

        if (py - U_LIMIT > 0)
        {
            float offset = py - U_LIMIT;
            DroneScript.SetHoverHeight(StartHoverHeight + offset);
        }

        //print(Hands[0].PalmNormal);
        //Palm Orientation Rotation
        if (Hands[0].PalmNormal.x > POS_ROTATION_LIMIT)
        {
            print("turn clock");
            TurnDegree = (Hands[0].PalmNormal.x - POS_ROTATION_LIMIT)*TurnPadding/2;
        }
        else if (Hands[0].PalmNormal.x < NEG_ROTATION_LIMIT)
         {
            print("turn counter");

            TurnDegree = (Hands[0].PalmNormal.x - NEG_ROTATION_LIMIT)*TurnPadding/2;
        }
        else
        {
            TurnDegree = 0f; 
        }

        // axis reversed in leap from unity
        Vector3 vec = new Vector3(dx, dy, -dz);
        print(Hands[0].PalmNormal.x);

        return new Vector3(dx, dy, -dz);
    }
}

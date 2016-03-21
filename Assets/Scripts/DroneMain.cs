using UnityEngine;
using System.Collections;

public class DroneMain : MonoBehaviour {

    /* Public : Set by the user */
    public float DroneMaxSpeed = 120f;
    public float InitialHoverHeight;

    /* Private */
    private Rigidbody DroneRigidBody;
    
    // Drone States

    private enum DroneState
    {
        TurnedOff = -1,
        Landed = 0,
        TakingOff = 1,
        Landing = 2,
        Hovering = 3,
        Moving = 4
    }

    private DroneState CurrentState; // current state of the drone

    // These variables are set by the API calls
    // and used by the calls in the Update() loop
    // to actually apply the physics simulations

    private float CurrentHoverHeight; // current height of drone
    private Vector3 CurrentMoveDirection; // vector to move in
    private float CurrentTurnDirection; // direction of turn

    // numbers tweaked for physics
    private float HoverForce = 120f; // force applied to hover
    private float MinHoverHeight = 5f;
    private float MovementSpeedScale = 120f;
    private float DecelerationRate = 1.3f; // velocity /= rate
    private float TurnSpeed = 50f; // how fast does the drone turn


    // Use this for initialization
    void Awake () {
        DroneRigidBody = GetComponent<Rigidbody>();

        CurrentHoverHeight = 0f;
        CurrentMoveDirection = Vector3.zero;
        CurrentTurnDirection = 0f;

        CurrentState = DroneState.TurnedOff;
	}

    // If we are in transitional states, just carry through
    void Update()
    {
        if (isLanding())
        {
            Land();
        }
        if (isTakingOff())
        {
            TakeOff();
        }
    }


    // FixedUpdate applies all the forces according to set variables
    // If everything is set to 0 should do nothing
    void FixedUpdate () {
        if (!isTurnedOff())
        {
            Hover();
            MoveDirection(CurrentMoveDirection);
            Turn(CurrentTurnDirection);
        }
        
	}

    /* ###### Private Methods for Drone Physics ##### */

    // Controls the hover force of the drone downwards up to
    // CurrentHoverHeight. The setting of this variable is done separately
    void Hover()
    {
        // if not set, just do nothing
        if (CurrentHoverHeight <= 0f)
        {
            return;
        }
        float proportionalHeight;
        Vector3 appliedHoverForce;
        // ray cast down
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        // If we hit something within current hover height
        // get how much force we need from proportion
        if (Physics.Raycast(ray, out hit, CurrentHoverHeight))
        {
            proportionalHeight = (CurrentHoverHeight - hit.distance) / CurrentHoverHeight;
            appliedHoverForce = Vector3.up * proportionalHeight * HoverForce;
            DroneRigidBody.AddForce(appliedHoverForce, ForceMode.Acceleration);
        }
        else
        {
            proportionalHeight = (transform.position.y - CurrentHoverHeight) / CurrentHoverHeight;

            float DownForce = (transform.position.y - MinHoverHeight) / MinHoverHeight;
            appliedHoverForce = Vector3.down * proportionalHeight * HoverForce * DownForce;

            if (transform.position.y <= MinHoverHeight)
            {
                DroneRigidBody.velocity = Vector3.zero;
            } else
            {
                DroneRigidBody.AddForce(appliedHoverForce);
            }

        }
    }
    
    // Move drone in DIRECTION, a normalized unit vector
    void MoveDirection(Vector3 direction)
    {
        if (direction == Vector3.zero)
        {
            DroneRigidBody.velocity /= DecelerationRate;
        }
        else 
        {
            float totalSpeed = DroneMaxSpeed * MovementSpeedScale * Time.deltaTime;

            // we don't want any force applied to up or down, rather adjust CurrentHeight
            CurrentHoverHeight += direction.y;
            if (CurrentHoverHeight <= MinHoverHeight)
            {
                CurrentHoverHeight = MinHoverHeight;
            }
            // get rid of up/down before applying, renormalize
            direction.y = 0f;
            direction.Normalize();
            Vector3 directionForce = direction * totalSpeed;
            DroneRigidBody.AddRelativeForce(directionForce);

            if (DroneRigidBody.velocity.magnitude > DroneMaxSpeed)
            {
                DroneRigidBody.velocity = DroneRigidBody.velocity * DroneMaxSpeed / DroneRigidBody.velocity.magnitude;
            }
        }

    }

    // Rotates the drone CW or CCW
    // CW : +1, CCW : -1, None: 0
    void Turn(float clockwise)
    {
        float turn = clockwise * TurnSpeed * Time.deltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        DroneRigidBody.MoveRotation(DroneRigidBody.rotation * turnRotation);
    }


    /* ######### Public Interface for Drone Controllers ####### 
     * 
     * The relationship between these calls and how they're implemented privately
     * should be abstracted away from each other.
     */

    public void TakeOff()
    {
        
        // Currently Landed ior TurnedOff, transition to TakingOff
        if (HasLanded())
        {
            print("CALLED TAKING OFF");
            CurrentHoverHeight += 1f;
            CurrentState = DroneState.TakingOff;
        }
        // If we are taking off, set CurrentHeight to InitialHoverHeight
        // Update() loop will apply actual force
        else if (isTakingOff() && CurrentHoverHeight < InitialHoverHeight)
        {
            print("Taking off still......");
            CurrentHoverHeight += 5f * Time.deltaTime;
        } 
        else if (isTakingOff())
        {
            CurrentState = DroneState.Hovering;
            print("FINISHED TAKEOFF!");
        }
    }

    public void Land()
    {

        if (InFlight())
        {
            print("CALLED LAND");

            CurrentState = DroneState.Landing;
            DroneRigidBody.velocity = Vector3.zero;
        }
        // until just above the ground, 
        else if (isLanding() && CurrentHoverHeight > 1f)
        {
            print("Landing....");
            CurrentHoverHeight -= 5f * Time.deltaTime;
            print(CurrentHoverHeight);



        }
        else if (isLanding())
        {
            CurrentHoverHeight = 0f;
            CurrentState = DroneState.Landed;
            print("FINISHED LANDING!");
        }
    }

    public void TurnOn()
    {
        print("CALLED TURN ON");
        CurrentState = DroneState.Landed;
    }

    public void TurnOff()
    {
        CurrentState = DroneState.TurnedOff;
        CurrentHoverHeight = 0f;
        CurrentMoveDirection = Vector3.zero;
        CurrentTurnDirection = 0f;
        print("CALLED TURN OFF");
    }

    public void Stabilize()
    {
        CurrentMoveDirection = Vector3.zero;
        CurrentTurnDirection = 0f;
    }

    public void Move(Vector3 direction)
    {
        CurrentMoveDirection = direction;
    }

    public void Rotate(float clockwise)
    {
        CurrentTurnDirection = clockwise;
    }

    public void ComeBackHome()
    {
        print("CALLED COME BACK HOME");
    }
    // Is the drone in any state of flight?
    public bool InFlight()
    {
        return ((int)CurrentState > 2);
    }

    // Is the drone currently landed?
    public bool HasLanded()
    {
        return CurrentState == DroneState.Landed;
    }


    public bool isTurnedOff()
    {
        return CurrentState == DroneState.TurnedOff;
    }

    public bool isTakingOff()
    {
        return CurrentState == DroneState.TakingOff;
    }

    public bool isLanding()
    {
        return CurrentState == DroneState.Landing;
    }
    
     void OnTriggerEnter(Collider col)
    {
        print("COLLISION");

        if (col.gameObject.tag == "Building")
        {
            print("COLLISION!");
            TurnOff();
            DroneRigidBody.velocity = new Vector3(0, 0, 0);
        }
    }
}

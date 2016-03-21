using UnityEngine;
using System.Collections;
using Leap;
using System;

public class LeapMotionDriver : MonoBehaviour
{
    /* Public Variables */
    public GameObject Drone; // Drone object to control
    public float SafeZoneSize; // Size of safe zone buffer relative to origin
    public float RotationLimit; // rotation bound relative to normal vector (0.5 = 45deg)
    public float TimeToSync; // time required to transition between sync/unsync


    /* Private */
    private bool SyncedWithDrone; // is the leap synced with the drone controls?
    private Vector3 PalmPosition; // main palm position
    private Vector3 PalmPosition2; // for two hands
    private Hand Hand; // main hand
    private Hand Hand2; // for two hands
    private float SyncUnSynctimer; // for sync/unsync


    // Bookkeeping variables
    private Controller LeapController;
    private DroneMain DroneScript;
    private Frame LeapFrame;
    private HandList Hands;

    // used for safe zone boundaries shaped like a box
    private float L_LIMIT; // left
    private float R_LIMIT; // right
    private float F_LIMIT; // front
    private float B_LIMIT; // back
    private float U_LIMIT; // up
    private float D_LIMIT; // down


    // Initialization
    void Awake()
    {
        // Setup Leap
        LeapController = new Controller();

        // Link Up DroneScript
        DroneScript = Drone.GetComponent<DroneMain>();

        //Enable recognition of finger twirl gesture 
        LeapController.EnableGesture(Gesture.GestureType.TYPE_CIRCLE);

        // original of the leap, used for setting up safezone
        Vector3 origin = new Vector3(0f, 130f, 0f);
        InitSafeZone(origin, SafeZoneSize);

        // TODO: set to false
        SyncedWithDrone = true;
        SyncUnSynctimer = 0f;

        if (LeapController.IsConnected)
        {
            print("Leap Motion Detected!");
        } else
        {
            print("Leap Motion Not Detected!");
        }

    }

    // Main Update Loop, all gesture recognition happens here
    void Update()
    {
        // if the drone is in any critical states, TakingOff, Landing
        // Don't do anything this frame
        if (DroneScript.isTakingOff() || DroneScript.isLanding())
        {
            return;
        }
        
        // scan hands update hand variables
        ScanHands();

        // two hand motions have precedence
        if (Hands.Count > 1)
        {
            // reset sync timer
            SyncUnSynctimer = 0f;

            if (DroneScript.isTurnedOff() && !TwoFists())
            {
                DroneScript.TurnOn();
            }
            if (TwoPalmsUp())
            {
                DroneScript.TakeOff();
            }
            if (TwoPalmsDown())
            {
                DroneScript.Land();
            }
            if (TwoFists() && !DroneScript.isTurnedOff())
            {
                DroneScript.TurnOff();
            }
        }
        // One Handed gestures, only when turned on
        else if (Hands.Count == 1)
        {

         
            //Checks whether gesture is a finger twirl 
          //  if (FingerTwirl(LeapFrame.Gestures()))
          //  {

         //       print("twirling");
         //       DroneScript.ComeBackHome();
         //   }

            // If synced with drones, and is in flight
            if (IsSynced() && DroneScript.InFlight())
            {
                // if in safe zone, or has a fist, stabilize
                if (InSafeZone() || OneFist())
                {
                    
                    DroneScript.Stabilize();
                    // if both, try UnSync()
                    if (InSafeZone() && OneFist())
                    {
                        UnSync();
                    }
                }
                // If just normal hand movement
                // move, rotate
                else
                {
                    // reset sync/unsync timer
                    SyncUnSynctimer = 0f;

                    // Get Direction from origin (safe zone)
                    Vector3 direction = GetDirectionVector();

                    // Get Wrist Rotation
                    float rotation = GetRotationDirection();

                    // Apply direction and rotation 
                    DroneScript.Move(direction);
                    DroneScript.Rotate(rotation);
                }
            }
            // Not Synced with drone
            else
            {
                if (InSafeZone() && OneFist())
                {
                    Sync();
                }
            }
        }
        // No hands, just do nothing for now
        else
        {
            // reset sync timer
            SyncUnSynctimer = 0f;
        }
    }



    /*######## Helper Functions #######*/

    // Initialize the SafeZone as +- size buffer in all directions
    void InitSafeZone(Vector3 origin, float size)
    {
        L_LIMIT = origin.x - size;
        R_LIMIT = origin.x + size;
        F_LIMIT = origin.z - size;
        B_LIMIT = origin.z + size;
        U_LIMIT = origin.y + size;
        D_LIMIT = origin.y - size;

    }

    // Scan all Hands in this frame, update PalmPosition fields
    void ScanHands()
    {
        LeapFrame = LeapController.Frame();
        Hands = LeapFrame.Hands;
        if (Hands.Count > 0)
        {
            Hand = Hands[0];
            PalmPosition = GetPalmPosition(Hand);
        }
        if (Hands.Count > 1)
        {
            Hand2 = Hands[1];
            PalmPosition = GetPalmPosition(Hand2);
        }
    }



    // Given HAND return PalmPosition
    Vector3 GetPalmPosition(Hand hand)
    {
        float[] positions = hand.PalmPosition.ToFloatArray();
        return new Vector3(positions[0], positions[1], positions[2]);
    }


    // Returns Palm position relative from safe zone,
    // normalized to a unit vector
    Vector3 GetDirectionVector()
    {
        
        float currX, currY, currZ, dirX, dirY, dirZ;
        currX = PalmPosition.x;
        currY = PalmPosition.y;
        currZ = PalmPosition.z;

        dirX = checkBounds(L_LIMIT, R_LIMIT, currX);
        dirY = checkBounds(D_LIMIT, U_LIMIT, currY);
        dirZ = checkBounds(F_LIMIT, B_LIMIT, currZ);

        // need to reverse z because unity and leap have different coord system
        Vector3 direction = new Vector3(dirX, dirY, -dirZ);
        direction.Normalize();
        return direction;
    }

    // check if CURR is beyond UPPER and LOWER
    float checkBounds(float lower, float upper, float curr) 
    {
        float result = 0f;
        if (curr < lower || curr > upper)
        {
            if (curr < lower)
            {
                result = curr - lower;
            }
            else 
            {
                result = curr - upper;
            }
        }
        return result;
    }



    // Returns the rotation direction
    // +1 for clockwise, -1 for counter-clockwise, 0 for no rotation
    float GetRotationDirection()
    {
        float normX, result = 0f;
        normX = Hand.PalmNormal.x;
        if (normX > RotationLimit)
        {
            result = -1f;
        }
        if (normX < -RotationLimit)
        {
            result = 1f;
        }

        return result;
    }
    // If not Synced, being here for 5 consecutive seconds
    // will "Sync",
    // else "UnSync"
    bool InSafeZone()
    {
        float dx, dy, dz, cx, cy, cz;
        bool safe = false;

        cx = PalmPosition.x;
        cy = PalmPosition.y;
        cz = PalmPosition.z;

        dx = checkBounds(L_LIMIT, R_LIMIT, cx);
        dy = checkBounds(D_LIMIT, U_LIMIT, cy);
        dz = checkBounds(F_LIMIT, B_LIMIT, cz);

        if (dx == 0f && dy == 0f && dz == 0f)
        {
            safe = true;
        }

        if (safe)
        {
            //print("IN SAFE ZONE");
        }

        return safe;
    }

    bool IsSynced()
    {
        return SyncedWithDrone;
    }

    // This function will increment timestamp for SyncUnsyncTimer
    // check if past sync time, will set SyncedWithDrone to true
    void Sync()
    {
        if (!SyncedWithDrone)
        {
            SyncUnSynctimer += Time.deltaTime;
            print("SYNCING...");
            if (SyncUnSynctimer >= TimeToSync)
            {
                SyncedWithDrone = true;
                SyncUnSynctimer = 0f;
                print("SYNCED WITH DRONE!");
            }
        }
    }

    void UnSync()
    {
        if (SyncedWithDrone)
        {
            SyncUnSynctimer += Time.deltaTime;
            print("UNSYNCING...");
            if (SyncUnSynctimer >= TimeToSync)
            {
                SyncedWithDrone = false;
                SyncUnSynctimer = 0f;
                print("UNSYNCED WITH DRONE!");
            }
        }
    }
    
    



    /*####### Gesture Recognition Functions ########*/

    // For Palms use normal.y
    bool TwoPalmsUp()
    {
        float normY1, normY2;
        normY1 = Hand.PalmNormal.y;
        normY2 = Hand2.PalmNormal.y;
        if (normY1 > 0.85f && normY2 > 0.85f)
        {
            return true;
        }

        return false;
    }

    bool TwoPalmsDown()
    {
        float normY1, normY2;
        normY1 = Hand.PalmNormal.y;
        normY2 = Hand2.PalmNormal.y;
        if (normY1 < -0.85f && normY2 < -0.85f)
        {
            return true;
        }
        return false;
    }

    bool TwoFists()
    {
        float grab1, grab2;
        grab1 = Hand.GrabStrength;
        grab2 = Hand2.GrabStrength;
        if (grab1 > 0.9f && grab2 > 0.9f)
        {
            return true;
        }
        return false;
    }

    bool OneFist()
    {
        if (Hand.GrabStrength > 0.9f)
        {
            return true;
        }
        return false;
    }


    // TODO: Bind this to ComeBackHome()
    bool FingerTwirl(GestureList trackedGesture)
    {
        return trackedGesture.Count == 1 && trackedGesture[0].Type == Gesture.GestureType.TYPE_CIRCLE;
    }
}
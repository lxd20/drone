using UnityEngine;
using System.Collections;
using Leap;
using System;


public class LeapHandController : MonoBehaviour
{
    public GameObject Palm;
    Controller LEAPcontroller;
    Frame frame;
    HandList Hands;
    Hand hand;
    Vector3 PalmPosition;
    //HandController handcontroller;
    Rigidbody rb;

    void Start()
    {
        LEAPcontroller = new Controller();
        //handcontroller = new HandController();
        rb = Palm.gameObject.GetComponent<Rigidbody>();
        if (LEAPcontroller.IsConnected)
        {
            Debug.Log("LEAP connected!");
        }
        else
        {
            Debug.Log("LEAP is NOT connected!");
        }

        LEAPcontroller.EnableGesture(Gesture.GestureType.TYPE_CIRCLE);
        LEAPcontroller.EnableGesture(Gesture.GestureType.TYPE_KEY_TAP);
        LEAPcontroller.EnableGesture(Gesture.GestureType.TYPE_SCREEN_TAP);
        LEAPcontroller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);
    }

    void Update()
    {


        frame = LEAPcontroller.Frame();


        Hands = frame.Hands;
        if (Hands.Count == 0 || Hands.Count > 1)
        {
            return;
        }

        //Vector3 unityPos = Hands[0].PalmPosition.ToUnityScaled(false);
        // Vector3 worldPos = handcontroller.transform.TransformPoint(unityPos);

        // Palm.gameObject.transform.position = worldPos;

        //https://developer.leapmotion.com/documentation/unity/api/Leap_Classes.html?proglang=unity
        float[] arr = Hands[0].PalmPosition.ToFloatArray();
        PalmPosition = new Vector3(arr[0] / 100, arr[1] / 100, -arr[2] / 100 - 8);

        Debug.Log("Palm Position: " + PalmPosition);
        if (-arr[2] / 100 - 8 > -7.0)
        {

            if (arr[0] / 100 < -1.3)
            {
                Debug.Log("Forced Appliedx");
                rb.velocity = new Vector3(-40, 0, 10);
            }

            else if (arr[0] / 100 > 1.1)
            {
                Debug.Log("Forced Reversedx");
                rb.velocity = new Vector3(40, 0, 10);

            }
            else
            {
                Debug.Log("Nullx");

                rb.velocity = new Vector3(0, 0, 40);
            }
        }

        else if (-arr[2] / 100 - 8 < -8.8)
        {

            if (arr[0] / 100 < -1.3)
            {
                Debug.Log("Forced Appliedx");
                rb.velocity = new Vector3(-40, 0, -40);
            }

            else if (arr[0] / 100 > 1.1)
            {
                Debug.Log("Forced Reversedx");
                rb.velocity = new Vector3(40, 0, -40);

            }
            else
            {
                Debug.Log("Nullx");

                rb.velocity = new Vector3(0, 0, -40);
            }

        }
        else
        {

            if (arr[0] / 100 < -1.3)
            {
                Debug.Log("Forced Appliedx");
                rb.velocity = new Vector3(-40, 0, 0);
            }

            else if (arr[0] / 100 > 1.1)
            {
                Debug.Log("Forced Reversedx");
                rb.velocity = new Vector3(40, 0, 0);

            }
            else
            {
                Debug.Log("Nullx");

                rb.velocity = Vector3.zero;
            }
        }





        //Palm.gameObject.transform.position = PalmPosition;

        GestureList gesturesInFrame = frame.Gestures();
        if (!gesturesInFrame.IsEmpty)
        {
            // Debug.Log(gesturesInFrame[0].Type);


            foreach (Gesture gesture in gesturesInFrame)
            {
                if (gesture.Type == Gesture.GestureType.TYPE_CIRCLE)
                {
                    Debug.Log(gesturesInFrame[0].Type);
                    Debug.Log(Hands[0].PalmPosition);
                    // float[] arr = Hands[0].PalmPosition.ToFloatArray();
                    // Debug.Log(PalmPosition);

                    // PalmPosition = new Vector3(arr[0]/100, arr[1]/100, -arr[2]/100-8);
                    // Debug.Log(PalmPosition);


                    // Palm.gameObject.transform.position = PalmPosition;

                }
            }




        }
        //HandModel handModel = GetComponent<HandModel>();
        //PalmPosition  = handModel.transform.TransformPoint(handModel.GetLeapHand().Fingers[0].TipPosition.ToUnityScaled());

        //PalmPosition = Hands[0].PalmPosition.ToUnityScaled();
        //Palm.gameObject.transform.up = PalmPosition*1*Time.deltaTime;
    }
}

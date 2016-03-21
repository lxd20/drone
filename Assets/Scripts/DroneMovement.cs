using UnityEngine;
using System.Collections;
using System;
public class DroneMovement : MonoBehaviour
{

    public float Speed;
    public float TurnSpeed;
    public float HoverForce;
    public float HoverHeight;
    public float MaxSpeed;



    public float PowerInput;
    public float TurnInput;
    private Rigidbody DroneRigidBody;
    public Boolean Deactivated;



    // Use this for initialization
    void Awake()
    {
        DroneRigidBody = GetComponent<Rigidbody>();
        MaxSpeed = 120f;
    }

    // Update is called once per frame
    void Update()
    {
        PowerInput = Input.GetAxis("Vertical");
        TurnInput = Input.GetAxis("Horizontal");
    }

    // apply the changes
    private void FixedUpdate()
    {
        // Move and turn the drone.
        if (!Deactivated)
        {
            Hover();
            Move();
            Turn();
        }
    }

    public void ZeroOut()
    {

        DroneRigidBody.velocity = Vector3.zero;
        //DroneRigidBody.rotation = Quaternion.Euler(0f, 0f, 0f);

        //if (DroneRigidBody.rotation != Quaternion.Euler(0f, 0f, 0f))
        // {
        //     Quaternion turnRotation = Quaternion.Euler(0f, -DroneRigidBody.rotation.y/4f, 0f);

        //     DroneRigidBody.MoveRotation(DroneRigidBody.rotation * turnRotation);

        // }
    }


    private void Hover()
    {

        Ray ray = new Ray(transform.position, -Vector3.up);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, HoverHeight))
        {

            float proportionalHeight = (HoverHeight - hit.distance) / HoverHeight;
            Vector3 appliedHoverForce = Vector3.up * proportionalHeight * HoverForce;
            DroneRigidBody.AddForce(appliedHoverForce, ForceMode.Acceleration);
        }
        else
        {
            float proportionalHeight = (transform.position.y - HoverHeight) / (20 * HoverHeight);
            Vector3 appliedHoverForce = Vector3.down * proportionalHeight * HoverForce;
            if (appliedHoverForce.z < 0.1)
            {
                DroneRigidBody.AddForce(appliedHoverForce, ForceMode.Acceleration);
            }
            DroneRigidBody.MovePosition(DroneRigidBody.position + appliedHoverForce);
        }
    }

    // method to move the drone
    public void MoveDirection(Vector3 direction)
    {
        Vector3 movement = direction * Speed * Time.deltaTime;
        //DroneRigidBody.MovePosition(DroneRigidBody.position + movement);
        DroneRigidBody.AddRelativeForce(movement);
        if (movement == Vector3.zero)
        {
            DroneRigidBody.velocity /= 1.1f;
        }

        //implement max speed of drone
        if (DroneRigidBody.velocity.magnitude > MaxSpeed)
        {
            DroneRigidBody.velocity = DroneRigidBody.velocity * MaxSpeed / DroneRigidBody.velocity.magnitude;
        }

        //DroneRigidBody.velocity = movement;
    }

    public void TurnDegree(float degree)
    {
        float turn = degree * TurnSpeed * Time.deltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        DroneRigidBody.MoveRotation(DroneRigidBody.rotation * turnRotation);

    }

    // method to set the hover height
    public void SetHoverHeight(float newHeight)
    {
        HoverHeight = newHeight;
    }

    private void Move()
    {
        // Adjust the position of the drone based on the player's input
        Vector3 movement = transform.forward * PowerInput * Speed * Time.deltaTime;
        DroneRigidBody.MovePosition(DroneRigidBody.position + movement);

    }

    private void Turn()
    {
        // Adjust the rotation of the drone based on the player's input.
        float turn = TurnInput * TurnSpeed * Time.deltaTime;

        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        DroneRigidBody.MoveRotation(DroneRigidBody.rotation * turnRotation);

    }



}

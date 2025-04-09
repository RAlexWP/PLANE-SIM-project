using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plane_Control_Sys : MonoBehaviour
{
    private Vector3 Velocity, LocalVelocity, LocalAngularVelocity, lastVelocity, LocalGForce;
    private float AngleOfAttack, AngleOfAttackYaw;

    //plane properties to add force
    public float Throttle = 10.0f;
    public float maxThrust = 50.0f;



    /* Take measurements of planes local frame of reference */
    void CalculateState(float dt)   {
        var invRotation = Quaternion.Inverse(GetComponent<Rigidbody>().rotation);
        Velocity = GetComponent<Rigidbody>().velocity;

        // transform world velocity into local space
        LocalVelocity = invRotation * Velocity;

        // transform into local space
        LocalAngularVelocity = invRotation * GetComponent<Rigidbody>().angularVelocity; 
    }


    void CalculateAngleOfAttack()
    {
        if(LocalVelocity.sqrMagnitude < 0.1f) {
            AngleOfAttack = 0;
            AngleOfAttackYaw = 0;
            return;
        }

        /* We use Trigonometry to find the Angle Of Attack (AOA)
         * AngleOfAttack:
         * attack measured on the pitch axis
         * 
         * AngleOfAttackYaw:
         * Measured in the Yaw axis*/
        AngleOfAttack = Mathf.Atan2(-LocalVelocity.y, LocalVelocity.z);
        AngleOfAttackYaw = Mathf.Atan2(LocalVelocity.x, LocalVelocity.z);
    }

    /* The GForce is derived from the current and previous velocity*/
    void CalculateGForce(float dt)  {
        var invRotation = Quaternion.Inverse(GetComponent<Rigidbody>().rotation);
        var acceleration = (Velocity - lastVelocity) / dt;
        LocalGForce = invRotation * acceleration;
        lastVelocity = Velocity;
    }

    /* Thrust is the simplest force to implement, we set a thruttle 
     * value from zero to one and we multiply that by the max thrust*/
    void UpdateThrust() {
        GetComponent<Rigidbody>().AddRelativeForce(Throttle * maxThrust * Vector3.right /*Vector3.forward*/);
    }


    /* update loop. 
     * first step, measure the planes current state "CalculateState(dt)"*/
    void FixedUpdate() {
        float dt = Time.fixedDeltaTime;

        CalculateState(dt);
        CalculateAngleOfAttack();
        CalculateGForce(dt);

        if (Input.GetKey(KeyCode.Space))
        {
            Debug.Log("Space key was pressed.");
            UpdateThrust();

        }
    }
}

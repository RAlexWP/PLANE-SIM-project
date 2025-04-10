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

    //Drag Properties:
    public AnimationCurve dragRight, dragLeft,
            dragTop, dragBottom,
            dragForward, dragBack;

    public static Scale_Custom sc;


    /* update loop. 
    * first step, measure the planes current state "CalculateState(dt)"*/
    void FixedUpdate()
    {
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

    /* refereces:
     * 
     * drag equation:
     * https://en.wikipedia.org/wiki/Drag_equation
     * 
     * drag from real physics to unity linear game physics:
     * https://discussions.unity.com/t/how-drag-is-calculated-by-unity-engine/97622/3 
     * 
     * faking it in unity the mass density aka Air density (rho) and the reference area (A)
     * will be ignored and we just control the planes drag by the coefficient of drag (Cd)
     * 
     * The coefficient of drag depends on which way the plane is facing relative to the air 
     * flow */
    void UdateDrag() {
        var lv = LocalVelocity;
        var lv2 = lv.sqrMagnitude; //velocity squared

        //calculate coefficient of drag depending on direction on velocity
        var coefficient = sc.ScaleCustom(
            lv.normalized,
            dragRight.Evaluate(Mathf.Abs(lv.x)),    dragLeft.Evaluate(Mathf.Abs(lv.x)),
            dragTop.Evaluate(Mathf.Abs(lv.y)),      dragBottom.Evaluate(Mathf.Abs(lv.y)),
            dragForward.Evaluate(Mathf.Abs(lv.z)),  dragBack.Evaluate(Mathf.Abs(lv.z))
         );

        var drag = coefficient.magnitude * lv2 * -lv.normalized; //drag is oppsite direction of velocity
        GetComponent<Rigidbody>().AddRelativeForce(drag);
    }
}

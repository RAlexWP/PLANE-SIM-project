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

    //Breaks / Slow down booleans
    public bool AirbrakeDeployed, FlapsDeployed;
    public float airbrakeDrag, flapsDrag;

    //induced drag
    public int inducedDrag;

    //flaps properties
    public float flapsLiftPower, flapsAOABias;

    //lift stuff
    public float liftPower;
    public AnimationCurve liftAOACurve;

    //rudder stuff
    public float rudderPower;
    public AnimationCurve rudderAOACurve;

    public Scale_Custom sc;


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
            //Debug.Log("Space key was pressed.");
            UpdateThrust();
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            //Debug.Log("shift key was pressed.");
            UpdateDrag();
        }

        if(Input.GetKey(KeyCode.DownArrow))
        {
            //Debug.Log("down key was pressed.");
            UpdateLift();
        }
        Debug.Log("AirbrakeDeployed: " + airbrakeDrag);
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
        GetComponent<Rigidbody>().AddRelativeForce(Throttle * maxThrust * Vector3.forward /*Vector3.right*/);
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
     * flow 
     *
     * The 6 drag coefficients are defined using Unity's animation curve class:
     * input of curve  = (speed)
     * output of curve = (coefficient of drag) 
     * NOTE: this allows for fine-tuning the drag behaviour at different speeds
     * */
    void UpdateDrag() {
        var lv = LocalVelocity;
        var lv2 = lv.sqrMagnitude; //velocity squared

        float airbrakeDrag = AirbrakeDeployed ? this.airbrakeDrag : 0;
        float flapsDrag = FlapsDeployed ? this.flapsDrag : 0;

        //calculate coefficient of drag depending on direction on velocity
        var coefficient = sc.ScaleCustom(
            lv.normalized,
            dragRight.Evaluate(Mathf.Abs(lv.x)),    dragLeft.Evaluate(Mathf.Abs(lv.x)),
            dragTop.Evaluate(Mathf.Abs(lv.y)),      dragBottom.Evaluate(Mathf.Abs(lv.y)),
            dragForward.Evaluate(Mathf.Abs(lv.z)) + airbrakeDrag + flapsDrag, //include extra drag fgor forward coefficient  
            dragBack.Evaluate(Mathf.Abs(lv.z))
         );

        var drag = coefficient.magnitude * lv2 * -lv.normalized; //drag is oppsite direction of velocity
        GetComponent<Rigidbody>().AddRelativeForce(drag);
    }

    /*Lift (force) equation:
     * 1) https://en.wikipedia.org/wiki/Lift_(force)
     * 2) https://www.flight-training-made-simple.com/post/the-lift-formula
     * 
     * Just like the drag function the Air density (rho) and the surface area (A) is ignored
     * NOTE: the coefficient is not a constant, it depends on the planes angle of attack.
     * 
     * after coefficient (Cl) we add another variable called liftPower
     * liftPower does not effect any paramters on real plane, just there to make hand trunning easier.
    */


    /*Angle of Attack:
     * 1) https://en.wikipedia.org/wiki/Angle_of_attack
     * 2) https://www.flight-training-made-simple.com/post/the-lift-formula
     * 3) unity example: 
     * https://stackoverflow.com/questions/49716989/unity-aircraft-physics
     * https://www.youtube.com/watch?v=7vAHo2B1zLc&ab_channel=Vazgriz
     * 
     * if velocity squared times the coeffiecient of lift times lift power is greater than
     * the mass of the plane, the plane will begin flying.
     */


    /*Induced drag
     * 1) https://en.wikipedia.org/wiki/Lift-induced_drag 
     * 2) https://www.grc.nasa.gov/www/k-12/VirtualAero/BottleRocket/airplane/induced.html#:~:text=The%20induced%20drag%20coefficient%20Cdi,times%20an%20efficiency%20factor%20e.&text=The%20aspect%20ratio%20is%20the,by%20the%20wing%20area%20A.
     * 
     * AR and e are constants relating to the shape of the wing
     * AR: Aspect ratio
     * e: Efficienty
     * 
     * instead of deviding by theses values we multiplied the coefficient of lift by a 
     * hand time parameter 
     */

    Vector3 CalculateLift(float angleOfAttack, Vector3 rightAxis, float liftPower, AnimationCurve aoaCurve) {

        //caluculate lift
        /*using Vector3.ProjeceOnPlane Find part of velocity that flows directly over the wings
         */
        
        /*the idea is air flowing sideways across the wing reduces the amount of lift.
         * we could approximate that by projecting the airflow Vector into this plane*/
        var liftVelocity = Vector3.ProjectOnPlane(LocalVelocity, rightAxis);
        var v2 = liftVelocity.sqrMagnitude; //calculate the V squared term

        /*lift = velocity^2 * coefficient * liftPower
         coefficient varies with AOA */
        //coefficient of lift is calculated using the AOA curve
        var liftCoefficient = aoaCurve.Evaluate(angleOfAttack * Mathf.Rad2Deg);
        var liftForce = v2 * liftCoefficient * liftPower; //product of these values is the liftForce

        //lift is perpendicular to velocity (aka air flow)
        var liftDirection = Vector3.Cross(liftVelocity.normalized, rightAxis);
        var lift = liftDirection * liftForce;

        //calculate induced drag
        //induced drag varies with square of lift coefficient
        var dragForce = liftCoefficient * liftCoefficient * this.inducedDrag;
        //applied opposite of the velocity (aka air flow)
        var dragDirection = -liftVelocity.normalized;
        var inducedDrag = dragDirection * v2 * dragForce;

        /* lift and indued drag are both force vectors so we return there sum */
        return lift + inducedDrag;
    }

    /*This lift is only used to change the planes velocity.
     * We calculate torque created by the rudders separately. */
    void UpdateLift() {
        if (LocalVelocity.sqrMagnitude < 1f) return;

        /*flaps deployed is a boolean property controlled by the payer
         * the extra lifts from flaps is incremented by increasing the 
         * lift power and AOA by hand-tuned amounts.*/
        float flapsLiftPower = FlapsDeployed ? this.flapsLiftPower : 0;
        float flapsAOABias = FlapsDeployed ? this.flapsAOABias : 0;

        var liftForce = CalculateLift(
            AngleOfAttack + (flapsAOABias * Mathf.Deg2Rad), Vector3.right,
            liftPower + flapsLiftPower,
            liftAOACurve
        );

        /* The sideways lift generated by the vertical stabilizers is
         also applied hear */
        var yawForce = CalculateLift(AngleOfAttackYaw, Vector3.up, rudderPower, rudderAOACurve);

        GetComponent<Rigidbody>().AddRelativeForce(liftForce);
        GetComponent<Rigidbody>().AddRelativeForce(yawForce);

    }

}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script requires you to have setup your animator with 3 parameters, "InputMagnitude", "InputX", "InputZ"
//With a blend tree to control the inputmagnitude and allow blending between animations.
[RequireComponent(typeof(CharacterController))]
public class MovementInput : MonoBehaviour {

    public float Velocity;
    [Space]

	private Animator anim;
	private Camera cam;
	private CharacterController controller;
	private bool isGrounded;
	private Vector3 desiredMoveDirection;
	private float InputX;
	private float InputZ;

	public bool blockRotationPlayer;
	public float desiredRotationSpeed = 0.1f;

	public float Speed;
	public float allowPlayerRotation = 0.1f;


    [Header("Animation Smoothing")]
    [Range(0, 1f)]
    public float HorizontalAnimSmoothTime = 0.2f;
    [Range(0, 1f)]
    public float VerticalAnimTime = 0.2f;
    [Range(0,1f)]
    public float StartAnimTime = 0.3f;
    [Range(0, 1f)]
    public float StopAnimTime = 0.15f;

    public float verticalVel;
    private Vector3 moveVector;

	void Start () {
		anim = this.GetComponent<Animator> ();
		cam = Camera.main;
		controller = this.GetComponent<CharacterController> ();
	}
	
	void Update () {

		InputMagnitude ();

        isGrounded = controller.isGrounded;
        if (isGrounded)
            verticalVel -= 0;
        else
            verticalVel -= 1;

        moveVector = new Vector3(0, verticalVel * .2f * Time.deltaTime, 0);
        controller.Move(moveVector);
    }

	void PlayerMoveAndRotation() {
		InputX = Input.GetAxis("Horizontal");
		InputZ = Input.GetAxis("Vertical");

		var camera = Camera.main;
		var forward = cam.transform.forward;
		var right = cam.transform.right;

		forward.y = 0f;
		right.y = 0f;

		forward.Normalize();
		right.Normalize();

		desiredMoveDirection = forward * InputZ + right * InputX;

		if (blockRotationPlayer == false) {
			//Camera
			transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredMoveDirection), desiredRotationSpeed);
			controller.Move(desiredMoveDirection * Time.deltaTime * Velocity);
		}
		else
		{
			//Strafe
			controller.Move((transform.forward * InputZ + transform.right  * InputX) * Time.deltaTime * Velocity);
		}
	}

    public void LookAt(Vector3 pos)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pos), desiredRotationSpeed);
    }

    public void RotateToCamera(Transform t)
    {
        var forward = cam.transform.forward;

        desiredMoveDirection = forward;
		Quaternion lookAtRotation = Quaternion.LookRotation(desiredMoveDirection);
		Quaternion lookAtRotationOnly_Y = Quaternion.Euler(transform.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, transform.rotation.eulerAngles.z);

		t.rotation = Quaternion.Slerp(transform.rotation, lookAtRotationOnly_Y, desiredRotationSpeed);
	}

	void InputMagnitude() {
		//Calculate Input Vectors
		InputX = Input.GetAxis ("Horizontal");
		InputZ = Input.GetAxis ("Vertical");

		//Calculate the Input Magnitude
		Speed = new Vector2(InputX, InputZ).sqrMagnitude;

		//Change animation mode if rotation is blocked
		anim.SetBool("shooting", blockRotationPlayer);

		//Physically move player
		if (Speed > allowPlayerRotation) {
			anim.SetFloat ("Blend", Speed, StartAnimTime, Time.deltaTime);
			anim.SetFloat("X", InputX, StartAnimTime/3, Time.deltaTime);
			anim.SetFloat("Y", InputZ, StartAnimTime/3, Time.deltaTime);
			PlayerMoveAndRotation ();
		} else if (Speed < allowPlayerRotation) {
			anim.SetFloat ("Blend", Speed, StopAnimTime, Time.deltaTime);
			anim.SetFloat("X", InputX, StopAnimTime/ 3, Time.deltaTime);
			anim.SetFloat("Y", InputZ, StopAnimTime/ 3, Time.deltaTime);
		}
	}
}

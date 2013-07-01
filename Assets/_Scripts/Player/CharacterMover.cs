using UnityEngine;
using System.Collections;

/**
 * This class holds all information about current player movement and executes them.
 */
[RequireComponent(typeof(CharacterController))]
public class CharacterMover : MonoBehaviour {
	
	// direction we get from the player input
	Vector3 controllerMovement = Vector3.zero;
	// sum of all forces which influence the character now
	Vector3 physicMovement = Vector3.zero;
	
	// speed of the character movement through player input
	public float movementSpeed = 5;
	// enable/disable controller movement
	public bool controlable = true;
	
	private CharacterController controller;
	
	void Awake() {
		controller = GetComponent<CharacterController>();
	}

	/**
	 * Update is called once per frame
	 * Apply movement from input and physics to the player
	 */
	void Update () {
		Vector3 frameMove = Vector3.zero;
		// add controller movement if controlable
		if( controlable )
			frameMove += controllerMovement * movementSpeed;
		// add physic movement
		frameMove += physicMovement;
		
		// move character respecting collision
		controller.Move( frameMove * Time.deltaTime );
	}
	
	/**
	 * Set the vector value of inputMovement.
	 * It will be normalized, if it isn't already since we only use the direction.
	 */
	public void SetControllerMovement( Vector3 vector ) {
		if( vector.magnitude != 1 )
			vector.Normalize();
		
		if( vector != controllerMovement )
			controllerMovement = vector;
	}
	
	/**
	 * Set the sum of the physical effects moving the character.
	 */
	public void SetPhysicMovement( Vector3 vector ) {
		if( vector != physicMovement )
			physicMovement = vector;
	}
	
	/**
	 * Immediatly set the charater to a new position.
	 */
	public void Teleport( Vector3 pos ) {
		transform.position = pos;	
	}
}

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterMover))]
public class InputManager : Photon.MonoBehaviour {
	
	// allow to enable/disable input controlls
	public bool controlable = true;
	
	// weapon cooldown
	public float cooldown = 0.5f;
	
	// sound and VFX
	public CursorBehaviour crosshair;
	public GameObject projectile;
	public GameObject muzzleFlash;
	public AudioSource walkSound;
	
	public GameObject upperBody;
	private Quaternion initRotUpperBody;
	
	public string groundTag = "Ground";
	private Match match;
	
	// data about the last shot
	private float lastShotTimestamp = 0;
	
	private CharacterMover mover;
	private PlayerManager pman;
	
	public Animator animator;
	private int shootState = Animator.StringToHash("Base Layer.shotState");
	
	// these to allow an rage-mode after respawn
	// a limit number of shoots with reduces cooldown
	private int rageCounter = 0;
	private float rageCooldown = 0.2f;
	
	private Vector3 movement;
	private Vector3 walkDir;
	private Vector3 viewDir;
	
	// The client who controls this character
	public PhotonPlayer controllingPlayer;
	
	// Use this for initialization
	public void Awake () {
		mover = GetComponent<CharacterMover>();
		match = GameObject.Find("PhotonNetman").GetComponent<Match>();
		pman = GetComponent<PlayerManager>();
		crosshair = GameObject.Find("CursorController").GetComponent<CursorBehaviour>();
		
		// disable this if there is no match runnig at the moment
		if(!match.IsRunning())
			enabled = false;
	}
	
	// Update is called once per frame
	public void Update () {
		// Only controlling player is allow to send inputs to this instance
		// also input only allowed if is controlable and macht is running
		if( (PhotonNetwork.player == controllingPlayer && controlable && match.IsRunning()) ) {
			
			// calculate movement vector from keyboard input
			movement = new Vector3(Input.GetAxis("Horizontal"),0,Input.GetAxis("Vertical"));
			animator.SetFloat("Speed",movement.magnitude);
			movement.Normalize();
			// apply previous calculated movement
			mover.SetControllerMovement( movement );
			
			// rotate in movement direction and play sound
			if( movement.magnitude > 0.1) {
				if( !walkSound.isPlaying )
					walkSound.Play();
			} else {
				if( walkSound.isPlaying )
					walkSound.Stop();
			}			
		}
		
		if( animator.GetNextAnimatorStateInfo(0).nameHash == shootState )
			animator.SetBool("Shot", false);
	}
	
	void LateUpdate() {
		// Get mous position.
		Vector3 hitPoint;
		hitPoint = GetMouseHitPoint();
		
		// shoot a missile to the mouse position on left-mouse click
		if( Input.GetButton("Fire1") && !pman.IsDead() ) {
			Shoot(hitPoint); 
		}
	
		if(pman.IsDead()) {
			// we are dead
			
			// reset rage-counter (0 means it's disabled)
			rageCounter = 0;
			
			// use mouse input to move respawn point
			if( Input.GetButton("Fire1") ) {
				Vector3 mousePos = GetMouseHitPoint();
				pman.SetSpawnPoint( mousePos );
			}
		}
		
		// rotate character into moving direction
		// if angle between movedir and viewdir is <90 degree
		walkDir = movement;
		viewDir = hitPoint - transform.position;
		if( Vector3.Angle( walkDir, viewDir ) > 90 )
			walkDir *= -1;
		
		if( walkDir == Vector3.back ) {
			// Quaternion.FromToRotation gives bad results for 180 degree rotation
			// so hack for back movement
			transform.rotation = Quaternion.AngleAxis(180, Vector3.up);
		} else { 
			transform.rotation = Quaternion.FromToRotation(Vector3.forward, walkDir);
		}
		
				
		// look at mouse cursor
		Quaternion rotZ = Quaternion.AngleAxis(-90f, new Vector3(0,0,1));
		Quaternion rotY = Quaternion.AngleAxis(-90f, new Vector3(0,1,0));
		upperBody.transform.LookAt( hitPoint + new Vector3(0,0.5f,0) );
		upperBody.transform.rotation = upperBody.transform.rotation * rotY * rotZ;
	}	
	
	/**
	 * Calculate hit point of mouse cursor ray with "Ground" collider
	 */
	private Vector3 GetMouseHitPoint() {
		Ray cursorRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit[] rayHits = Physics.RaycastAll(cursorRay);
		Vector3 hitPoint = Vector3.zero;
		foreach(RaycastHit hit in rayHits) {
			if (hit.collider.CompareTag(groundTag)) {
				hitPoint = hit.point;
				break;
			}
		}
		hitPoint.y = 0;
		return hitPoint;
	}
	
	/**
	 * Proceed shoot into mouse cursour direction!
	 * Check for cooldown; then intantiate and setup rocket.
	 * @param	mousePos	Current position of the mouse cursour.
	 */
	private void Shoot(Vector3 mousePos) {
		// check if we are off cooldown
		if( (Time.time > lastShotTimestamp + cooldown) 
			|| (rageCounter > 0 && Time.time > lastShotTimestamp + rageCooldown) ) {
			
			animator.SetBool("Shot",true);
			
			// animate cooldown
			crosshair.StartAnimation();
			
			// calculate normalized direction
			Vector3 direction = mousePos - this.transform.position;
			direction.y = 0;
			direction.Normalize();
			
			// set start position of the projectile
			Vector3 pos = this.transform.position + direction.normalized * 0.7f + Vector3.up * 0.5f;
			// instatiate VFX
			PhotonNetwork.Instantiate(muzzleFlash.name,
										pos, 
										Quaternion.LookRotation(direction), 0);
			// instatiate projectile
			GameObject handle = PhotonNetwork.Instantiate(projectile.name, 
										pos, 
										Quaternion.LookRotation(direction), 0);
			handle.GetPhotonView().RPC("InstatiateTimeStamp",PhotonTargets.AllBuffered,(float)PhotonNetwork.time);
			handle.GetPhotonView().RPC("SetTarget",PhotonTargets.AllBuffered,mousePos);
			
			// save information about shoot
			lastShotTimestamp = Time.time;
			
			// decrease rage counter
			// change crosshair animation speed according to next cooldown
			
			
			if( rageCounter > 0) {
				rageCounter--;
				crosshair.SetAnimationTime( rageCooldown );
			} else {
				crosshair.SetAnimationTime( cooldown );
			}
		}
	}
	
	/**
	 * Change which client this input manager is listing to.
	 */ 
	[RPC]
	public void SetPlayer(PhotonPlayer player) {
		controllingPlayer = player;
	}
	
	/**
	 * Get the player this input manager is listing to.
	 */ 
	[RPC]
	public PhotonPlayer GetPlayer() {
		return controllingPlayer;	
	}
}

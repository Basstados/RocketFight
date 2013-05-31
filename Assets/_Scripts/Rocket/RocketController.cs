using UnityEngine;
using System.Collections;


[RequireComponent(typeof(PhotonView))]
public class RocketController : Photon.MonoBehaviour {
	
	public float speed = 10;
	public float lifetime = 3;
	public float explosionRange = 2;
	public float explosionForce = 20;
	public FlightPath flightPath = FlightPath.ballisitic;
	public float ballisticAngle = 60f;
	public GameObject explosion;
	public string playerTag = "Player";
	private float birthTime;
	private bool localGame = false;
	
	public enum FlightPath {
		linear,
		ballisitic,
		controlled
	}

	// Use this for initialization
	void Start () {
		birthTime = Time.time;
		
		switch( flightPath ) {
		case FlightPath.linear:
			this.rigidbody.useGravity = false;
			break;
		case FlightPath.ballisitic:
			this.rigidbody.useGravity = true;
			break;
		case FlightPath.controlled:
			this.rigidbody.useGravity = false;
			break;
		}
	}
	
	// Update is called once per frame
	void Update () {
		switch( flightPath ) {
		case FlightPath.linear:
			this.transform.Translate( Vector3.forward * speed * Time.deltaTime );
			break;
		case FlightPath.ballisitic:
			// Mathf.Cos and Sin working with radians, so we need to convert the angle
			// float alpha = Mathf.Deg2Rad * ballisticAngle; 
			// Vector3 move = Vector3.forward * speed * Mathf.Cos(alpha) + Vector3.up * speed * Mathf.Sin(alpha);
			//this.transform.Translate( move * Time.deltaTime );
			break;
		case FlightPath.controlled:
			this.transform.Translate( Vector3.forward * speed * Time.deltaTime );
			if( Input.GetButtonDown("Fire1") && (Time.time - birthTime > 0.1) ) {
				Explode();
				PhotonNetwork.Destroy( this.gameObject );
			}
			break;
		}
		
		if ( (birthTime + lifetime < Time.time) && ((photonView.owner == PhotonNetwork.player)) ) {
			PhotonNetwork.Destroy( this.gameObject );	
		}
		if ( (birthTime + lifetime < Time.time) || localGame) {
			Destroy( this.gameObject );	
		}
	}
	
	void OnCollisionEnter( Collision collision ) {
		if ( photonView.owner == PhotonNetwork.player ) {
			Explode();
			PhotonNetwork.Destroy( this.gameObject );
		}
		
		if ( localGame ) {
			Explode();
			Destroy( this.gameObject );
		}
	}
	
	public void SetLocaGame(bool val) {
		localGame = val;
	}
	
	public void SetRange(float range) {
		if( flightPath == FlightPath.ballisitic) {
			// Mathf.Cos and Sin working with radians, so we need to convert the angle
			float alpha = Mathf.Deg2Rad * ballisticAngle;
			speed = Mathf.Sqrt( (range * Physics.gravity.magnitude) / Mathf.Sin(2 * alpha) );
			Vector3 force = (Vector3.forward * speed * Mathf.Cos(alpha) + Vector3.up * speed * Mathf.Sin(alpha));
			this.rigidbody.AddRelativeForce( Vector3.forward * speed * Mathf.Cos(alpha) + Vector3.up * speed * Mathf.Sin(alpha), ForceMode.VelocityChange );
		}
	}
	
	public void Explode() {
		if( explosion != null)
				PhotonNetwork.Instantiate(explosion.name, this.transform.position, Quaternion.identity, 0);
		
		GameObject[] gos = GameObject.FindGameObjectsWithTag( playerTag );
		foreach( GameObject playerGo in gos ) {
			// TODO annulate y axis
			Vector3 direction = playerGo.transform.position - this.transform.position;
			if( direction.magnitude <= explosionRange ) {
				float strengh = explosionForce * (1 - (direction.magnitude / explosionRange));
				Debug.Log("Explosion strength: " + strengh );
				Vector3 playerForce = direction.normalized * strengh;
				
				playerGo.gameObject.GetPhotonView().RPC("ApplyForce",PhotonTargets.AllBuffered,playerForce);	
			}
		}
	}
}

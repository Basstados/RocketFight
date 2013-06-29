using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[RequireComponent(typeof(PhotonView))]
public class Rocket : Photon.MonoBehaviour {
	
	public float speed = 10;
	public float lifetime = 3;
	public float explosionRange = 2;
	public float explosionForce = 20;
	public FlightPath flightPath = FlightPath.ballisitic;
	public float ballisticAngle = 60f;
	public GameObject explosion;
	public string playerTag = "Player";
	
	public List<float> zoneRadii = new List<float>();
	public List<float> zoneStrength = new List<float>();
	
	private float birthTime;
	
	public enum FlightPath {
		linear,
		ballisitic,
		controlled
	}

	// Use this for initialization
	void Start () {
		birthTime = Time.time;
		
		if(zoneRadii.Count < 1 || zoneStrength.Count != zoneRadii.Count ) {
			Debug.LogError("You must define atleast one explosion zone (radius & strength) for the Rocket!");	
		}
		
		/*switch( flightPath ) {
		case FlightPath.linear:
			this.rigidbody.useGravity = false;
			break;
		case FlightPath.ballisitic:
			this.rigidbody.useGravity = true;
			break;
		case FlightPath.controlled:
			this.rigidbody.useGravity = false;
			break;
		}*/
	}
	
	// Update is called once per frame
	void Update () {
		switch( flightPath ) {
		case FlightPath.linear:
			this.transform.Translate( Vector3.back * speed * Time.deltaTime );
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
	}
	
	void OnCollisionEnter( Collision collision ) {
		if ( photonView.owner == PhotonNetwork.player ) {
			Explode();
			PhotonNetwork.Destroy( this.gameObject );
		}
	}
	
	public void SetRange(float range) {
		if( flightPath == FlightPath.ballisitic) {
			// Mathf.Cos and Sin working with radians, so we need to convert the angle
			float alpha = Mathf.Deg2Rad * ballisticAngle;
			speed = Mathf.Sqrt( (range * Physics.gravity.magnitude) / Mathf.Sin(2 * alpha) );
			// Vector3 force = (Vector3.forward * speed * Mathf.Cos(alpha) + Vector3.up * speed * Mathf.Sin(alpha));
			Debug.Log( Vector3.forward );
			this.rigidbody.AddRelativeForce( Vector3.back * speed * Mathf.Cos(alpha) + Vector3.up * speed * Mathf.Sin(alpha), ForceMode.VelocityChange );
		}
	}
	
	public void Explode() {
		if( explosion != null)
				PhotonNetwork.Instantiate(explosion.name, this.transform.position, Quaternion.identity, 0);
		
		GameObject[] gos = GameObject.FindGameObjectsWithTag( playerTag );
		foreach( GameObject playerGo in gos ) {
			Vector3 direction = playerGo.transform.position - this.transform.position;
			direction.y = 0;
			for( int i=0; i<zoneRadii.Count; i++ ) {
				if( direction.magnitude < zoneRadii[i] ) {
					Vector3 playerForce = direction.normalized * explosionForce * zoneStrength[i];
					Debug.Log("Explosion strength: " + playerForce.magnitude );
					
					playerGo.gameObject.GetPhotonView().RPC("ApplyForce",PhotonTargets.AllBuffered,playerForce);	
					playerGo.gameObject.GetPhotonView().RPC("HitBy",PhotonTargets.AllBuffered, photonView.owner);
					break;
				}
			}
		}
	}
	
	void OnDrawGizmos() {
		for( int i=0; i<zoneRadii.Count; i++ ) {
			Gizmos.color = new Color( 1f, 1-zoneStrength[i], 0f, 1f );
			Gizmos.DrawWireSphere( transform.position, zoneRadii[i] );	
		}
	}
}

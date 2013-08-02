using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerManager : Photon.MonoBehaviour {
	
	public GameObject scorePopup;
	public GameObject deathVFX;
	public GameObject invulnable;
	public GameObject marker;
	public GameObject circleMarker;
	private PlayerMarker markerInstance;
	
	// stuff for explosion on respawn
	public GameObject explosion;
	public string playerTag = "Player";
	public float explosionForce = 4;
	public List<float> zoneRadii = new List<float>();
	public List<float> zoneStrength = new List<float>();
	
	public AudioSource hitSound;
	
	public Material[] playerMaterials;
	public SkinnedMeshRenderer materialTarget;
	
	private Color color;
	private PhotonPlayer lastHit;
	private List<Hit> hitList = new List<Hit>();
	public int maxHitListCount = 3;
	private Netman netman;
	private GameObject spawnPointObj;
	
	public float assistTime = 3f;
	public float respawnTime = 3f;
	private float deathTime;
	private bool requestSpawn = false;
	
	private CharacterMover mover;
	private Match match;
	
	
	void Awake() {
		if (photonView.owner == PhotonNetwork.player) {	
			netman = GameObject.Find("PhotonNetman").GetComponent<Netman>();
			mover = GetComponent<CharacterMover>();
			markerInstance = ( (GameObject) Instantiate( marker ) ).GetComponent<PlayerMarker>();
			circleMarker.SetActive( true );
		}
		match = GameObject.Find("PhotonNetman").GetComponent<Match>();
	}
	
	void Update() {	
		if( photonView.owner == PhotonNetwork.player ) {
			if( Time.time > deathTime + respawnTime && requestSpawn) {
				Respawn();
				requestSpawn = false;	
			}
			
			if( Time.time > deathTime + respawnTime + 2 && !GetComponent<PlayerPhysic>().vulnerable) {
				// become vunable again
				GetComponent<PlayerPhysic>().vulnerable = true;
				photonView.RPC("HideInvulnable",PhotonTargets.All);
			}
		}
	}
	
	public bool IsDead() {
		return requestSpawn;	
	}
	
	public void SetSpawnPoint( Vector3 position ) {
		spawnPointObj.GetComponent<RespawnPoint>().SetPos( position );
	}
	
	[RPC]
	public void SetColor( Vector3 rgb ) {
		List<Color> usedColors = match.GetUsedColors();
		
		color = new Color(rgb[0],rgb[1],rgb[2], 1.0f);
		for( int i=0; i<usedColors.Count; i++) {
			if( usedColors[i] == color  && materialTarget != null) {
				materialTarget.material = playerMaterials[playerMaterials.Length - 1 - i];	
				break;
			}
		}
		
		if( photonView.owner == PhotonNetwork.player ) {
			markerInstance.SetParent(transform);
			circleMarker.renderer.material.SetColor("_Color",color);	
		}
	}
	
	public Color GetColor() {
		return color;	
	}
	
	/**
	 * Add an player and timeStamp to the hitList at last place 
	 * and make sure there are no hits with the same player left.
	 */
	[RPC]
	public void HitBy( PhotonPlayer player ) {
		// play sound
		hitSound.Play();
		
		// return if you hit yourself
		if( player == photonView.owner )
			return;
		
		// return if last hit is equal new hit
		if( hitList.Count > 0 )
			if( hitList[hitList.Count - 1].player == player ) 
				return;
		
		// find old assist entries
		List<Hit> results = hitList.FindAll(delegate(Hit hit) {
			if(hit.player == player) 
				return true;
			else
				return false;
		});
		// remove old assist entries
		foreach( Hit oldHit in results ) {
			hitList.Remove( oldHit );	
		}
		
		// add new hit
		hitList.Add( new Hit(Time.time, player) );
		
		// trim hit list if it's to long now
		while( hitList.Count > maxHitListCount ) {
			hitList.RemoveAt(0);	
		}
	}
	
	[RPC]
	public void PopupScore(int score) {
	 	GameObject handle = (GameObject) Instantiate(scorePopup, transform.position + Vector3.up, Quaternion.identity);
		handle.GetComponent<UILabel>().text = "+" + score;
		handle.GetComponent<UILabel>().color = color;
		handle.transform.parent = GameObject.Find("UI Root 3D").transform;	
	}
	
	public void OnDeath() {
		if( photonView.owner == PhotonNetwork.player ) {
			if( requestSpawn == false ) {
				// give score points to killer and assistances
				if ( hitList.Count > 0 ) {
					Debug.Log("Killed by " + hitList[hitList.Count - 1].player.name + " [" + hitList[hitList.Count - 1].player.ID + "]");
					
					GameObject.Find("PanelDeath").GetComponent<DeathPanel>().Activate( hitList[hitList.Count - 1].player );
					
					netman.gameObject.GetPhotonView().RPC("IncreaseScore",PhotonTargets.AllBuffered,hitList[hitList.Count -1].player.ID, 2);
					
					for( int i=0; i<hitList.Count-1; i++) {
						if( hitList[i] != null && hitList[i].timestamp > (Time.time - assistTime) ) {
							netman.gameObject.GetPhotonView().RPC("IncreaseScore",PhotonTargets.AllBuffered,hitList[i].player.ID, 1);
						}
					}
				}
				// empty hitList
				hitList.Clear();
				
				// if we don't know our spawn point: find it!
				if( spawnPointObj == null ) {
					GameObject[] gos = GameObject.FindGameObjectsWithTag("Respawn");
					foreach( GameObject go in gos ) {
						if ( go.GetComponent<RespawnPoint>().player == photonView.owner ) {
							spawnPointObj = go;
							spawnPointObj.GetComponent<RespawnPoint>().SetPMan( this );
							//spawnPointObj.GetPhotonView().viewID = PhotonNetwork.AllocateViewID();
							break;
						}
					}
				}
				
				deathTime = Time.time;
				requestSpawn = true;
				mover.controlable = false;
				mover.SetControllerMovement( Vector3.zero );
				GetComponent<InputManager>().controlable = false;
				photonView.RPC("ShowDeath",PhotonTargets.All);
				
				// Reset buff if we carry it
				ScoreBuff sb = gameObject.GetComponentInChildren<ScoreBuff>();
				if( sb != null )
					sb.Drop();
				}
			
			
				// TODO Spawnschutz
				GetComponent<PlayerPhysic>().vulnerable = false;
			
				// TODO choose spawn with single click
				GetComponent<InputManager>().respawnFree = true;
		}
	}
	
	[RPC]
	public void ShowDeath() {
		if( GetComponentInChildren<SkinnedMeshRenderer>() != null
			&& GetComponentInChildren<SkinnedMeshRenderer>() != null) {
			GetComponentInChildren<SkinnedMeshRenderer>().enabled = false;
			GetComponentInChildren<MeshRenderer>().enabled = false;
		}
		Instantiate(deathVFX,transform.position,Quaternion.identity);
	}
	
	private void Respawn() {
		mover.Teleport( spawnPointObj.transform.position );
		transform.rotation = Quaternion.identity;
		spawnPointObj.GetPhotonView().RPC("StartAnimation",PhotonTargets.All);
		
		mover.SetPhysicMovement( Vector3.zero );
		mover.controlable = true;
		GetComponent<InputManager>().controlable = true;
		photonView.RPC("ShowRespawn",PhotonTargets.AllBuffered);
		
		// explosion on respawn
		Explode();
	}
	
	[RPC]
	public void ShowRespawn() {
		if( GetComponentInChildren<SkinnedMeshRenderer>() != null
			&& GetComponentInChildren<MeshRenderer>() != null ) {
			GetComponentInChildren<SkinnedMeshRenderer>().enabled = true;
			GetComponentInChildren<MeshRenderer>().enabled = true;
			invulnable.SetActive( true );
		}
	}
	
	[RPC]
	public void HideInvulnable() {
		invulnable.SetActive( false );
	}
	
	public void Explode() {
		if( explosion != null)
				PhotonNetwork.Instantiate(explosion.name, this.transform.position, Quaternion.identity, 0);
		
		GameObject[] gos = GameObject.FindGameObjectsWithTag( playerTag );
		foreach( GameObject playerGo in gos ) {
			Vector3 direction = playerGo.transform.position - this.transform.position;
			direction.y = 0;
			for( int i=0; i<zoneRadii.Count; i++ ) {
				if( direction.magnitude < zoneRadii[i] && playerGo.GetPhotonView().owner != photonView.owner ) {
					Vector3 playerForce = direction.normalized * explosionForce * zoneStrength[i];
					Debug.Log("Explosion strength: " + playerForce.magnitude );
					
					playerGo.gameObject.GetPhotonView().RPC("ApplyForce",PhotonTargets.AllBuffered,playerForce);	
					playerGo.gameObject.GetPhotonView().RPC("HitBy",PhotonTargets.AllBuffered, photonView.owner);
					break;
				}
			}
		}
	}
	
	
	private class Hit {
		public float timestamp;
		public PhotonPlayer player;
		
		public Hit( float time, PhotonPlayer pplayer) {
			timestamp = time;
			player = pplayer;
		}
		
		public override string ToString() {
			return "Hit by " + player.name + " [" + player.ID + "] at time " + timestamp;
		}
	}
}

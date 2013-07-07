using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Match : Photon.MonoBehaviour {
	
	public List<RocketFightPlayer> playerList = new List<RocketFightPlayer>();
	public int maxPlayerCount = 4;
	public bool running;
	public bool allReady = false;
	public bool startRequest = false;
	public float gameTime;
	
	public string respawnTag = "Respawn";
	
	public UILabel playerListLabel;
	public UIMenu uiMenu;
	public GameObject playerPrefab;
	public List<Color> freeColors = new List<Color>();
	
	private List<Color> usedColors = new List<Color>();
	
	void Update() {
		if(running) {
			gameTime -= Time.deltaTime;
			if( gameTime <= 0 ) {
				running = false;
				if(PhotonNetwork.isMasterClient) {
					foreach( RocketFightPlayer rfp in playerList ) {
						PhotonNetwork.DestroyPlayerObjects(rfp.photonPlayer);	
					}
				}
				uiMenu.ChanceState(UIMenu.UIState.LOBBY);
			}
		} else if(startRequest && allReady && PhotonNetwork.isMasterClient) {
			photonView.RPC("StartMatch",PhotonTargets.AllBuffered);
			startRequest = false;
		}
	}
	
	
	public void Reset() {
		playerList.Clear();
		running = false;
		startRequest = false;
		gameTime = 15;
		UpdateUIPlayerList();
	}
	
	public void Init() {
		gameTime = 15;
		foreach( RocketFightPlayer rfp in playerList ) {
			rfp.score = 0;	
		}
	}
	
	[RPC]
	public void AddPlayer(PhotonPlayer newPlayer) {
		// check playerlist count
		if( playerList.Count >= maxPlayerCount ) 
			return;
		
		// ensure the player isn't already in the list
		List<RocketFightPlayer> results = playerList.FindAll(delegate(RocketFightPlayer rfp) {
			if(rfp.photonPlayer == newPlayer)
				return true;
			else
				return false;
		});
		if( results.Count > 0 ) return;
		
		// get Color
		Color playerColor = freeColors[0];
		freeColors.RemoveAt(0);
		usedColors.Add(playerColor);
		
		// add new RocketFightPlayer
		playerList.Add( new RocketFightPlayer(newPlayer, playerColor));
		
		// update UI
		UpdateUIPlayerList();
	}
	
	[RPC]
	public void RemovePlayer(PhotonPlayer player) {
		// find player
		foreach(RocketFightPlayer rfp in playerList) {
			if( rfp.photonPlayer == player ) {
				// free color
				usedColors.Remove(rfp.color);
				freeColors.Add(rfp.color);
				// fremove from list
				playerList.Remove(rfp);
				// update UI
				UpdateUIPlayerList();
				return;
			}
		}
	}
	
	public void UpdateUIPlayerList() {
		string labelString = "";
		foreach( RocketFightPlayer rfp in playerList ) {
			labelString += "[" + ColorX.RGBToHex(rfp.color) + "]";
			labelString += rfp.photonPlayer.name + " : (" + rfp.score + ")";
			labelString += "[ffffff]";
			if( rfp.photonPlayer.isMasterClient ) 
				labelString += " (Master)";
			labelString += "\n";
		}
		
		playerListLabel.text = labelString;	
	}
	
	[RPC]
	public void LoadingFinished(PhotonPlayer player) {
		Debug.Log("Loading finished by: " + player.name + " [" + player.ID + "]");
		allReady = true;
		
		// set levelLoaded = true
		foreach( RocketFightPlayer rfp in playerList ) {
			if( rfp.photonPlayer == player ){
				rfp.levelLoaded = true;	
			}
			// check if someone is not finished yet
			if( !rfp.levelLoaded) {
				allReady = false;	
			}
		}
	}
	
	public void RequestStart() {
		startRequest = true;
	}
	
	[RPC]
	public void StartMatch() {
		Debug.Log("Match started!");
		running = true;
		Init();
		if( PhotonNetwork.isMasterClient )
			OrganizeSpawning();
	}
	
	/**
	 * Called when new level was loaded.
	 * If we loaded the game scene, find a respawn point for you.
	 * Then spawn your player object at this spawn point.
	 */
	[RPC]
	public void SpawnPlayer(Vector3 spawnPt, Vector3 rgb) {
		Debug.Log("Instatiate player at " + spawnPt);
		GameObject handle = PhotonNetwork.Instantiate(playerPrefab.name,spawnPt,Quaternion.identity,0);
		handle.GetComponent<InputManager>().SendMessage("SetPlayer", PhotonNetwork.player);
		handle.GetPhotonView().RPC("SetColor",PhotonTargets.AllBuffered,rgb);
	}
	
	public void OrganizeSpawning() {
		if( !PhotonNetwork.isMasterClient ) 
			return;
		
		// find spawnpoints
		GameObject[] gos = GameObject.FindGameObjectsWithTag(respawnTag);
		List<GameObject> spawnPoints = new List<GameObject>(gos);
		Debug.Log("Spawnpoints : " + spawnPoints.Count);
		
		foreach( RocketFightPlayer rfp in playerList ) {
			// choose random spawn point
			int randIndex = Mathf.RoundToInt(Random.Range(0, spawnPoints.Count));
			GameObject sp = spawnPoints[randIndex];
			spawnPoints.RemoveAt(randIndex);
				
			Vector3 rgb = new Vector3( rfp.color.r, rfp.color.g,rfp.color.b );
			
			// assign spawpoint
			sp.GetPhotonView().RPC("AssignTo",PhotonTargets.AllBuffered,rfp.photonPlayer);
			sp.GetPhotonView().RPC("SetColor",PhotonTargets.AllBuffered,rgb);
			
			// spawn player incl. color
			photonView.RPC("SpawnPlayer",rfp.photonPlayer,sp.transform.position, rgb);
			// init score
			photonView.RPC("SetScore",PhotonTargets.AllBuffered, rfp.photonPlayer, 0);
		}
	}
	
	[RPC]
	public void IncreaseScore(int playerID, int val) {
		foreach( RocketFightPlayer rfp in playerList ) {
			if( rfp.photonPlayer.ID == playerID ) {
				rfp.score += val;
				
				if( rfp.photonPlayer == PhotonNetwork.player ) {
					GameObject[] characterObjects = GameObject.FindGameObjectsWithTag("Player");
					foreach( GameObject character in characterObjects ) {
						if( character.GetPhotonView().owner == rfp.photonPlayer ) {
							character.GetComponent<PlayerManager>().PopupScore( val );
						}
					}
				}
			}
		}
	}
	
	[RPC]
	public void SetScore(PhotonPlayer target, int val) {
		foreach( RocketFightPlayer rfp in playerList ) {
			if( rfp.photonPlayer == target ) {
				rfp.score = val;
				return;
			}
		}
	}
}

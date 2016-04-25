
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using Sfs2X.Logging;
using Sfs2X.Requests.MMO;
using Sfs2X.Util;

// The Neywork manager sends the messages to server and handles the response.
public class NetworkManager : MonoBehaviour
{

	public GameObject[] playerModels;//this will hold 2 male and female to spwan players
	//private PlayerController localPlayerController;
	private GameObject localPlayer; //this will hold the actual player
	private Dictionary<int, GameObject> remotePlayers = new Dictionary<int, GameObject>();
	private Dictionary<int, NetworkTransformReceiver> recipients = new Dictionary<int, NetworkTransformReceiver>();
	private bool running = false;
	private string aucname="";
	private string Request1,Request2,userDecision;
	private static NetworkManager instance;
	public static NetworkManager Instance {
		get {
			return instance;
		}
	}
	
	private SmartFox smartFox;  // The reference to SFS client
	
	void Awake() {
		instance = this;	
	}
	

			
	// This is needed to handle server events in queued mode
	void FixedUpdate() {
		if (!running) return;
		smartFox.ProcessEvents();
		if (smartFox != null) {
			smartFox.ProcessEvents();
		}
	}
	/**
	 * This is where we receive events about people in proximity (AoI).
	 * We get two lists, one of new users that have entered the AoI and one with users that have left our proximity area.
	 */
	public void OnProximityListUpdate(BaseEvent evt)
	{
		var addedUsers = (List<User>) evt.Params["addedUsers"];
		var removedUsers = (List<User>) evt.Params["removedUsers"];

		// Handle all new Users
		foreach (User user in addedUsers)
		{
			SpawnRemotePlayer (
				(SFSUser) user, 
				Convert.ToInt16(user.GetVariable ("g").Value), 
				new Vector3(user.AOIEntryPoint.FloatX, 5.36f, user.AOIEntryPoint.FloatZ),
				new Vector3(0, (float) user.GetVariable("r").GetDoubleValue() , 0)
			);		
		}

		// Handle removed users
		foreach (User user in removedUsers)
		{
			RemoveRemotePlayer((SFSUser) user);
		}
	}
		
	/**
	 * When user variable is updated on any client within the AoI, then this event is received.
	 * This is where most of the game logic for this example is contained.
	 */
	public void OnUserVariableUpdate(BaseEvent evt) {
		#if UNITY_WSA && !UNITY_EDITOR
		List<string> changedVars = (List<string>)evt.Params["changedVars"];
		#else
		ArrayList changedVars = (ArrayList)evt.Params["changedVars"];
		#endif

		SFSUser user = (SFSUser)evt.Params["user"];

		if (user == smartFox.MySelf) {//spwanLocalPlayer
			if (localPlayer == null) {
				NetworkTransform transform = NetworkTransform.FromUserVariables ((float)user.GetVariable ("x").GetDoubleValue (), 5.36f, (float)user.GetVariable ("z").GetDoubleValue (), 0, (float)user.GetVariable ("r").GetDoubleValue (), 0, Convert.ToDouble (user.GetVariable ("t").Value));
			
				SpawnLocalPlayer (Convert.ToInt16(user.GetVariable ("g").Value), transform);
			}
			return;
		}

		if (recipients.ContainsKey(user.Id))
		{
		// Check if the remote user changed his position or rotation
		if (changedVars.Contains("x") ||  changedVars.Contains("z") || changedVars.Contains("r")) {
			// Move the character to a new position...
			NetworkTransform transform= NetworkTransform.FromUserVariables((float)user.GetVariable("x").GetDoubleValue(), 5.36f, (float)user.GetVariable("z").GetDoubleValue(),0, (float)user.GetVariable("r").GetDoubleValue(), 0, Convert.ToDouble(user.GetVariable("t").Value));
			NetworkTransformReceiver recipent = recipients [user.Id];
			if(recipent!=null)
			{
				recipent.ReceiveTransform (transform);
			}
		}
		// Remote client selected new model?
		if (changedVars.Contains("model")) {
			SpawnRemotePlayer(user, user.GetVariable("model").GetIntValue(),  remotePlayers[user.Id].transform.position, remotePlayers[user.Id].transform.localEulerAngles);
		}
		}
	}
	/// <summary>
	///  Send a request to get typ of player
	/// </summary>
	public void StartWorking()
	{
		smartFox = SmartFoxConnection.Connection;
		UnsubscribeDelegates ();
		Debug.Log ("inside: StartWorking()");
		running = true;
		SubscribeDelegates ();
		Debug.Log ("after: smartFox.Send(request);");
		TimeManager.Instance.Init ();


	}
	private void SubscribeDelegates() {
		smartFox.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
		smartFox.AddEventListener(SFSEvent.USER_VARIABLES_UPDATE, OnUserVariableUpdate);
		smartFox.AddEventListener(SFSEvent.PROXIMITY_LIST_UPDATE, OnProximityListUpdate);
		smartFox.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
	}
	private void UnsubscribeDelegates() {
		smartFox.RemoveAllEventListeners();
	}
	//----------------------------------------------------------
	// Private player helper methods
	//----------------------------------------------------------
	private void SpawnRemotePlayer(SFSUser user, int numModel,  Vector3 pos, Vector3 rot) {
		// See if there already exists a model so we can destroy it first
		if (remotePlayers.ContainsKey(user.Id) && remotePlayers[user.Id] != null) {
			Destroy(remotePlayers[user.Id]);
			remotePlayers.Remove(user.Id);
		}
		// Lets spawn our remote player model
		GameObject remotePlayer = GameObject.Instantiate(playerModels[1]) as GameObject;
		remotePlayer.transform.position = pos;
		remotePlayer.transform.localEulerAngles = rot;
		Debug.Log ("inside spwan remote player");
		remotePlayer.AddComponent<NetworkTransformInterpolation>();
		remotePlayer.AddComponent<NetworkTransformReceiver>();

		recipients.Add(user.Id, remotePlayer.GetComponent<NetworkTransformReceiver> ());
		remotePlayers.Add(user.Id, remotePlayer);
	}

	private void RemoveRemotePlayer(SFSUser user) {
		if (user == smartFox.MySelf) return;

		if (remotePlayers.ContainsKey(user.Id)) {
			Destroy(remotePlayers[user.Id]);
			remotePlayers.Remove(user.Id);
		}
		if (recipients.ContainsKey(user.Id) && recipients[user.Id] != null) {
			Destroy(recipients[user.Id]);
			recipients.Remove(user.Id);
		}
	}

	/// <summary>
	/// When connection is lost we load the login scene
	/// </summary>
	private void OnConnectionLost(BaseEvent evt) {
		UnsubscribeDelegates();
		//change view
	}


	private void SpawnLocalPlayer(int numModel, NetworkTransform trans) {
		Vector3 pos;
		Vector3 rot;
		// See if there already exists a model - if so, take its pos+rot before destroying it
		if (localPlayer != null) {
			pos = localPlayer.transform.position;
			rot = localPlayer.transform.localEulerAngles;
			//	Camera.main.transform.parent = null;
			Destroy(localPlayer);
		} else {
			pos =trans.Position;
			rot = trans.AngleRotationFPS;
		}
		// Lets spawn our local player model
		localPlayer = GameObject.Instantiate(playerModels[0]) as GameObject;
		localPlayer.transform.position = pos;
		localPlayer.transform.localEulerAngles = rot;
		localPlayer.AddComponent<NetworkTransformSender>();
		//localPlayer.SendMessage ("StratSendTransform");
		NetworkTransformSender.Instance.StartSendTransform();
		SendCheckAuctionStart ();//
	}
	/// <summary>
	/// Send local transform to the server
	/// </summary>
	/// <param name="ntransform">
	/// A <see cref="NetworkTransform"/>
	/// </param>
	public void SendTransform(NetworkTransform ntransform) {
		
		if (localPlayer != null  ) {
			Debug.Log ("SendTransform");
			ISFSObject trans = new SFSObject ();
			ntransform.ToSFSObject (trans);
			Debug.Log (smartFox.LastJoinedRoom.Name);
			smartFox.Send(new ExtensionRequest ("control.tr",trans, smartFox.LastJoinedRoom));		
		}
	}

	/// <summary>
	/// Request the current server time. Used for time synchronization
	/// </summary>	
	public void TimeSyncRequest() {
		Debug.Log ("TimeSyncRequest() ");
		ExtensionRequest request = new ExtensionRequest("control.ti", new SFSObject(), smartFox.LastJoinedRoom);
		smartFox.Send(request);
	}
	private void OnApplicationQuit() {
		UnsubscribeDelegates();
	}

	void SendCheckAuctionStart() {
		if (smartFox.LastJoinedRoom.Name == "city") {
			smartFox.Send (new ExtensionRequest ("auction.checkS", new SFSObject (), smartFox.LastJoinedRoom));	
//			TaskManager.main.WaitFor (600f).Then (SendCheckAuctionStart);
		}

	}


	public void sendReserveAuction( ISFSObject data)
	{
		ExtensionRequest request = new ExtensionRequest("auction.reserve", data, smartFox.LastJoinedRoom);
		smartFox.Send(request);

	}

	public void sendGetSlots()
	{
		ExtensionRequest request = new ExtensionRequest("auction.slots", new SFSObject(), smartFox.LastJoinedRoom);
		smartFox.Send(request);

	}

	public void GetAccountInfo(ISFSObject data,String userRequest)
    {
        ExtensionRequest request = new ExtensionRequest("ViewAccount.view", data, smartFox.LastJoinedRoom);
		Request1=userRequest;
        smartFox.Send(request);
    }

	public void DeleteAccount(ISFSObject data,String userRequest)
	{
		ExtensionRequest request = new ExtensionRequest("ViewAccount.delete", data, smartFox.LastJoinedRoom);
		Request2=userRequest;
		smartFox.Send(request);
	}

	public void UpdateAccount(ISFSObject data)
	{
		ExtensionRequest request = new ExtensionRequest("ViewAccount.edit", data, smartFox.LastJoinedRoom);
		smartFox.Send(request);
	}

	public void CreateRoom(ISFSObject data)
	{
		ExtensionRequest request = new ExtensionRequest("Room.create", data, smartFox.LastJoinedRoom);
		smartFox.Send(request);
	}

	public void DeleteRoom(ISFSObject data)
	{
		ExtensionRequest request = new ExtensionRequest("Room.delete", data, smartFox.LastJoinedRoom);
		smartFox.Send(request);
	}

	public void RequestAccessRoom(ISFSObject data)
	{
		ExtensionRequest request = new ExtensionRequest("Room.accessRequest", data, smartFox.LastJoinedRoom);
		smartFox.Send(request);
	}

	public void GetNotifications(ISFSObject data)
	{
		ExtensionRequest request = new ExtensionRequest("Room.getNotifications", data, smartFox.LastJoinedRoom);
		smartFox.Send(request);
	}

	public void GetNotifications()
	{
		ExtensionRequest request = new ExtensionRequest("Room.getAuctions", new SFSObject(), smartFox.LastJoinedRoom);
		smartFox.Send(request);
	}

	public void GetFriendsList(ISFSObject data)
	{
		ExtensionRequest request = new ExtensionRequest("Room.getFriends", data, smartFox.LastJoinedRoom);
		smartFox.Send(request);
	}

	public void Decision(ISFSObject data,string decision)
	{   userDecision = decision;
		ExtensionRequest request = new ExtensionRequest("Room.decision", data, smartFox.LastJoinedRoom);
		smartFox.Send(request);
	}

	public void AuctionDecision(ISFSObject data)
	{
		ExtensionRequest request = new ExtensionRequest("Room.auction", data, smartFox.LastJoinedRoom);
		smartFox.Send(request);
	}

	public void AddNewAdmin(ISFSObject data)
	{
		ExtensionRequest request = new ExtensionRequest("Room.addAdmin", data, smartFox.LastJoinedRoom);
		smartFox.Send(request);
	}
    /////////////////////////////////////////////////////////////////////ON Extension Response
    /// 
    ///
    // This method handles all the responses from the server
    private void OnExtensionResponse(BaseEvent evt) {
		Debug.Log ("inside: OnExtensionResponse");
		try {
			string cmd = (string)evt.Params["cmd"];
			ISFSObject dt = (SFSObject)evt.Params["params"];

			if (cmd == "time") {//spwan Player
			HandleServerTime(dt);
			}
			else if (cmd == "slots") {//spwan Player
				ManageAuction.Instance.SetSlots(dt, true);
			}
			else if (cmd == "slotef") {//spwan Player
				ManageAuction.Instance.SetSlots(dt, false);
			}
			else if (cmd == "ReserveS") {//spwan Player
				ManageAuction.Instance.ReserveSuccess();
			}
			else if(cmd == "ViewAccount") {//spwan Player

				ISFSArray useraccountinfo = dt.GetSFSArray("account");
				string username = useraccountinfo.GetSFSObject(0).GetUtfString("username");

				int AdminIndex = username.IndexOf("n");
					string admin = username.Substring(0, AdminIndex + 1);

				      if (admin.Equals("Admin"))
					     manageAdminAccount.Instance.ViewAdminAccount(dt);
				else if (Request1=="")
					     manageMemberAccount.Instance.ViewMemberAccount(dt);
				else if (Request1=="AdminRequest")
					ViewMemberAccount.Instance.ViewAccountInfo(dt);
				
			}
			else if (cmd == "DeleteAccount") {//spwan Player
				string result = dt.GetUtfString("DeleteResult");
				int dotIndex = result.IndexOf(".");
				string admin = result.Substring(dotIndex+1);
				admin = admin.Substring(0, result.IndexOf("n")+1);
				if (admin.Equals("Admin"))
					manageAdminAccount.Instance.DeleteAccount(dt);
				else if (Request2=="")
					manageMemberAccount.Instance.DeleteAccount(dt);
				else if (Request2=="AdminRequest")
					ViewMemberAccount.Instance.DeleteAccount(dt);
			}
			else if (cmd == "UpdateAccount") {//spwan Player
				string result = dt.GetUtfString("UpdateResult");
				int dotIndex = result.IndexOf(".");
				string admin = result.Substring(dotIndex+1);
				admin = admin.Substring(0, result.IndexOf("n")+1);
				if (admin.Equals("Admin"))
					manageAdminAccount.Instance.UpdateAccount(dt);
				else
					manageMemberAccount.Instance.UpdateAccount(dt);

			}else if (cmd == "CreateRoom") {//spwan Player
				manageMemberAccount.Instance.createRoom(dt);
			}else if (cmd == "DeleteRoom") {//spwan Player
				manageMemberAccount.Instance.deleteRoom(dt);
			}else if (cmd == "RequestAccessRoom") {//spwan Player
				RequestAccess.Instance.RequestAccessPRoom(dt);
			}else if (cmd == "ViewNotifications") {//spwan Player
				ViewNotifications.Instance.ViewMyNotifications(dt);
			}else if (cmd == "ViewFriends") {//spwan Player
				ViewFriends.Instance.ViewMyFriends(dt);
			}else if (cmd == "ViewAuctionRequests") {//spwan Player
				ViewAuction.Instance.ViewMyNotifications(dt);
			}else if (cmd == "Decision") {//spwan Player
				if (userDecision=="AccessRoom")
				ScrollableNotificationsPanel.Instance.AccessDecision(dt);
				else 
					ScrollableFriendsPanel.Instance.DeleteDecision(dt);
			}else if (cmd == "AuctionDecision") {//spwan Player
				ScrollableAdminNotificationsPanel.Instance.AuctionDecision(dt);
			}else if (cmd == "AddAdmin") {//spwan Player
				AddAdmin.Instance.AddNewAdminResult(dt);
			}




		}
		catch (Exception e) {
			Debug.Log("Exception handling response: "+e.Message+" >>> "+e.StackTrace);
		}

	}

	private void HandleServerTime(ISFSObject dt )
	{
		long time = dt.GetLong ("t");
		TimeManager.Instance.Synchronize (Convert.ToDouble(time));
	}



}

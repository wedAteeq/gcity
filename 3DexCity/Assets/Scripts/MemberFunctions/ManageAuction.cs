using UnityEngine;
	using Sfs2X;
	using Sfs2X.Core;
	using Sfs2X.Requests;
	using Sfs2X.Entities.Data;
	using UnityEngine.UI;
	using Sfs2X.Util;
	using UnityEngine.EventSystems;
	using System.Collections.Generic;
	public class ManageAuction : MonoBehaviour
	{
		private string username;
		private string aucName;
		private string aucType;
		private string card;
		private string cardEndMonth;
		private string cardEndYear;
		private string aucDate;
		private string aucTime;
		private int Error = 0;
		private int room = 0;
		private int login=1;
		private List<int> slots;
		private List<string> dates;
		private int[] daysSlots = new int[540];
		private int firstLoad=0;

		//----------------------------------------------------------
		// UI elements
		//----------------------------------------------------------

		public InputField AucName;
		public Dropdown AucType;
		public InputField Card;
		public Text ErrorMessage;
		public Dropdown CardEndMonth;
		public Dropdown CardEndYear;
		public Dropdown AucDate;
		public GameObject Slot;
		public Transform ChooseTime;
		public Transform ReserveAuctionForm;
		//-----------------
	private static ManageAuction instance;
	public static ManageAuction Instance {
		get {
			return instance;
		}
	}
	void Awake() {
		instance = this;
	}

		//----------------------------------------------------------
		// Public interface methods for UI
		//----------------------------------------------------------

		public void onReserveAuctionFormClicked()
		{
	     	ErrorMessage.text = "";
			//this will show the form and request a new slots
			slots = new List<int> ();
			dates = new List<string> ();
	  	NetworkManager.Instance.sendGetSlots ();

		}
		public void onDatesMenuValueChanged()
		{	
			GameObject myButton = null;
			for (int i = 0; i < 16; i++) {
				myButton = Slot.transform.Find (i + "").gameObject;
				myButton.GetComponent<Button> ().interactable = true;
			myButton.GetComponent<Button> ().image.color= Color.white;

			}
		int index=AucDate.value;//getDayIndex
			if (index != 0) {

				int start = (index - 1) * 16;
				//dinamkly disable buttens and change their colors
				for (int i = 0; i < 16; i++) {
					if (daysSlots [start++] == 0) {//0 means disabled
					myButton = Slot.transform.Find (i + "").gameObject;

						myButton.GetComponent<Button> ().interactable = false;
					}

					
				}
			}

		}
		public void OnChooseTimeButtonClicked ()
		{
			int index=AucDate.value;//getDayIndex
			if (index == 0) {
				ErrorMessage.text = "Please choose one date for your auction";
				return;
			}

			//display choose thime interface and hide the other interfaces
			ReserveAuctionForm.gameObject.SetActive(false);
			ChooseTime.gameObject.SetActive(true);
		}
		//end ChooseTimeButton

		public void OnTimeButtonClicked ()//to change color of the button
		{
			Color x = EventSystem.current.currentSelectedGameObject.GetComponent<Button> ().image.color;
			if (x.Equals (Color.white)) {
				EventSystem.current.currentSelectedGameObject.GetComponent<Button> ().image.color = Color.yellow;
				//add slot
				slots.Add (int.Parse(EventSystem.current.currentSelectedGameObject.name));
			} else {
				EventSystem.current.currentSelectedGameObject.GetComponent<Button> ().image.color = Color.white;
				//remove slot
				slots.Remove(int.Parse(EventSystem.current.currentSelectedGameObject.name));
			}
		}
		public void OnOKChooseTimeClicked ()//after chooseing his slots
		{
			ErrorMessage.text="";
			ChooseTime.gameObject.SetActive(false);
			ReserveAuctionForm.gameObject.SetActive(true);
			
		}
		//end TimeButton
		public void OnReserveAuctionButtonClicked()
		{
			//check the user has choosen the time slot
			slots.Sort ();
			for (int i = 0; i < slots.Count - 1; i++) {
				if (slots [i]-slots [i + 1]== -1) {
					continue;
				} else {
					//message they should be consequantive
					ErrorMessage.text="The slots should be consequantive";
					return;
				}
			}	
			if (slots.Count == 0) {
				//erroe message
				ErrorMessage.text="You Forget to choose time slots for your auction";
				return;
			}
			aucName= AucName.text;
			//check name
			if (string.IsNullOrEmpty (aucName)) {
				//erroe message
				ErrorMessage.text="The auction name is required";
				return;
			}
			if (aucName.Length>15) {
				//erroe message
				ErrorMessage.text="The auction name should be less than 15 character";
				return;
			}
			aucType = AucType.options [AucType.value].text;
			card=Card.text;
			//check card naumer
			if (string.IsNullOrEmpty (card)) {
				//erroe message
				ErrorMessage.text="The credit card number is required";
				return;
			}
		if (CardEndYear.value == 0) {
			ErrorMessage.text="The credit card (End Year) is required";
			return;
		}
		if (CardEndMonth.value == 0) {
			ErrorMessage.text="The credit card (End Month) is required";
			return;
		}
			if (!CreditCardUtility.IsValidNumber (card)) {
				ErrorMessage.text="The credit card number is invalid";
				return;
			}
			cardEndMonth=  CardEndMonth.options [CardEndMonth.value].text;
			cardEndYear=CardEndYear.options [CardEndYear.value].text;
			aucDate=AucDate.options [AucDate.value].text;
			aucTime="";
			aucTime = slots [0] + ","+ slots [slots.Count-1] ;
			//we ended the check steps
			//enableInterface(false);

			{
				ISFSObject objOut = new SFSObject();
				objOut.PutUtfString ("aucName", aucName);
				objOut.PutUtfString ("aucType", aucType);
				objOut.PutUtfString ("card", card);
				objOut.PutUtfString ("cardEndMonth", cardEndMonth);
				objOut.PutUtfString ("cardEndYear", cardEndYear);
				objOut.PutUtfString ("aucDate", aucDate);
				objOut.PutUtfString ("aucTime", aucTime);
			objOut.PutUtfString ("cardType",CreditCardUtility.GetType(card) );

			NetworkManager.Instance.sendReserveAuction(objOut);
			}
		}
		private void enableInterface(bool enable)
		{
			AucName.interactable = enable;
			AucType.interactable = enable;
			Card.interactable = enable;
			ErrorMessage.text =  "";
			CardEndMonth.interactable = enable;
			CardEndYear.interactable = enable;
			AucDate.interactable = enable;

		}
		

	public void SetSlots(ISFSObject objIn, bool x)
	{
		int end = 0;
		string info = objIn.GetUtfString ("slots");
		end = info.Length;
		for (int i = 0; i < end; i++) {
			daysSlots [i] = int.Parse (info [i] + "");
		}
		string[] d;
		if (x) {
			d = objIn.GetUtfStringArray ("dates");
			dates.Add ("Auction Date");
			for (int i = 0; i < d.Length; i++) {
				dates.Add (d [i]);
			}
			AucDate.ClearOptions ();
			AucDate.AddOptions (dates);
		}

		else if (!x) {
			ErrorMessage.text = "Your auction not rigesterd, try choose another time";
		}
	}

	public void ReserveSuccess()
	{
		ErrorMessage.text = "Your auction id rigesterd, but not confirmed yet, we will send email for you if it is confirmed";

	}

}

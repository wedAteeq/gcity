﻿using UnityEngine;
using UnityEngine.UI;
using Sfs2X;
using Sfs2X.Util;
using Sfs2X.Core;
using Sfs2X.Requests;
using Sfs2X.Entities.Data;
using UnityEditor;

public class ViewMemberAccount : MonoBehaviour
{
    private string ServerIP = "127.0.0.1";// Default host
    private int defaultTcpPort = 9933;// Default TCP port
    private int defaultWsPort = 8888;			// Default WebSocket port
    private string ZoneName = "3DexCityZone";
    private int ServerPort = 0;

    private SmartFox sfs;
    private string username;

    //----------------------------------------------------------
    // UI elements
    //----------------------------------------------------------
    public InputField UserName;
    public Text TextMessage;
    public Transform AdminView;
    public Transform viewPage, view, Delete;

    public Text View_username;
    public Text Email;
    public Text FirstName;
    public Text LastName;
    public Text accountType, act_code, active;
    public Text ActivateRoom, Biography;
    public Text Avatar;


    private int viewStatus = 0;
    private int deleteStatus = 0;


    //----------------------------------------------------------
    // Unity calback methods
    //----------------------------------------------------------

    // Use this for initialization
    void Start()
    {
        TextMessage.text = "";

    }

    // Update is called once per frame
    void Update()
    {
        // As Unity is not thread safe, we process the queued up callbacks on every frame
        if (sfs != null)
            sfs.ProcessEvents();
    }

    //----------------------------------------------------------
    // Public interface methods for UI
    //----------------------------------------------------------

    public void OnViewMemberAccountButtonclicked()
    {
        username = UserName.text;
        TextMessage.text = "";
        View_username.text = "UserName:\n";
        Email.text = "Email:\n";
        FirstName.text = "First Name:\n";
        LastName.text = "Last Name:\n";
        Biography.text = "Biography:\n";
        ActivateRoom.text = "Has Room:\n";
        Avatar.text = "Avatar Gender:\n";
        accountType.text = "Account Type:\n";
        act_code.text = "Activation Code:\n";
        active.text = "Activate Account:\n";

        int AdminIndex = username.IndexOf("n");
        string admin = username.Substring(0, AdminIndex + 1);
        if (!admin.Equals("Admin"))
        {
            Debug.Log("view account");
            viewStatus = 1;


#if UNITY_WEBGL
            {
             sfs = new SmartFox(UseWebSocket.WS);
             ServerPort = defaultWsPort;
            }
#else
            {
                sfs = new SmartFox();
                ServerPort = defaultTcpPort;
            }
#endif

            sfs.ThreadSafeMode = true;
            sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
            sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
            sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
            sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
            sfs.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);

            sfs.Connect(ServerIP, ServerPort);
        }
        else
            TextMessage.text = "Sorry, You can not view any admin account only members";


    }//end

    private void OnConnection(BaseEvent evt)
    {
        if ((bool)evt.Params["success"])
        {
            // Login
            Debug.Log("Successfully Connected!");

            sfs.Send(new LoginRequest("", "", ZoneName));
        }
        else
        {
            Debug.Log("Connection Failed!");
            // Remove SFS2X listeners and re-enable interface
            reset();


        }
    }

    private void OnLogin(BaseEvent evt)
    {
        Debug.Log("Logged In: " + evt.Params["user"]);

        ISFSObject objOut = new SFSObject();
        objOut.PutUtfString("username", username);
        sfs.Send(new ExtensionRequest("ViewAccount", objOut));

    }


    private void OnConnectionLost(BaseEvent evt)
    {
        // Remove SFS2X listeners and re-enable interface
        reset();

        string reason = (string)evt.Params["reason"];

        if (reason != ClientDisconnectionReason.MANUAL)
        {
            // Show error message
            TextMessage.text = "Connection was lost; reason is: " + reason;
        }
    }//end

    private void OnLoginError(BaseEvent evt)
    {    // Show error message
        string message = (string)evt.Params["errorMessage"];
        // TextMessage.text = "Login failed: " + message;
        Debug.Log("Login failed: " + message);

        // Disconnect
        sfs.Disconnect();

        // Remove SFS2X listeners and re-enable interface
        reset();
    }

    private void OnExtensionResponse(BaseEvent evt)
    {
        Debug.Log("2: ");

        ISFSObject objIn = (SFSObject)evt.Params["params"];
        if (viewStatus == 1)
        {
            viewStatus = 0;
            ISFSArray useraccountinfo = objIn.GetSFSArray("account");
            if (useraccountinfo.Size() == 0)
                // EditorUtility.DisplayDialog("Warning Message", "               The username is not true", "ok");
                TextMessage.text = "Invalid username";
            else
            {
                viewPage.gameObject.SetActive(true);
                view.gameObject.SetActive(false);



                View_username.text = View_username.text + useraccountinfo.GetSFSObject(0).GetUtfString("username");

                Email.text = Email.text + useraccountinfo.GetSFSObject(0).GetUtfString("email");
                FirstName.text = FirstName.text + useraccountinfo.GetSFSObject(0).GetUtfString("firstName");

                if (useraccountinfo.GetSFSObject(0).GetUtfString("biography") == null)
                    Biography.text = Biography.text + " null";
                else
                    Biography.text = Biography.text + useraccountinfo.GetSFSObject(0).GetUtfString("biography");

                LastName.text = LastName.text + useraccountinfo.GetSFSObject(0).GetUtfString("lastName");


                if (useraccountinfo.GetSFSObject(0).GetUtfString("hasRoom").Equals("Y"))
                    ActivateRoom.text = ActivateRoom.text + " Yes";
                else
                    ActivateRoom.text = ActivateRoom.text + " No";



                if (useraccountinfo.GetSFSObject(0).GetUtfString("avatar").Equals("F"))
                    Avatar.text = Avatar.text + " Female";
                else
                    Avatar.text = Avatar.text + " Male";

                accountType.text = accountType.text + useraccountinfo.GetSFSObject(0).GetUtfString("accountType");
                act_code.text = act_code.text + useraccountinfo.GetSFSObject(0).GetUtfString("act_code");

                if (useraccountinfo.GetSFSObject(0).GetUtfString("active").Equals("Y"))
                    active.text = active.text + " Yes";
                else
                    active.text = active.text + " No";

            }
        }//view account
        else
             if (deleteStatus == 1)
        {
            string result = objIn.GetUtfString("DeleteResult");

            if (result == "Successful")
            {
                Debug.Log("Successful");
                EditorUtility.DisplayDialog("Waring Message", "         The account has been deleted successfully: " + username, "ok");
                AdminView.gameObject.SetActive(true);
                viewPage.gameObject.SetActive(false);
                Delete.gameObject.SetActive(false);

            }
            else
            {
                Debug.Log("error");
                //TextMessage.text = "Your account has not been deleted";
                EditorUtility.DisplayDialog("Waring Message", "         The account has not been deleted" + username, "ok");
            }
            deleteStatus = 0;
        }//delete account

    }//end extension

    public void OnDeleteButtonClocked()
    {
        Debug.Log("delete account");
        if (sfs != null && sfs.IsConnected)
        {
            deleteStatus = 1;
            Debug.Log("1");

            ISFSObject objOut = new SFSObject();
            objOut.PutUtfString("username", username);
            sfs.Send(new ExtensionRequest("DeleteAccount", objOut));
        }
    }//delte account


    private void reset()
    {
        // Remove SFS2X listeners
        sfs.RemoveAllEventListeners();

        sfs = null;

        // Enable interface
        enableInterface(true);
    }


    private void enableInterface(bool enable)
    {
        TextMessage.text = "";

    }


}
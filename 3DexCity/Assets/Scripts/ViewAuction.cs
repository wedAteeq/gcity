using UnityEngine;
using UnityEngine.UI;
using Sfs2X;
using Sfs2X.Util;
using Sfs2X.Core;
using Sfs2X.Requests;
using Sfs2X.Entities.Data;


public class ViewAuction : MonoBehaviour
{

    //----------------------------------------------------------
    // Private properties (Connection part)
    //----------------------------------------------------------
    private string ServerIP = "127.0.0.1";// Default host
    private int defaultTcpPort = 9933;// Default TCP port
    private int defaultWsPort = 8888;           // Default WebSocket port
    private string ZoneName = "3DexCityZone";
    private int ServerPort = 0;

    private SmartFox sfs;
    private int display = 0;
    //----------------------------------------------------------
    // UI elements
    //----------------------------------------------------------
    public Transform NotificationForm;
    public GameObject itemPrefab;
    public GameObject Parent;


    //----------------------------------------------------------
    // Unity calback methods
    //----------------------------------------------------------

    // Use this for initialization
    void Start()
    {
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

    public void OnViewNotificationsButtonclicked()
    {
        Transverser.itemPrefab3 = itemPrefab;
        Transverser.itemPrefab3Parent = Parent;

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

            // Show error message
            Debug.Log("Connection failed; is the server running at all?");
        }
    }

    private void OnLogin(BaseEvent evt)
    {
        Debug.Log("Logged In: " + evt.Params["user"]);

        ISFSObject objOut = new SFSObject();
        sfs.Send(new ExtensionRequest("ViewAuctionRequests", objOut));
    }



    private void OnConnectionLost(BaseEvent evt)
    {
        // Remove SFS2X listeners and re-enable interface
        reset();

        string reason = (string)evt.Params["reason"];

        if (reason != ClientDisconnectionReason.MANUAL)
        {
            // Show error message
            Debug.Log("Connection was lost; reason is: " + reason);
        }
    }//end

    private void OnLoginError(BaseEvent evt)
    {    // Show error message
        string message = (string)evt.Params["errorMessage"];
        Debug.Log("Login failed: " + message);

        // Disconnect
        sfs.Disconnect();

        // Remove SFS2X listeners and re-enable interface
        reset();
    }

    private void OnExtensionResponse(BaseEvent evt)
    {
        Debug.Log("extension");

        ISFSObject objIn = (SFSObject)evt.Params["params"];

        ISFSArray AuctionReq = objIn.GetSFSArray("AuctionRequests");
        ISFSArray MembershipReq = objIn.GetSFSArray("membershipRequests");
        ISFSArray CreditCard = objIn.GetSFSArray("CreditCard");

        Debug.Log(AuctionReq.Size());
        Debug.Log(MembershipReq.Size());

        Transverser.AuctionReq = AuctionReq;
        Transverser.MembershipReq = MembershipReq;
        Transverser.CreditCard = CreditCard;
        Debug.Log(CreditCard.Size());

        Debug.Log("Display Notifications");
        NotificationForm.gameObject.SetActive(true);
        ScrollableAdminNotificationsPanel m = new ScrollableAdminNotificationsPanel();
        if (display > 0)
            m.Start();
        display++;
    }//end extension

    private void reset()
    {
        // Remove SFS2X listeners
        sfs.RemoveAllEventListeners();
        sfs = null;
    }

}
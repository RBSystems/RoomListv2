using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.EthernetCommunication;
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading

namespace RoomListv2
{
    #region Event Arg Classes
    public class AvailabilityEventArgs : EventArgs
    {
        public bool availableForSending;
        public bool availableForReceiving;
    }
    public class UpdateEventArgs : EventArgs
    {
        public string name;
        public RoomInputValues inputs;
    }
    public class RequestConnectionEventArgs : EventArgs
    {
        public bool requesting;
        public bool receiving;
        public bool sending;
        public uint roomID;
        public RoomInputValues inputValues;
        public bool clear;

        public RequestConnectionEventArgs()
        {
            requesting = false;
            receiving = false;
            sending = false;
            roomID = 0;
            inputValues = null;
            clear = false;

        }
    }
    #endregion

    class Room
    {
        #region Properties
        public Dictionary<uint, uint> SwitcherDictionary { set; get; }
        private ThreeSeriesTcpIpEthernetIntersystemCommunications _eisc { get; set; }
        
        public uint ID { set; get; }
        public string Name { set; get; }
        public bool Enabled { set; get; }
        public bool OverflowEnabled { set; get; }
        public bool RoomState { set; get; }                     //If System is either On or Off, used to update room availability
        public string ReveivingRoomName { set; get; }           //Holds the Name of the receiving room
        public string ReceivingRoomList { set; get; }           //String of Room Names that is Receiving this rooms Send

        public bool ReceivingSelected { set; get; }             //Holds the Value if the Receiving Selected button is select, used when Processing the Output List to show on the TPS

        public bool AvailableForReceiving { set; get; }         //Is the room available for Receiving, gets sent to all other rooms
        public bool AvailableForLocalReceiving { set; get; }    //If the room is receiving, this is set to allow it to still be seen when ReceivingSelected is true
        public bool AvailableForSending { set; get; }           //Is the room available for Receiving, gets sent to all other rooms

        private CTimer ProcessRoomTimer { set; get; }          //Timer to wait to process logic, using as a Re-Triggerable One Shot
        private CTimer ProcessEISCTimer { set; get; }          //Timer to wait to process logic, using as a Re-Triggerable One Shot
        private bool _availabilityNeedsUpdate { set; get; }    //Used to notify if availability needs to be sent to other rooms

        public List<List<RoomListItem>> roomList { set; get; }  //Holds the list of all the other rooms without this room
        public List<List<RoomListItem>> outputRoomList { set; get; } //List created from the room list to show to the TPS

        public RoomInputValues Inputs { set; get; }             //Holds the values of its inputs for Audio, Inputs, Displays, and Cameras
        public RoomInputValues ReceivingInputValues { set; get; } //Holds the values of the current sending room

        private SwitcherManager SwitcherManager { set; get; }
        #endregion

        #region Delegates and Events
        public delegate void AvailabilityEvent(Room sender, AvailabilityEventArgs e);
        public delegate void UpdateEvent(Room sender, UpdateEventArgs e);
        public delegate void RequestConnectionEvent(Room sender, RequestConnectionEventArgs e);

        public event AvailabilityEvent availabilityEvent;
        public event UpdateEvent updateEvent;
        public event RequestConnectionEvent requestConnectionEvent;
        #endregion

        //Constructor
        public Room(uint id, string name, Dictionary<uint, uint> switcherDictionary, ThreeSeriesTcpIpEthernetIntersystemCommunications eisc, SwitcherManager switcherManager)
        {
            //Sent Properties from Contructor parameters
            ID = id;
            Name = name;
            SwitcherDictionary = switcherDictionary;
            _eisc = eisc;
            SwitcherManager = switcherManager;

            //Generate Lists that are needed
            roomList = new List<List<RoomListItem>>();
            outputRoomList = new List<List<RoomListItem>>();

           //Create 6 Lists to hold the other rooms
            for (int i = 0; i < 6; i++)
            {
                roomList.Add(new List<RoomListItem>());
                outputRoomList.Add(new List<RoomListItem>());
            }

            //Set other Class properties to default values
            RoomState = false;
            AvailableForReceiving = false;
            AvailableForSending = false;
            AvailableForLocalReceiving = false;
            Enabled = false;
            OverflowEnabled = false;
            ReceivingSelected = true;
            _availabilityNeedsUpdate = false;

            //Set Input Class properties with default values
            ReceivingInputValues = new RoomInputValues();
            Inputs = new RoomInputValues();

            ProcessRoomTimer = new CTimer(ProcessRoom, Timeout.Infinite);
            ProcessEISCTimer = new CTimer(ProcessEISC, Timeout.Infinite);

            #region Attach Event Handlers
            eisc.OnlineStatusChange += new Crestron.SimplSharpPro.OnlineStatusChangeEventHandler(eisc_OnlineStatusChange);
            eisc.SigChange += new Crestron.SimplSharpPro.DeviceSupport.SigEventHandler(eisc_SigChange);
            #endregion
        }

        #region EISC Event Handlers
        void eisc_SigChange(Crestron.SimplSharpPro.DeviceSupport.BasicTriList currentDevice, Crestron.SimplSharpPro.SigEventArgs args)
        {
            switch (args.Sig.Type)
            {
                case eSigType.Bool:
                    //CrestronConsole.PrintLine("Received Bool Update from Sig: {0}, Value: {1}", args.Sig.Number, args.Sig.BoolValue);
                    #region Bool Switch Cases
                        switch (args.Sig.Number)
                        {
                            case 2: //Display 1 Enabled/Disabled
                                Inputs.Displays[0].enabled = args.Sig.BoolValue;
                                UpdateInputs();
                                break;
                            case 4: //Display 2 Enabled/Disabled
                                Inputs.Displays[1].enabled = args.Sig.BoolValue;
                                UpdateInputs();
                                break;
                            case 6: //Display 3 Enabled/Disabled
                                Inputs.Displays[2].enabled = args.Sig.BoolValue;
                                UpdateInputs();
                                break;
                            case 8: //Display 4 Enabled/Disabled
                                Inputs.Displays[3].enabled = args.Sig.BoolValue;
                                UpdateInputs();
                                break;
                            case 10: //Camera 1 Enabled/Disabled
                                Inputs.Cameras[0].enabled = args.Sig.BoolValue;
                                UpdateInputs();
                                break;
                            case 12: //Camera 2 Enabled/Disabled
                                Inputs.Cameras[1].enabled = args.Sig.BoolValue;
                                UpdateInputs();
                                break;
                            case 14: //Camera 3 Enabled/Disabled
                                Inputs.Cameras[2].enabled = args.Sig.BoolValue;
                                UpdateInputs();
                                break;
                            case 85: //System is On
                                if(args.Sig.BoolValue)
                                    setRoomState(args.Sig.BoolValue);
                                //CrestronConsole.PrintLine("Room: {0}, Roomstate was set from Event Handler, State: {1}", Name, !args.Sig.BoolValue);
                                break;
                            case 86: //System is Off
                                if(args.Sig.BoolValue)
                                    setRoomState(!args.Sig.BoolValue);
                                //CrestronConsole.PrintLine("Room: {0}, Roomstate was set from Event Handler, State: {1}", Name, !args.Sig.BoolValue);
                                break;
                            case 87: //Clear All Receiving
                                if (args.Sig.BoolValue)
                                {
                                    ClearReceivingRoomsButton(0);
                                }
                                break;
                            case 91: //Send To Selected
                                if(args.Sig.BoolValue)
                                    setReceivingSelected(false);
                                break;
                            case 92: //Receive From Room
                                if (args.Sig.BoolValue)
                                    setReceivingSelected(true);
                                break;
                            case 95: //System Overflow Enabled
                                if(args.Sig.BoolValue)
                                    setOverflow(!args.Sig.BoolValue);
                                break;
                            case 96: //System Overflow Disabled
                                if(args.Sig.BoolValue)
                                    setOverflow(args.Sig.BoolValue);
                                break;
                            case 120: //Clear Sending Room
                                if (args.Sig.BoolValue)
                                {
                                    ClearSendingRoomsButton(0);
                                }
                                break;
                            default:
                                break;
                        }

                    #endregion

                    #region If List Button Selected
                    if (args.Sig.Number >= 16 && args.Sig.Number <= 80)
                    {
                        if (args.Sig.Number >= 16 && args.Sig.Number <= 30) //List 1 Button Selected
                        {
                            if (ReceivingSelected)
                            {
                                if (args.Sig.BoolValue && !outputRoomList[0][(int)args.Sig.Number - 16].SelectedForSending)
                                    ReceiveRoomButton(1, args.Sig.Number - 15);
                                else if (args.Sig.BoolValue && outputRoomList[0][(int)args.Sig.Number - 16].SelectedForSending)
                                    ClearSendingRoomsButton(outputRoomList[0][(int)args.Sig.Number - 16].ID);
                            }
                            else
                            {
                                if (args.Sig.BoolValue && !outputRoomList[0][(int)args.Sig.Number - 16].SelectedForReceiving)
                                    SendRoomButton(1, args.Sig.Number - 15);
                                else if (args.Sig.BoolValue && outputRoomList[0][(int)args.Sig.Number - 16].SelectedForReceiving)
                                    ClearReceivingRoomsButton(outputRoomList[0][(int)args.Sig.Number - 16].ID);
                            }
                        }
                        else if (args.Sig.Number >= 31 && args.Sig.Number <= 40)//List 2 Button Selected
                        {
                            if (ReceivingSelected)
                            {
                                if (args.Sig.BoolValue && !outputRoomList[1][(int)args.Sig.Number - 31].SelectedForSending)
                                    ReceiveRoomButton(2, args.Sig.Number - 30);
                                else if (args.Sig.BoolValue && outputRoomList[1][(int)args.Sig.Number - 31].SelectedForSending)
                                    ClearSendingRoomsButton(outputRoomList[1][(int)args.Sig.Number - 31].ID);
                            }
                            else
                            {
                                if (args.Sig.BoolValue && !outputRoomList[1][(int)args.Sig.Number - 31].SelectedForReceiving)
                                    SendRoomButton(2, args.Sig.Number - 30);
                                else if (args.Sig.BoolValue && outputRoomList[1][(int)args.Sig.Number - 31].SelectedForReceiving)
                                    ClearReceivingRoomsButton(outputRoomList[1][(int)args.Sig.Number - 31].ID);
                            }
                        }
                        else if (args.Sig.Number >= 41 && args.Sig.Number <= 50)//List 3 Button Selected
                        {
                            if (ReceivingSelected)
                            {
                                if (args.Sig.BoolValue && !outputRoomList[2][(int)args.Sig.Number - 41].SelectedForSending)
                                    ReceiveRoomButton(3, args.Sig.Number - 40);
                                else if (args.Sig.BoolValue && outputRoomList[2][(int)args.Sig.Number - 41].SelectedForSending)
                                    ClearSendingRoomsButton(outputRoomList[2][(int)args.Sig.Number - 41].ID);
                            }
                            else
                            {
                                if (args.Sig.BoolValue && !outputRoomList[2][(int)args.Sig.Number - 41].SelectedForReceiving)
                                    SendRoomButton(3, args.Sig.Number - 40);
                                else if (args.Sig.BoolValue && outputRoomList[2][(int)args.Sig.Number - 41].SelectedForReceiving)
                                    ClearReceivingRoomsButton(outputRoomList[2][(int)args.Sig.Number - 41].ID);
                            }
                        }
                        else if (args.Sig.Number >= 51 && args.Sig.Number <= 60)//List 4 Button Selected
                        {
                            if (ReceivingSelected)
                            {
                                if (args.Sig.BoolValue && !outputRoomList[3][(int)args.Sig.Number - 51].SelectedForSending)
                                    ReceiveRoomButton(4, args.Sig.Number - 50);
                                else if (args.Sig.BoolValue && outputRoomList[3][(int)args.Sig.Number - 51].SelectedForSending)
                                    ClearSendingRoomsButton(outputRoomList[3][(int)args.Sig.Number - 51].ID);
                            }
                            else
                            {
                                if (args.Sig.BoolValue && !outputRoomList[3][(int)args.Sig.Number - 51].SelectedForReceiving)
                                    SendRoomButton(4, args.Sig.Number - 50);
                                else if (args.Sig.BoolValue && outputRoomList[3][(int)args.Sig.Number - 51].SelectedForReceiving)
                                    ClearReceivingRoomsButton(outputRoomList[3][(int)args.Sig.Number - 51].ID);
                            }
                        }
                        else if (args.Sig.Number >= 61 && args.Sig.Number <= 70)//List 5 Button Selected
                        {
                            if (ReceivingSelected)
                            {
                                if (args.Sig.BoolValue && !outputRoomList[4][(int)args.Sig.Number - 61].SelectedForSending)
                                    ReceiveRoomButton(5, args.Sig.Number - 60);
                                else if (args.Sig.BoolValue && outputRoomList[4][(int)args.Sig.Number - 61].SelectedForSending)
                                    ClearSendingRoomsButton(outputRoomList[4][(int)args.Sig.Number - 61].ID);
                            }
                            else
                            {
                                if (args.Sig.BoolValue && !outputRoomList[4][(int)args.Sig.Number - 61].SelectedForReceiving)
                                    SendRoomButton(5, args.Sig.Number - 60);
                                else if (args.Sig.BoolValue && outputRoomList[4][(int)args.Sig.Number - 61].SelectedForReceiving)
                                    ClearReceivingRoomsButton(outputRoomList[4][(int)args.Sig.Number - 61].ID);
                            }
                        }
                        else if (args.Sig.Number >= 71 && args.Sig.Number <= 80)//List 6 Button Selected
                        {
                            if (ReceivingSelected)
                            {
                                if (args.Sig.BoolValue && !outputRoomList[5][(int)args.Sig.Number - 71].SelectedForSending)
                                    ReceiveRoomButton(6, args.Sig.Number - 70);
                                else if (args.Sig.BoolValue && outputRoomList[5][(int)args.Sig.Number - 71].SelectedForSending)
                                    ClearSendingRoomsButton(outputRoomList[5][(int)args.Sig.Number - 71].ID);
                            }
                            else
                            {
                                if (args.Sig.BoolValue && !outputRoomList[5][(int)args.Sig.Number - 71].SelectedForReceiving)
                                    SendRoomButton(6, args.Sig.Number - 70);
                                else if (args.Sig.BoolValue && outputRoomList[5][(int)args.Sig.Number - 71].SelectedForReceiving)
                                    ClearReceivingRoomsButton(outputRoomList[5][(int)args.Sig.Number - 71].ID);
                            }
                        }
                    }
                    #endregion
                    break;
                case eSigType.UShort:
                    //CrestronConsole.PrintLine("Received analog Update from Sig: {0}, Value: {1}", args.Sig.Number, args.Sig.ShortValue);
                    #region UShort Switch Case
                    switch (args.Sig.Number)
                    {
                        case 14: //Display 1 Source Value
                            Inputs.Displays[0].InputValue = args.Sig.UShortValue;
                            UpdateInputs();
                            break;
                        case 15: //Display 1 Icon Value
                            Inputs.Displays[0].IconValue = args.Sig.UShortValue;
                            UpdateInputs();
                            break;
                        case 16: //Display 2 Source Value
                            Inputs.Displays[1].InputValue = args.Sig.UShortValue;
                            UpdateInputs();
                            break;
                        case 17: //Display 2 Icon Value
                            Inputs.Displays[1].IconValue = args.Sig.UShortValue;
                            UpdateInputs();
                            break;
                        case 18: //Display 3 Source Value
                            Inputs.Displays[2].InputValue = args.Sig.UShortValue;
                            UpdateInputs();
                            break;
                        case 19: //Display 3 Icon Value
                            Inputs.Displays[2].IconValue = args.Sig.UShortValue;
                            UpdateInputs();
                            break;
                        case 20: //Display 4 Source Value
                            Inputs.Displays[3].InputValue = args.Sig.UShortValue;
                            UpdateInputs();
                            break;
                        case 21: //Display 4 Icon Value
                            Inputs.Displays[3].IconValue = args.Sig.UShortValue;
                            UpdateInputs();
                            break;
                        case 22: //Camera 1 Source Value
                            Inputs.Cameras[0].InputValue = args.Sig.UShortValue;
                            UpdateInputs();
                            break;
                        case 23: //Camera 2 Source Value
                            Inputs.Cameras[1].InputValue = args.Sig.UShortValue;
                            UpdateInputs();
                            break;
                        case 24: //Camera 3 Source Value
                            Inputs.Cameras[2].InputValue = args.Sig.UShortValue;
                            UpdateInputs();
                            break;
                        case 32: //Audio Value
                            Inputs.AudioValue = args.Sig.UShortValue;
                            UpdateInputs();
                            break;
                        default:
                            break;
                    }
                    #endregion
                    break;
                case eSigType.String:
                    //CrestronConsole.PrintLine("Received serial Update from Sig: {0}, Value {1}", args.Sig.Number, args.Sig.StringValue);
                    #region String Switch Case
                    switch (args.Sig.Number)
                    {
                        case 1: //Room Name
                            //CrestronConsole.PrintLine("Room ID: {0} Incoming Room Name", ID, args.Sig.StringValue); 
                            SetRoomName(args.Sig.StringValue);
                            break;
                        case 2: //Display 1 Name
                            Inputs.Displays[0].OutputName = args.Sig.StringValue;
                            goto default;
                        case 3: //Display 1 Source Name
                            Inputs.Displays[0].InputName = args.Sig.StringValue;
                            goto default;
                        case 4: //Display 2 Name
                            Inputs.Displays[1].OutputName = args.Sig.StringValue;
                            goto default;
                        case 5: //Display 2 Source Name
                            Inputs.Displays[1].InputName = args.Sig.StringValue;
                            goto default;
                        case 6: //Display 3 Name
                            Inputs.Displays[2].OutputName = args.Sig.StringValue;
                            goto default;
                        case 7: //Display 3 Source Name
                            Inputs.Displays[2].InputName = args.Sig.StringValue;
                            goto default;
                        case 8: //Display 4 Name
                            Inputs.Displays[3].OutputName = args.Sig.StringValue;
                            goto default;
                        case 9: //Display 4 Source Name
                            Inputs.Displays[3].InputName = args.Sig.StringValue;
                            goto default;
                        case 10: //Camera 1 Name
                            Inputs.Cameras[0].OutputName = args.Sig.StringValue;
                            goto default;
                        case 11: //Camera 2 Name
                            Inputs.Cameras[1].OutputName = args.Sig.StringValue;
                            goto default;
                        case 12: //Camera 3 Name
                            Inputs.Cameras[2].OutputName = args.Sig.StringValue;
                            goto default;
                        default:
                            UpdateInputs();
                            break;
                    }
                    #endregion
                    break;
                default:
                    break;
            }

        }
        void eisc_OnlineStatusChange(Crestron.SimplSharpPro.GenericBase currentDevice, Crestron.SimplSharpPro.OnlineOfflineEventArgs args)
        {
            if (args.DeviceOnLine)
            {
                Enabled = true;
                AvailabilityNeedsUpdate(true);
                CrestronConsole.PrintLine("Room: {0} EISC online", Name);
                SetOnline(true);
                StartUpdateInputTimer();
            }
            else
            {
                CrestronConsole.PrintLine("Room: {0} EISC offline", Name);
                Enabled = false;
                AvailableForReceiving = false;
                AvailableForSending = false;
                AvailableForLocalReceiving = false;
                OverflowEnabled = false;
                ReceivingSelected = false;
                ClearReceivingRoomsButton(0);   //Sends Command to all rooms to clear if it was receiving

            }
        }
        #endregion

        /// <summary>
        /// Adds a Room to the RoomList Property
        /// </summary>
        /// <param name="id">ID of the room adding to list</param>
        /// <param name="name">Name of Room</param>
        /// <param name="enabled">Is the Room Enabled</param>
        /// <param name="list">What List are we adding the room to</param>
        public void AddRoomToList(uint id, string name, bool enabled, uint list)
        {
            if (ID != id) //If the ID is not this room
            {
                roomList[(int)list - 1].Add(new RoomListItem(id, name, enabled)); //Add the room to the list creating a new instance of RoomListItem
                StartUpdateInputTimer(); //Process the Room (May want to remove this, and put it somewhere else to be more efficient
            }
            

        }

        #region Event Driven Functions
        public void RecievedUpdateListItem(uint listNumber, uint id, string name, bool availableForSending, bool availableForReceiving, bool enabled)
        {
            //CrestronConsole.PrintLine("Room: {0} Received Update Item from {1}: Available for Sending: {2}, Available for Receiving{3}, is Enabled, {4}", Name, id, availableForSending, availableForReceiving, enabled);
            foreach (RoomListItem listItem in roomList[(int)listNumber - 1])
            {
                if (listItem.ID == id)
                {
                    listItem.Name = name;
                    listItem.AvailableForSending = availableForSending;
                    listItem.AvailableForReceiving = availableForReceiving;
                    listItem.Enabled = enabled;
                    StartUpdateInputTimer();
                }
            }
        }
        public void RecievedUpdateRoomName(uint id, string name, uint listNumber)
        {
            foreach (RoomListItem listItem in roomList[(int)listNumber - 1])
            {
                if (listItem.ID == id)
                {
                    listItem.Name = name;
                    StartUpdateInputTimer();
                    break;
                }
            }
        }
        public void RecievedRequestFromReceivingRoom(uint id)
        {
            if (Enabled)
            {
                foreach (List<RoomListItem> list in roomList)
                    foreach (RoomListItem item in list)
                    {
                        if (item.ID == id)
                        {
                            item.SelectedForReceiving = true;
                            item.AvailableForReceiving = false;
                            item.AvailableForSending = false;
                            AvailabilityNeedsUpdate(true);
                            StartUpdateInputTimer();
                            break;
                        }

                    }

                if (availabilityEvent != null)
                {
                    requestConnectionEvent(this, new RequestConnectionEventArgs { receiving = true, roomID = id, inputValues = Inputs });
                }
            }      
        }
        public void ReceivedReplyFromSendingRoom(uint id, RoomInputValues inputs, string roomName)
        {
            if (Enabled)
            {
                
                foreach (List<RoomListItem> list in roomList)
                    foreach (RoomListItem item in list)
                    {
                        if (item.ID == id)
                        {
                            //CrestronConsole.PrintLine("Room: {0} Recieved Reply from Sending Room", Name);
                            //processInputs(inputs, id, roomName);
                            item.SelectedForSending = true;
                            StartOneshotThread();
                            StartUpdateInputTimer();
                            break;
                        }
                    }
            }
        }
        public void RecievedRequestFromSenderToRemove(uint roomID)
        {
            if(Enabled)
            {
                foreach (List<RoomListItem> list in roomList)
                {
                    foreach (RoomListItem item in list)
                    {
                        if (roomID == item.ID)
                        {
                            if (item.SelectedForSending)
                            {
                                if (SwitcherDictionary[ID] != SwitcherDictionary[roomID])
                                {
                                    SwitcherManager.ClearReceivingRoom(roomID, ID);
                                }
                                item.SelectedForSending = false;
                                _eisc.BooleanInput[81].BoolValue = true;
                                AvailabilityNeedsUpdate(true);
                                StartUpdateInputTimer();
                            }
                            break;
                        }
                    }
                }
            }
            
        }
        public void RecievedRequestFromReceiverToRemove(uint roomID)
        {
            if (Enabled)
            {
                foreach (List<RoomListItem> list in roomList)
                {
                    foreach (RoomListItem item in list)
                    {
                        if (roomID == item.ID)
                        {
                            if (SwitcherDictionary[ID] != SwitcherDictionary[roomID])
                            {
                                SwitcherManager.ClearReceivingRoom(ID, roomID);
                            }
                            item.SelectedForReceiving = false;
                            AvailabilityNeedsUpdate(true);
                            StartUpdateInputTimer();
                            break;
                        }
                    }
                }
            }
        }
        public void RecievedUpdateInput(uint roomID, RoomInputValues inputs)
        {
            if (Enabled)
            {
                foreach (List<RoomListItem> list in roomList)
                {
                    foreach (RoomListItem item in list)
                    {
                        if ((roomID == item.ID) && item.SelectedForSending)
                        {
                            CrestronConsole.PrintLine("Room: {0} Recieved Update Input", Name);
                            processInputs(inputs, roomID, item.Name);
                        }

                    }
                }
            }
        }
        #endregion

        #region EISC Requests
        public void ClearReceivingRoomsButton(uint roomID)
        {
            if(Enabled)
            {
                if (roomID == 0)
                {
                    processInputsAndClear();
                    foreach (List<RoomListItem> list in roomList)
                    {
                        foreach (RoomListItem item in list)
                        {
                            item.SelectedForReceiving = false;
                        }
                    }
                }
                else
                {
                    foreach (List<RoomListItem> list in roomList)
                    {
                        foreach (RoomListItem item in list)
                        {
                            if (roomID == item.ID)
                            {
                                processInputsAndClear();
                                item.SelectedForReceiving = false;
                                break;
                            }
                        }
                    }
                }
            }
            StartUpdateInputTimer();
            if (requestConnectionEvent != null)
            {
                requestConnectionEvent(this, new RequestConnectionEventArgs { receiving = true, clear = true, roomID = roomID });
            }
        }
        public void ClearSendingRoomsButton(uint roomID)
        {
            if (Enabled)
            {
                processInputsAndClear();
                if (roomID == 0)
                {
                    foreach (List<RoomListItem> list in roomList)
                    {
                        foreach (RoomListItem item in list)
                        {
                            if (item.SelectedForSending)
                            {
                                item.SelectedForSending = false;
                                if (requestConnectionEvent != null)
                                {
                                    requestConnectionEvent(this, new RequestConnectionEventArgs { sending = true, clear = true, roomID = item.ID });
                                }
                            }
                        }
                    }
                    StartUpdateInputTimer();
                }
                else
                {
                    foreach (List<RoomListItem> list in roomList)
                    {
                        foreach (RoomListItem item in list)
                        {
                            if (roomID == item.ID)
                            {
                                item.SelectedForSending = false;
                                processInputsAndClear();
                                StartUpdateInputTimer();
                                if (requestConnectionEvent != null)
                                {
                                    requestConnectionEvent(this, new RequestConnectionEventArgs { sending = true, clear = true, roomID = roomID });
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
        public void ReceiveRoomButton(uint list, uint item)
        {
            if (Enabled)
            {
                uint id = outputRoomList[(int)list - 1][(int)item - 1].ID;
                if (id != 0)
                {
                    ClearSendingRoomsButton(0);
                    foreach (List<RoomListItem> _list in roomList)
                    {
                        foreach (RoomListItem _item in _list)
                        {
                            if (id == _item.ID)
                            {
                                _item.SelectedForSending = true;
                            }
                        }
                    }

                    if (requestConnectionEvent != null)
                    {
                        requestConnectionEvent(this, new RequestConnectionEventArgs { requesting = true, roomID = id });
                    }
                    StartUpdateInputTimer();
                }
            }
        }
        public void SendRoomButton(uint list, uint item)
        {
            if (Enabled)
            {
                uint id = outputRoomList[(int)list - 1][(int)item - 1].ID;
                if (id != 0)
                {
                    foreach (List<RoomListItem> _list in roomList)
                    {
                        foreach (RoomListItem _item in _list)
                        {
                            if (id == _item.ID)
                            {
                                _item.SelectedForReceiving = true;
                            }
                        }
                    }

                    if (requestConnectionEvent != null)
                    {
                        requestConnectionEvent(this, new RequestConnectionEventArgs { sending = true, roomID = id, inputValues = Inputs });
                    }
                    StartUpdateInputTimer();
                }
            }
        }
        public void setOverflow(bool state)
        {
            if (Enabled)
            {
                OverflowEnabled = state;
                if (!OverflowEnabled)
                    ClearReceivingRoomsButton(0);
                //CrestronConsole.PrintLine("Overflow just enabled: {0}", OverflowEnabled);
                AvailabilityNeedsUpdate(true);
                StartUpdateInputTimer();
            }
        }
        public void setRoomState(bool state)
        {
            //CrestronConsole.PrintLine("Room ID: {0} Roomstate is about to be set to: {1}, Enabled: {2}", ID, state, Enabled);
            if(Enabled)
            {
                RoomState = state;
                //CrestronConsole.PrintLine("Room ID: {0} Roomstate just set: {1}, from state: {0}", ID, RoomState, state);
                AvailabilityNeedsUpdate(true);
                StartUpdateInputTimer();
            }
        }
        public void setReceivingSelected(bool state)
        {
            if (Enabled)
            {
                ReceivingSelected = state;
                StartUpdateInputTimer();
            }
        }

        /// <summary>
        /// Sets the RoomName an fires off an updateEvent sending the name to other rooms
        /// </summary>
        /// <param name="_name">Name of the New Room Name</param>
        public void SetRoomName(string _name)
        {
            //CrestronConsole.PrintLine("Room ID: {0} Updating Room Name: {1}", ID, _name);
            Name = _name;
            if (updateEvent != null)
                updateEvent(this, new UpdateEventArgs { name = _name });
            StartUpdateInputTimer();
        }
        #endregion

        #region Update Helpter Functions
        public void UpdateAvailability()
        {
            if (availabilityEvent != null)
            {
                availabilityEvent(this, new AvailabilityEventArgs { availableForSending = AvailableForSending, availableForReceiving = AvailableForReceiving });
            }
        }
        public void UpdateOutputList()
        {
            foreach (List<RoomListItem> list in outputRoomList)
                list.Clear();
            int i = 0;
            if (ReceivingSelected)
            {
                foreach (List<RoomListItem> list in roomList)
                {
                    foreach (RoomListItem item in list)
                    {
                        if (item.AvailableForSending || item.SelectedForSending)
                        {
                            //CrestronConsole.PrintLine("Room: {0} Checking if Available for Sending", Name);
                            if(SwitcherDictionary[ID] == SwitcherDictionary[item.ID])
                            {
                                outputRoomList[i].Add(item);
                            }
                            else if (SwitcherManager.RouteAvailable(SwitcherDictionary[item.ID], SwitcherDictionary[ID], item.ID))//Added for InterSwitcher Logic
                            {
                                outputRoomList[i].Add(item);
                            }    
                        }
                    }
                    if (outputRoomList[i].Count() == 0)
                    {
                        outputRoomList[i].Add(new RoomListItem());
                    }
                    i++;
                }
            }
            else
            {
                foreach (List<RoomListItem> list in roomList)
                {
                    if (AvailableForSending)
                    {
                        foreach (RoomListItem item in list)
                        {
                            if (item.AvailableForReceiving || item.SelectedForReceiving)
                            {
                                //CrestronConsole.PrintLine("Room: {0} Checking if Available for Receiving", Name);
                                if(SwitcherDictionary[ID] == SwitcherDictionary[item.ID])
                                {
                                    outputRoomList[i].Add(item);
                                }
                                else if (SwitcherManager.RouteAvailable(SwitcherDictionary[ID], SwitcherDictionary[item.ID], ID)) //Added for InterSwitcher Logic
                                    outputRoomList[i].Add(item);
                            }
                        }
                        if (outputRoomList[i].Count() == 0)
                        {
                            outputRoomList[i].Add(new RoomListItem());
                        }
                        i++;
                    }
                }
            }

        }
        public void UpdateInputs()
        {
            //CrestronConsole.PrintLine("Update Inputs Called Room: {0}", ID);
            if (updateEvent != null)
                updateEvent(this, new UpdateEventArgs { inputs = Inputs });
        }
        private void ProcessRoom(object o)
        {
            if (Enabled)
            {
                bool isReceiving = false;
                bool isSending = false;
                _eisc.BooleanInput[81].BoolValue = false;
                _eisc.BooleanInput[85].BoolValue = false;
                foreach (List<RoomListItem> list in roomList)
                {
                    foreach (RoomListItem item in list)
                    {
                        if (item.SelectedForReceiving)
                        {
                            isSending = true;
                        }
                        if (item.SelectedForSending)
                        {
                            isReceiving = true;
                        }
                    }
                }
                if (isReceiving)
                {
                    AvailableForReceiving = false;
                    AvailableForSending = false;
                    AvailableForLocalReceiving = false;
                }
                else if (!RoomState)
                {
                    AvailableForReceiving = true;
                    AvailableForSending = false;
                    AvailableForLocalReceiving = false;
                }
                else if (OverflowEnabled)
                {
                    AvailableForReceiving = false;
                    AvailableForSending = true;
                    AvailableForLocalReceiving = true;
                }
                else if (!OverflowEnabled)
                {
                    AvailableForReceiving = false;
                    AvailableForSending = false;
                    AvailableForLocalReceiving = true;
                }
                if(_availabilityNeedsUpdate)
                {
                    UpdateAvailability();
                    AvailabilityNeedsUpdate(false);
                }
                UpdateInputs();
                makeSendingRoomList();
                UpdateOutputList();
                StartUpdateEISCTimer();
            }
        }
        private void ProcessEISC(object o)
        {
            //CrestronConsole.PrintLine("Room: {0} Process EISC Called", Name);
            try
            {
                UpdateEISCSignals();
            }
            catch
            {
                ErrorLog.Error("Error in Room: {0} {1}", ID, Name);
            }
        }
        public void processInputs(RoomInputValues inputValues, uint roomID, string roomName)
        {
            CrestronConsole.PrintLine("Room: {0} Process Inputs Called", Name);
            ReveivingRoomName = roomName;
            CrestronConsole.PrintLine("{0}", inputValues.ToString());
            if (SwitcherDictionary[roomID] == SwitcherDictionary[ID])
            {
                //CrestronConsole.PrintLine("Process Inputs Called inside same switcher");
                ReceivingInputValues = new RoomInputValues(inputValues);
                //CrestronConsole.PrintLine("Display Availability: {0}", inputValues.Displays[0].enabled);
                StartUpdateEISCTimer();
            }
            else
            {
                //CrestronConsole.PrintLine("Room: {0} Processessing Different Switcher Called", Name);
                ReceivingInputValues = SwitcherManager.AttachSendingRoom(roomID, SwitcherDictionary[roomID], ID, SwitcherDictionary[ID], inputValues); //Added for InterSwitcher Logic
                //CrestronConsole.PrintLine("Room: {0} Done Processessing Different Switcher", Name);
                //CrestronConsole.PrintLine(ReceivingInputValues.ToString());
                StartUpdateEISCTimer();
            }


        }
        public void processInputsAndClear()
        {
            CrestronConsole.PrintLine("Process Inputs and Clear Called Room: {0}", ID);
            ReceivingInputValues.Reset();
            ReveivingRoomName = String.Empty;
            StartUpdateEISCTimer();
        }
        private void UpdateEISCSignals()
        {
            //CrestronConsole.PrintLine("Room: {0} Updating EISC", Name);
            #region UpdateBools
            //Displays and Cameras
            //CrestronConsole.PrintLine("Room: {0} Updating Bool Sig 2: {1}", Name, ReceivingInputValues.Displays[0].enabled);
            _eisc.BooleanInput[2].BoolValue = ReceivingInputValues.Displays[0].enabled;
            //CrestronConsole.PrintLine("Room: {0} Updating Bool Sig 4: {1}", Name, ReceivingInputValues.Displays[1].enabled);
            _eisc.BooleanInput[4].BoolValue = ReceivingInputValues.Displays[1].enabled;
            //CrestronConsole.PrintLine("Room: {0} Updating Bool Sig 6: {1}", Name, ReceivingInputValues.Displays[2].enabled);
            _eisc.BooleanInput[6].BoolValue = ReceivingInputValues.Displays[2].enabled;
            //CrestronConsole.PrintLine("Room: {0} Updating Bool Sig 8: {1}", Name, ReceivingInputValues.Displays[3].enabled);
            _eisc.BooleanInput[8].BoolValue = ReceivingInputValues.Displays[3].enabled;
            //CrestronConsole.PrintLine("Room: {0} Updating Bool Sig 10: {1}", Name, ReceivingInputValues.Cameras[0].enabled);
            _eisc.BooleanInput[10].BoolValue = ReceivingInputValues.Cameras[0].enabled;
            //CrestronConsole.PrintLine("Room: {0} Updating Bool Sig 12: {1}", Name, ReceivingInputValues.Cameras[1].enabled);
            _eisc.BooleanInput[12].BoolValue = ReceivingInputValues.Cameras[1].enabled;
            //CrestronConsole.PrintLine("Room: {0} Updating Bool Sig 14: {1}", Name, ReceivingInputValues.Cameras[2].enabled);
            _eisc.BooleanInput[14].BoolValue = ReceivingInputValues.Cameras[2].enabled;

            //_eisc.BooleanInput[81].BoolValue; //Send to System to Notify to Shutdown if Sending Room Drops Send Used in this Function:RecievedRequestFromSenderToRemove() and ProcessUpdate()
            //_eisc.BooleanInput[82].BoolValue; //Send to System to Notify to Show Sending Page if Receiving Room selects(Dont Think we need this)
            //_eisc.BooleanInput[85].BoolValue; //Send to System to Notify to Show Receiving Page if Sending Room Selects Used in this Function: ReceivedReplyFromSendingRoom() and ProcessUpdate()

            #region List Button Available
            uint x = 90;
            foreach (RoomListItem item in outputRoomList[0])
            {
                _eisc.BooleanInput[x].BoolValue = true;
                x++;
            }
            while (x < 105)
            {
                _eisc.BooleanInput[x].BoolValue = false;
                x++;
            }
            foreach (RoomListItem item in outputRoomList[1])
            {
                _eisc.BooleanInput[x].BoolValue = true;
                x++;
            }
            while (x < 115)
            {
                _eisc.BooleanInput[x].BoolValue = false;
                x++;
            }
            foreach (RoomListItem item in outputRoomList[2])
            {
                _eisc.BooleanInput[x].BoolValue = true;
                x++;
            }
            while (x < 125)
            {
                _eisc.BooleanInput[x].BoolValue = false;
                x++;
            }
            foreach (RoomListItem item in outputRoomList[3])
            {
                _eisc.BooleanInput[x].BoolValue = true;
                x++;
            }
            while (x < 135)
            {
                _eisc.BooleanInput[x].BoolValue = false;
                x++;
            }
            foreach (RoomListItem item in outputRoomList[4])
            {
                _eisc.BooleanInput[x].BoolValue = true;
                x++;
            }
            while (x < 145)
            {
                _eisc.BooleanInput[x].BoolValue = false;
                x++;
            }
            foreach (RoomListItem item in outputRoomList[5])
            {
                _eisc.BooleanInput[x].BoolValue = true;
                x++;
            }
            while (x < 155)
            {
                _eisc.BooleanInput[x].BoolValue = false;
                x++;
            }
            #endregion

            #region List Button Checked

            x = 155;
            foreach (RoomListItem item in outputRoomList[0])
            {
                if (item.SelectedForSending || item.SelectedForReceiving)
                    _eisc.BooleanInput[x].BoolValue = true;
                else
                    _eisc.BooleanInput[x].BoolValue = false;
                x++;
            }
            x = 170;
            foreach (RoomListItem item in outputRoomList[1])
            {
                if (item.SelectedForSending || item.SelectedForReceiving)
                    _eisc.BooleanInput[x].BoolValue = true;
                else
                    _eisc.BooleanInput[x].BoolValue = false;
                x++;
            }
            x = 180;
            foreach (RoomListItem item in outputRoomList[2])
            {
                if (item.SelectedForSending || item.SelectedForReceiving)
                    _eisc.BooleanInput[x].BoolValue = true;
                else
                    _eisc.BooleanInput[x].BoolValue = false;
                x++;
            }
            x = 190;
            foreach (RoomListItem item in outputRoomList[3])
            {
                if (item.SelectedForSending || item.SelectedForReceiving)
                    _eisc.BooleanInput[x].BoolValue = true;
                else
                    _eisc.BooleanInput[x].BoolValue = false;
                x++;
            }
            x = 200;
            foreach (RoomListItem item in outputRoomList[4])
            {
                if (item.SelectedForSending || item.SelectedForReceiving)
                    _eisc.BooleanInput[x].BoolValue = true;
                else
                    _eisc.BooleanInput[x].BoolValue = false;
                x++;
            }
            x = 210;
            foreach (RoomListItem item in outputRoomList[5])
            {
                if (item.SelectedForSending || item.SelectedForReceiving)
                    _eisc.BooleanInput[x].BoolValue = true;
                else
                    _eisc.BooleanInput[x].BoolValue = false;
                x++;
            }
            #endregion

            #endregion

            #region Update uints

            //_eisc.UShortInput[7].UShortValue //Display Output Value (Dont Think We Need This)
            //_eisc.UShortInput[8].UShortValue //Display Output Value (Dont Think We Need This)
            //_eisc.UShortInput[9].UShortValue //Display Output Value (Dont Think We Need This)
            //_eisc.UShortInput[10].UShortValue //Display Output Value (Dont Think We Need This)

            _eisc.UShortInput[14].UShortValue = (ushort)ReceivingInputValues.Displays[0].InputValue;
            _eisc.UShortInput[15].UShortValue = (ushort)ReceivingInputValues.Displays[0].IconValue;
            _eisc.UShortInput[16].UShortValue = (ushort)ReceivingInputValues.Displays[1].InputValue;
            _eisc.UShortInput[17].UShortValue = (ushort)ReceivingInputValues.Displays[1].IconValue;
            _eisc.UShortInput[18].UShortValue = (ushort)ReceivingInputValues.Displays[2].InputValue;
            _eisc.UShortInput[19].UShortValue = (ushort)ReceivingInputValues.Displays[2].IconValue;
            _eisc.UShortInput[20].UShortValue = (ushort)ReceivingInputValues.Displays[3].InputValue;
            _eisc.UShortInput[21].UShortValue = (ushort)ReceivingInputValues.Displays[3].IconValue;
            _eisc.UShortInput[22].UShortValue = (ushort)ReceivingInputValues.Cameras[0].InputValue;
            _eisc.UShortInput[23].UShortValue = (ushort)ReceivingInputValues.Cameras[1].InputValue;
            _eisc.UShortInput[24].UShortValue = (ushort)ReceivingInputValues.Cameras[2].InputValue;

            _eisc.UShortInput[32].UShortValue = (ushort)ReceivingInputValues.AudioValue;

            #endregion

            #region Update Strings

            _eisc.StringInput[1].StringValue = ReveivingRoomName;
            //CrestronConsole.PrintLine("Room: {0} Updating Serial Sig 2: {1}", Name, ReceivingInputValues.Displays[0].OutputName);
            _eisc.StringInput[2].StringValue = ReceivingInputValues.Displays[0].OutputName;
            _eisc.StringInput[3].StringValue = ReceivingInputValues.Displays[0].InputName;
            //CrestronConsole.PrintLine("Room: {0} Updating Serial Sig 4: {1}", Name, ReceivingInputValues.Displays[1].OutputName);
            _eisc.StringInput[4].StringValue = ReceivingInputValues.Displays[1].OutputName;
            _eisc.StringInput[5].StringValue = ReceivingInputValues.Displays[1].InputName;
            //CrestronConsole.PrintLine("Room: {0} Updating Serial Sig 6: {1}", Name, ReceivingInputValues.Displays[2].OutputName);
            _eisc.StringInput[6].StringValue = ReceivingInputValues.Displays[2].OutputName;
            _eisc.StringInput[7].StringValue = ReceivingInputValues.Displays[2].InputName;
            //CrestronConsole.PrintLine("Room: {0} Updating Serial Sig 8: {1}", Name, ReceivingInputValues.Displays[3].OutputName);
            _eisc.StringInput[8].StringValue = ReceivingInputValues.Displays[3].OutputName;
            _eisc.StringInput[9].StringValue = ReceivingInputValues.Displays[3].InputName;
            //CrestronConsole.PrintLine("Room: {0} Updating Serial Sig 10: {1}", Name, ReceivingInputValues.Cameras[0].OutputName);
            _eisc.StringInput[10].StringValue = ReceivingInputValues.Cameras[0].OutputName;
            //CrestronConsole.PrintLine("Room: {0} Updating Bool Sig 11: {1}", Name, ReceivingInputValues.Cameras[1].OutputName);
            _eisc.StringInput[11].StringValue = ReceivingInputValues.Cameras[1].OutputName;
            //CrestronConsole.PrintLine("Room: {0} Updating Bool Sig 112: {1}", Name, ReceivingInputValues.Cameras[2].OutputName);
            _eisc.StringInput[12].StringValue = ReceivingInputValues.Cameras[2].OutputName;
            _eisc.StringInput[150].StringValue = ReceivingRoomList;

            x = 13;
            foreach (RoomListItem item in outputRoomList[0])
            {
                _eisc.StringInput[x].StringValue = item.Name;
                x++;
            }
            x = 28;
            foreach (RoomListItem item in outputRoomList[1])
            {
                _eisc.StringInput[x].StringValue = item.Name;
                x++;
            }
            x = 38;
            foreach (RoomListItem item in outputRoomList[2])
            {
                _eisc.StringInput[x].StringValue = item.Name;
                x++;
            }
            x = 48;
            foreach (RoomListItem item in outputRoomList[3])
            {
                _eisc.StringInput[x].StringValue = item.Name;
                x++;
            }
            x = 58;
            foreach (RoomListItem item in outputRoomList[4])
            {
                _eisc.StringInput[x].StringValue = item.Name;
                x++;
            }
            x = 68;
            foreach (RoomListItem item in outputRoomList[5])
            {
                _eisc.StringInput[x].StringValue = item.Name;
                x++;
            }

            #endregion
            //CrestronConsole.PrintLine("Finished Updating EISCs");
        }
        public void makeSendingRoomList()
        {
            ReceivingRoomList = String.Empty;
            foreach (List<RoomListItem> list in roomList)
            {
                foreach (RoomListItem item in list)
                {
                    if (item.SelectedForReceiving)
                        ReceivingRoomList += item.Name + ", ";
                }
            }
            if (ReceivingRoomList == String.Empty)
            {
                ReceivingRoomList = "Not Sending";
            }
            else
            {
                ReceivingRoomList.TrimEnd(',');
            }
        }
        private void AvailabilityNeedsUpdate(bool state)
        {
            _availabilityNeedsUpdate = state;
        }
        public void SetOnline(bool state)
        {
            _eisc.BooleanInput[1].BoolValue = state;
        }
        #endregion

        #region Print Functions
        public void PrintOutputReceivingRoomList()
        {
            CrestronConsole.PrintLine("Output List");
            CrestronConsole.PrintLine(Name);
            CrestronConsole.PrintLine("Floor 1 & 2\t\tFloor 3\t\tFloor 4\t\tFloor 5\t\tFloor 6\t\tFLoor 7");
            CrestronConsole.PrintLine("--------------------------------------------------------------");
            for (int i = 0; i < 20; i++)
            {
                CrestronConsole.PrintLine("{0}:{1}\t\t\t{2}:{3}\t\t{4}:{5}\t\t{6}:{7}\t\t{8}:{9}\t\t{10}:{11}", FormatRoomName(outputRoomList[0], i), FormatRoomReceiving(outputRoomList[0], i),
                                                                             FormatRoomName(outputRoomList[1], i), FormatRoomReceiving(outputRoomList[1], i),
                                                                             FormatRoomName(outputRoomList[2], i), FormatRoomReceiving(outputRoomList[2], i),
                                                                             FormatRoomName(outputRoomList[3], i), FormatRoomReceiving(outputRoomList[3], i),
                                                                             FormatRoomName(outputRoomList[4], i), FormatRoomReceiving(outputRoomList[4], i),
                                                                             FormatRoomName(outputRoomList[5], i), FormatRoomReceiving(outputRoomList[5], i));
            }
        }
        public void PrintOutputSendingRoomList()
        {
            CrestronConsole.PrintLine("Output List");
            CrestronConsole.PrintLine(Name);
            CrestronConsole.PrintLine("Floor 1 & 2\t\tFloor 3\t\tFloor 4\t\tFloor 5\t\tFloor 6\t\tFLoor 7");
            CrestronConsole.PrintLine("--------------------------------------------------------------");
            for (int i = 0; i < 20; i++)
            {
                CrestronConsole.PrintLine("{0}:{1}\t\t\t{2}:{3}\t\t{4}:{5}\t\t{6}:{7}\t\t{8}:{9}\t\t{10}:{11}", FormatRoomName(outputRoomList[0], i), FormatRoomReceiving(outputRoomList[0], i),
                                                                             FormatRoomName(outputRoomList[1], i), FormatRoomReceiving(outputRoomList[1], i),
                                                                             FormatRoomName(outputRoomList[2], i), FormatRoomReceiving(outputRoomList[2], i),
                                                                             FormatRoomName(outputRoomList[3], i), FormatRoomReceiving(outputRoomList[3], i),
                                                                             FormatRoomName(outputRoomList[4], i), FormatRoomReceiving(outputRoomList[4], i),
                                                                             FormatRoomName(outputRoomList[5], i), FormatRoomReceiving(outputRoomList[5], i));
            }
        }
        public void PrintRoomList()
        {
            CrestronConsole.PrintLine("RoomList");
            CrestronConsole.PrintLine(Name);
            CrestronConsole.PrintLine("Floor 1 & 2\t\t\tFloor 3\t\t\tFloor 4\t\t\tFloor 5\t\t\tFloor 6\t\t\tFLoor 7");
            CrestronConsole.PrintLine("--------------------------------------------------------------");
            for (int i = 0; i < 20; i++)
            {
                CrestronConsole.PrintLine("{0}:{1}:{2}:{3}:{4}\t\t{5}:{6}:{7}:{8}:{9}\t\t{10}:{11}:{12}:{13}:{14}\t\t{15}:{16}:{17}:{18}:{19}\t\t{20}:{21}:{22}:{23}:{24}\t\t{25}:{26}:{27}:{28}:{29}",
                                         FormatRoomName(roomList[0], i), FormatRoomReceiving(roomList[0], i), FormatRoomSending(roomList[0], i), FormatAvailRoomReceiving(roomList[0], i), FormatAvailRoomSending(roomList[0], i),
                                         FormatRoomName(roomList[1], i), FormatRoomReceiving(roomList[1], i), FormatRoomSending(roomList[1], i), FormatAvailRoomReceiving(roomList[1], i), FormatAvailRoomSending(roomList[1], i),
                                         FormatRoomName(roomList[2], i), FormatRoomReceiving(roomList[2], i), FormatRoomSending(roomList[2], i), FormatAvailRoomReceiving(roomList[2], i), FormatAvailRoomSending(roomList[2], i),
                                         FormatRoomName(roomList[2], i), FormatRoomReceiving(roomList[2], i), FormatRoomSending(roomList[2], i), FormatAvailRoomReceiving(roomList[2], i), FormatAvailRoomSending(roomList[2], i),
                                         FormatRoomName(roomList[3], i), FormatRoomReceiving(roomList[3], i), FormatRoomSending(roomList[3], i), FormatAvailRoomReceiving(roomList[3], i), FormatAvailRoomSending(roomList[3], i),
                                         FormatRoomName(roomList[4], i), FormatRoomReceiving(roomList[4], i), FormatRoomSending(roomList[4], i), FormatAvailRoomReceiving(roomList[4], i), FormatAvailRoomSending(roomList[4], i),
                                         FormatRoomName(roomList[5], i), FormatRoomReceiving(roomList[5], i), FormatRoomSending(roomList[5], i), FormatAvailRoomReceiving(roomList[5], i), FormatAvailRoomSending(roomList[5], i));
            }
        }
        public void PrintRoomInputs()
        {
            foreach (VideoSource input in Inputs.Displays)
            {
                CrestronConsole.PrintLine("Room Name: {0}, Display Name: {1}, Display Input: {2}, Display Enabled, {3}", Name, input.OutputName, input.InputName, input.enabled);
            }
            foreach (VideoSource input in Inputs.Cameras)
            {
                CrestronConsole.PrintLine("Room Name: {0}, Camera Name: {1}, Camera Enabled, {2}", Name, input.OutputName, input.enabled);
            }
        }
        public void PrintRoomStatus()
        {
            CrestronConsole.PrintLine("Room Name: {0}, Avail for sending: {1}, Avail for receiving: {2}, Room State: {3}, Overflow On: {4}, Enabled: {5}", Name, AvailableForSending, AvailableForReceiving, RoomState, OverflowEnabled, Enabled);
        }

        public void PrintRoomReceivingInputs()
        {
            foreach (VideoSource input in ReceivingInputValues.Displays)
            {
                CrestronConsole.PrintLine("Room: {0} ,Display Name: {1}, Display Input: {2}, Display Enabled, {3}", Name, input.OutputName, input.InputName, input.enabled);
            }
            foreach (VideoSource input in ReceivingInputValues.Cameras)
            {
                CrestronConsole.PrintLine("Room: {0} ,Camera Name: {1}, Camera Enabled, {2}", Name, input.OutputName, input.enabled);
            }
        }

        //Formatting functions used with Print functions
        private static string FormatRoomName(List<RoomListItem> list, int index)
        {
            try 
	        {	        
        	    return list[index].Name + " ";	
	        }
	        catch (Exception)
	        {
        	    return "None Rm";	
	        } 
        }
        private static string FormatRoomSending(List<RoomListItem> list, int index)
        {
            try
            {
                if (list[index].SelectedForReceiving)
                    return "X";
                else
                {
                    return "O";
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
        private static string FormatAvailRoomSending(List<RoomListItem> list, int index)
        {
            try
            {
                if (list[index].AvailableForSending)
                    return "X";
                else
                {
                    return "O";
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
        private static string FormatAvailRoomReceiving(List<RoomListItem> list, int index)
        {
            try
            {
                if (list[index].AvailableForReceiving)
                    return "X";
                else
                {
                    return "O";
                }
            }
            catch (Exception)
            {
                return "O";
            }
        }
        private static string FormatRoomReceiving(List<RoomListItem> list, int index)
        {
            try
            {
                if (list[index].SelectedForSending)
                    return "X";
                else
                {
                    return "O";
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion

        #region Thread Functions
        void StartOneshotThread()
        {
            Thread th = new Thread(SendOneshotToSystemShutdown, new Object(), Thread.eThreadStartOptions.Running);
        }
        object SendOneshotToSystemShutdown(object o)
        {
            _eisc.BooleanInput[85].BoolValue = true;
            Thread.Sleep(2);
            _eisc.BooleanInput[85].BoolValue = false;

            return null;
        }

        void StartUpdateInputTimer()
        {
            ProcessRoomTimer.Reset(200);
        }

        void StartUpdateEISCTimer()
        {
            ProcessEISCTimer.Reset(1000);
        }
        #endregion
    }
}

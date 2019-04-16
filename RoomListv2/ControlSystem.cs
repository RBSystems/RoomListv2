using System;
using System.Linq;
using System.Collections.Generic;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.EthernetCommunication;
using Crestron.SimplSharpPro.UI;

namespace RoomListv2
{
    public class ControlSystem : CrestronControlSystem
    {

        #region GlobalVariables
        private List<ThreeSeriesTcpIpEthernetIntersystemCommunications> EISCs;
        private List<ThreeSeriesTcpIpEthernetIntersystemCommunications> SwitcherEISCs;
        private static List<Room> rooms;
        private static Dictionary<uint, uint> roomListDictionary;
        private static Dictionary<uint, uint> roomSwitcherDictionary;
        private static SwitcherManager switcherManager;
        #endregion

        /// <summary>
        /// ControlSystem Constructor. Starting point for the SIMPL#Pro program.
        /// Use the constructor to:
        /// * Initialize the maximum number of threads (max = 400)
        /// * Register devices
        /// * Register event handlers
        /// * Add Console Commands
        /// 
        /// Please be aware that the constructor needs to exit quickly; if it doesn't
        /// exit in time, the SIMPL#Pro program will exit.
        /// 
        /// You cannot send / receive data in the constructor
        /// </summary>
        public ControlSystem()
            : base()
        {
            try
            {

                Thread.MaxNumberOfUserThreads = 80;

                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(ControlSystem_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(ControlSystem_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(ControlSystem_ControllerEthernetEventHandler);

                CrestronConsole.AddNewConsoleCommand(PrintReceivingOutputList, "PrintROutputList", "This will Print the Receiving Output List", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(PrintSendingOutputList, "PrintSOutputList", "This will Print the Sending Output List", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(UpdateOutputList, "UpdateOutputList", "This will Update the Output List", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(PrintRoomList, "PrintRoomList", "This will print the List", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(PrintRoomOutputs, "PrintRoomOutputs", "This will print the Outputs", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(PrintReceivingInputs, "PrintReceivingInputs", "This will print the Outputs", ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(PrintRoomStatus, "PrintRoomStatus", "Prints the rooms status", ConsoleAccessLevelEnum.AccessOperator);
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        #region Room Class: Event Handlers
        static void room_requestConnectionEvent(Room sender, RequestConnectionEventArgs e)
        {
            if (e.requesting)
            {
                foreach (Room room in rooms)
                {
                    if (room.ID == e.roomID)
                    {
                        room.RecievedRequestFromReceivingRoom(sender.ID);
                    }
                }
            }
            else if (e.sending && !e.clear)
            {
                foreach (Room room in rooms)
                {
                    if (room.ID == e.roomID)
                    {
                        room.ReceivedReplyFromSendingRoom(sender.ID, e.inputValues, sender.Name);
                    }
                }
            }
            else if (e.sending && e.clear)
            {
                foreach (Room room in rooms)
                {
                    if (room.ID == e.roomID)
                    {
                        room.RecievedRequestFromReceiverToRemove(sender.ID);
                    }
                }
            }
            else if (e.receiving && !e.clear)
            {
                foreach (Room room in rooms)
                    if (room.ID == e.roomID)
                    {
                        room.ReceivedReplyFromSendingRoom(sender.ID, e.inputValues, sender.Name);
                    }
            }
            else if (e.receiving && e.clear)
            {
                foreach (Room room in rooms)
                    if (room.ID == e.roomID || e.roomID == 0)
                    {
                        room.RecievedRequestFromSenderToRemove(sender.ID);
                    }
            }
        }

        static void room_availabilityEvent(Room sender, AvailabilityEventArgs e)
        {
            foreach (Room room in rooms)
            {
                room.RecievedUpdateListItem(roomListDictionary[sender.ID], sender.ID, sender.Name, e.availableForSending, e.availableForReceiving, sender.Enabled);
            }

        }

        static void room_updateEvent(Room sender, UpdateEventArgs e)
        {
            if (e.name != null)
            {
                foreach (Room room in rooms)
                {
                    room.RecievedUpdateRoomName(sender.ID, e.name, roomListDictionary[sender.ID]);
                }
            }
            else if (e.inputs != null)
            {
                foreach (Room room in rooms)
                {
                    room.RecievedUpdateInput(sender.ID, e.inputs);
                }
            }

        }
        #endregion

        #region EISC Event Handlers

        #endregion

        /// <summary>
        /// InitializeSystem - this method gets called after the constructor 
        /// has finished. 
        /// 
        /// Use InitializeSystem to:
        /// * Start threads
        /// * Configure ports, such as serial and verisports
        /// * Start and initialize socket connections
        /// Send initial device configurations
        /// 
        /// Please be aware that InitializeSystem needs to exit quickly also; 
        /// if it doesn't exit in time, the SIMPL#Pro program will exit.
        /// </summary>
        public override void InitializeSystem()
        {
            try
            {
                #region Room Class: Initialize roomListDictionary
                //Creates the Dictionary that holds the roomID and what list that roomID should live
                BuildRoomListDictionary(40);
                BuildRoomSwitcherDictionary(40);
                #endregion

                #region EISC Creation
                if (this.SupportsEthernet)
                {
                    EISCs = new List<ThreeSeriesTcpIpEthernetIntersystemCommunications>();
                    SwitcherEISCs = new List<ThreeSeriesTcpIpEthernetIntersystemCommunications>();
                    for (uint i = 0; i < 10; i++)
                    {
                        EISCs.Add(new ThreeSeriesTcpIpEthernetIntersystemCommunications((i + 192), "127.0.0.2", this));
                    }
                    for (uint i = 0; i < 3; i++)
                    {
                        SwitcherEISCs.Add(new ThreeSeriesTcpIpEthernetIntersystemCommunications((i + 233), "127.0.0.2", this));
                    }
                }
                #endregion
                try
                {
                    switcherManager = new SwitcherManager(SwitcherEISCs);
                }
                catch(Exception e)
                {
                    ErrorLog.Error("SwitchManager Error, Error in InitializeSystem: {0}", e.Message);   
                }

                #region Room Class Instantiate Rooms
                rooms = new List<Room>();

                for (uint i = 0; i < 10; i++)
                {
                    rooms.Add(new Room(i + 1, String.Format("Room {0}", i + 1), roomSwitcherDictionary, EISCs[(int)i], switcherManager));
                }

                foreach (Room room in rooms)
                {
                    for (int i = 0; i < rooms.Count; i++)
                        room.AddRoomToList(rooms[i].ID, rooms[i].Name, rooms[i].Enabled, roomListDictionary[rooms[i].ID]);
                }
                #endregion

                #region Room Class Attach Event Handlers to Rooms
                foreach (Room room in rooms)
                {
                    room.availabilityEvent += new Room.AvailabilityEvent(room_availabilityEvent);
                    room.updateEvent += new Room.UpdateEvent(room_updateEvent);
                    room.requestConnectionEvent += new Room.RequestConnectionEvent(room_requestConnectionEvent);
                }
                #endregion

                #region EISC Register
                if (this.SupportsEthernet)
                {
                    foreach (ThreeSeriesTcpIpEthernetIntersystemCommunications eisc in EISCs)
                    {
                        if (eisc.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                        {
                            ErrorLog.Error("Error in Registering EISC: {0}", eisc.RegistrationFailureReason);
                        }
                    }
                }
                #endregion

            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        void BuildRoomListDictionary(uint numberOfRooms)
        {
            roomListDictionary = new Dictionary<uint, uint>();  //Create a new Dictionary to hold the list of rooms and their list number they belong in
            for (uint i = 1; i <= numberOfRooms; i++)
            {
                if (i < 4)
                    roomListDictionary.Add(i, 1);
                else if (i < 10)
                    roomListDictionary.Add(i, 2);
                else if (i < 20)
                    roomListDictionary.Add(i, 3);
                else if (i < 31)
                    roomListDictionary.Add(i, 4);
                else if (i < 38)
                    roomListDictionary.Add(i, 5);
                else if (i <= 40)
                    roomListDictionary.Add(i, 6);
            }

        }
        void BuildRoomSwitcherDictionary(uint numberOfRooms)
        {
            roomSwitcherDictionary = new Dictionary<uint, uint>(); //Use this to pick which rooms go where
            for (uint i = 1; i <= numberOfRooms; i++)
            {
                if (i < 4)
                    roomSwitcherDictionary.Add(i, 1);
                else if (i < 31)
                    roomSwitcherDictionary.Add(i, 2);
                else if (i <= 40)
                    roomSwitcherDictionary.Add(i, 3);
            }
        }

        #region Control System Event Handlers
        /// <summary>
        /// Event Handler for Ethernet events: Link Up and Link Down. 
        /// Use these events to close / re-open sockets, etc. 
        /// </summary>
        /// <param name="ethernetEventArgs">This parameter holds the values 
        /// such as whether it's a Link Up or Link Down event. It will also indicate 
        /// wich Ethernet adapter this event belongs to.
        /// </param>
        void ControlSystem_ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {//Determine the event type Link Up or Link Down
                case (eEthernetEventType.LinkDown):
                    //Next need to determine which adapter the event is for. 
                    //LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                        //
                    }
                    break;
                case (eEthernetEventType.LinkUp):
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {

                    }
                    break;
            }
        }

        /// <summary>
        /// Event Handler for Programmatic events: Stop, Pause, Resume.
        /// Use this event to clean up when a program is stopping, pausing, and resuming.
        /// This event only applies to this SIMPL#Pro program, it doesn't receive events
        /// for other programs stopping
        /// </summary>
        /// <param name="programStatusEventType"></param>
        void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Stopping):
                    foreach (Room room in rooms)
                    {
                        room.SetOnline(false);
                    }
                    foreach(ThreeSeriesTcpIpEthernetIntersystemCommunications eisc in EISCs)
                    {
                        eisc.UnRegister();
                    }
                    //The program has been stopped.
                    //Close all threads. 
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events
                    break;
            }

        }

        /// <summary>
        /// Event Handler for system events, Disk Inserted/Ejected, and Reboot
        /// Use this event to clean up when someone types in reboot, or when your SD /USB
        /// removable media is ejected / re-inserted.
        /// </summary>
        /// <param name="systemEventType"></param>
        void ControlSystem_ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case (eSystemEventType.DiskInserted):
                    //Removable media was detected on the system
                    break;
                case (eSystemEventType.DiskRemoved):
                    //Removable media was detached from the system
                    break;
                case (eSystemEventType.Rebooting):
                    //The system is rebooting. 
                    //Very limited time to preform clean up and save any settings to disk.
                    break;
            }

        }
        #endregion

        void PrintReceivingOutputList(string param)
        {
            foreach(Room room in rooms)
            {
                if(room.ID == int.Parse(param))
                {
                    rooms[int.Parse(param) - 1].PrintOutputReceivingRoomList();
                    return;
                }
            }
            CrestronConsole.PrintLine("Room Not Found");   
        }

        void PrintSendingOutputList(string param)
        {
            foreach (Room room in rooms)
            {
                if (room.ID == int.Parse(param))
                {
                    rooms[int.Parse(param) - 1].PrintOutputSendingRoomList();
                    return;
                }
            }
            CrestronConsole.PrintLine("Room Not Found");
        }

        void UpdateOutputList(string param)
        {
            foreach (Room room in rooms)
            {
                if (room.ID == int.Parse(param))
                {
                    rooms[int.Parse(param) - 1].UpdateOutputList();
                    return;
                }
            }
            CrestronConsole.PrintLine("Room Not Found");
        }

        void PrintRoomList(string param)
        {
            foreach (Room room in rooms)
            {
                if (room.ID == int.Parse(param))
                {
                    rooms[int.Parse(param) - 1].PrintRoomList();
                    return;
                }
            }
            CrestronConsole.PrintLine("Room Not Found");
        }

        void PrintRoomOutputs(string param)
        {
            for(int i = 0; i < rooms.Count(); i++)
            {
                rooms[i].PrintRoomInputs();
            }
        }

        void PrintReceivingInputs(string param)
        {
            for(int i = 0; i < rooms.Count(); i++)
            {
                rooms[i].PrintRoomReceivingInputs();
            }
        }

        void PrintRoomStatus(string param)
        {
            foreach (Room room in rooms)
            {
                if (room.ID == int.Parse(param))
                {
                    rooms[int.Parse(param) - 1].PrintRoomStatus();
                    return;
                }
            }
            CrestronConsole.PrintLine("Room Not Found");
        }
    }
}
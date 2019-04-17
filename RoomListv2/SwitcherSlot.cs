using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace RoomListv2
{
    public class SwitcherSlot
    {
        public uint SendingRoomID { set; get; }
        public List<uint> ReceivingRoomIDs { set; get; }
        public List<uint> Outputs { set; get; }
        public List<uint> Inputs { set; get; }
        public bool Available { set; get; }
        public RoomInputValues RouteValues { set; get; }

        public SwitcherSlot(uint startingOutput, uint startingInput)
        {
            SendingRoomID = 0;
            ReceivingRoomIDs = new List<uint>();
            Outputs = new List<uint>();
            Inputs = new List<uint>();
            Available = true;
            RouteValues = new RoomInputValues();
            for (uint i = 0; i < 4; i++)
            {
                Outputs.Add((startingOutput + i));
                Inputs.Add((startingInput + i));
            }
        }

        public RoomInputValues AddSlot(uint sendingRoomID, uint receivingRoomID, RoomInputValues inputValues)
        {
            //CrestronConsole.PrintLine("Adding Sending Room {0} an Receiving Room {1} to slot", sendingRoomID, receivingRoomID);
            SendingRoomID = sendingRoomID;
            RouteValues = new RoomInputValues(inputValues);
            RoomInputValues _inputValues;
            _inputValues = new RoomInputValues(inputValues);
            bool IDFound = false;
            foreach (uint RoomID in ReceivingRoomIDs)
            {
                if (receivingRoomID == RoomID)
                {
                    IDFound = true;
                }
            }
            if (!IDFound)
            {
                ReceivingRoomIDs.Add(receivingRoomID);
            }
            Available = false;
            
            _inputValues.Displays[0].InputValue = Inputs[0];
            _inputValues.Displays[1].InputValue = Inputs[1];
            _inputValues.Displays[2].enabled = false;
            _inputValues.Displays[3].enabled = false;
            _inputValues.Cameras[0].InputValue = Inputs[2];
            _inputValues.Cameras[1].InputValue = Inputs[3];
            _inputValues.Cameras[2].enabled = false;
            CrestronConsole.Print("Sending Room ID: {0} || ", SendingRoomID);
            foreach(uint receivingRoom in ReceivingRoomIDs)
            {
                CrestronConsole.Print("Receiving Room ID: {0} || ", receivingRoom);
            }
            CrestronConsole.PrintLine("=================");
            return _inputValues;
        }

        public bool RemoveSlot(uint sendingRoomID, uint receivingRoomID)
        {
            if (ReceivingRoomIDs.Contains(receivingRoomID))
            {
                ReceivingRoomIDs.Remove(receivingRoomID);
                CrestronConsole.PrintLine("Removing Receiving Room ID: {0}", receivingRoomID);
            }
            if (ReceivingRoomIDs.Count == 0)
            {
                SendingRoomID = 0;
                CrestronConsole.PrintLine("Setting Slot to Available!");
                Available = true;
                RouteValues.Reset();
                return true;
            }
            CrestronConsole.Print("Sending Room ID: {0} || ", SendingRoomID);
            foreach (uint receivingRoom in ReceivingRoomIDs)
            {
                CrestronConsole.Print("Receiving Room ID: {0} || ", receivingRoom);
            }
            CrestronConsole.PrintLine("=================");
            return false;
        }
    }
}
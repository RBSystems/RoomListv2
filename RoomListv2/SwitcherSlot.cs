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

            for (uint i = 0; i < 4; i++)
            {
                Outputs.Add((startingOutput + i));
                Inputs.Add((startingInput + i));
            }
        }

        public RoomInputValues AddSlot(uint sendingRoomID, uint receivingRoomID, RoomInputValues inputValues)
        {
            SendingRoomID = sendingRoomID;
            RouteValues = inputValues;
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
            
            inputValues.Displays[0].InputValue = Inputs[0];
            inputValues.Displays[1].InputValue = Inputs[1];
            inputValues.Displays[2].enabled = false;
            inputValues.Displays[3].enabled = false;
            inputValues.Cameras[0].InputValue = Inputs[2];
            inputValues.Cameras[1].InputValue = Inputs[3];
            inputValues.Cameras[1].enabled = false;
            return inputValues;
        }

        public bool RemoveSlot(uint sendingRoomID, uint receivingRoomID)
        {
            bool IDFound = false;
            uint i = 0;
            foreach (uint RoomID in ReceivingRoomIDs)
            {
                if (receivingRoomID == RoomID)
                {
                    IDFound = true;
                    break;
                }
                i++;
            }
            if (IDFound)
            {
                ReceivingRoomIDs.Remove(i);
            }
            if (ReceivingRoomIDs.Count == 0)
            {
                SendingRoomID = 0;
                Available = true;
                RouteValues.Reset();
                return true;
            }
            return false;
        }
    }
}
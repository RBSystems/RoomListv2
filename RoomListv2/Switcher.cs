using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.EthernetCommunication;

namespace RoomListv2
{
    public class Switcher
    {
        public List<SwitcherSlot> SendingSlots { set; get; }
        public uint SwitcherID { set; get; }
        public List<uint> Outputs;
        public ThreeSeriesTcpIpEthernetIntersystemCommunications EISC;
        public Switcher(uint switcherID, ThreeSeriesTcpIpEthernetIntersystemCommunications _eisc)
        {
            SwitcherID = switcherID;
            Outputs = new List<uint>();
            EISC = _eisc;
        }

        public bool PathAvailable(uint sendingRoomId)
        {
            foreach (SwitcherSlot slot in SendingSlots)
            {
                if (slot.Available || slot.SendingRoomID == sendingRoomId)
                    return true;
            }
            return false;
        }

        public RoomInputValues AddSlot(uint sendingRoomID, uint receivingRoomID, RoomInputValues inputValues)
        {
            foreach(SwitcherSlot slot in SendingSlots)
                if (slot.Available || slot.SendingRoomID == sendingRoomID)
                {
                    inputValues = slot.AddSlot(sendingRoomID, receivingRoomID, inputValues);
                    UpdateOutputs();
                    return inputValues;
                }
            CrestronConsole.PrintLine("Error!!! No Room attached in Switcher AddSlot");
            return new RoomInputValues();
        }

        public RoomInputValues AddSlot(uint sendingRoomID, uint receivingRoomID, RoomInputValues inputValues, uint slotNumber)
        {
            inputValues = SendingSlots[(int)slotNumber].AddSlot(sendingRoomID, receivingRoomID, inputValues);
            UpdateOutputs();
            return inputValues;

        }

        public bool RemoveSlots(uint sendingRoomID, uint receivingRoomID)
        {
            foreach (SwitcherSlot slot in SendingSlots)
            {
                return slot.RemoveSlot(sendingRoomID, receivingRoomID);
            }
            return false;
        }

        public void UpdateOutputs()
        {
            for (int i = 0; i < SendingSlots.Count; i++)
            {
                EISC.UShortInput[(uint)(1 + (i * 4))].ShortValue = (short)SendingSlots[i].RouteValues.Displays[0].InputValue;
                EISC.UShortInput[(uint)(2 + (i * 4))].ShortValue = (short)SendingSlots[i].RouteValues.Displays[1].InputValue;
                EISC.UShortInput[(uint)(3 + (i * 4))].ShortValue = (short)SendingSlots[i].RouteValues.Cameras[0].InputValue;
                EISC.UShortInput[(uint)(4 + (i * 4))].ShortValue = (short)SendingSlots[i].RouteValues.Cameras[1].InputValue;
            }
        }
    }
}
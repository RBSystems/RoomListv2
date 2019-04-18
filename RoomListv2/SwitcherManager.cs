using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.EthernetCommunication;

namespace RoomListv2
{
    public class SwitcherManager
    {

        List<Switcher> Switchers;
        public SwitcherManager(List<ThreeSeriesTcpIpEthernetIntersystemCommunications> eiscs)
        {
            Switchers = new List<Switcher>();
            uint i = 1;
            foreach (ThreeSeriesTcpIpEthernetIntersystemCommunications eisc in eiscs)
            {
                try
                {
                    Switchers.Add(new Switcher(i, eisc)); //Switchers[0]
                    i++;
                }
                catch (Exception e)
                {
                    ErrorLog.Error("Error In Switcher Manager Adding Switchers");
                }
            }
            try{
            Switchers[0].SendingSlots.Add(new SwitcherSlot(41, 73));
            Switchers[0].SendingSlots.Add(new SwitcherSlot(45, 77));
            Switchers[1].SendingSlots.Add(new SwitcherSlot(81, 81));
            Switchers[1].SendingSlots.Add(new SwitcherSlot(85, 85));
            Switchers[1].SendingSlots.Add(new SwitcherSlot(89, 41));
            Switchers[1].SendingSlots.Add(new SwitcherSlot(93, 45));
            Switchers[2].SendingSlots.Add(new SwitcherSlot(105, 65));
            Switchers[2].SendingSlots.Add(new SwitcherSlot(109, 69));
            }catch(Exception e)
            {ErrorLog.Error("Error In Switcher Manager Adding Switcher Slots");
            }
        }

        public bool RouteAvailable(uint sendingSwitcher, uint receivingSwitcher, uint sendingRoomId)
        {
            //CrestronConsole.PrintLine("Room ID: {0} Checking Route Available sendingSwitcher {1} receiving {2}", sendingRoomId, sendingSwitcher, receivingSwitcher);
            if (((int)sendingSwitcher - (int)receivingSwitcher) == 1)
            {
                //CrestronConsole.PrintLine("Room ID: {0} Checking Route Available sendingSwitcher - receiving == 1", sendingRoomId);
                return SingleRouteAvailable(sendingSwitcher, receivingSwitcher, sendingRoomId);
            }

            else if (((int)sendingSwitcher - (int)receivingSwitcher) == -1)
            {
                //CrestronConsole.PrintLine("Room ID: {0} Checking Route Available sendingSwitcher - receiving == -1", sendingRoomId);
                return SingleRouteAvailable(sendingSwitcher, receivingSwitcher, sendingRoomId);
            }

            else if (((int)sendingSwitcher - (int)receivingSwitcher) == -2)
            {
                //CrestronConsole.PrintLine("Room ID: {0} Checking Route Available sendingSwitcher - receiving == -2", sendingRoomId);
                if (SingleRouteAvailable(sendingSwitcher, sendingSwitcher + 1, sendingRoomId) &&
                    SingleRouteAvailable(sendingSwitcher + 1, sendingSwitcher + 2, sendingRoomId))
                    return true;
            }
            else if (((int)sendingSwitcher - (int)receivingSwitcher) == 2)
            {
                //CrestronConsole.PrintLine("Room ID: {0} Checking Route Available sendingSwitcher - receiving == 2", sendingRoomId);
                if (SingleRouteAvailable(sendingSwitcher, sendingSwitcher - 1, sendingRoomId) &&
                    SingleRouteAvailable(sendingSwitcher - 1, sendingSwitcher - 2, sendingRoomId))
                    return true;
            }
            return false;
        }

        private bool SingleRouteAvailable(uint sendingSwitcher, uint receivingSwitcher, uint sendingRoomID)
        {
            foreach (Switcher switcher in Switchers)
            {
                if (sendingSwitcher == switcher.SwitcherID)
                {
                    if (sendingSwitcher == 2 && receivingSwitcher == 1)
                    {
                        //CrestronConsole.PrintLine("sendingRoomID: {0} Checking if sending swithcer == 2, and receiving switcher == 1 path is avaiable", sendingRoomID);
                        if (switcher.SendingSlots[2].Available || switcher.SendingSlots[3].Available || switcher.SendingSlots[2].SendingRoomID == sendingRoomID || switcher.SendingSlots[3].SendingRoomID == sendingRoomID)
                        {
                            
                            return true;
                        }
                    }
                    else if (sendingSwitcher == 2 && receivingSwitcher == 3)
                    {
                        //CrestronConsole.PrintLine("sendingRoomID: {0} Checking if sending swithcer == 2, and receiving switcher == 3 path is avaiable", sendingRoomID);
                        if (switcher.SendingSlots[0].Available || switcher.SendingSlots[1].Available || switcher.SendingSlots[0].SendingRoomID == sendingRoomID || switcher.SendingSlots[1].SendingRoomID == sendingRoomID)
                        {

                            return true;
                        }
                    }
                    else
                    {
                        //CrestronConsole.PrintLine("sendingRoomID: {0} Calling switcher.PathAvailable", sendingRoomID);
                        return switcher.PathAvailable(sendingRoomID);
                    }
                }
            }
            return false;
        }

        public RoomInputValues AttachSendingRoom(uint sendingRoomID, uint sendingSwitcher, uint receivingRoomID, uint receivingSwitcher, RoomInputValues roomValues)
        {
            //CrestronConsole.PrintLine("Sending Room ID {0} Called AttacheSendingRoom, Sending Switcher {1}, Receiving Room ID {2}, ReceivingSwitcher {3}", sendingRoomID, sendingSwitcher, receivingRoomID, receivingSwitcher);
            RoomInputValues roomInputVals;
            #region Single Switcher Logic
            if ((((int)sendingSwitcher - (int)receivingSwitcher) == 1) && sendingSwitcher != 2)
            {
                //CrestronConsole.PrintLine("Sending Switcher - Receiving Switcher = 1 and Switcher != 2");
                return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues);
            }
            else if((((int)sendingSwitcher - (int)receivingSwitcher) == -1) && sendingSwitcher != 2)
            {
                //CrestronConsole.PrintLine("Sending Switcher - Receiving Switcher = -1 and Switcher != 2");
                return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues);
            }

            else if (((((int)sendingSwitcher - (int)receivingSwitcher) == 1) || (((int)sendingSwitcher - (int)receivingSwitcher) == -1)) && sendingSwitcher == 2)
            {
                //CrestronConsole.PrintLine("Sending Switcher - Receiving Switcher = -1 or 1 and Sending Switcher == 2");
                if (sendingSwitcher == 2 && receivingSwitcher == 3)
                {
                    //CrestronConsole.PrintLine("Sending Switcher = 2 and Receiving Switcher = 3 and Sending Switcher == 2");
                    if (Switchers[(int)sendingSwitcher - 1].SendingSlots[0].Available || Switchers[(int)sendingSwitcher - 1].SendingSlots[0].SendingRoomID == sendingRoomID)
                    {
                        //CrestronConsole.PrintLine("Sending switcher Slot 0 is available or Sending Room ID Already in SendingSlot");
                        return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues, 0);
                    }
                    else if (Switchers[(int)sendingSwitcher - 1].SendingSlots[1].Available || Switchers[(int)sendingSwitcher - 1].SendingSlots[1].SendingRoomID == sendingRoomID)
                    {
                        //CrestronConsole.PrintLine("Sending switcher Slot 1 is available or Sending Room ID Already in SendingSlot");
                        return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues, 1);
                    }
                }
                else if (sendingSwitcher == 2 && receivingSwitcher == 1)
                {
                    //CrestronConsole.PrintLine("Sending Switcher = 2 and Receiving Switcher = 1 and Sending Switcher == 2");
                    if (Switchers[(int)sendingSwitcher - 1].SendingSlots[2].Available || Switchers[(int)sendingSwitcher - 1].SendingSlots[2].SendingRoomID == sendingRoomID)
                    {
                        //CrestronConsole.PrintLine("Sending switcher Slot 2 is available or Sending Room ID Already in SendingSlot");
                        return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues, 2);
                    }
                    else if (Switchers[(int)sendingSwitcher - 1].SendingSlots[3].Available || Switchers[(int)sendingSwitcher - 1].SendingSlots[3].SendingRoomID == sendingRoomID)
                    {
                        //CrestronConsole.PrintLine("Sending switcher Slot 3 is available or Sending Room ID Already in SendingSlot");
                        return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues, 3);
                    }
                }
            }
            #endregion
            #region Mutiple Switcher Logic
            else if (((int)sendingSwitcher - (int)receivingSwitcher) == -2)
            {
                roomInputVals = new RoomInputValues(Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues));
                if (Switchers[1].SendingSlots[0].Available || Switchers[1].SendingSlots[0].SendingRoomID == sendingRoomID)
                {
                    return Switchers[1].AddSlot(sendingRoomID, receivingRoomID, roomInputVals, 0);
                }
                else if (Switchers[1].SendingSlots[1].Available || Switchers[1].SendingSlots[1].SendingRoomID == sendingRoomID)
                {
                    return Switchers[1].AddSlot(sendingRoomID, receivingRoomID, roomInputVals, 1);
                }
            }
            else
            {
                roomInputVals = new RoomInputValues(Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues));
                if (Switchers[1].SendingSlots[2].Available || Switchers[1].SendingSlots[2].SendingRoomID == sendingRoomID)
                {
                    return Switchers[1].AddSlot(sendingRoomID, receivingRoomID, roomInputVals, 2);
                }
                else if (Switchers[1].SendingSlots[3].Available || Switchers[1].SendingSlots[3].SendingRoomID == sendingRoomID)
                {
                    return Switchers[1].AddSlot(sendingRoomID, receivingRoomID, roomInputVals, 3);
                }
            }
            #endregion
            //CrestronConsole.PrintLine("Error!!! No Room attached in SwitcherManager AttachSendingRoom");
            return new RoomInputValues();
        }

        public void ClearReceivingRoom(uint sendingRoomID, uint receivingRoomID)
        {
            foreach( Switcher switcher in Switchers)
            {
                if (switcher.RemoveSlots(sendingRoomID, receivingRoomID))
                {
                    switcher.UpdateOutputs();
                }
            }
            
        }
    }
}

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
            Switchers[0].SendingSlots.Add(new SwitcherSlot(41, 41));
            Switchers[0].SendingSlots.Add(new SwitcherSlot(45, 45));
            Switchers[1].SendingSlots.Add(new SwitcherSlot(89, 73));
            Switchers[1].SendingSlots.Add(new SwitcherSlot(93, 77));
            Switchers[1].SendingSlots.Add(new SwitcherSlot(81, 65));
            Switchers[1].SendingSlots.Add(new SwitcherSlot(85, 69));
            Switchers[2].SendingSlots.Add(new SwitcherSlot(105, 81));
            Switchers[2].SendingSlots.Add(new SwitcherSlot(109, 85));
            }catch(Exception e)
            {ErrorLog.Error("Error In Switcher Manager Adding Switcher Slots");
            }
        }

        public bool RouteAvailable(uint sendingSwitcher, uint receivingSwitcher, uint sendingRoomId)
        {
            if (Math.Abs(sendingSwitcher - receivingSwitcher) == 1)
            {
                return SingleRouteAvailable(sendingSwitcher, receivingSwitcher, sendingRoomId);
            }

            if ((int)(sendingSwitcher - receivingSwitcher) == -2)
            {
                if (SingleRouteAvailable(sendingSwitcher, sendingSwitcher + 1, sendingRoomId) &&
                    SingleRouteAvailable(sendingSwitcher + 1, sendingSwitcher + 2, sendingRoomId))
                    return true;
            }
            else if (((int)(sendingSwitcher - receivingSwitcher) == 2))
            {
                if (SingleRouteAvailable(sendingSwitcher, sendingSwitcher - 1, sendingRoomId) &&
                    SingleRouteAvailable(sendingSwitcher - 1, sendingSwitcher + 2, sendingRoomId))
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
                        if (switcher.SendingSlots[2].Available || switcher.SendingSlots[3].Available || switcher.SendingSlots[2].SendingRoomID == sendingRoomID || switcher.SendingSlots[3].SendingRoomID == sendingRoomID)
                        {
                            return true;
                        }
                    }
                    else if (sendingSwitcher == 2 && receivingSwitcher == 3)
                    {
                        if (switcher.SendingSlots[0].Available || switcher.SendingSlots[1].Available || switcher.SendingSlots[0].SendingRoomID == sendingRoomID || switcher.SendingSlots[1].SendingRoomID == sendingRoomID)
                        {
                            return true;
                        }
                    }
                    else
                        return switcher.PathAvailable(sendingRoomID);
                }
            }
            return false;
        }

        public RoomInputValues AttachSendingRoom(uint sendingRoomID, uint sendingSwitcher, uint receivingRoomID, uint receivingSwitcher, RoomInputValues roomValues)
        {

            RoomInputValues roomInputVals;
            #region Single Switcher Logic
            if ((Math.Abs(sendingSwitcher - receivingSwitcher) == 1) && sendingSwitcher != 2)
            {
                return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues);
            }
            else if ((Math.Abs(sendingSwitcher - receivingSwitcher) == 1) && sendingSwitcher == 2)
            {
                if (sendingSwitcher == 2 && receivingSwitcher == 3)
                {
                    if (Switchers[(int)sendingSwitcher - 1].SendingSlots[0].Available || Switchers[(int)sendingSwitcher - 1].SendingSlots[0].SendingRoomID == sendingRoomID)
                    {
                        return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues, 0);
                    }
                    else if (Switchers[(int)sendingSwitcher - 1].SendingSlots[1].Available || Switchers[(int)sendingSwitcher - 1].SendingSlots[1].SendingRoomID == sendingRoomID)
                    {
                        return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues, 1);
                    }
                }
                else if (sendingSwitcher == 2 && receivingSwitcher == 1)
                {
                    if (Switchers[(int)sendingSwitcher - 1].SendingSlots[2].Available || Switchers[(int)sendingSwitcher - 1].SendingSlots[2].SendingRoomID == sendingRoomID)
                    {
                        return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues, 2);
                    }
                    else if (Switchers[(int)sendingSwitcher - 1].SendingSlots[3].Available || Switchers[(int)sendingSwitcher - 1].SendingSlots[3].SendingRoomID == sendingRoomID)
                    {
                        return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues, 3);
                    }
                }
            }
            #endregion
            #region Mutiple Switcher Logic
            else if ((int)(sendingSwitcher - receivingSwitcher) == -2)
            {
                roomInputVals = Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues);
                if (Switchers[(int)sendingSwitcher - 1].SendingSlots[0].Available || Switchers[(int)sendingSwitcher - 1].SendingSlots[0].SendingRoomID == sendingRoomID)
                {
                    return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomInputVals, 0);
                }
                else if (Switchers[(int)sendingSwitcher - 1].SendingSlots[1].Available || Switchers[(int)sendingSwitcher - 1].SendingSlots[1].SendingRoomID == sendingRoomID)
                {
                    return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomInputVals, 1);
                }
            }
            else
            {
                roomInputVals = Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomValues);
                if (Switchers[(int)sendingSwitcher - 1].SendingSlots[2].Available || Switchers[(int)sendingSwitcher - 1].SendingSlots[2].SendingRoomID == sendingRoomID)
                {
                    return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomInputVals, 2);
                }
                else if (Switchers[(int)sendingSwitcher - 1].SendingSlots[3].Available || Switchers[(int)sendingSwitcher - 1].SendingSlots[3].SendingRoomID == sendingRoomID)
                {
                    return Switchers[(int)sendingSwitcher - 1].AddSlot(sendingRoomID, receivingRoomID, roomInputVals, 3);
                }
            }
            #endregion
            CrestronConsole.PrintLine("Error!!! No Room attached in SwitcherManager AttachSendingRoom");
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoomListv2
{
    public class RoomListItem
    {
        #region Properties
        public uint ID {set; get; }
        public string Name { set; get; }
        public bool Enabled { set; get; }
        public bool AvailableForReceiving { set; get; }
        public bool AvailableForSending { set; get; }

        public bool SelectedForReceiving { set; get; }
        public bool SelectedForSending { set; get; }
        #endregion

        #region Contructors
        public RoomListItem ()
	{
        ID = 0;
        Name = "N/A";
        AvailableForReceiving = false;
        AvailableForSending = false;
        Enabled = false;
        SelectedForReceiving = false;
        SelectedForSending = false;
	}
        public RoomListItem(uint id, string name, bool enabled)
        {
            ID = id;
            Name = name;
            Enabled = enabled;
            AvailableForReceiving = false;
            AvailableForSending = false;
        }
        #endregion

    }
}

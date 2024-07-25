using System.Collections;
using System.Collections.Generic;

namespace Portal.Entity
{
    public class BasicInfo
    {
        public string User { get; set; }
        public string Location { get; set; }
        public bool PPEEnabled { get; set; }
        public IList<string> HostLocations { get; set; }
    }
}
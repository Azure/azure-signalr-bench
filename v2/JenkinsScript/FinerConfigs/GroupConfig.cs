using System;
using System.Collections.Generic;
using System.Text;

namespace JenkinsScript.Config.FinerConfigs
{
    public class GroupConfig
    {
        public List<int> GroupConnectionBase {get; set;}
        public List<int> GroupConnectionStep {get; set;}
        public int GroupConnectionLength {get; set;}
        public List<int> GroupNumBase {get; set;}
        public List<int> GroupNumStep {get; set;}
        public int GroupNumLength {get; set;}
    }
}

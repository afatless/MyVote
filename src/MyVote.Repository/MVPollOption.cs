//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MyVote.Repository
{
    [GeneratedCode("DbContext 1.0.2.0", "EF 4.3.1")]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public partial class MVPollOption
    {
        [GeneratedCode("DbContext 1.0.2.0", "EF 4.3.1")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public MVPollOption()
        {
            this.MVPollResponses = new HashSet<MVPollResponse>();
        }
    
        public int PollOptionID { get; set; }
        public int PollID { get; set; }
        public short OptionPosition { get; set; }
        public string OptionText { get; set; }
    
        public virtual MVPoll MVPoll { get; set; }
        public virtual ICollection<MVPollResponse> MVPollResponses { get; set; }
    }
    
}

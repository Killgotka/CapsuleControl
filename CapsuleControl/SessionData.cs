using System;

namespace CapsuleControl;

public class SessionData
{
    public int      Capsule     { get; set; }
    public DateTime StartTime   { get; set; }
    public bool     IsExtension { get; set; }
    public DateTime EndTime     { get; set; }
}

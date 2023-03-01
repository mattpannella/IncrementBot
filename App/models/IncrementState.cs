namespace IncrementBot;

class IncremementState
{
    public string channel { get; set; }
    public int count { get; set; }
    public ulong? mostRecentUser { get; set; }
    public Dictionary<ulong, int> userTotals { get; set; }

    public IncremementState()
    {
        channel = "";
        count = 0;
        mostRecentUser = null;
        userTotals = new Dictionary<ulong, int>();
    }
}
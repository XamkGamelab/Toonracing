namespace Items
{
    public enum EventType
    {
        FinishLineCrossed,
        ItemCollected,
        StartRace,
        PlayerJoinedRoom,
    }

    public class ItemEvent
    {
        public readonly EventType eventType;
        public readonly int playerID;
        public readonly int hitItemID;
        public readonly double timestamp;

        public ItemEvent(EventType type, int playerID, int hitItemID, double timestamp)
        { 
            eventType = type;
            this.playerID = playerID;
            this.hitItemID = hitItemID;
            this.timestamp = timestamp;
        }
    }
}
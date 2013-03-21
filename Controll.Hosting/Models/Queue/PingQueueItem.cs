namespace Controll.Hosting.Models.Queue
{
    public class PingQueueItem : QueueItem
    {
        public override QueueItemType Type
        {
            get { return QueueItemType.Ping; }
        }
    }
}

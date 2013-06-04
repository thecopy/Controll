namespace Controll.Hosting.Models.Queue
{
    public class DownloadActivityQueueItem : QueueItem
    {
        public virtual string Url { get; set; }

        public override QueueItemType Type
        {
            get { return QueueItemType.DownloadActivity; }
        }
    }
}

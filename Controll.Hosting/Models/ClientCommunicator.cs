using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Models
{
    public abstract class ClientCommunicator
    {
        private readonly IList<ControllClient> _connectedClients = new List<ControllClient>();

        public virtual int Id { get; set; }
        public virtual IList<ControllClient> ConnectedClients
        {
            get { return _connectedClients; }
        }
    }

    public static class ClientCommunicatorExtensions
    {
        public static Zombie AsZombie(this ClientCommunicator clientCommunicator)
        {
            if (clientCommunicator as Zombie != null)
                return (Zombie) clientCommunicator;

            throw new InvalidOperationException("Cannot cast this instance to 'Zombie'");
        }
    }
}

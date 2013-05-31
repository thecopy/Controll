﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Helpers;
using Controll.Hosting.Infrastructure;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using NHibernate;
using NHibernate.Criterion;

namespace Controll.Hosting.Hubs
{
    [AuthorizeClaim(ControllClaimTypes.UserIdentifier)]
    public class ZombieHub : BaseHub
    {
        public ZombieHub(IControllRepository controllRepository,
                         IControllService controllService,
                         ISession session)
            : base(session, controllRepository, controllService)
        {}

        [AuthorizeClaim(ControllClaimTypes.ZombieIdentifier)]
        public void SignIn()
        {
            using (var transaction = Session.BeginTransaction())
            {
                var zombie = GetZombie();

                Console.WriteLine("Zombie logged in with connection id" + Context.ConnectionId);
                zombie.ConnectedClients.Add(new ControllClient {ConnectionId = Context.ConnectionId});

                Session.Update(zombie);
                ControllService.ProcessUndeliveredMessagesForZombie(zombie);
                
                transaction.Commit();
            }
        }

        [AuthorizeClaim(ControllClaimTypes.ZombieIdentifier)]
        public void SignOut()
        {
            using (var transaction = Session.BeginTransaction())
            {
                var zombie = GetZombie();

                zombie.ConnectedClients
                      .Where(x => x.ConnectionId == Context.ConnectionId).ToList()
                      .ForEach(x => zombie.ConnectedClients.Remove(x));

                Session.Update(zombie);
                transaction.Commit();
            }
        }

        [AuthorizeClaim(ControllClaimTypes.ZombieIdentifier)]
        public void QueueItemDelivered(Guid ticket)
        {
            using (var transaction = Session.BeginTransaction())
            {
                Console.WriteLine("A Zombie confirms delivery of ticket " + ticket);
                ControllService.MarkQueueItemAsDelivered(ticket);
                transaction.Commit();
            }
        }

        [AuthorizeClaim(ControllClaimTypes.ZombieIdentifier)]
        public void SynchronizeActivities(ICollection<ActivityViewModel> activities)
        {
            var zombie = GetZombie();

            Console.WriteLine("Synchronizing activities for zombie " + zombie.Name + " for user " + zombie.Owner.UserName);
            
            using (var transaction = Session.BeginTransaction())
            {
                for (int i = 0; i < zombie.Activities.Count(syncedActivity => activities.Count(a => a.Key == syncedActivity.Id) == 1); i++)
                {
                    var syncedActivity = zombie.Activities.ToList().Where(s => activities.Count(a => a.Key == s.Id) == 1).ToList()[i];
                    Console.WriteLine(syncedActivity.Name + ": existing in database. Updating (ONLY VERSION!!!!)...");
                    var installedActivity = activities.Single(a => a.Key == syncedActivity.Id);

                    zombie.Activities[i].Version = installedActivity.Version;
                }

                foreach (var syncedActivity in zombie.Activities.ToList().Where(syncedActivity => activities.Count(a => a.Key == syncedActivity.Id) == 0))
                {
                    Console.WriteLine(syncedActivity.Name + ": Not installed at zombie. Removing...");
                    zombie.Activities.Remove(syncedActivity);
                }

                foreach (var installedActivity in activities.Where(installedActivity => zombie.Activities.Count(a => a.Id == installedActivity.Key) == 0))
                {
                    Console.WriteLine(installedActivity.Name + ": Adding activity...");
                    zombie.Activities.Add(installedActivity.CreateConcreteClass());
                }

                Session.Update(zombie);
                transaction.Commit();
            }
        }

        [AuthorizeClaim(ControllClaimTypes.ZombieIdentifier)]
        public void ActivityMessage(Guid ticket, ActivityMessageType type, string message)
        {
            using (var transaction = Session.BeginTransaction())
            {
                ControllService.InsertActivityLogMessage(ticket, type, message);
                ControllService.InsertActivityMessage(ticket, type, message);

                transaction.Commit();
            }
            
        }

        [AuthorizeClaim(ControllClaimTypes.ZombieIdentifier)]
        public void ActivityResult(Guid ticket, ActivityCommandViewModel result)
        {
            Console.WriteLine("Activity result recieved.");
            using (var transaction = Session.BeginTransaction())
            {
                ControllService.InsertActivityResult(ticket, result.CreateConcreteClass());
                transaction.Commit();
            }
        }
    }
}

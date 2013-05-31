using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common.ViewModels;
using Controll.Hosting;
using Controll.Hosting.Helpers;
using Controll.Hosting.Infrastructure;
using Controll.Hosting.Models;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using NHibernate;

namespace Controll.IntegrationTests
{
    public class StandAloneServerProvider
    {
        private static readonly ISessionFactory Factory = NHibernateHelper.GetSessionFactoryForTesting();
        private IDisposable _serverApp;

        private void Sweep()
        {
            using(var session = Factory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var ccs = session.QueryOver<ControllClient>().List<ControllClient>();
                foreach (var cc in ccs)
                    session.Delete(cc);

                tx.Commit();
            }   
        }

        public Activity Activity { get; private set; }

        public void Start()
        {
            using (var session = Factory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                var repo = new ControllRepository(session);

                session.Save(new ControllUser
                    {
                        Email = "email",
                        Password = "password",
                        UserName = "username"
                    });

                var user = repo.GetUserFromUserName("username");
                var zombie = new Hosting.Models.Zombie
                    {
                        Name = "zombieName",
                        Owner = user,
                        Activities = new List<Activity>()
                    };
                user.Zombies.Add(zombie);

                session.Update(user);

                Activity = getActivity(Guid.NewGuid()).CreateConcreteClass();

                session.Save(Activity);

                zombie.Activities.Add(Activity);

                transaction.Commit();
            }

            var server = new ControllStandAloneServer("http://*:10244/")
                .UseBootstrapConfiguration(new BootstrapConfiguration
                    {
                        UseCustomSessionFactory = true,
                        CustomSessionFactory = Factory,
                        ClearDatabase = true
                    });

            _serverApp = server.Start();
        }

        public void Dispose()
        {
            _serverApp.Dispose();
        }

        private ActivityViewModel getActivity(Guid key)
        {
            var mockedActivity = new ActivityViewModel
            {
                CreatorName = "name",
                Description = "mocked",
                Key = key,
                LastUpdated = DateTime.Now,
                Name = "Mocked Activity",
                Version = new Version(1, 2, 3, 4),
                Commands = new List<ActivityCommandViewModel>
                        {
                            new ActivityCommandViewModel
                                {
                                    Label = "command-label",
                                    Name = "commandName",
                                    ParameterDescriptors = new List<ParameterDescriptorViewModel>
                                        {
                                            new ParameterDescriptorViewModel
                                                {
                                                    Description = "pd-description",
                                                    IsBoolean = true,
                                                    Label = "pd-label",
                                                    Name = "pd-name",
                                                    PickerValues = new List<PickerValueViewModel>
                                                        {
                                                            new PickerValueViewModel
                                                                {
                                                                    CommandName = "pv-commandname",
                                                                    Description = "pv-description",
                                                                    Identifier = "pv-id",
                                                                    IsCommand = true,
                                                                    Label = "pv-label",
                                                                    Parameters = new Dictionary<string, string>
                                                                        {
                                                                            {"param1", "value1"}
                                                                        }
                                                                }
                                                        }
                                                }
                                        }
                                }
                        }
            };

            return mockedActivity;
        }

    }
}

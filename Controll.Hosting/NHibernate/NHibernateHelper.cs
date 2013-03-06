<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.NHibernate.Mappings;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Collection.Generic;
using NHibernate.Tool.hbm2ddl;
using Configuration = NHibernate.Cfg.Configuration;

namespace Controll.Hosting.NHibernate
{
    public class NHibernateHelper
    {
        public static ISessionFactory GetSessionFactoryForMockedData()
        {
            ConnectionStringSettings mockedConnectionString = ConfigurationManager.ConnectionStrings["mocked"];
            if (mockedConnectionString == null)
                throw new ConfigurationErrorsException("No ConnectionString named \"mocked\"");

            Configuration config = Fluently.Configure()
                                           .Database(
                                               MsSqlConfiguration.MsSql2008.ConnectionString(
                                                   mockedConnectionString.ConnectionString))
                                           .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ControllUser>())
                                           .BuildConfiguration();

          //  new SchemaExport(config).Execute(false, true,false); Kör endast om nödvändigt

            new SchemaValidator(config).Validate();
            

            return config.BuildSessionFactory();
        }
    }
}
=======
﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.NHibernate.Mappings;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Collection.Generic;
using NHibernate.Tool.hbm2ddl;

namespace Controll.Hosting.NHibernate
{
    public class NHibernateHelper
    {
        private static string _dbFile = "Data\\ControllDatabase.sdf";
        private static Configuration configuration;
        public static bool IsInTesting = false;
        public static IDbConnection Connection;

        public static ISession OpenSession()
        {
            return GetFactory().OpenSession();
        }

        private static ISessionFactory _sessionFactory;
        private static ISessionFactory GetFactory()
        {
            if (_sessionFactory == null)
            {
                if (IsInTesting)
                    _dbFile = "Data\\ControllDatabase.sdf";

                var configuration = MsSqlCeConfiguration.Standard.ConnectionString("DataSource=" + _dbFile);

                _sessionFactory = Fluently.Configure()
                    .Database(configuration)
                    .Mappings(m =>
                        m.FluentMappings.AddFromAssemblyOf<ActivityInvocationQueueItem>()
                        )
                        
                    .ExposeConfiguration(BuildSchema)
                    .BuildSessionFactory();

                if (!IsInTesting)
                    InsertInitialData();
            }

            return _sessionFactory;
        }

        private static void InsertInitialData()
        {
            using(var session = _sessionFactory.OpenSession())
            using(var transaction = session.BeginTransaction())
            {
                var defaultUser = new ControllUser();
                defaultUser.UserName = "Erik";
                defaultUser.Password = "password";
                defaultUser.Zombies = new List<Zombie>();

                var spotifyPlugin = new Activity()
                    {
                        Name = "Spotify Plugin",
                        CreatorName = "Erik Jonsson Thorén",
                        LastUpdated = DateTime.Parse("2012-07-15 17:55"),
                        FilePath = ".\\Activities\\SpotifyPlugin.activity",
                        Description = "playpause Play/Pause\nnext Next Track\nprev Previous Track\nfind:(track | album | artist):(query) Search and play the track/album/artist best matching the result",
                        Id = Guid.Parse("27611FAD-17CD-463B-A179-796F3E3B1120"),
                        Commands = new List<ActivityCommand>
                        { 
                            new ActivityCommand
                            {
                                Name = "FindTrackName",
                                Label= "Play Track", 
                                ParameterDescriptors = new List<ParameterDescriptor>
                                    {
                                        new ParameterDescriptor
                                            {
                                                Name = "trackName",
                                                Label = "Track Name",
                                                Description = "The name of the track to try to find and play"
                                            },
                                            
                                        new ParameterDescriptor
                                            {
                                                Name = "artistName",
                                                Label = "Artist Name",
                                                Description = "(Optional) The artist of the track to try to find and play"
                                            }
                                    }
                            }
                        }
                    };

                var newActivity = new Activity()
                    {
                        Name = "New Activity",
                        CreatorName = "Erik Jonsson Thorén",
                        LastUpdated = DateTime.Parse("2012-07-16 11:22"),
                        FilePath = ".\\Activities\\NewActivity.activity",
                        Description = "Blablabla\nBlaBla",
                        Id = Guid.Parse("27611FAD-17CD-463B-A179-796F3E3B1121"),
                        Commands = new List<ActivityCommand>
                            {
                                new ActivityCommand
                                    {
                                        Name = "DoSomethingCool",
                                        Label = "GÖR NÅÅT CÅÅÅLK",
                                        ParameterDescriptors = new List<ParameterDescriptor>
                                            {
                                                new ParameterDescriptor
                                                    {
                                                        Name = "theCoolThing",
                                                        Label = "The cool thing to do",
                                                        Description = "After you have done the cool thing you should go to bed!"
                                                    }
                                            }
                                    }
                            }
                    };

                var defaultZombie = new Zombie
                {
                    Activities = new List<Activity> { spotifyPlugin },
                    ConnectionId = Guid.NewGuid().ToString(),
                    Name = "zombieTest"
                };

                defaultUser.Zombies.Add(defaultZombie);

                session.Save(newActivity);

                session.Save(spotifyPlugin);

                session.Save(defaultUser);

                transaction.Commit();
            }
        }

        private static void BuildSchema(Configuration config)
        {
            configuration = config;
            new SchemaExport(config)
                .Execute(false, true, false);
        }

        public static void ClearDb()
        {
            new SchemaExport(configuration)
                .Execute(false, true, false);
        }
    }
}
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1

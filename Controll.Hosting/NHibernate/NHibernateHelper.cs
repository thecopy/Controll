using System;
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
        public static ISessionFactory GetSessionFactoryForConnectionStringAlias(string connectionStringAlias)
        {
            ConnectionStringSettings mockedConnectionString = ConfigurationManager.ConnectionStrings[connectionStringAlias];
            if (mockedConnectionString == null)
                throw new ConfigurationErrorsException("No ConnectionString named \"" + connectionStringAlias + "\"");

            Configuration config = Fluently.Configure()
                                           .Database(
                                               MsSqlConfiguration.MsSql2008.ConnectionString(
                                                   mockedConnectionString.ConnectionString))
                                           .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ControllUser>())
                                           .BuildConfiguration();

            return config.BuildSessionFactory();
        }

        public static ISessionFactory GetSessionFactoryForAppHarbor()
        {
            ConnectionStringSettings mockedConnectionString = ConfigurationManager.ConnectionStrings["appharbor"];
            if (mockedConnectionString == null)
                throw new ConfigurationErrorsException("No ConnectionString named \"appharbor\"");

            Configuration config = Fluently.Configure()
                                           .Database(
                                               MsSqlConfiguration.MsSql2008.ConnectionString(
                                                   mockedConnectionString.ConnectionString))
                                           .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ControllUser>())
                                           .BuildConfiguration();

            return config.BuildSessionFactory();
        }

        public static ISessionFactory GetSessionFactoryForTesting()
        {
            ConnectionStringSettings mockedConnectionString = ConfigurationManager.ConnectionStrings["testing"];
            if (mockedConnectionString == null)
                throw new ConfigurationErrorsException("No ConnectionString named \"testing\"");

            Configuration config = Fluently.Configure()
                                           .Database(
                                               MsSqlConfiguration.MsSql2008.ConnectionString(
                                                   mockedConnectionString.ConnectionString))
                                           .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ControllUser>())
                                           .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ParameterDescriptor>())
                                           .BuildConfiguration();

            return config.BuildSessionFactory();
        }

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

            //Kör endast om nödvändigt
#if FALSE
                var export = new SchemaExport(config);
                export.Drop(true, true);
                export.Create(true, true);
            }
            new SchemaValidator(config).Validate();
#endif

            return config.BuildSessionFactory();
        }
    }
}
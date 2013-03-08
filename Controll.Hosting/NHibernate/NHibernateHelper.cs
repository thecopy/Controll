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
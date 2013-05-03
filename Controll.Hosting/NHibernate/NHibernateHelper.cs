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
using FluentNHibernate.Conventions.Helpers;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Collection.Generic;
using NHibernate.Tool.hbm2ddl;
using Configuration = NHibernate.Cfg.Configuration;
using ParameterDescriptor = Controll.Hosting.Models.ParameterDescriptor;

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
                                           .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ControllUser>()
                                               .Conventions.Setup(x => x.Add(AutoImport.Never())))
                                               .ExposeConfiguration(TreatConfiguration)
                                           .BuildConfiguration();

            //{
            //    var export = new SchemaExport(config);
            //    export.Drop(true, true);
            //    export.Create(true, true);

            //    new SchemaValidator(config).Validate();
            //}
            return config.BuildSessionFactory();
        }

        protected static void TreatConfiguration(Configuration configuration)
        {
            var update = new SchemaUpdate(configuration);
            update.Execute(false, true);
        }


        public static ISessionFactory GetSessionFactoryForTesting()
        {
            return GetSessionFactoryForConnectionStringAlias("testing");
        }

        public static ISessionFactory GetSessionFactoryForMockedData()
        {
            return GetSessionFactoryForConnectionStringAlias("mocked");
        }
    }
}
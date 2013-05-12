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

            var schemaExportPath = Path.Combine(System.Environment.CurrentDirectory, "Mappings");
            if (!Directory.Exists(schemaExportPath))
                Directory.CreateDirectory(schemaExportPath);

            Configuration config = Fluently
                .Configure()
                .Database(MsSqlConfiguration.MsSql2008
                                            //.ShowSql()
                                            .ConnectionString(mockedConnectionString.ConnectionString))
                .Mappings(m =>
                    {
                        m.FluentMappings
                         .AddFromAssemblyOf<ControllUser>();
                        //m.AutoMappings.ExportTo(schemaExportPath);
                        //m.FluentMappings.ExportTo(schemaExportPath);
                    })
                .Diagnostics(x => x.Enable())
                .ExposeConfiguration(TreatConfiguration)
                .BuildConfiguration();

            return config.BuildSessionFactory();
        }

        protected static void TreatConfiguration(Configuration configuration)
        {
            if (true)
            {
                var update = new SchemaUpdate(configuration);
                update.Execute(false, true);
            }
            else
            {
                var export = new SchemaExport(configuration);
                export.Drop(true, true);
                export.Create(true, true);
            }
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
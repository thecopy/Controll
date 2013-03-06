using System;
using System.Collections.Generic;
using Controll.Common;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace Controll.NHibernate
{
    public class NHibernateHelper
    {
        public static bool IsInTesting = false;
        private const string DbFile = "Data\\ControllZombieDatabase.sdf";

        private static ISessionFactory _sessionFactory;
        private static ISessionFactory GetFactory()
        {
            if (_sessionFactory == null)
            {
                _sessionFactory = Fluently.Configure()
                    .Database(MsSqlCeConfiguration.Standard.ConnectionString("DataSource=" + DbFile))
                    .Mappings(m =>
                        m.FluentMappings.AddFromAssemblyOf<ActivitySessionLog>()
                        )
                        
                    .ExposeConfiguration(BuildSchema)
                    .BuildSessionFactory();
                }

            return _sessionFactory;
        }

        private static void BuildSchema(Configuration config)
        {
            new SchemaExport(config)
                .Execute(false, true, false);
        }

        public static ISession OpenSession()
        {
            return GetFactory().OpenSession();
        }
    }
}

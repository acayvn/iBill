﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DALDotNet;
using DALDotNet.DataAccess;
using DALDotNet.DataAttributes;

namespace LyncBillingBase.DataModels
{
    [DataSource(Name = "GatewaysDetails", Type = GLOBALS.DataSource.Type.DBTable, AccessMethod = GLOBALS.DataSource.AccessMethod.SingleSource)]
    public class GatewayInfo : DataModel
    {
        [IsIDField]
        [AllowIDInsert]
        [DbColumn("GatewayID")]
        public int GatewayID { set; get; }

        [DbColumn("SiteID")]
        public int SiteID { set; get; }

        [DbColumn("PoolID")]
        public int PoolID { set; get; }

        [AllowNull]
        [DbColumn("Description")]
        public string Description { set; get; }


        //
        // Relations
        [DataRelation(WithDataModel = typeof(Gateway), OnDataModelKey = "ID", ThisKey = "GatewayID")]
        public Gateway Gateway { get; set; }

        [DataRelation(WithDataModel = typeof(GatewayRate), OnDataModelKey = "GatewayID", ThisKey = "GatewayID", RelationType = GLOBALS.DataRelation.Type.UNION)]
        public GatewayRate GatewayRatesInfo { get; set; }

        [DataRelation(WithDataModel = typeof(Site), OnDataModelKey = "ID", ThisKey = "SiteID")]
        public Site Site { get; set; }

        [DataRelation(WithDataModel = typeof(Pool), OnDataModelKey = "ID", ThisKey = "PoolID")]
        public Pool Pool { get; set; }
    }
}

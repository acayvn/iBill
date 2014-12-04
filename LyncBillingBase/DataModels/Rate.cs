﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LyncBillingBase.DataAccess;
using LyncBillingBase.DataAttributes;

namespace LyncBillingBase.DataModels
{
    [DataSource(Name = "GatewaysDetails", Type = GLOBALS.DataSource.Type.DBTable, AccessMethod = GLOBALS.DataSource.AccessMethod.DistributedSource)]
    public class Rate : DataModel
    {
        [IsIDField]
        [DbColumn("Rate_ID")]
        public long RateID { get; set; }

        [DbColumn("country_code_dialing_prefix")]
        public long DialingCode { get; set; }

        [DbColumn("rate")]
        public decimal Price { get; set; }
                

        //
        // Relations
        [DataRelation(WithDataModel = typeof(NumberingPlan), OnDataModelKey = "DialingPrefix", ThisKey = "DialingCode")]
        public NumberingPlan NumberingPlan { get; set; }
    }

}

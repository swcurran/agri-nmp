﻿using Microsoft.AspNetCore.Mvc;
using SERVERAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agri.Interfaces;
using Agri.Models.Farm;

namespace SERVERAPI.ViewComponents
{
    public class ReportFields : ViewComponent
    {
        private IAgriConfigurationRepository _sd;
        private Models.Impl.UserData _ud;

        public ReportFields(IAgriConfigurationRepository sd, Models.Impl.UserData ud)
        {
            _sd = sd;
            _ud = ud;
        }


        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View(await ReportFieldsAsync());
        }

        private Task<ReportFieldsViewModel> ReportFieldsAsync()
        {
            ReportFieldsViewModel fvm = new ReportFieldsViewModel();

            FarmDetails fd = _ud.FarmDetails();


            List<Field> flds = _ud.GetFields();

            foreach (var m in flds)
            {
                DisplayReportField dc = new DisplayReportField();
                dc.fldName = m.fieldName;
                if (m.soilTest != null)
                {
                    dc.sampleDate = m.soilTest.sampleDate.ToString("MMM-yyyy");
                    dc.dispNO3H = m.soilTest.valNO3H.ToString();
                    dc.dispP = m.soilTest.ValP.ToString();
                    dc.dispK = m.soilTest.valK.ToString();
                    dc.dispPH = m.soilTest.valPH.ToString();
                }
                fvm.fields.Add(dc);
            }

            return Task.FromResult(fvm);
        }
    }
    public class ReportFieldsViewModel
    {
        public List<DisplayReportField> fields { get; set; }
    }
    public class DisplayReportField
    {
        public string fldName { get; set; }
        public string sampleDate { get; set; }
        public string dispNO3H { get; set; }
        public string dispP { get; set; }
        public string dispK { get; set; }
        public string dispPH { get; set; }
    }
}

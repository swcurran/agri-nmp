﻿using Agri.Interfaces;
using Agri.Models.Configuration;
using Agri.Models.Farm;
using Agri.Models.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SERVERAPI.Models.Impl;
using SERVERAPI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SERVERAPI.Controllers
{
    //[RedirectingAction]
    public class SoilController : BaseController
    {
        private ILogger<SoilController> _logger;
        public IHostingEnvironment _env { get; set; }
        public UserData _ud { get; set; }
        public IAgriConfigurationRepository _sd { get; set; }
        private ISoilTestConverter _soilTestConversions;
        public AppSettings _settings;

        public SoilController(ILogger<SoilController> logger,
            IHostingEnvironment env, 
            UserData ud, 
            IAgriConfigurationRepository sd, 
            ISoilTestConverter soilTestConversions)
        {
            _logger = logger;
            _env = env;
            _ud = ud;
            _sd = sd;
            _soilTestConversions = soilTestConversions;
        }
        [HttpGet]
        public IActionResult SoilTest()
        {
            SoilTestViewModel fvm = new SoilTestViewModel();

            FarmDetails fd = _ud.FarmDetails();
            fvm.selTstOption = fd.testingMethod;

            if (!string.IsNullOrEmpty(fd.testingMethod))
                fvm.testSelected = true;

            fvm.tstOptions = new List<SelectListItem>();
            fvm.tstOptions = _sd.GetSoilTestMethodsDll().ToList();

            List<Field> fl = _ud.GetFields();

            fvm.fldsFnd = (fl.Count() > 0) ? true : false;

            fvm.warningMsg = _sd.GetUserPrompt("soiltestwarning");

            return View(fvm);
        }
        [HttpPost]
        public IActionResult SoilTest(SoilTestViewModel fvm)
        {
            fvm.tstOptions = new List<SelectListItem>();
            fvm.tstOptions = _sd.GetSoilTestMethodsDll().ToList();

            if (fvm.buttonPressed == "MethodChange")
            {
                ModelState.Clear();
                FarmDetails fd = _ud.FarmDetails();
                fd.testingMethod = fvm.selTstOption == "select" ? string.Empty : fvm.selTstOption;
                _ud.UpdateFarmDetails(fd);
                fvm.testSelected = string.IsNullOrEmpty(fd.testingMethod) ? false : true;
                List<Field> fl = _ud.GetFields();
                
                //update fields with convert STP and STK
                _ud.UpdateSTPSTK(fl);
                
                //update the Nutrient calculations with the new/changed soil test data
                Utility.ChemicalBalanceMessage cbm = new Utility.ChemicalBalanceMessage(_ud, _sd);
                cbm.RecalcCropsSoilTestMessagesByFarm();

                RedirectToAction("SoilTest", "Soil");
            }
            return View(fvm);
        }
        [HttpGet]
        public IActionResult SoilTestDetails(string fldName)
        {
            SoilTestDetailsViewModel tvm = new SoilTestDetailsViewModel();
            tvm.title = "Update";
            tvm.url = _sd.GetExternalLink("soiltestexplanation");
            tvm.urlText = _sd.GetUserPrompt("moreinfo");
            tvm.SoilTestValuesMsg = _sd.GetUserPrompt("SoilTestValuesMessage");
            tvm.SoilTestNitrogenNitrateMsg = _sd.GetUserPrompt("SoilTestNitrogenNitrateMessage");
            tvm.SoilTestPhosphorousMsg = _sd.GetUserPrompt("SoilTestPhosphorousMessage");
            tvm.SoilTestPotassiumMsg = _sd.GetUserPrompt("SoilTestPotassiumMessage");
            tvm.SoilTestPHMsg = _sd.GetUserPrompt("SoilTestPHMessage");

            Field fld = _ud.GetFieldDetails(fldName);
            tvm.fieldName = fldName;
            if (fld.soilTest != null)
            {                
                tvm.sampleDate = fld.soilTest.sampleDate.ToString("MMM-yyyy");
                tvm.dispK = fld.soilTest.valK.ToString("G29");
                tvm.dispNO3H = fld.soilTest.valNO3H.ToString("G29");
                tvm.dispP = fld.soilTest.ValP.ToString("G29");
                tvm.dispPH = fld.soilTest.valPH.ToString("G29");
            }

            return View(tvm);
        }
        [HttpPost]
        public IActionResult SoilTestDetails(SoilTestDetailsViewModel tvm)
        {
            decimal nmbr;

            if(ModelState.IsValid)
            {
                if (!Decimal.TryParse(tvm.dispNO3H, out nmbr))
                {
                    ModelState.AddModelError("dispNO3H", "Numbers only.");
                }
                else
                {
                    if(nmbr < 0)
                    {
                        ModelState.AddModelError("dispNO3H", "Invalid.");
                    }
                }
                if (!Decimal.TryParse(tvm.dispP, out nmbr))
                {
                    ModelState.AddModelError("dispP", "Numbers only.");
                }
                else
                {
                    if (nmbr < 0)
                    {
                        ModelState.AddModelError("dispP", "Invalid.");
                    }
                }
                if (!Decimal.TryParse(tvm.dispK, out nmbr))
                {
                    ModelState.AddModelError("dispK", "Numbers only.");
                }
                else
                {
                    if (nmbr < 0)
                    {
                        ModelState.AddModelError("dispK", "Invalid.");
                    }
                }
                if (!Decimal.TryParse(tvm.dispPH, out nmbr))
                {
                    ModelState.AddModelError("dispPH", "Numbers only.");
                }
                else
                {
                    if (nmbr < 0 ||
                        nmbr > 14)
                    {
                        ModelState.AddModelError("dispPH", "Invalid.");
                    }
                }
                if(!ModelState.IsValid)
                {
                    return View(tvm);
                }

                Field fld = _ud.GetFieldDetails(tvm.fieldName);
                if(fld.soilTest == null)
                {
                    fld.soilTest = new SoilTest();
                }
                fld.soilTest.sampleDate = Convert.ToDateTime(tvm.sampleDate);
                fld.soilTest.ValP = Convert.ToDecimal(tvm.dispP);
                fld.soilTest.valK = Convert.ToDecimal(tvm.dispK);
                fld.soilTest.valNO3H = Convert.ToDecimal(tvm.dispNO3H);
                fld.soilTest.valPH = Convert.ToDecimal(tvm.dispPH);
                fld.soilTest.ConvertedKelownaK = _soilTestConversions.GetConvertedSTK(_ud.FarmDetails()?.testingMethod, fld.soilTest);
                fld.soilTest.ConvertedKelownaP = _soilTestConversions.GetConvertedSTP(_ud.FarmDetails()?.testingMethod, fld.soilTest);

                _ud.UpdateFieldSoilTest(fld);

                //update the Nutrient calculations with the new/changed soil test data
                Utility.ChemicalBalanceMessage cbm = new Utility.ChemicalBalanceMessage(_ud, _sd);
                cbm.RecalcCropsSoilTestMessagesByField(tvm.fieldName);

                string target = "#test";
                string url = Url.Action("RefreshTestList", "Soil");
                return Json(new { success = true, url = url, target = target });

            }
            return View(tvm);
        }
        [HttpGet]
        public IActionResult SoilTestErase(string fldName)
        {
            SoilTestDeleteViewModel tvm = new SoilTestDeleteViewModel();
            tvm.title = "Erase";

            tvm.fieldName = fldName;

            return View(tvm);
        }
        [HttpPost]
        public ActionResult SoilTestErase(SoilTestDeleteViewModel dvm)
        {
            if (ModelState.IsValid)
            {
                Field fld = _ud.GetFieldDetails(dvm.fieldName);

                fld.soilTest = null;

                _ud.UpdateFieldSoilTest(fld);

                string target = "#test";
                string url = Url.Action("RefreshTestList", "Soil");
                return Json(new { success = true, url = url, target = target });
            }
            return PartialView("SoilTestErase", dvm);
        }
        public IActionResult RefreshTestList()
        {
            return ViewComponent("SoilTests");
        }
        [HttpGet]
        public IActionResult MissingMethod()
        {
            return View();
        }
        public IActionResult MissingTests(string target)
        {
            MissingTestsViewModel mvm = new MissingTestsViewModel();
            mvm.target = target;
            mvm.msg = _sd.GetSoilTestWarning();

            return View(mvm);
        }
        [HttpPost]
        public IActionResult MissingTests(MissingTestsViewModel mvm)
        {

            return View(mvm);
        }
    }
}

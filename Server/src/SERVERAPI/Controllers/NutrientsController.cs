﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using SERVERAPI.ViewModels;
using SERVERAPI.Models;
using Newtonsoft.Json;
using SERVERAPI.Models.Impl;
using SERVERAPI.ViewComponents;
using SERVERAPI.Utility;
using static SERVERAPI.Models.StaticData;
using System.Configuration;
using Microsoft.Extensions.Options;

namespace SERVERAPI.Controllers
{
    public class NutrientsController : Controller
    {
        public IHostingEnvironment _env { get; set; }
        public UserData _ud { get; set; }
        public Models.Impl.StaticData _sd { get; set; }
        public IViewRenderService _viewRenderService { get; set; }
        public AppSettings _settings;

        public NutrientsController(IHostingEnvironment env, UserData ud, Models.Impl.StaticData sd, IOptions<AppSettings> settings)
        {
            _env = env;
            _ud = ud;
            _sd = sd;
            _settings = settings.Value;
        }
        // GET: /<controller>/
        public IActionResult Calculate(string id)
        {
            CalculateViewModel cvm = new CalculateViewModel();
            cvm.fields = new List<Field>();

            // not id entered so default to the first one for the farm
            if(id == null)
            {
                List<Field> fldLst = _ud.GetFields();

                if(fldLst.Count() == 0)
                {
                    cvm.fldsFnd = false;
                }
                else
                {
                    cvm.fldsFnd = true;
                    foreach(var f in fldLst)
                    {
                        cvm.fields.Add(f);
                    }
                    cvm.currFld = cvm.fields[0].fieldName;
                }
            }
            else
            {
                cvm.currFld = id;
                List<Field> fldLst = _ud.GetFields();
                cvm.fldsFnd = true;
                foreach (var f in fldLst)
                {
                    cvm.fields.Add(f);
                }
            }

            return View(cvm);
        }
        [HttpPost]
        public IActionResult Calculate(CalculateViewModel cvm)
        {

            return View(cvm);
        }

        public IActionResult ManureDetails(string fldName, int? id)
        {
            Utility.CalculateNutrients calculateNutrients = new CalculateNutrients(_env, _ud, _sd);
            NOrganicMineralizations nOrganicMineralizations = new NOrganicMineralizations();

            ManureDetailsViewModel mvm = new ManureDetailsViewModel();

            mvm.fieldName = fldName;
            mvm.title = id == null ? "Add" : "Edit";
            mvm.btnText = id == null ? "Calculate" : "Return";
            mvm.id = id;
            mvm.avail = string.Empty;
            mvm.nh4 = string.Empty;
            mvm.stdN = true;
            mvm.stdAvail = true;

            //var farmData = HttpContext.Session.GetObjectFromJson<FarmData>("FarmData");
            ManureDetailsSetup(ref mvm);

            if(id != null)
            {
                int regionid = _ud.FarmDetails().farmRegion.Value;
                Region region = _sd.GetRegion(regionid);
                nOrganicMineralizations = calculateNutrients.GetNMineralization(Convert.ToInt16(mvm.selManOption), region.locationid);

                NutrientManure nm = _ud.GetFieldNutrientsManure(fldName, id.Value);

                mvm.avail = nm.nAvail.ToString("###");
                mvm.selRateOption = nm.unitId;
                mvm.selManOption = nm.manureId;
                mvm.selApplOption = nm.applicationId;
                mvm.rate = nm.rate.ToString();
                mvm.nh4 = nm.nh4Retention.ToString("###");
                mvm.yrN = nm.yrN.ToString();
                mvm.yrP2o5 = nm.yrP2o5.ToString();
                mvm.yrK2o = nm.yrK2o.ToString();
                mvm.ltN = nm.ltN.ToString();
                mvm.ltP2o5 = nm.ltP2o5.ToString();
                mvm.ltK2o = nm.ltK2o.ToString();
                Models.StaticData.Manure man = _sd.GetManure(nm.manureId);
                mvm.currUnit = man.solid_liquid;
                mvm.rateOptions = _sd.GetUnitsDll(mvm.currUnit).ToList();

                mvm.stdN = Convert.ToDecimal(mvm.nh4) != (calculateNutrients.GetAmmoniaRetention(Convert.ToInt16(mvm.selManOption), Convert.ToInt16(mvm.selApplOption)) * 100) ? false : true;
                mvm.stdAvail = Convert.ToDecimal(mvm.avail) != (nOrganicMineralizations.OrganicN_FirstYear * 100) ? false : true;

            }
            else

            {
                mvm.yrN = "  0";
                mvm.yrP2o5 = "  0";
                mvm.yrK2o = "  0";
                mvm.ltN = "  0";
                mvm.ltP2o5 = "  0";
                mvm.ltK2o = "  0";
            }

            return PartialView(mvm);
        }
        [HttpPost]
        public IActionResult ManureDetails(ManureDetailsViewModel mvm)
        {
            Utility.CalculateNutrients calculateNutrients = new CalculateNutrients(_env, _ud, _sd);
            NOrganicMineralizations nOrganicMineralizations = new NOrganicMineralizations();

            ManureDetailsSetup(ref mvm);

            if (mvm.buttonPressed == "ResetN")
            {
                ModelState.Clear();
                mvm.buttonPressed = "";
                mvm.btnText = "Calculate";

                // reset to calculated amount                
                mvm.nh4 = (calculateNutrients.GetAmmoniaRetention(Convert.ToInt16(mvm.selManOption), Convert.ToInt16(mvm.selApplOption))* 100).ToString("###");

                mvm.stdN = true;
                return View(mvm);
            }

            if (mvm.buttonPressed == "ResetA")
            {
                ModelState.Clear();
                mvm.buttonPressed = "";
                mvm.btnText = "Calculate";

                // reset to calculated amount
                int regionid = _ud.FarmDetails().farmRegion.Value;
                Region region = _sd.GetRegion(regionid);
                nOrganicMineralizations = calculateNutrients.GetNMineralization(Convert.ToInt16(mvm.selManOption), region.locationid);

                mvm.avail = (nOrganicMineralizations.OrganicN_FirstYear * 100).ToString("###");

                mvm.stdAvail = true;
                return View(mvm);
            }

            if (mvm.buttonPressed == "TypeChange")
            {
                ModelState.Clear();
                mvm.buttonPressed = "";
                mvm.btnText = "Calculate";

                if (mvm.selManOption != "")
                {
                    Models.StaticData.Manure man = _sd.GetManure(mvm.selManOption);
                    if(mvm.currUnit != man.solid_liquid)
                    {
                        mvm.currUnit = man.solid_liquid;
                        mvm.rateOptions = _sd.GetUnitsDll(mvm.currUnit).ToList();
                        mvm.selRateOption = mvm.rateOptions[0].Id.ToString();
                    }

                    // if application is present then recalc N and A
                    int regionid = _ud.FarmDetails().farmRegion.Value;
                    Region region = _sd.GetRegion(regionid);
                    nOrganicMineralizations = calculateNutrients.GetNMineralization(Convert.ToInt16(mvm.selManOption), region.locationid);

                    mvm.avail = (nOrganicMineralizations.OrganicN_FirstYear * 100).ToString("###");
                }
                else
                {
                    mvm.nh4 = string.Empty;
                    mvm.avail = string.Empty;
                }
                return View(mvm);
            }

            if (mvm.buttonPressed == "ApplChange")
            {
                ModelState.Clear();
                mvm.buttonPressed = "";
                mvm.btnText = "Calculate";

                if (mvm.selApplOption != "")
                {
                    // recalc N and A values
                    mvm.nh4 = (calculateNutrients.GetAmmoniaRetention(Convert.ToInt16(mvm.selManOption), Convert.ToInt16(mvm.selApplOption)) * 100).ToString("###");
                }
                else
                {
                    mvm.nh4 = string.Empty;
                    mvm.avail = string.Empty;
                }
                return View(mvm);
            }

            if (ModelState.IsValid)
            {
                if (mvm.btnText == "Calculate")
                {
                    ModelState.Clear();
                    NutrientInputs nutrientInputs = new NutrientInputs();

                    calculateNutrients.manure = mvm.selManOption;
                    calculateNutrients.applicationSeason = mvm.selApplOption;
                    calculateNutrients.applicationRate = Convert.ToDecimal(mvm.rate);
                    calculateNutrients.applicationRateUnits = mvm.selRateOption;
                    calculateNutrients.ammoniaNRetentionPct = Convert.ToDecimal(mvm.nh4);
                    calculateNutrients.firstYearOrganicNAvailablityPct = Convert.ToDecimal(mvm.avail);

                    calculateNutrients.GetNutrientInputs(nutrientInputs);

                    mvm.yrN = nutrientInputs.N_FirstYear.ToString();
                    mvm.yrP2o5 = nutrientInputs.P2O5_FirstYear.ToString();
                    mvm.yrK2o = nutrientInputs.K2O_FirstYear.ToString();
                    mvm.ltN = nutrientInputs.N_LongTerm.ToString();
                    mvm.ltP2o5 = nutrientInputs.P2O5_LongTerm.ToString();
                    mvm.ltK2o = nutrientInputs.K2O_LongTerm.ToString();

                    mvm.btnText = mvm.id == null ? "Add to Field" : "Update Field";

                    // determine if values on screen are book value or not
                    int regionid = _ud.FarmDetails().farmRegion.Value;
                    Region region = _sd.GetRegion(regionid);
                    nOrganicMineralizations = calculateNutrients.GetNMineralization(Convert.ToInt16(mvm.selManOption), region.locationid);

                    if (Convert.ToDecimal(mvm.nh4) != (calculateNutrients.GetAmmoniaRetention(Convert.ToInt16(mvm.selManOption), Convert.ToInt16(mvm.selApplOption)) * 100))
                    {
                        mvm.stdN = false;
                    }
                    if (Convert.ToDecimal(mvm.avail) != (nOrganicMineralizations.OrganicN_FirstYear * 100))
                    {
                        mvm.stdAvail = false;
                    }
                }
                else
                {
                    if(mvm.id == null)
                    {

                        NutrientManure nm = new NutrientManure()
                        {
                            manureId = mvm.selManOption,
                            applicationId = mvm.selApplOption,
                            unitId = mvm.selRateOption,
                            rate = Convert.ToDecimal(mvm.rate),
                            nh4Retention = Convert.ToDecimal(mvm.nh4),
                            nAvail = Convert.ToDecimal(mvm.avail),
                            yrN = Convert.ToDecimal(mvm.yrN),
                            yrP2o5 = Convert.ToDecimal(mvm.yrP2o5),
                            yrK2o = Convert.ToDecimal(mvm.yrK2o),
                            ltN = Convert.ToDecimal(mvm.ltN),
                            ltP2o5 = Convert.ToDecimal(mvm.ltP2o5),
                            ltK2o = Convert.ToDecimal(mvm.ltK2o)
                        };

                        _ud.AddFieldNutrientsManure(mvm.fieldName, nm);
                    }
                    else
                    {
                        NutrientManure nm = _ud.GetFieldNutrientsManure(mvm.fieldName, mvm.id.Value);
                        nm.manureId = mvm.selManOption;
                        nm.applicationId = mvm.selApplOption;
                        nm.unitId = mvm.selRateOption;
                        nm.rate = Convert.ToDecimal(mvm.rate);
                        nm.nh4Retention = Convert.ToDecimal(mvm.nh4);
                        nm.nAvail = Convert.ToDecimal(mvm.avail);
                        nm.yrN = Convert.ToDecimal(mvm.yrN);
                        nm.yrP2o5 = Convert.ToDecimal(mvm.yrP2o5);
                        nm.yrK2o = Convert.ToDecimal(mvm.yrK2o);
                        nm.ltN = Convert.ToDecimal(mvm.ltN);
                        nm.ltP2o5 = Convert.ToDecimal(mvm.ltP2o5);
                        nm.ltK2o = Convert.ToDecimal(mvm.ltK2o);

                        _ud.UpdateFieldNutrientsManure(mvm.fieldName, nm);
                    }
                    return Json(ReDisplay("#manure", mvm.fieldName));
                }
            }

            return PartialView(mvm);
        }
        private void ManureDetailsSetup(ref ManureDetailsViewModel mvm)
        {
            mvm.manOptions = new List<Models.StaticData.SelectListItem>();
            mvm.manOptions = _sd.GetManuresDll().ToList();

            mvm.applOptions = new List<Models.StaticData.SelectListItem>();
            mvm.applOptions = _sd.GetApplicationsDll().ToList();

            mvm.rateOptions = new List<Models.StaticData.SelectListItem>();
            mvm.rateOptions = _sd.GetUnitsDll(mvm.currUnit).ToList();
            mvm.selRateOptionText = "(lb/ac)";

            return;
        }

        public IActionResult CropDetails(string fldName, int? id)
        {
            CropDetailsViewModel cvm = new CropDetailsViewModel();

            cvm.fieldName = fldName;
            cvm.title = id == null ? "Add" : "Edit";
            cvm.btnText = id == null ? "Calculate" : "Return";
            cvm.id = id;

            if (id != null)
            {
                FieldCrop cp = _ud.GetFieldCrop(fldName, id.Value);

                cvm.fieldName = fldName;
                cvm.id = id;
                cvm.reqN = cp.reqN.ToString();
                cvm.reqP2o5 = cp.reqP2o5.ToString();
                cvm.reqK2o = cp.reqK2o.ToString();
                cvm.remN = cp.remN.ToString();
                cvm.remP2o5 = cp.remP2o5.ToString();
                cvm.remK2o = cp.remK2o.ToString();
                cvm.yield = cp.yield.ToString();
                cvm.crude = cp.crudeProtien.ToString();
                cvm.selCropOption = cp.cropId;
                cvm.selPrevOption = cp.prevCropId.ToString();
                cvm.coverCropHarvested = cp.coverCropHarvested;

                if(!string.IsNullOrEmpty(cp.cropOther))
                {
                    cvm.manEntry = true;
                    Yield yld = _sd.GetYield(1);
                    cvm.cropDesc = cp.cropOther;
                    cvm.yieldUnit = "(" + yld.yielddesc + ")";
                    cvm.selTypOption = _settings.OtherCropId;
                }
                else
                {
                    Crop crop = _sd.GetCrop(Convert.ToInt32(cp.cropId));
                    Yield yld = _sd.GetYield(crop.yieldcd);
                    cvm.yieldUnit = "(" + yld.yielddesc + ")";
                    cvm.selTypOption = crop.croptypeid.ToString();
                }
            }
            else
            {
                cvm.reqN = "  0";
                cvm.reqP2o5 = "  0";
                cvm.reqK2o = "  0";
                cvm.remN = "  0";
                cvm.remP2o5 = "  0";
                cvm.remK2o = "  0";
            }

            CropDetailsSetup(ref cvm);

            return PartialView(cvm);
        }
        [HttpPost]
        public IActionResult CropDetails(CropDetailsViewModel cvm)
        {
            CropDetailsSetup(ref cvm);

            if (cvm.buttonPressed == "TypeChange")
            {
                ModelState.Clear();
                cvm.buttonPressed = "";
                cvm.btnText = "Calculate";
                cvm.crude = "";
                cvm.cropDesc = "";
                cvm.coverCropHarvested = null;

                if (cvm.selTypOption != "select")
                {
                    CropType crpTyp = _sd.GetCropType(Convert.ToInt32(cvm.selTypOption));

                    if (crpTyp.customcrop)
                    {
                        cvm.manEntry = true;
                        cvm.reqN = string.Empty;
                        cvm.reqP2o5 = string.Empty;
                        cvm.reqK2o = string.Empty;
                        cvm.remN = string.Empty;
                        cvm.remP2o5 = string.Empty;
                        cvm.remK2o = string.Empty;
                    }
                    else
                    {
                        cvm.manEntry = false;
                        cvm.reqN = "  0";
                        cvm.reqP2o5 = "  0";
                        cvm.reqK2o = "  0";
                        cvm.remN = "  0";
                        cvm.remP2o5 = "  0";
                        cvm.remK2o = "  0";
                    }
                }
                else
                {
                    cvm.manEntry = false;
                    cvm.reqN = "  0";
                    cvm.reqP2o5 = "  0";
                    cvm.reqK2o = "  0";
                    cvm.remN = "  0";
                    cvm.remP2o5 = "  0";
                    cvm.remK2o = "  0";
                }
                return View(cvm);
            }

            if (cvm.buttonPressed == "PrevChange")
            {
                ModelState.Clear();
                cvm.buttonPressed = "";
                cvm.btnText = "Calculate";
                return View(cvm);
            }

            if (cvm.buttonPressed == "CropChange")
            {
                ModelState.Clear();
                cvm.buttonPressed = "";
                cvm.btnText = "Calculate";

                if (cvm.selCropOption != "")
                {
                    Crop cp = _sd.GetCrop(Convert.ToInt32(cvm.selCropOption));
                    Yield yld = _sd.GetYield(cp.yieldcd);

                    cvm.yieldUnit = "(" + yld.yielddesc + ")";
                }
                cvm.selPrevOption = string.Empty;

                if (cvm.showCrude)
                {
                    CalculateCropRequirementRemoval calculateCropRequirementRemoval = new CalculateCropRequirementRemoval(_env, _ud, _sd);
                    cvm.crude = calculateCropRequirementRemoval.GetCrudeProtienByCropId(Convert.ToInt16(cvm.selCropOption)).ToString("#.#");
                }

                CalculateCropRequirementRemoval ccrr = new CalculateCropRequirementRemoval(_env, _ud, _sd);
                decimal? defaultYield = ccrr.GetDefaultYieldByCropId(Convert.ToInt16(cvm.selCropOption));
                if (defaultYield.HasValue)
                    cvm.yield = defaultYield.Value.ToString("#.#"); 
                
                return View(cvm);
            }

            if (ModelState.IsValid)
            {
                if(cvm.coverCrop)
                {
                    if(!cvm.coverCropHarvested.HasValue)
                    {
                        ModelState.AddModelError("coverCropHarvested", "Required.");
                        return View(cvm);
                    }
                }

                if(!string.IsNullOrEmpty(cvm.crude))
                {
                    decimal crd;
                    if(decimal.TryParse(cvm.crude, out crd))
                    {
                        if(crd < 0 || crd > 100)
                        {
                            ModelState.AddModelError("crude", "Not a valid percentage.");
                            return View(cvm);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("crude", "Not a valid number.");
                        return View(cvm);
                    }
                }

                decimal tmpDec;
                int tmp;
                if (decimal.TryParse(cvm.yield, out tmpDec))
                {
                    if (tmpDec <= 0)
                    {
                        ModelState.AddModelError("yield", "Not a valid yield.");
                        return View(cvm);
                    }
                }
                else
                {
                    ModelState.AddModelError("yield", "Not a valid number.");
                    return View(cvm);
                }

                //if((string.IsNullOrEmpty(cvm.selCropOption) ||
                //    cvm.selCropOption == "select") &&
                //    cvm.selTypOption != _settings.OtherCropId)  // none
                //{
                //    ModelState.AddModelError("selCropOption", "Required.");
                //    return View(cvm);
                //}

                if(cvm.manEntry)
                {
                    if (string.IsNullOrEmpty(cvm.cropDesc))
                    {
                        ModelState.AddModelError("cropDesc", "Required.");
                        return View(cvm);
                    }

                    if (string.IsNullOrEmpty(cvm.reqN))
                    {
                        ModelState.AddModelError("reqN", "Reqd.");
                        return View(cvm);
                    }
                    else
                    {
                        if (int.TryParse(cvm.reqN, out tmp))
                        {
                            if (tmp <= 0)
                            {
                                ModelState.AddModelError("reqN", "Invalid.");
                                return View(cvm);
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("reqN", "Invalid.");
                            return View(cvm);
                        }
                    }
                    if (string.IsNullOrEmpty(cvm.reqP2o5))
                    {
                        ModelState.AddModelError("reqP2o5", "Reqd.");
                        return View(cvm);
                    }
                    else
                    {
                        if (int.TryParse(cvm.reqP2o5, out tmp))
                        {
                            if (tmp <= 0)
                            {
                                ModelState.AddModelError("reqP2o5", "Invalid.");
                                return View(cvm);
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("reqP2o5", "Invalid.");
                            return View(cvm);
                        }
                    }
                    if (string.IsNullOrEmpty(cvm.reqK2o))
                    {
                        ModelState.AddModelError("reqK2o", "Reqd.");
                        return View(cvm);
                    }
                    else
                    {
                        if (int.TryParse(cvm.reqK2o, out tmp))
                        {
                            if (tmp <= 0)
                            {
                                ModelState.AddModelError("reqK2o", "Invalid.");
                                return View(cvm);
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("reqK2o", "Invalid.");
                            return View(cvm);
                        }
                    }
                    if (string.IsNullOrEmpty(cvm.remN))
                    {
                        ModelState.AddModelError("remN", "Reqd.");
                        return View(cvm);
                    }
                    else
                    {
                        if (int.TryParse(cvm.remN, out tmp))
                        {
                            if (tmp <= 0)
                            {
                                ModelState.AddModelError("remN", "Invalid.");
                                return View(cvm);
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("remN", "Invalid.");
                            return View(cvm);
                        }
                    }
                    if (string.IsNullOrEmpty(cvm.remP2o5))
                    {
                        ModelState.AddModelError("remP2o5", "Reqd.");
                        return View(cvm);
                    }
                    else
                    {
                        if (int.TryParse(cvm.remP2o5, out tmp))
                        {
                            if (tmp <= 0)
                            {
                                ModelState.AddModelError("remP2o5", "Invalid.");
                                return View(cvm);
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("remP2o5", "Invalid.");
                            return View(cvm);
                        }
                    }
                    if (string.IsNullOrEmpty(cvm.remK2o))
                    {
                        ModelState.AddModelError("remK2o", "Reqd.");
                        return View(cvm);
                    }
                    else
                    {
                        if (int.TryParse(cvm.remK2o, out tmp))
                        {
                            if (tmp <= 0)
                            {
                                ModelState.AddModelError("remK2o", "Invalid.");
                                return View(cvm);
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("remK2o", "Invalid.");
                            return View(cvm);
                        }
                    }
                }

                if (cvm.btnText == "Calculate")
                {
                    ModelState.Clear();
                    if (!cvm.manEntry)
                    {
                        CropRequirementRemoval cropRequirementRemoval = new CropRequirementRemoval();
                        CalculateCropRequirementRemoval calculateCropRequirementRemoval = new CalculateCropRequirementRemoval(_env, _ud, _sd);

                        calculateCropRequirementRemoval.cropid = Convert.ToInt16(cvm.selCropOption);
                        calculateCropRequirementRemoval.yield = Convert.ToDecimal(cvm.yield);
                        if (cvm.crude == null)
                            calculateCropRequirementRemoval.crudeProtien = null;
                        else
                            calculateCropRequirementRemoval.crudeProtien = Convert.ToDecimal(cvm.crude);
                        calculateCropRequirementRemoval.coverCropHarvested = cvm.coverCropHarvested;
                        

                        cropRequirementRemoval = calculateCropRequirementRemoval.GetCropRequirementRemoval();

                        cvm.reqN = cropRequirementRemoval.N_Requirement.ToString();
                        cvm.reqP2o5 = cropRequirementRemoval.P2O5_Requirement.ToString();
                        cvm.reqK2o = cropRequirementRemoval.K2O_Requirement.ToString();
                        cvm.remN = cropRequirementRemoval.N_Removal.ToString();
                        cvm.remP2o5 = cropRequirementRemoval.P2O5_Removal.ToString();
                        cvm.remK2o = cropRequirementRemoval.K2O_Removal.ToString();
                    }

                    cvm.btnText = cvm.id == null ? "Add to Field" : "Update Field";
                }
                else
                {
                    if (cvm.id == null)
                    {
                        int prevCrop = 0;
                        if (cvm.selPrevOption != "select")
                            prevCrop = Convert.ToInt32(cvm.selPrevOption);

                        int thisCrop = 0;
                        if (cvm.selCropOption != "select")
                            thisCrop = Convert.ToInt32(cvm.selCropOption);

                        FieldCrop crp = new FieldCrop()
                        {
                            cropId = thisCrop.ToString(),
                            cropOther = cvm.cropDesc,
                            yield = Convert.ToDecimal(cvm.yield),
                            reqN = Convert.ToDecimal(cvm.reqN),
                            reqP2o5 = Convert.ToDecimal(cvm.reqP2o5),
                            reqK2o = Convert.ToDecimal(cvm.reqK2o),
                            remN = Convert.ToDecimal(cvm.remN),
                            remP2o5 = Convert.ToDecimal(cvm.remP2o5),
                            remK2o = Convert.ToDecimal(cvm.remK2o),
                            crudeProtien = Convert.ToDecimal(cvm.crude),
                            prevCropId = prevCrop,
                            coverCropHarvested =  cvm.coverCropHarvested
                        };

                        _ud.AddFieldCrop(cvm.fieldName, crp);
                    }
                    else
                    {
                        int prevCrop = 0;
                        if (cvm.selPrevOption != "select")
                            prevCrop = Convert.ToInt32(cvm.selPrevOption);

                        int thisCrop = 0;
                        if (cvm.selCropOption != "select")
                            thisCrop = Convert.ToInt32(cvm.selCropOption);

                        FieldCrop crp = _ud.GetFieldCrop(cvm.fieldName, cvm.id.Value);
                        crp.cropId = thisCrop.ToString();
                        crp.cropOther = cvm.cropDesc;
                        crp.yield = Convert.ToDecimal(cvm.yield);
                        crp.reqN = Convert.ToDecimal(cvm.reqN);
                        crp.reqP2o5 = Convert.ToDecimal(cvm.reqP2o5);
                        crp.reqK2o = Convert.ToDecimal(cvm.reqK2o);
                        crp.remN = Convert.ToDecimal(cvm.remN);
                        crp.remP2o5 = Convert.ToDecimal(cvm.remP2o5);
                        crp.remK2o = Convert.ToDecimal(cvm.remK2o);
                        crp.crudeProtien = Convert.ToDecimal(cvm.crude);
                        crp.prevCropId = prevCrop;
                        crp.coverCropHarvested = cvm.coverCropHarvested;

                        _ud.UpdateFieldCrop(cvm.fieldName, crp);
                    }
                    return Json(ReDisplay("#crop", cvm.fieldName));
                }
            }

            return PartialView(cvm);
        }
        private void CropDetailsSetup(ref CropDetailsViewModel cvm)
        {
            cvm.showCrude = false;
            cvm.typOptions = new List<Models.StaticData.SelectListItem>();
            cvm.typOptions = _sd.GetCropTypesDll().ToList();

            cvm.cropOptions = new List<Models.StaticData.SelectListItem>();
            if (!string.IsNullOrEmpty(cvm.selTypOption) &&
                cvm.selTypOption != "select")
            {
                cvm.cropOptions = _sd.GetCropsDll(Convert.ToInt32(cvm.selTypOption)).ToList();

                if(cvm.selTypOption != "select")
                {
                    CropType crpTyp = _sd.GetCropType(Convert.ToInt32(cvm.selTypOption));
                    cvm.showCrude = crpTyp.crudeproteinrequired;
                    cvm.coverCrop = crpTyp.covercrop;
                    cvm.manEntry = crpTyp.customcrop;
                }
            }

            cvm.prevOptions = new List<Models.StaticData.SelectListItem>();
            if (!string.IsNullOrEmpty(cvm.selCropOption))
            {
                if (cvm.selCropOption != "select" &&
                    cvm.selCropOption != "0")
                {
                    Crop crp = _sd.GetCrop(Convert.ToInt32(cvm.selCropOption));
                    cvm.prevOptions = _sd.GetPrevCropTypesDll(crp.prevcropcd.ToString()).ToList();
                }
            }

            return;
        }
        public IActionResult RefreshManureList(string fieldName)
        {
            return ViewComponent("CalcManure", new { fldName = fieldName });
        }
        public IActionResult RefreshFieldList(string fieldName)
        {
            return RedirectToAction("Calculate", "Nutrients");
            //return ViewComponent("FieldList");
        }
        public IActionResult RefreshSummary(string fieldName)
        {
            return ViewComponent("CalcSummary", new { fldName = fieldName });
        }
        public IActionResult RefreshHeading(string fieldName)
        {
            return ViewComponent("CalcHeading", new { fldName = fieldName });
        }
        [HttpGet]
        public ActionResult ManureDelete(string fldName, int id)
        {
            ManureDeleteViewModel dvm = new ManureDeleteViewModel();
            dvm.id = id;
            dvm.fldName = fldName;

            NutrientManure nm = _ud.GetFieldNutrientsManure(fldName, id);
            dvm.matType = _sd.GetManure(nm.manureId).name;

            dvm.act = "Delete";

            return PartialView("ManureDelete", dvm);
        }
        [HttpPost]
        public ActionResult ManureDelete(ManureDeleteViewModel dvm)
        {
            if (ModelState.IsValid)
            {
                _ud.DeleteFieldNutrientsManure(dvm.fldName, dvm.id);

                return Json(ReDisplay("#manure", dvm.fldName));
            }
            return PartialView("ManureDelete", dvm);
        }
        public IActionResult RefreshCropList(string fieldName)
        {
            return ViewComponent("CalcCrop", new { fldName = fieldName });
        }
        [HttpGet]
        public ActionResult CropDelete(string fldName, int id)
        {
            CropDeleteViewModel dvm = new CropDeleteViewModel();
            dvm.id = id;
            dvm.fldName = fldName;

            FieldCrop crp = _ud.GetFieldCrop(fldName, id);
            if (!string.IsNullOrEmpty(crp.cropOther))
                dvm.cropName = crp.cropOther;
            else
                dvm.cropName = _sd.GetCrop(Convert.ToInt32(crp.cropId)).cropname;

            dvm.act = "Delete";

            return PartialView("CropDelete", dvm);
        }
        [HttpPost]
        public ActionResult CropDelete(CropDeleteViewModel dvm)
        {
            if (ModelState.IsValid)
            {
                _ud.DeleteFieldCrop(dvm.fldName, dvm.id);

                return Json(ReDisplay("#crop", dvm.fldName));
            }
            return PartialView("CropDelete", dvm);
        }
        public IActionResult OtherDetails(string fldName, int? id)
        {
            Utility.CalculateNutrients calculateNutrients = new CalculateNutrients(_env, _ud, _sd);
            NOrganicMineralizations nOrganicMineralizations = new NOrganicMineralizations();

            OtherDetailsViewModel ovm = new OtherDetailsViewModel();

            ovm.fieldName = fldName;
            ovm.title = id == null ? "Add" : "Edit";
            ovm.btnText = id == null ? "Add to Field" : "Update Field";
            ovm.id = id;

            if (id != null)
            {
                ovm.title = "Edit";

                NutrientOther no = _ud.GetFieldNutrientsOther(fldName, id.Value);
                ovm.source = no.description;
                ovm.nitrogen = no.nitrogen.ToString();
                ovm.phospherous = no.phospherous.ToString();
                ovm.potassium = no.potassium.ToString();
            }
            else

            {
                ovm.title = "Add";
            }

            return PartialView(ovm);
        }
        [HttpPost]
        public IActionResult OtherDetails(OtherDetailsViewModel ovm)
        {
            if(ModelState.IsValid)
            {
                decimal tmp = 0;
                if (decimal.TryParse(ovm.nitrogen, out tmp))
                {
                    if (tmp < 0)
                    {
                        ModelState.AddModelError("nitrogen", "Must be greater than 0.");
                        return View(ovm);
                    }
                }
                else
                {
                    ModelState.AddModelError("nitrogen", "Not a valid number.");
                    return View(ovm);
                }
                if (decimal.TryParse(ovm.phospherous, out tmp))
                {
                    if (tmp < 0)
                    {
                        ModelState.AddModelError("phospherous", "Must be greater than 0.");
                        return View(ovm);
                    }
                }
                else
                {
                    ModelState.AddModelError("phospherous", "Not a valid number.");
                    return View(ovm);
                }
                if (decimal.TryParse(ovm.potassium, out tmp))
                {
                    if (tmp < 0)
                    {
                        ModelState.AddModelError("potassium", "Must be greater than 0.");
                        return View(ovm);
                    }
                }
                else
                {
                    ModelState.AddModelError("potassium", "Not a valid number.");
                    return View(ovm);
                }
                if (ovm.id == null)
                {
                    NutrientOther no = new NutrientOther()
                    {
                        description = ovm.source,
                        nitrogen = Convert.ToDecimal(ovm.nitrogen),
                        phospherous = Convert.ToDecimal(ovm.phospherous),
                        potassium = Convert.ToDecimal(ovm.potassium)
                    };

                    _ud.AddFieldNutrientsOther(ovm.fieldName, no);
                }
                else
                {
                    NutrientOther no = _ud.GetFieldNutrientsOther(ovm.fieldName, ovm.id.Value);
                    no.description = ovm.source;
                    no.nitrogen = Convert.ToDecimal(ovm.nitrogen);
                    no.phospherous = Convert.ToDecimal(ovm.phospherous);
                    no.potassium = Convert.ToDecimal(ovm.potassium);

                    _ud.UpdateFieldNutrientsOther(ovm.fieldName, no);
                }

                return Json(ReDisplay("#other", ovm.fieldName));
            }

            return View(ovm);
        }
        [HttpGet]
        public ActionResult OtherDelete(string fldName, int id)
        {
            OtherDeleteViewModel ovm = new OtherDeleteViewModel();
            ovm.id = id;
            ovm.fldName = fldName;

            NutrientOther no = _ud.GetFieldNutrientsOther(fldName, id);
            ovm.source = no.description;

            ovm.act = "Delete";

            return PartialView("OtherDelete", ovm);
        }
        [HttpPost]
        public ActionResult OtherDelete(OtherDeleteViewModel ovm)
        {
            if (ModelState.IsValid)
            {
                _ud.DeleteFieldNutrientsOther(ovm.fldName, ovm.id);

                return Json(ReDisplay("#other", ovm.fldName));
            }
            return PartialView("OtherDelete", ovm);
        }
        public object ReDisplay(string target, string fldName)
        {
            string url = Url.Action("RefreshOtherList", "Nutrients", new { fieldName = fldName });
            string urlSumm = Url.Action("RefreshSummary", "Nutrients", new { fieldName = fldName });
            string urlHead = Url.Action("RefreshHeading", "Nutrients", new { fieldName = fldName });
            string urlMsg = Url.Action("RefreshMessages", "Nutrients", new { fieldName = fldName });

            var result = new { success = true, url = url, target = target, urlSumm = urlSumm, urlHead = urlHead , urlMsg = urlMsg};
            return result;
        }
    }
}

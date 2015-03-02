using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using CommonMark;
using NFig.SampleWebApplication.Models;

namespace NFig.SampleWebApplication.Controllers
{
    [RoutePrefix("settings")]
    public class SettingsController : Controller
    {
        [Route("")]
        public ActionResult Index(string help)
        {
            var model = new SettingsListModel();
            model.SettingInfos = Config.NFigAsyncStore.GetAllSettingInfos(Config.ApplicationName);
            return View(model);
        }

        [Route("edit/{settingName}")]
        [HttpGet]
        public ActionResult Edit(string settingName, DataCenter? dc = null)
        {
            if (!Config.NFigAsyncStore.SettingExists(settingName))
                return HttpNotFound();

            var model = new SettingEditModel();
            model.EditingDataCenter = dc ?? DataCenter.Any;

            PopulateSettingEditModel(settingName, model);

            var over = model.SettingInfo.GetOverrideFor(Config.Tier, model.EditingDataCenter);
            if (over != null && over.DataCenter == model.EditingDataCenter)
                model.Value = over.Value;

            return View(model);
        }

        [Route("edit/{settingName}")]
        [HttpPost]
        public ActionResult Edit(string settingName, string action, SettingEditModel model, DataCenter? dc = null)
        {
            if (model.EditingTier != Config.Tier)
                throw new Exception("Don't try to fake which tier you're on when editing settings, idiot.");

            if (action == "save-override")
            {
                if (Config.NFigAsyncStore.IsValidStringForSetting(settingName, model.Value))
                {
                    // set the override
                    Config.NFigAsyncStore.SetOverride(Config.ApplicationName, settingName, model.Value, Config.Tier, model.EditingDataCenter);
                }
                else
                {
                    model.IsInvalid = true;
                }
            }
            else if (action == "clear-override")
            {
                Config.NFigAsyncStore.ClearOverride(Config.ApplicationName, settingName, Config.Tier, model.EditingDataCenter);
                model.Value = null;
            }
            else
            {
                throw new Exception("Unknown setting edit action: " + action);
            }

            if (model.IsInvalid)
            {
                PopulateSettingEditModel(settingName, model);
                return View(model);
            }

            return RedirectToAction("Edit", new { settingName, dc });
        }

        private void PopulateSettingEditModel(string settingName, SettingEditModel model)
        {
            model.SettingInfo = Config.NFigAsyncStore.GetSettingInfo(Config.ApplicationName, settingName);

            model.EditingTier = Config.Tier;
            model.DescriptionHtml = CommonMarkConverter.Convert(model.SettingInfo.Description);
            model.RequiresRestart = model.SettingInfo.PropertyInfo.GetCustomAttribute<RequiresRestartAttribute>() != null;

            IEnumerable<DataCenter> dcs = (DataCenter[])Enum.GetValues(typeof(DataCenter));
            dcs = Config.Tier == Tier.Local ? dcs.Where(d => d == DataCenter.Any || d == DataCenter.Local) : dcs.Where(d => d != DataCenter.Local);
            model.AvailableDataCenters = dcs.ToList();
        }
    }
}
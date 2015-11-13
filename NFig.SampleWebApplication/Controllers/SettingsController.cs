using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace NFig.SampleWebApplication.Controllers
{
    [RoutePrefix("settings")]
    public class SettingsController : Controller
    {
        [Route("")]
        public ActionResult Index(string help) => View();


        [Route("json")]
        public async Task<ActionResult> Json()
        {
            try
            {
                return Content(await Config.GetSettingsJsonAsync(), "application/json");
            }
            catch (Exception ex)
            {
                return new ErrorResult(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

        [Route("set")]
        [HttpPost]
        public  async Task<ActionResult> SetOverride(string settingName, string value, DataCenter dataCenter)
        {
            try
            {
                if (!await Config.AllowsOverrideFor(settingName, dataCenter))
                    return new ErrorResult(HttpStatusCode.NotImplemented, $"Setting {settingName} does not allow overrides for Data Center {dataCenter}");

                if (!Config.NFigAsyncStore.IsValidStringForSetting(settingName, value))
                    return new ErrorResult(HttpStatusCode.Conflict, $"\"{value}\" is an invalid value for setting {settingName}");

                await Config.NFigAsyncStore.SetOverrideAsync(Config.ApplicationName, settingName, value, Config.Tier, dataCenter);

                return Content(await Config.GetSettingJsonAsync(settingName), "application/json");
            }
            catch (Exception ex)
            {
                // Do something with the error
                return new ErrorResult(HttpStatusCode.InternalServerError, "An error occurred while processing the request.");
            }
        }

        [Route("clear")]
        [HttpPost]
        public async Task<ActionResult> ClearOverride(string settingName, DataCenter dataCenter)
        {
            try
            {
                await Config.NFigAsyncStore.ClearOverrideAsync(Config.ApplicationName, settingName, Config.Tier, dataCenter);
                return Content(await Config.GetSettingJsonAsync(settingName), "application/json");
            }
            catch (Exception ex)
            {
                // Do something with the error
                return new ErrorResult(HttpStatusCode.InternalServerError, "An error occurred while processing the request.");
            }
        }

        public class ErrorResult : ActionResult
        {
            readonly HttpStatusCode _code;
            readonly string _content;


            public ErrorResult(HttpStatusCode code, string content)
            {
                _code = code;
                _content = content;
            }


            public override void ExecuteResult(ControllerContext context)
            {
                if (context == null)
                    throw new ArgumentNullException(nameof(context));

                context.HttpContext.Response.StatusCode = (int)_code;
                if (_content == null)
                    return;

                context.HttpContext.Response.Write(_content);
            }
        }
    }
}
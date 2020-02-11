using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Facebook.Chatbot.API.Helpers;
using System.IO;
using Newtonsoft.Json;

namespace Facebook.Chatbot.API.Controllers
{
    [Route("api/[controller]")]
    public class FacebookMessageController : Controller
    {
        public async Task WriteLog(string value)
        {
            using (var writer = new StreamWriter(@"C:\Websites\Logs\chatbot.txt", true))
            {
                await writer.WriteLineAsync("------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");
                await writer.WriteLineAsync(value);
                await writer.WriteLineAsync("------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");
            }
        }


        private readonly IFacebookMessageHelper _facebookMessageHelper;
        public FacebookMessageController(IFacebookMessageHelper facebookMessageHelper)
        {
            this._facebookMessageHelper = facebookMessageHelper;
        }

        // Authorize hook
        [HttpGet]
        public ContentResult Get()
        {
            var hub_mode = HttpContext.Request.Query["hub.mode"].ToString();
            var hub_challenge = HttpContext.Request.Query["hub.challenge"].ToString();
            var hub_verify_token = HttpContext.Request.Query["hub.verify_token"].ToString();

            if (hub_mode == "subscribe" && hub_verify_token == "iamchatbot")
            {
                return Content(hub_challenge);
            }
            else
            {
                return Content("Failed");
            }
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody]dynamic value)
        {
            try
            {
                var data = value;

                //await WriteLog(JsonConvert.SerializeObject(data));

                foreach (var pageEntry in data.entry)
                {
                    var pageID = pageEntry.id;
                    var timeOfEvent = pageEntry.time;

                    foreach (var messagingEvent in pageEntry.messaging)
                    {
                        if (messagingEvent.referral != null)
                        {
                            await this._facebookMessageHelper.ReceivedMessageRefferal(messagingEvent);
                        }
                        if (messagingEvent.optin != null)
                        {
                            await this._facebookMessageHelper.ReceivedAuthentication(messagingEvent);
                        }
                        else if (messagingEvent.message != null)
                        {
                            await this._facebookMessageHelper.ReceivedMessage(messagingEvent);
                        }
                        else if (messagingEvent.delivery != null)
                        {
                            await this._facebookMessageHelper.ReceivedDeliveryConfirmation(messagingEvent);
                        }
                        else if (messagingEvent.postback != null)
                        {
                            await this._facebookMessageHelper.ReceivedPostback(messagingEvent);
                        }
                        else if (messagingEvent.read != null)
                        {
                            await this._facebookMessageHelper.ReceivedMessageRead(messagingEvent);
                        }
                        else if (messagingEvent.account_linking != null)
                        {
                            await this._facebookMessageHelper.ReceivedAccountLink(messagingEvent);
                        }
                        else
                        {
                            //await this._facebookMessageHelper.WriteLog("Webhook received unknown messagingEvent: " + messagingEvent);
                            //console.log("Webhook received unknown messagingEvent: ", messagingEvent);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(new { message = "Error: " + ex.Message });
            }

            return Ok(new { message = "OK" });
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using RestSharp;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Facebook.Chatbot.API.Helpers
{
    public class LoggingEvents
    {
        public const int DATABASE_LOG = 4000;
        public const int EVENT_LOG = 4001;
    }

    public class FacebookMessageHelper : IFacebookMessageHelper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public FacebookMessageHelper(IConfiguration configuration, ILogger<FacebookMessageHelper> logger)
        {
            this._configuration = configuration;
            this._logger = logger;
        }

        #region SETTINGS

        private const string _AppId = "231193944562086";
        private const string _AppSecret = "fb609f5a70c97a87ba0b4504b7b987dc";
        private string _PageAccessToken = "EAADSRQjwmaYBABuraNr5P2LaRM5w8na0z2pL9EaOFMHJI4LtLptSmwoQYW8C0XDUhOrptMQBi8LVGpMoKQR1lQhy7ACUKLD0voD8PsdVze85rpvrgNq5pkHktEDmFFey3rcQWOVad2WvMngLcSLCrZBjbxPJKZAsVLM0csQnn6vnMJZA8B3B37rq0YM2JMZD";

        private const string FacebookUrl = "https://graph.facebook.com/v4.0/me/messages";
        private const string ServerUrl = "https://i-chatbot.com";
        private const string ValidationToken = "iamchatbot";
        #endregion

        #region RECEIVED MESSAGES
        public async void ReceivedAuthentication(dynamic messagingEvent)
        {
            var senderId = messagingEvent.sender.id;
            var recipientId = messagingEvent.recipient.id;
            var timeOfAuth = messagingEvent.timestamp;

            // The 'ref' field is set in the 'Send to Messenger' plugin, in the 'data-ref'
            // The developer can set this to an arbitrary value to associate the 
            // authentication callback with the 'Send to Messenger' click event. This is
            // a way to do account linking when the user clicks the 'Send to Messenger' 
            // plugin.
            var passThroughParam = messagingEvent.optin.@ref;

            // console.log("Received authentication for user %d and page %d with pass " + "through param '%s' at %d", senderID, recipientID, passThroughParam, timeOfAuth);

            // When an authentication is received, we'll send a message back to the sender
            // to let them know it was successful.
            await SendTextMessage(senderId, "Authentication successful", string.Empty);
        }

        public async Task ReceivedMessageRefferal(dynamic messagingEvent)
        {
            var senderId = messagingEvent.sender.id;
            var recipientId = messagingEvent.recipient.id;
            var pageId = (string)recipientId;
            var pageAccessToken = this._configuration.GetValue<string>($"pageAccessTokens:{pageId}");
            var referral = messagingEvent.referral;
            var _ref = referral["ref"];
            if (_ref != null)
            {
                return;
            }
        }


        public async Task ReceivedDeliveryConfirmation(dynamic messagingEvent)
        {
            var senderId = messagingEvent.sender.id;
            var recipientId = messagingEvent.recipient.id;
            var delivery = messagingEvent.delivery;
            var messageIDs = delivery.mids;
            var watermark = delivery.watermark;
            var sequenceNumber = delivery.seq;

            if (messageIDs != null)
            {
                var userId = (string)senderId;

                foreach (var messageId in messageIDs)
                {
                    var text = "Received delivery confirmation for message ID: " + messageId;
                    await WriteLog(text);

                    // console.log("All message before %d were delivered.", watermark);

                    // TODO: PROCESS QUEUE MESSAGES
                }
            }
        }

        public async Task ReceivedPostback(dynamic messagingEvent)
        {
            var senderId = messagingEvent.sender.id;
            var recipientId = messagingEvent.recipient.id;
            var timeOfPostback = messagingEvent.timestamp;

            // The 'payload' param is a developer-defined field which is set in a postback 
            // button for Structured Messages. 
            var postback_payload = messagingEvent.postback.payload;

            //console.log("Received postback for user %d and page %d with payload '%s' " + "at %d", senderID, recipientID, payload, timeOfPostback);

            // When a postback is called, we'll send a message back to the sender to 
            // let them know it was successful            
            var payload = (string)postback_payload;

            await SendTextMessage(senderId, "Postback called: " + payload, string.Empty);
        }

        public async Task ReceivedMessageRead(dynamic messagingEvent)
        {
            var senderId = messagingEvent.sender.id;
            var recipientId = messagingEvent.recipient.id;

            // All messages before watermark (a timestamp) or sequence have been seen.
            var watermark = messagingEvent.read.watermark;
            var sequenceNumber = messagingEvent.read.seq;

            await WriteLog($"Received message read event for watermark {watermark} and sequence number {sequenceNumber}");
            //console.log("Received message read event for watermark %d and sequence " + "number %d", watermark, sequenceNumber);
        }

        public async Task ReceivedAccountLink(dynamic messagingEvent)
        {
            var senderId = messagingEvent.sender.id;
            var recipientId = messagingEvent.recipient.id;

            var status = messagingEvent.account_linking.status;
            var authCode = messagingEvent.account_linking.authorization_code;

            await WriteLog($"Received account link event with for user {senderId} with status {status} and auth code {authCode}");
            //console.log("Received account link event with for user %d with status %s " + "and auth code %s ", senderID, status, authCode);
        }

        public async Task ReceivedMessage(dynamic messagingEvent)
        {
            //WriteLog(JsonConvert.SerializeObject(messagingEvent));

            #region GET MESSAGE DATA
            var senderId = messagingEvent.sender.id;
            var recipientId = messagingEvent.recipient.id;
            var timeOfMessage = messagingEvent.timestamp;
            var message = messagingEvent.message;

            var isEcho = message.is_echo;
            var messageId = message.mid;
            var appId = message.app_id;
            var metadata = message.metadata;

            // You may get a text or attachment but not both
            var messageText = message.text;
            var messageAttachments = message.attachments;
            var quickReply = message.quick_reply;
            #endregion

            // TODO: SAVE LOG TO DATABASE            
           

            #region ECHO
            if (isEcho != null)
            {
                // Just logging message echoes to console
                //console.log("Received echo for message %s and app %d with metadata %s", messageId, appId, metadata);              
                return;
            }
            #endregion

            #region QUICK REPLY
            if (quickReply != null)
            {
                var quickReplyPayload = quickReply.payload;
                //console.log("Quick reply for message %s with payload %s", messageId, quickReplyPayload);
                // TODO: XỬ LÝ MESSSAGE POSTBACK
                await SendTextMessage(senderId, "Quick reply tapped : " + quickReplyPayload.ToString(), "");
                return;
            }
            #endregion

            #region TEXT MESSAGE

            var text = ((string)messageText).ToLower();

            // If we receive a text message, check to see if it matches any special
            // keywords and send back the corresponding example. Otherwise, just echo
            // the text we received.

            // TODO: XỬ LÝ MESSAGE ở DATABASE
            try
            {
                SendTextMessage(senderId, "Lấy nội dung từ database rồi send lại cho user", string.Empty);
            }
            catch (Exception ex)
            {
                SendTextMessage(senderId, ex.Message, string.Empty);
                await WriteLog(ex.Message);
            }
            #endregion

            if (messageAttachments != null)
            {
                await SendTextMessage(senderId, "Message with attachment received", string.Empty);
            }
        }


        #endregion

        #region SEND MESSAGES
        public async Task SendTextMessage(dynamic recipientId, string messageText, string metadata)
        {
            var messageData = new
            {
                recipient = new
                {
                    id = (string)recipientId
                },
                message = new
                {
                    text = messageText,
                    metadata = metadata
                }
            };

            await SendToFacebookMessengerAsync(messageData);
        }

        public async Task SendImageMessage(dynamic recipientId, string imageUrl)
        {
            var messageData = new
            {
                recipient = new
                {
                    id = (string)recipientId
                },
                message = new
                {
                    attachment = new
                    {
                        type = "image",
                        payload = new
                        {
                            url = imageUrl,
                            //is_reusable = true,
                        }
                    }
                }
            };

            await SendToFacebookMessengerAsync(messageData);
        }

        public async Task SendGifMessage(dynamic recipientId, string imageUrl)
        {
            var messageData = new
            {
                recipient = new
                {
                    id = (string)recipientId
                },
                message = new
                {
                    attachment = new
                    {
                        type = "image",
                        payload = new
                        {
                            url = imageUrl
                        }
                    }
                }
            };

            await SendToFacebookMessengerAsync(messageData);
        }

        public async Task SendAudioMessage(dynamic recipientId, string audioUrl)
        {
            var messageData = new
            {
                recipient = new
                {
                    id = (string)recipientId
                },
                message = new
                {
                    attachment = new
                    {
                        type = "audio",
                        payload = new
                        {
                            url = audioUrl
                        }
                    }
                }
            };

            await SendToFacebookMessengerAsync(messageData);
        }

        public async Task SendVideoMessage(dynamic recipientId, string videoUrl)
        {
            var messageData = new
            {
                recipient = new
                {
                    id = (string)recipientId
                },
                message = new
                {
                    attachment = new
                    {
                        type = "video",
                        payload = new
                        {
                            url = videoUrl
                        }
                    }
                }
            };

            await SendToFacebookMessengerAsync(messageData);
        }

        public async Task SendFileMessage(dynamic recipientId, string fileUrl)
        {
            var messageData = new
            {
                recipient = new
                {
                    id = (string)recipientId
                },
                message = new
                {
                    attachment = new
                    {
                        type = "file",
                        payload = new
                        {
                            url = fileUrl
                        }
                    }
                }
            };

            await SendToFacebookMessengerAsync(messageData);
        }

        public async Task SendToFacebookMessengerAsync(dynamic messageData)
        {
            var url = $"{FacebookUrl}?access_token={this._PageAccessToken}";
            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(messageData);

            var response = await client.ExecuteAsync(request);
            await WriteLog("SendToFacebookMessengerAsync:\n" + JsonConvert.SerializeObject(response.Content));
        }

        #endregion

        #region BUILD MESSAGE   

        #endregion

        #region LOGS
        public async Task WriteLog(string value)
        {

            _logger.LogInformation(LoggingEvents.EVENT_LOG, value);
            using (var writer = new StreamWriter(@"C:\Websites\Logs\chatbot.txt", true))
            {
                await writer.WriteLineAsync("------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");
                await writer.WriteLineAsync(value);
                await writer.WriteLineAsync("------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");
            }
        }
      
        #endregion
    }
}

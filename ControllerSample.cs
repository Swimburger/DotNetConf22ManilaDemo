using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;

namespace TwilioWebhookDemo
{
    public class ControllerSample : TwilioController
    {
        [HttpPost("/message")]
        public IActionResult Message(SmsRequest smsRequest)
        {
            var messagingResponse = new MessagingResponse();
            messagingResponse.Message($"You said: {smsRequest.Body}");
            return TwiML(messagingResponse);
        }
    }
}

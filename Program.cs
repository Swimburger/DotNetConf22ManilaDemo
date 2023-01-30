using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text;
using Twilio.AspNet.Core;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML;

var builder = WebApplication.CreateBuilder(args);

#region HTTP Logging

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.All;
    options.MediaTypeOptions.AddText("application/x-www-form-urlencoded", Encoding.UTF8);
    options.RequestHeaders.Add("x-twilio-signature");
});

#endregion

builder.Services
    .AddTwilioClient()
    .AddTwilioRequestValidation();

builder.Services.Configure<ForwardedHeadersOptions>(
    options => options.ForwardedHeaders = ForwardedHeaders.All
);

var app = builder.Build();

#region HTTP Logging

app.UseHttpLogging();

// force body to be read for the sake of HTTP logging middleware
// the logging middleware only logs the body if it is read by the app.
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await new StreamReader(context.Request.Body)
        .ReadToEndAsync(context.RequestAborted)
        .ConfigureAwait(false);
    context.Request.Body.Position = 0;
    await next();
});

#endregion

app.UseForwardedHeaders();

app.MapPost("/message", () => new MessagingResponse()
    .Message("Ahoy!")
    .ToTwiMLResult()
).ValidateTwilioRequest();

//app.MapPost("/message", async (HttpRequest request, CancellationToken cancellationToken) =>
//{
//    var form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
//    var body = form["Body"];

//    return new MessagingResponse()
//        .Message($"You said: {body}")
//        .ToTwiMLResult();
//});

#region Configure webhook URL
var devTunnelUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL");
if (devTunnelUrl != null)
{
    using var scope = app.Services.CreateScope();
    var twilioClient = scope.ServiceProvider.GetRequiredService<ITwilioRestClient>();
    await IncomingPhoneNumberResource.UpdateAsync(
        pathSid: app.Configuration["Twilio:PhoneNumberSid"],
        smsUrl: new Uri($"{devTunnelUrl}message", UriKind.Absolute),
        client: twilioClient
    );
}
#endregion

app.Run();
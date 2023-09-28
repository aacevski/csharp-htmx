using System.Net;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Text;
using CC.CSX;
using CC.CSX.Web;
using CC.CSX.Htmx;
using chat.models;
using static CC.CSX.HtmlElements;
using static CC.CSX.HtmlAttributes;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseWebSockets();


static HtmlResult MainPage(HtmlItem children) =>
    Html(
        Head(
            Script(src("https://cdn.tailwindcss.com"), type("text/javascript")),
            Script(src("https://unpkg.com/htmx.org/dist/htmx.js"), type("text/javascript")),
            Script(src("https://unpkg.com/htmx.org/dist/ext/ws.js"), type("text/javascript")),
            Script(src("https://unpkg.com/hyperscript.org@0.9.11"), type("text/javascript")),
            Body(
                children
            )
        )).ToResponse();

app.MapGet("/", () => MainPage(
    Div(
        @class("flex-1 relative h-screen overflow-y-auto"),
        HtmxAttributes.hxExt("ws"),
        new HtmlAttribute("ws-connect", "/ws"),

        Form(id("form"),
            new HtmlAttribute("_", "on submit target.reset()"),
            new HtmlAttribute("ws-send"),
            
            Input(
                @class("w-full h-10 px-3 text-base placeholder-gray-600 border rounded-lg focus:shadow-outline"),
                type("text"),
                name("message"),
                placeholder("Type a message")
            )
        ),
        
        Div(
            @class("flex-1 relative h-screen overflow-y-auto flex flex-col"),
            HtmxAttributes.hxSwapOob("morphdom"),
            new HtmlAttribute("id", "chat_room"))
    )
));


var connections = new List<WebSocket>();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();

        connections.Add(ws);

        async void HandleMessage(WebSocketReceiveResult result, byte[] buffer)
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var htmxWebsocketRequest = HtmxWsRequest.FromBuffer(buffer, result.Count);
                
                await Broadcast(Div(
                    @class("text-gray-500 w-full"),
                    HtmxAttributes.hxSwapOob("beforeend:#chat_room"),
                    
                     Div(htmxWebsocketRequest?.Message)
                    )
                );
            }

            else if (result.MessageType == WebSocketMessageType.Close || ws.State == WebSocketState.Aborted)
            {
                connections.Remove(ws);
                await ws.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
        }

        await ReceiveMessage(ws, HandleMessage);
    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
});

async Task ReceiveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
{
    var buffer = new byte[1024 * 4];
    while (socket.State == WebSocketState.Open)
    {
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        handleMessage(result, buffer);
    }
}

async Task Broadcast(HtmlNode message)
{
    var bytes = Encoding.UTF8.GetBytes(message.ToString());
    foreach (var socket in connections)
    {
        if (socket.State != WebSocketState.Open) continue;
        var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
        await socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}

app.Run();

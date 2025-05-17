using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class NotificationApiController : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> SendOrderNotification([FromQuery] string orderId, [FromQuery] string message)
    {
        if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(message))
        {
            return BadRequest("OrderId and message are required.");
        }

        try
        {
            var notification = new Message
            {
                Notification = new Notification
                {
                    Title = "Thông báo đơn hàng",
                    Body = message
                },
                Data = new Dictionary<string, string>
                {
                    { "orderId", orderId },
                    { "type", "order_update" }
                },
                Topic = "orders"
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(notification);
            return Ok(new { MessageId = response });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending notification: {ex.Message}");
            return StatusCode(500, "Internal Server Error: " + ex.Message);
        }
    }
}
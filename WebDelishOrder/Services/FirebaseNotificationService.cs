using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Builder.Extensions;

public class FirebaseNotificationService
{
    public FirebaseNotificationService()
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("Firebase/firebase-adminsdk.json") // Đường dẫn đến tệp JSON
            });
        }
    }

    public async Task SendNotificationAsync(string title, string body, string token)
    {
        var message = new Message()
        {
            Notification = new Notification()
            {
                Title = title,
                Body = body
            },
            Token = token
        };

        try
        {
            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            Console.WriteLine("Successfully sent message: " + response);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending message: " + ex.Message);
        }
    }
}